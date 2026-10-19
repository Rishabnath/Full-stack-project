# FoodSupply frontend

Angular 20 customer and operations app for the FoodSupply ASP.NET Core API. The interface uses the API's real role permissions and DTOs; dashboard charts are derived from API responses rather than sample counters.

## Run locally

1. Start PostgreSQL and RabbitMQ, then run the API with its HTTP profile from the solution directory:

   ```powershell
   dotnet run --project .\backend\foodsupply.api\foodsupply.api.csproj --launch-profile http
   ```

2. In a second terminal, go to `frontend` and install/start Angular:

   ```powershell
   cd .\frontend
   npm install
   npm start
   ```

3. Open `http://localhost:4200`. The frontend calls `http://localhost:5215/api`, matching the backend's HTTP launch profile. Change `src/app/core/environment.ts` if the API uses another address.

## Screens and workflows

- **Customer:** browse restaurants and menus, filter categories, keep a basket, place a cash-on-delivery order, view order details/history, cancel an order before acceptance, manage delivery addresses, and request RestaurantOwner or DeliveryPartner access.
- **RestaurantOwner:** create/update/deactivate restaurant storefronts, manage menu items and availability, and accept, decline, prepare, and pack orders.
- **DeliveryPartner:** view packed orders, claim one, mark it picked up, and mark it delivered.
- **Admin:** view API-backed dashboard counters and charts, review role applications, manage categories and restaurants, manage menus, and operate restaurant orders.
- **All signed-in roles:** review active login sessions and revoke other sessions.

The dashboard refreshes API data every 30 seconds. Delivery status is ordinary request/response polling; this app does not use GPS tracking or SignalR. JWT access/refresh tokens live in `sessionStorage` for the current browser tab; the HTTP interceptor adds the access token and refreshes it once after an expired-token response.

## Images

FoodSupply displays direct Unsplash image URLs from the backend's demo catalogue and uses one of those credited demo photos as the sign-in hero and fallback. Image attribution is recorded in `../backend/foodsupply.api/SeedData/DEMO_IMAGE_CREDITS.md`. Restaurant and menu forms accept direct HTTP/HTTPS image URLs; file uploads are not part of the current API.
