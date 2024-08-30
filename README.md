# Microservices Overview

## AuthAPI
**Description:**  
Provides authentication and authorization functionalities, including JWT token generation and validation.

**Technology:**  
- ASP.NET Core
- Identity

**Endpoints:**  
- `POST /api/auth/login`  
- `POST /api/auth/register`  
- `GET /api/auth/validate-token`

**DTOs & Mappings:**  
- **LoginDto:** Used for user login requests.
- **RegisterDto:** Used for user registration.
- **Mapping:** DTOs are mapped to `ApplicationUser` entities using AutoMapper in the `AuthService`.

## OrderAPI
**Description:**  
Manages customer orders, including order creation, status updates, and order history.

**Technology:**  
- ASP.NET Core
- Entity Framework Core

**Endpoints:**  
- `POST /api/orders`  
- `GET /api/orders/{orderId}`  
- `GET /api/orders/customer/{customerId}`

**DTOs & Mappings:**  
- **OrderDto:** Represents order data for creation and retrieval.
- **OrderDetailDto:** Represents detailed order information.
- **Mapping:** DTOs are mapped to `Order` and `OrderDetail` entities using AutoMapper in the `OrderService`.

## ProductAPI
**Description:**  
Manages the catalog of products, including product information, categories, and inventory management.

**Technology:**  
- ASP.NET Core
- Entity Framework Core


**Real-time Communication:**  
- SignalR is used to provide real-time updates to the shopping cart. For example, when an item is added or removed from the cart, the update is pushed to the connected clients instantly.

**Audit Logging:**  
- All authentication-related actions are logged for audit purposes, including user ID, timestamp, and action type (login, register, etc.).
- IP tracing is enabled to record the IP address of the client making the request for security monitoring and analysis.

**Endpoints:**  
- `GET /api/products`  
- `GET /api/products/{productId}`  
- `POST /api/products`

**DTOs & Mappings:**  
- **ProductDto:** Represents product data.
- **CategoryDto:** Represents product category data.
- **Mapping:** DTOs are mapped to `Product` and `Category` entities using AutoMapper in the `ProductService`.

## ShoppingCartAPI
**Description:**  
Manages shopping cart operations, including adding, updating, and removing items from the cart.

**Technology:**  
- ASP.NET Core

**Endpoints:**  
- `POST /api/cart`  
- `GET /api/cart/{customerId}`  
- `DELETE /api/cart/{customerId}/items/{itemId}`

**DTOs & Mappings:**  
- **CartItemDto:** Represents an item in the shopping cart.
- **ShoppingCartDto:** Represents the entire shopping cart.
- **Mapping:** DTOs are mapped to `CartItem` and `ShoppingCart` entities using AutoMapper in the `ShoppingCartService`.

## Gateway
**Description:**  
Acts as a reverse proxy, routing client requests to the appropriate microservices.

**Technology:**  
- Ocelot
- ASP.NET Core

**Configuration:**  
- Defined in `ocelot.json`

## WebApplicationUI
**Description:**  
An ASP.NET MVC web application that provides a user interface for customers to interact with the system, including browsing products, managing their cart, and placing orders.

**Technology:**  
- ASP.NET MVC
- Razor Views

**Endpoints:**  
- `/home`  
- `/products`  
- `/cart`  
- `/orders`
