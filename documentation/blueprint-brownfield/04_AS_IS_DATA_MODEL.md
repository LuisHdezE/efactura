# As-Is Data Model

## Modelo de datos y duplicaciones
- Existe duplicación de modelos entre los persistentes/dominio. 
- Evidencia (`OBSERVED`): Coexisten `Customer.cs` y `Customers.cs`, `Product.cs` y `Products.cs`, `Supplier.cs` y `Suppliers.cs` en `ApplicationCore.Entities`.
- `HYP-004: CONFIRMED`.
