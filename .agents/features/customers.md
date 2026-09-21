# Feature — Customers

## Purpose
Maintain milk customers (name, phone, address, default rate, active).

## Frontend
- `features/customers/customers-list.page.{ts,html,scss}`
- `features/customers/customer-form.page.{ts,html,scss}`
- `features/customers/customers.service.ts`

## Backend
- `Controllers/CustomersController.cs`
- `Services/ICustomerService.cs`, `CustomerService.cs`
- `Models/Customer.cs`

## API
- `GET/POST /api/v1/customers`
- `GET/PUT/DELETE /api/v1/customers/{id}`

## Rules
- Soft deactivate via `isActive` preferred over hard delete when referenced.


# Feature — Customers (backend)

- Controller: `Controllers/CustomersController.cs`
- Abstractions: `Services/Abstractions/ICustomerService.cs`
- Service: `Services/CustomerService.cs`
- Model: `Models/Customer.cs`
- DTOs: `DTOs/CustomerDtos.cs`
