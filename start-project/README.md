# ECommerce - Start Project

This is a brownfield ecommerce application that you'll migrate to .NET Aspire during the workshop exercises.

## Architecture

The application consists of three projects:

- **ECommerce.Api** - ASP.NET Core Web API that provides product and order management endpoints
- **ECommerce.Web** - ASP.NET Core Razor Pages web frontend that consumes the API
- **ECommerce.Shared** - Shared models and contracts used by both API and Web

## Current State

This is a traditional .NET application without Aspire:

- Manual configuration of service URLs
- No built-in service discovery
- No centralized observability
- Manual container orchestration needed
- No development dashboard

## Running the Application

### Prerequisites

- .NET 9.0 SDK or later
- Your favorite IDE (Visual Studio, VS Code, or Rider)

### Running Locally

1. Start the API:
```bash
cd ECommerce.Api
dotnet run
```

The API will start at `https://localhost:7001`

2. In a new terminal, start the Web app:
```bash
cd ECommerce.Web
dotnet run
```

The Web app will start at `https://localhost:7002`

3. Open your browser and navigate to `https://localhost:7002`

### API Endpoints

The API provides the following endpoints:

**Products:**
- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `GET /api/products/category/{category}` - Get products by category

**Orders:**
- `GET /api/orders` - Get all orders
- `GET /api/orders/{id}` - Get order by ID
- `POST /api/orders` - Create a new order
- `PUT /api/orders/{id}/status` - Update order status

### Testing the API

You can test the API using the built-in OpenAPI (Swagger) interface at `https://localhost:7001/openapi/v1.json` or use curl:

```bash
# Get all products
curl https://localhost:7001/api/products

# Get a specific product
curl https://localhost:7001/api/products/1
```

## Project Structure

```
ECommerce/
├── ECommerce.Api/
│   ├── Services/
│   │   ├── ProductService.cs
│   │   └── OrderService.cs
│   └── Program.cs
├── ECommerce.Web/
│   ├── Pages/
│   │   ├── Products.cshtml
│   │   └── Products.cshtml.cs
│   ├── Services/
│   │   └── ApiClient.cs
│   └── Program.cs
└── ECommerce.Shared/
    └── Models/
        ├── Product.cs
        └── Order.cs
```

## What You'll Learn

Throughout the workshop exercises, you'll:

1. **Exercise 1**: Add Aspire orchestration and create a system topology
2. **Exercise 2**: Deploy your application using Aspire deployment features
3. **Exercise 3**: Extend Aspire with custom components and integrations

Let's get started! 🚀
