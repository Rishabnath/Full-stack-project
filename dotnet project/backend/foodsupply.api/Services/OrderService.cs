using FoodSupply.Api.DTOs.Orders;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Exceptions;
using FoodSupply.Api.Repositories.Interfaces;
using FoodSupply.Api.Services.Interfaces;

namespace FoodSupply.Api.Services;

public sealed class OrderService : IOrderService
{
    private const int MaximumPageSize = 50;
    private const int MaximumItemsPerOrder = 50;

    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICustomerAddressRepository _addressRepository;
    private readonly IUserRepository _userRepository;

    public OrderService(
        IOrderRepository orderRepository,
        IRestaurantRepository restaurantRepository,
        IMenuItemRepository menuItemRepository,
        ICustomerAddressRepository addressRepository,
        IUserRepository userRepository)
    {
        _orderRepository = orderRepository;
        _restaurantRepository = restaurantRepository;
        _menuItemRepository = menuItemRepository;
        _addressRepository = addressRepository;
        _userRepository = userRepository;
    }

    public async Task<OrderListResponse> GetMineAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await GetActiveCustomerAsync(customerId, cancellationToken);

        if (page < 1)
        {
            throw new BadRequestException("Page must be at least 1.");
        }

        if (pageSize < 1 || pageSize > MaximumPageSize)
        {
            throw new BadRequestException($"Page size must be between 1 and {MaximumPageSize}.");
        }

        var skipLong = (long)(page - 1) * pageSize;
        if (skipLong > int.MaxValue)
        {
            throw new BadRequestException("The requested page is too large.");
        }

        var (orders, totalCount) = await _orderRepository.GetPageByCustomerIdAsync(
            customerId,
            (int)skipLong,
            pageSize,
            cancellationToken);

        return new OrderListResponse
        {
            Items = orders.Select(MapToSummary).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<OrderResponse> GetMineByIdAsync(
        Guid customerId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        await GetActiveCustomerAsync(customerId, cancellationToken);
        ValidateOrderId(orderId);

        var order = await _orderRepository.GetByIdForCustomerAsync(
            customerId,
            orderId,
            cancellationToken);

        return order is null
            ? throw new NotFoundException($"Order with ID {orderId} was not found.")
            : MapToResponse(order);
    }

    public async Task<OrderResponse> CreateAsync(
        Guid customerId,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetActiveCustomerAsync(customerId, cancellationToken);

        if (request is null)
        {
            throw new BadRequestException("Order request body is required.");
        }

        if (request.RestaurantId <= 0)
        {
            throw new BadRequestException("Restaurant ID must be greater than zero.");
        }

        if (request.AddressId <= 0)
        {
            throw new BadRequestException("Address ID must be greater than zero.");
        }

        if (!Enum.IsDefined(typeof(PaymentMethod), request.PaymentMethod))
        {
            throw new BadRequestException("A valid payment method is required.");
        }

        if (request.Items is null ||
            request.Items.Count == 0 ||
            request.Items.Count > MaximumItemsPerOrder)
        {
            throw new BadRequestException(
                $"An order must contain between 1 and {MaximumItemsPerOrder} menu items.");
        }

        var recipientName = NormalizeRequiredText(
            request.RecipientName,
            "Recipient name",
            2,
            200);
        var recipientPhone = NormalizeRequiredText(
            request.RecipientPhoneNumber,
            "Recipient phone number",
            7,
            30);
        var customerNote = NormalizeOptionalText(request.CustomerNote, "Customer note", 500);

        var duplicateMenuItemIds = request.Items
            .GroupBy(item => item.MenuItemId)
            .Any(group => group.Count() > 1);

        if (duplicateMenuItemIds)
        {
            throw new BadRequestException(
                "Each menu item can appear only once. Set its quantity on that item instead.");
        }

        foreach (var item in request.Items)
        {
            if (item.MenuItemId <= 0)
            {
                throw new BadRequestException("Every menu item ID must be greater than zero.");
            }

            if (item.Quantity is < 1 or > 50)
            {
                throw new BadRequestException("Each item quantity must be between 1 and 50.");
            }

            item.SpecialInstructions = NormalizeOptionalText(
                item.SpecialInstructions,
                "Special instructions",
                500);
        }

        var restaurant = await _restaurantRepository.GetByIdAsync(
            request.RestaurantId,
            cancellationToken);

        if (restaurant is null || !restaurant.IsActive)
        {
            throw new NotFoundException(
                $"Active restaurant with ID {request.RestaurantId} was not found.");
        }

        var address = await _addressRepository.GetByIdAsync(
            customerId,
            request.AddressId,
            cancellationToken);

        if (address is null)
        {
            throw new NotFoundException(
                $"Delivery address with ID {request.AddressId} was not found.");
        }

        var requestedMenuItemIds = request.Items
            .Select(item => item.MenuItemId)
            .ToArray();
        var availableMenuItems = await _menuItemRepository.GetAvailableByIdsAsync(
            restaurant.Id,
            requestedMenuItemIds,
            cancellationToken);

        if (availableMenuItems.Count != requestedMenuItemIds.Length)
        {
            throw new ConflictException(
                "One or more menu items are unavailable or do not belong to this restaurant.");
        }

        var menuItemsById = availableMenuItems.ToDictionary(menuItem => menuItem.Id);
        var now = DateTime.UtcNow;
        var orderItems = request.Items.Select(item =>
        {
            var menuItem = menuItemsById[item.MenuItemId];

            return new OrderItem
            {
                MenuItemId = menuItem.Id,
                ItemName = menuItem.Name,
                UnitPrice = menuItem.Price,
                Quantity = item.Quantity,
                SpecialInstructions = item.SpecialInstructions
            };
        }).ToList();

        var subtotal = decimal.Round(
            orderItems.Sum(item => item.UnitPrice * item.Quantity),
            2,
            MidpointRounding.AwayFromZero);

        // Delivery fees and taxes remain zero until pricing rules are configured.
        const decimal deliveryFee = 0m;
        const decimal taxAmount = 0m;
        var totalAmount = subtotal + deliveryFee + taxAmount;

        var order = new Order
        {
            OrderNumber = $"FS-{Guid.NewGuid():N}".ToUpperInvariant(),
            CustomerId = customer.Id,
            RestaurantId = restaurant.Id,
            Restaurant = restaurant,
            Status = OrderStatus.Placed,
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            CurrencyCode = "INR",
            RecipientName = recipientName,
            RecipientPhoneNumber = recipientPhone,
            DeliveryAddressLine1 = address.Line1,
            DeliveryAddressLine2 = address.Line2,
            DeliveryCity = address.City,
            DeliveryState = address.State,
            DeliveryPostalCode = address.PostalCode,
            DeliveryCountry = address.Country,
            CustomerNote = customerNote,
            PlacedAtUtc = now,
            UpdatedAtUtc = now,
            Items = orderItems,
            Payments =
            [
                new Payment
                {
                    Method = request.PaymentMethod,
                    Status = PaymentStatus.Pending,
                    Amount = totalAmount,
                    CurrencyCode = "INR",
                    CreatedAtUtc = now
                }
            ],
            StatusHistory =
            [
                new OrderStatusHistory
                {
                    Status = OrderStatus.Placed,
                    ChangedAtUtc = now,
                    Note = "Order placed by customer.",
                    ChangedByUserId = customer.Id
                }
            ]
        };

        await _orderRepository.AddAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(order);
    }

    public async Task<OrderListResponse> GetRestaurantOrdersAsync(
        Guid callerUserId,
        int restaurantId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var caller = await GetActiveUserAsync(callerUserId, cancellationToken);
        if (caller.Role is not (UserRole.Admin or UserRole.RestaurantOwner))
        {
            throw new ForbiddenException(
                "Only restaurant owners and administrators can view restaurant orders.");
        }

        if (restaurantId <= 0)
        {
            throw new BadRequestException("Restaurant ID must be greater than zero.");
        }

        if (status.HasValue && !Enum.IsDefined(typeof(OrderStatus), status.Value))
        {
            throw new BadRequestException("A valid order status is required.");
        }

        var restaurant = await _restaurantRepository.GetByIdAsync(
            restaurantId,
            cancellationToken);

        if (restaurant is null)
        {
            throw new NotFoundException($"Restaurant with ID {restaurantId} was not found.");
        }

        if (caller.Role == UserRole.RestaurantOwner && restaurant.OwnerId != caller.Id)
        {
            throw new ForbiddenException(
                "You can view orders only for restaurants that you own.");
        }

        var (skip, validatedPageSize) = ValidatePage(page, pageSize);
        var (orders, totalCount) = await _orderRepository.GetPageByRestaurantIdAsync(
            restaurantId,
            status,
            skip,
            validatedPageSize,
            cancellationToken);

        return MapToList(orders, page, validatedPageSize, totalCount);
    }

    public async Task<OrderListResponse> GetAvailableDeliveryOrdersAsync(
        Guid deliveryPartnerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var partner = await GetActiveUserAsync(deliveryPartnerId, cancellationToken);
        if (partner.Role != UserRole.DeliveryPartner)
        {
            throw new ForbiddenException(
                "Only delivery partners can view orders available for delivery.");
        }

        var (skip, validatedPageSize) = ValidatePage(page, pageSize);
        var (orders, totalCount) = await _orderRepository.GetAvailableForDeliveryPageAsync(
            skip,
            validatedPageSize,
            cancellationToken);

        return MapToList(orders, page, validatedPageSize, totalCount);
    }

    public async Task<OrderListResponse> GetMyDeliveryOrdersAsync(
        Guid deliveryPartnerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var partner = await GetActiveUserAsync(deliveryPartnerId, cancellationToken);
        if (partner.Role != UserRole.DeliveryPartner)
        {
            throw new ForbiddenException("Only delivery partners can view assigned deliveries.");
        }

        var (skip, validatedPageSize) = ValidatePage(page, pageSize);
        var (orders, totalCount) = await _orderRepository.GetDeliveryPartnerPageAsync(
            partner.Id,
            skip,
            validatedPageSize,
            cancellationToken);

        return MapToList(orders, page, validatedPageSize, totalCount);
    }

    public async Task<OrderResponse> GetOperationalByIdAsync(
        Guid callerUserId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var caller = await GetActiveUserAsync(callerUserId, cancellationToken);
        ValidateOrderId(orderId);

        var order = await _orderRepository.GetByIdForWorkflowAsync(
            orderId,
            cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Order with ID {orderId} was not found.");
        }

        var isAdministrator = caller.Role == UserRole.Admin;
        var ownsRestaurant = caller.Role == UserRole.RestaurantOwner &&
                             order.Restaurant.OwnerId == caller.Id;
        var isAssignedDeliveryPartner = caller.Role == UserRole.DeliveryPartner &&
                                        order.DeliveryPartnerId == caller.Id;

        if (!isAdministrator && !ownsRestaurant && !isAssignedDeliveryPartner)
        {
            throw new ForbiddenException(
                "You do not have access to the operational details for this order.");
        }

        return MapToResponse(order);
    }

    public async Task ClaimDeliveryAsync(
        Guid deliveryPartnerId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var partner = await GetActiveUserAsync(deliveryPartnerId, cancellationToken);
        if (partner.Role != UserRole.DeliveryPartner)
        {
            throw new ForbiddenException("Only delivery partners can claim deliveries.");
        }

        ValidateOrderId(orderId);
        var order = await _orderRepository.GetByIdForWorkflowAsync(
            orderId,
            cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Order with ID {orderId} was not found.");
        }

        if (order.Status != OrderStatus.Packed)
        {
            throw new ConflictException("Only packed orders can be claimed for delivery.");
        }

        if (order.DeliveryPartnerId == partner.Id)
        {
            return;
        }

        if (order.DeliveryPartnerId.HasValue)
        {
            throw new ConflictException("This order has already been claimed by another partner.");
        }

        var claimed = await _orderRepository.TryClaimDeliveryAsync(
            orderId,
            partner.Id,
            cancellationToken);

        if (!claimed)
        {
            throw new ConflictException(
                "This order is no longer available. Refresh the delivery list and try another order.");
        }
    }

    public async Task ChangeStatusAsync(
        Guid callerUserId,
        int orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var caller = await GetActiveUserAsync(callerUserId, cancellationToken);
        ValidateOrderId(orderId);

        if (request is null)
        {
            throw new BadRequestException("Status update request body is required.");
        }

        if (!Enum.IsDefined(typeof(OrderStatus), request.Status))
        {
            throw new BadRequestException("A valid order status is required.");
        }

        var note = NormalizeOptionalText(request.Note, "Status note", 500);
        var order = await _orderRepository.GetByIdForWorkflowAsync(
            orderId,
            cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Order with ID {orderId} was not found.");
        }

        var requireAssignedDeliveryPartner = false;

        if (caller.Role is UserRole.Admin or UserRole.RestaurantOwner)
        {
            if (caller.Role == UserRole.RestaurantOwner && order.Restaurant.OwnerId != caller.Id)
            {
                throw new ForbiddenException(
                    "You can update orders only for restaurants that you own.");
            }

            if (!CanRestaurantChangeStatus(order.Status, request.Status))
            {
                throw new ConflictException(
                    $"A restaurant cannot change an order from {order.Status} to {request.Status}.");
            }

            if (request.Status == OrderStatus.Rejected && note is null)
            {
                throw new BadRequestException("A rejection note is required.");
            }
        }
        else if (caller.Role == UserRole.DeliveryPartner)
        {
            if (order.DeliveryPartnerId != caller.Id)
            {
                throw new ForbiddenException(
                    "You can update delivery status only for orders assigned to you.");
            }

            if (!CanDeliveryPartnerChangeStatus(order.Status, request.Status))
            {
                throw new ConflictException(
                    $"A delivery partner cannot change an order from {order.Status} to {request.Status}.");
            }

            requireAssignedDeliveryPartner = true;
        }
        else
        {
            throw new ForbiddenException(
                "Your role cannot update operational order status.");
        }

        var updated = await _orderRepository.TryTransitionStatusAsync(
            orderId,
            order.Status,
            request.Status,
            caller.Id,
            requireAssignedDeliveryPartner,
            note,
            cancellationToken);

        if (!updated)
        {
            throw new ConflictException(
                "The order changed while you were updating it. Refresh it and try again.");
        }
    }

    private async Task<ApplicationUser> GetActiveCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var customer = await GetActiveUserAsync(customerId, cancellationToken);

        if (customer.Role != UserRole.Customer)
        {
            throw new ForbiddenException("Only customers can place and view customer orders.");
        }

        return customer;
    }

    private async Task<ApplicationUser> GetActiveUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedException("A valid authenticated user is required.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("Your account is unavailable.");
        }

        return user;
    }

    private static (int Skip, int PageSize) ValidatePage(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new BadRequestException("Page must be at least 1.");
        }

        if (pageSize < 1 || pageSize > MaximumPageSize)
        {
            throw new BadRequestException($"Page size must be between 1 and {MaximumPageSize}.");
        }

        var skipLong = (long)(page - 1) * pageSize;
        if (skipLong > int.MaxValue)
        {
            throw new BadRequestException("The requested page is too large.");
        }

        return ((int)skipLong, pageSize);
    }

    private static OrderListResponse MapToList(
        IReadOnlyList<Order> orders,
        int page,
        int pageSize,
        int totalCount)
    {
        return new OrderListResponse
        {
            Items = orders.Select(MapToSummary).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static bool CanRestaurantChangeStatus(
        OrderStatus currentStatus,
        OrderStatus nextStatus)
    {
        return (currentStatus, nextStatus) switch
        {
            (OrderStatus.Placed, OrderStatus.Accepted) => true,
            (OrderStatus.Placed, OrderStatus.Rejected) => true,
            (OrderStatus.Accepted, OrderStatus.Preparing) => true,
            (OrderStatus.Preparing, OrderStatus.Packed) => true,
            _ => false
        };
    }

    private static bool CanDeliveryPartnerChangeStatus(
        OrderStatus currentStatus,
        OrderStatus nextStatus)
    {
        return (currentStatus, nextStatus) switch
        {
            (OrderStatus.Packed, OrderStatus.PickedUp) => true,
            (OrderStatus.PickedUp, OrderStatus.Delivered) => true,
            _ => false
        };
    }

    private static void ValidateOrderId(int orderId)
    {
        if (orderId <= 0)
        {
            throw new BadRequestException("Order ID must be greater than zero.");
        }
    }

    private static string NormalizeRequiredText(
        string? value,
        string fieldName,
        int minimumLength,
        int maximumLength)
    {
        var normalizedValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            throw new BadRequestException($"{fieldName} is required.");
        }

        if (normalizedValue.Length < minimumLength || normalizedValue.Length > maximumLength)
        {
            throw new BadRequestException(
                $"{fieldName} must be between {minimumLength} and {maximumLength} characters.");
        }

        return normalizedValue;
    }

    private static string? NormalizeOptionalText(
        string? value,
        string fieldName,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > maximumLength)
        {
            throw new BadRequestException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalizedValue;
    }

    private static OrderSummaryResponse MapToSummary(Order order)
    {
        return new OrderSummaryResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            RestaurantId = order.RestaurantId,
            RestaurantName = order.Restaurant.Name,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CurrencyCode = order.CurrencyCode,
            PlacedAtUtc = order.PlacedAtUtc
        };
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            RestaurantId = order.RestaurantId,
            RestaurantName = order.Restaurant.Name,
            Status = order.Status,
            Subtotal = order.Subtotal,
            DeliveryFee = order.DeliveryFee,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            CurrencyCode = order.CurrencyCode,
            RecipientName = order.RecipientName,
            RecipientPhoneNumber = order.RecipientPhoneNumber,
            DeliveryAddressLine1 = order.DeliveryAddressLine1,
            DeliveryAddressLine2 = order.DeliveryAddressLine2,
            DeliveryCity = order.DeliveryCity,
            DeliveryState = order.DeliveryState,
            DeliveryPostalCode = order.DeliveryPostalCode,
            DeliveryCountry = order.DeliveryCountry,
            CustomerNote = order.CustomerNote,
            PlacedAtUtc = order.PlacedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc,
            Items = order.Items.Select(item => new OrderItemResponse
            {
                MenuItemId = item.MenuItemId,
                ItemName = item.ItemName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                LineTotal = item.UnitPrice * item.Quantity,
                SpecialInstructions = item.SpecialInstructions
            }).ToList(),
            Payments = order.Payments.Select(payment => new PaymentResponse
            {
                Id = payment.Id,
                Method = payment.Method,
                Status = payment.Status,
                Amount = payment.Amount,
                CurrencyCode = payment.CurrencyCode,
                ProviderReference = payment.ProviderReference,
                CreatedAtUtc = payment.CreatedAtUtc,
                PaidAtUtc = payment.PaidAtUtc
            }).ToList(),
            StatusHistory = order.StatusHistory
                .OrderBy(history => history.ChangedAtUtc)
                .Select(history => new OrderStatusHistoryResponse
                {
                    Status = history.Status,
                    ChangedAtUtc = history.ChangedAtUtc,
                    Note = history.Note
                })
                .ToList()
        };
    }
}
