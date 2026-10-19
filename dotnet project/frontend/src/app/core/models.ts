export type UserRole = 'Customer' | 'RestaurantOwner' | 'Admin' | 'DeliveryPartner';
export type OrderStatus = 'Placed' | 'Accepted' | 'Preparing' | 'Packed' | 'PickedUp' | 'Delivered' | 'Rejected' | 'Cancelled';
export type ApplicationStatus = 'Pending' | 'Approved' | 'Rejected';

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  sessionId: string;
  tokenType: string;
  expiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
}

export interface Page<T> { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number; }
export interface Category { id: number; name: string; description?: string | null; isActive: boolean; }
export interface Restaurant { id: number; name: string; description?: string | null; address: string; city: string; phoneNumber?: string | null; imageUrl?: string | null; isActive: boolean; createdAtUtc: string; }
export interface MenuItem { id: number; restaurantId: number; categoryId: number; categoryName: string; name: string; description?: string | null; price: number; imageUrl?: string | null; isAvailable: boolean; createdAtUtc: string; }
export interface Address { id: number; label: string; line1: string; line2?: string | null; city: string; state: string; postalCode: string; country: string; isDefault: boolean; createdAtUtc: string; }
export interface OrderSummary { id: number; orderNumber: string; restaurantId: number; restaurantName: string; status: OrderStatus; totalAmount: number; currencyCode: string; placedAtUtc: string; }
export interface OrderItem { menuItemId: number; itemName: string; unitPrice: number; quantity: number; lineTotal: number; specialInstructions?: string | null; }
export interface Order { id: number; orderNumber: string; restaurantId: number; restaurantName: string; status: OrderStatus; subtotal: number; deliveryFee: number; taxAmount: number; totalAmount: number; currencyCode: string; recipientName: string; recipientPhoneNumber: string; deliveryAddressLine1: string; deliveryAddressLine2?: string | null; deliveryCity: string; deliveryState: string; deliveryPostalCode: string; deliveryCountry: string; customerNote?: string | null; placedAtUtc: string; updatedAtUtc: string; items: OrderItem[]; payments: unknown[]; statusHistory: { status: OrderStatus; changedAtUtc: string; note?: string | null }[]; }
export interface RoleApplication { id: string; applicantUserId: string; applicantName: string; applicantEmail: string; requestedRole: UserRole; status: ApplicationStatus; businessName?: string | null; applicationDetails: string; reviewNote?: string | null; submittedAtUtc: string; reviewedAtUtc?: string | null; }
export interface UserSession { id: string; deviceName: string; createdAtUtc: string; expiresAtUtc: string; isCurrent: boolean; }
export interface CartLine { item: MenuItem; quantity: number; }

export const ORDER_STATUSES: OrderStatus[] = ['Placed', 'Accepted', 'Preparing', 'Packed', 'PickedUp', 'Delivered', 'Rejected', 'Cancelled'];
