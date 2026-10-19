# FoodSupply backend

FoodSupply.Api is the .NET 10 ASP.NET Core Web API. It uses PostgreSQL with EF Core Code First, JWT authentication, role-based authorization, a bounded in-process ASP.NET memory cache, and RabbitMQ order events published through a PostgreSQL outbox.

## Run locally

Start PostgreSQL and RabbitMQ, then run these from the solution directory:

```powershell
dotnet ef database update --project .\backend\foodsupply.api\foodsupply.api.csproj
dotnet run --project .\backend\foodsupply.api\foodsupply.api.csproj --launch-profile https
```

Swagger is at `https://localhost:7065/swagger`. The local CORS policy accepts Angular at `http://localhost:4200` and Vite at `http://localhost:5173`.

The PostgreSQL connection string and `Jwt:SecretKey` belong in .NET User Secrets locally. The optional bootstrap administrator uses `BootstrapAdmin:Email` and `BootstrapAdmin:Password`; the password must be at least 12 characters. Do not put secrets in `appsettings.json` or commit them. Local RabbitMQ defaults are in `appsettings.json`; configure a non-guest RabbitMQ account through user secrets for deployment.

## API routes

All successful enum values are JSON strings, such as `"Placed"` and `"Customer"`. Protected routes require `Authorization: Bearer <access-token>`.

| Area | Routes | Access |
| --- | --- | --- |
| Authentication | `POST /api/auth/register`, `/login`, `/refresh`, `/logout`; `GET /api/auth/sessions`; `DELETE /api/auth/sessions/{sessionId}` | Register, login, refresh, and logout are public. Session routes require a token. |
| Categories | `GET /api/categories`, `GET /api/categories/{id}`; `POST`, `PUT`, `DELETE` on the category routes | Reads are public; writes are Admin only. Delete deactivates. |
| Restaurants | `GET /api/restaurants?page=1&pageSize=20&city=Hyderabad`, `GET /api/restaurants/{id}`, `GET /api/restaurants/mine`; `POST /api/restaurants`, `PUT /api/restaurants/{id}`, `DELETE /api/restaurants/{id}` | Browse is public. Owners manage their restaurants; Admin can update or deactivate. |
| Menu | `GET /api/restaurants/{restaurantId}/menu-items`, `GET .../{menuItemId}`; `GET .../manage`; `POST`, `PUT`, `DELETE` on the restaurant menu route | Browse is public. The owning RestaurantOwner or Admin manages items. Delete marks an item unavailable so past orders retain their snapshots. |
| Addresses | `GET` and `POST /api/addresses`; `GET`, `PUT`, `DELETE /api/addresses/{id}`; `PUT /api/addresses/{id}/default` | Customer only; each address is scoped to its owner. |
| Orders | `POST /api/orders`, `GET /api/orders/mine`, `GET /api/orders/{id}`, `POST /api/orders/{id}/cancel` | Customer only. Cancellation is allowed only while status is `Placed`. |
| Restaurant order handling | `GET /api/orders/restaurant/{restaurantId}?page=1&pageSize=20&status=Placed`, `GET /api/orders/{id}/operations`, `PATCH /api/orders/{id}/status` | RestaurantOwner for their own restaurant, or Admin. |
| Delivery handling | `GET /api/orders/delivery/available`, `GET /api/orders/delivery/mine`, `POST /api/orders/{id}/claim`, `PATCH /api/orders/{id}/status` | DeliveryPartner only; a partner can claim a packed order once. |
| Role applications | `POST /api/role-applications`, `GET /api/role-applications/mine`, `GET /api/role-applications/pending`, `POST /api/role-applications/{id}/approve`, `/reject` | Customers apply for RestaurantOwner or DeliveryPartner. Admin reviews applications. |
| Health | `GET /health/live`, `GET /health/ready` | Public. Readiness checks PostgreSQL connectivity. |

## Order and messaging flow

Restaurant status transitions are `Placed -> Accepted -> Preparing -> Packed`; the restaurant may reject a `Placed` order. A customer can cancel only a `Placed` order. A DeliveryPartner claims a `Packed` order, then moves it through `PickedUp -> Delivered`.

Order creation, status history, and the outbox event are committed together in PostgreSQL. The background publisher sends persistent messages to RabbitMQ and retries failures. The consumer currently records received order lifecycle events in structured logs. The frontend reads order state from the order endpoints; there is no SignalR live tracking. Orders are Cash on Delivery only; there is no payment gateway.

Restaurant and menu image fields accept absolute HTTP/HTTPS image URLs. The development seed catalog uses public Unsplash URLs; this API does not upload image files.

## Cache and operations

Public catalog reads use a five-minute cache; user and order reads use a 15-second cache. Writes invalidate affected cache tags, and cache keys for private data include the owning user. The memory cache is bounded to 4,096 entries and is local to one API process. Authentication/session validation and order-changing commands remain database-backed so revocations and state transitions are enforced. HTTP responses themselves use `no-store`; this keeps browser/proxy caching separate from the server-side cache.

Logs are emitted as structured JSON to stdout, ready for a later Elastic Agent/Logstash pipeline. For a deployed frontend, configure `Cors__AllowedOrigins__0` to its HTTPS origin. For deployment, also configure the PostgreSQL connection string, a 32-byte-or-longer `Jwt__SecretKey`, and non-guest RabbitMQ credentials in the platform's secret store.
