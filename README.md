# E-Commerce API

A .NET 9 e-commerce backend API with JWT authentication, product management, shopping cart, and order processing.

## Tech Stack

- **.NET 9** — Web API
- **PostgreSQL** — Database
- **Entity Framework Core 9** — ORM
- **JWT Bearer Authentication** — Auth
- **BCrypt** — Password hashing
- **Swagger / OpenAPI** — API documentation

## Project Structure

```
src/
├── Ecommerce.API            # Web API controllers, middleware, configuration
├── Ecommerce.Application    # DTOs, interfaces, application logic
├── Ecommerce.Domain         # Entities, enums, base classes
├── Ecommerce.Infrastructure # EF Core DbContext, services, persistence config
└── Ecommerce.Shared         # Shared utilities
```

## Authentication

The API uses JWT Bearer tokens for authentication.

### Auth Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | No | Register a new user |
| POST | `/api/auth/login` | No | Login and get tokens |
| POST | `/api/auth/refresh` | No | Refresh access token |
| POST | `/api/auth/logout` | Yes | Invalidate refresh token |
| POST | `/api/auth/change-password` | Yes | Change password |
| GET | `/api/auth/me` | Yes | Get current user info |

### Register

```json
POST /api/auth/register
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "phoneNumber": "+1234567890"
}
```

### Login

```json
POST /api/auth/login
{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

### Response

```json
{
  "userId": "guid",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "base64string..."
}
```

### Using the Token

Include the token in the `Authorization` header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

## API Endpoints

### Products

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/products` | No | List products (with search, filter, pagination) |
| GET | `/api/products/{id}` | No | Get product by ID |
| POST | `/api/products` | Admin | Create product |
| PUT | `/api/products/{id}` | Admin | Update product |
| DELETE | `/api/products/{id}` | Admin | Delete product |

### Cart

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/cart` | Yes | Get user's cart |
| POST | `/api/cart/items` | Yes | Add item to cart |
| PUT | `/api/cart/items/{variantId}` | Yes | Update item quantity |
| DELETE | `/api/cart/items/{variantId}` | Yes | Remove item from cart |
| DELETE | `/api/cart` | Yes | Clear cart |

### Orders

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/orders` | Yes | Get user's orders |
| GET | `/api/orders/{orderId}` | Yes | Get order details |
| POST | `/api/orders/checkout` | Yes | Checkout (create order from cart) |
| PUT | `/api/orders/{orderId}/cancel` | Yes | Cancel order |

## Setup

### Prerequisites

- .NET 9 SDK
- PostgreSQL

### Configuration

Update `src/Ecommerce.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=EcommerceDb;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-here",
    "Issuer": "EcommerceAPI",
    "Audience": "EcommerceClient",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Run

```bash
dotnet run --project src/Ecommerce.API
```

The API will be available at `http://localhost:5000` with Swagger UI at `http://localhost:5000/swagger`.

### Database Migrations

```bash
dotnet ef migrations add InitialCreate --project src/Ecommerce.Infrastructure --startup-project src/Ecommerce.API
dotnet ef database update --project src/Ecommerce.Infrastructure --startup-project src/Ecommerce.API
```

## Roles

- **Customer** — Default role on registration. Can manage own cart and orders.
- **Admin** — Can create, update, and delete products.