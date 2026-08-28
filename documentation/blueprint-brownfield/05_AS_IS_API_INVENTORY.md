# AS-IS API Inventory

Baseline: `efactura@a6c9bf96572b8a0a88efde2c68b0749a71020a18`.

## Summary

- Controller files: **16**
- Controllers with actions: **14**
- Empty controller shells: **2** (`SupplierTypeController`, `TaxTypeController`)
- HTTP actions/endpoints observed: **69**
- Version prefix: **none**; routes begin with `/api/...`
- Swagger: configured with bearer security metadata, Development-only UI/document serving.
- AuthN: JWT bearer middleware configured.
- AuthZ: **no `[Authorize]`, policy, role or fallback authorization enforcement observed on these controllers/configuration**.
- Common result pattern: service returns `ResultObject`; controller returns HTTP 200 when `Status=true`, otherwise HTTP 400. Most create endpoints return 200 rather than 201.

`Swagger bearer metadata != endpoint authorization enforcement`.

## Endpoint-by-endpoint inventory

| # | Controller | Method | Route | Input | Output | Service | AuthZ observed | Error behavior / notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | ContactDetail | GET | `/api/ContactDetail/{id}` | route `id:int` | ResultObject | IContactDetailService.GetById | NONE_OBSERVED | 200/400 |
| 2 | ContactDetail | GET | `/api/ContactDetail` | none | ResultObject | GetAll | NONE_OBSERVED | 200/400 |
| 3 | ContactDetail | POST | `/api/ContactDetail` | CreateContactDetailVO body | ResultObject | Create | NONE_OBSERVED | 200/400 |
| 4 | ContactDetail | PUT | `/api/ContactDetail/{id}` | route id + UpdateContactDetailVO | ResultObject | Update | NONE_OBSERVED | bare 400 on id mismatch; else 200/400 |
| 5 | ContactDetail | DELETE | `/api/ContactDetail/{id}` | route id | ResultObject | Delete | NONE_OBSERVED | 200/400 |
| 6 | ContactDetail | GET | `/api/ContactDetail/by-customer/{customerId}` | route customerId | ResultObject | GetByCustomerIdAsync | NONE_OBSERVED | casts long to int; 200/400 |
| 7 | ContactType | GET | `/api/ContactType` | none | ResultObject | GetAll | NONE_OBSERVED | 200/400 |
| 8 | ContactType | GET | `/api/ContactType/{id}` | route id | ResultObject | GetById | NONE_OBSERVED | 200/400 |
| 9 | ContactType | POST | `/api/ContactType` | CreateContactTypeVO | ResultObject | Create | NONE_OBSERVED | 200/400 |
| 10 | ContactType | PUT | `/api/ContactType/{id}` | id + UpdateContactTypeVO | ResultObject | Update | NONE_OBSERVED | 400 mismatch; else 200/400 |
| 11 | ContactType | DELETE | `/api/ContactType/{id}` | route id | ResultObject | Delete | NONE_OBSERVED | 200/400 |
| 12 | Country | GET | `/api/Country` | none | ResultObject | GetAll | NONE_OBSERVED | 200/400 |
| 13 | Country | GET | `/api/Country/{id}` | route id | ResultObject | GetById | NONE_OBSERVED | 200/400 |
| 14 | Country | POST | `/api/Country` | CreateCountryVO | ResultObject | Create | NONE_OBSERVED | 200/400 |
| 15 | Country | PUT | `/api/Country/{id}` | id + UpdateCountryVO | ResultObject | Update | NONE_OBSERVED | 400 mismatch; else 200/400 |
| 16 | Country | DELETE | `/api/Country/{id}` | route id | ResultObject | Delete | NONE_OBSERVED | 200/400 |
| 17 | Customer | GET | `/api/Customer/GetCustomerById?id={id}` | query id | ResultObject | GetCustomerById | NONE_OBSERVED | action-named route; 200/400 |
| 18 | Customer | GET | `/api/Customer/GetCustomerPaginated?Page=&RowsPerPage=` | query paging | ResultObject | GetCustomerPaginated | NONE_OBSERVED | action-named route; defaults 1/10 |
| 19 | Customer | POST | `/api/Customer/CreateCustomer` | CreateCustomerVO | ResultObject | Create | NONE_OBSERVED | 200/400, not 201 |
| 20 | Customer | PUT | `/api/Customer/UpdateCustomer` | UpdateCustomerVO | ResultObject | Update | NONE_OBSERVED | no route id; 200/400 |
| 21 | Customer | DELETE | `/api/Customer/DeleteCustomer` | DeleteCustomerVO body | ResultObject | Delete | NONE_OBSERVED | DELETE request body; 200/400 |
| 22 | CustomerType | GET | `/api/CustomerType/GetCustomerTypeById?Id=` | query Id | ResultObject | GetCustomerTypeById | NONE_OBSERVED | action-named route |
| 23 | CustomerType | GET | `/api/CustomerType/GetCustomerTypresPaginated?Page=&RowsPerPage=` | paging query | ResultObject | GetCustomerTypresPaginated | NONE_OBSERVED | route contains typo `Typres` |
| 24 | CustomerType | POST | `/api/CustomerType` | CreateCustomerTypeVO | ResultObject | Create | NONE_OBSERVED | 200/400 |
| 25 | CustomerType | PUT | `/api/CustomerType` | UpdateCustomerTypeVO | ResultObject | Update | NONE_OBSERVED | 200/400 |
| 26 | CustomerType | DELETE | `/api/CustomerType?Id=` | query Id | ResultObject | Delete | NONE_OBSERVED | 200/400 |
| 27 | Department | GET | `/api/Department` | none | ResultObject | GetAll | NONE_OBSERVED | 200/400 |
| 28 | Department | GET | `/api/Department/{id}` | route id | ResultObject | GetById | NONE_OBSERVED | 200/400 |
| 29 | Department | POST | `/api/Department` | CreateDepartmentVO | ResultObject | Create | NONE_OBSERVED | 200/400 |
| 30 | Department | PUT | `/api/Department/{id}` | id + UpdateDepartmentVO | ResultObject | Update | NONE_OBSERVED | 400 mismatch; else 200/400 |
| 31 | Department | DELETE | `/api/Department/{id}` | route id | ResultObject | Delete | NONE_OBSERVED | 200/400 |
| 32 | DocumentType | GET | `/api/DocumentType` | none | ResultObject | GetAll | NONE_OBSERVED | 200/400 |
| 33 | DocumentType | GET | `/api/DocumentType/{id}` | route id | ResultObject | GetById | NONE_OBSERVED | 200/400 |
| 34 | DocumentType | POST | `/api/DocumentType` | CreateDocumentTypeVO | ResultObject | Create | NONE_OBSERVED | 200/400 |
| 35 | DocumentType | PUT | `/api/DocumentType/{id}` | id + UpdateDocumentTypeVO | ResultObject | Update | NONE_OBSERVED | 400 mismatch; else 200/400 |
| 36 | DocumentType | DELETE | `/api/DocumentType/{id}` | route id | ResultObject | Delete | NONE_OBSERVED | 200/400 |
| 37 | Info | GET | `/api/Info/ping` | none | direct value | internal | NONE_OBSERVED | 200 |
| 38 | Info | GET | `/api/Info/version` | none | anonymous app name/version | IConfiguration | NONE_OBSERVED | 200 |
| 39 | Info | GET | `/api/Info/fecha` | none | server DateTime.Now | internal | NONE_OBSERVED | 200; server-local clock exposed |
| 40 | InvoiceIndicator | GET | `/api/InvoiceIndicator` | none | ResultObject | GetAll | NONE_OBSERVED | 200/400 |
| 41 | InvoiceIndicator | GET | `/api/InvoiceIndicator/{id}` | route id | ResultObject | GetById | NONE_OBSERVED | 200/400 |
| 42 | InvoiceIndicator | POST | `/api/InvoiceIndicator` | CreateInvoiceIndicatorVO | ResultObject | Create | NONE_OBSERVED | 200/400 |
| 43 | InvoiceIndicator | PUT | `/api/InvoiceIndicator/{id}` | id + UpdateInvoiceIndicatorVO | ResultObject | Update | NONE_OBSERVED | 400 mismatch; else 200/400 |
| 44 | InvoiceIndicator | DELETE | `/api/InvoiceIndicator/{id}` | route id | ResultObject | Delete | NONE_OBSERVED | 200/400 |
| 45 | PaymentMethod | GET | `/api/PaymentMethod` | none | ResultObject | GetAll | NONE_OBSERVED | 200/400 |
| 46 | PaymentMethod | GET | `/api/PaymentMethod/{id}` | route id | ResultObject | GetById | NONE_OBSERVED | 200/400 |
| 47 | PaymentMethod | POST | `/api/PaymentMethod` | CreatePaymentMethodVO | ResultObject | Create | NONE_OBSERVED | 200/400 |
| 48 | PaymentMethod | PUT | `/api/PaymentMethod/{id}` | id + UpdatePaymentMethodVO | ResultObject | Update | NONE_OBSERVED | 400 mismatch; else 200/400 |
| 49 | PaymentMethod | DELETE | `/api/PaymentMethod/{id}` | route id | ResultObject | Delete | NONE_OBSERVED | 200/400 |
| 50 | ProductCategory | GET | `/api/ProductCategory` | none | ResultObject | GetAll | NONE_OBSERVED | 200/400 |
| 51 | ProductCategory | GET | `/api/ProductCategory/{id}` | route id | ResultObject | GetById | NONE_OBSERVED | 200/400 |
| 52 | ProductCategory | POST | `/api/ProductCategory` | CreateProductCategoryVO | ResultObject | Create | NONE_OBSERVED | 200/400 |
| 53 | ProductCategory | PUT | `/api/ProductCategory/{id}` | id + UpdateProductCategoryVO | ResultObject | Update | NONE_OBSERVED | 400 mismatch; else 200/400 |
| 54 | ProductCategory | DELETE | `/api/ProductCategory/{id}` | route id | ResultObject | Delete | NONE_OBSERVED | 200/400 |
| 55 | Products | GET | `/api/Products/GetProductById?Id=` | query Id | ResultObject | GetProductById | NONE_OBSERVED | action-named route |
| 56 | Products | GET | `/api/Products/GetProductsPaginated?Page=&RowsPerPage=` | paging query | ResultObject | GetProductsPaginated | NONE_OBSERVED | defaults 1/10 |
| 57 | Products | POST | `/api/Products` | CreateProductVO | ResultObject | Create | NONE_OBSERVED | 200/400 |
| 58 | Products | PUT | `/api/Products` | UpdateProductVO | ResultObject | Update | NONE_OBSERVED | 200/400 |
| 59 | Products | DELETE | `/api/Products?customerTypeId=&deletedBy=` | query parameters | ResultObject | Delete | NONE_OBSERVED | suspicious parameter name `customerTypeId`; 200/400 |
| 60 | Supplier | GET | `/api/Supplier` | none | ResultObject | GetAll | NONE_OBSERVED | 200/400 |
| 61 | Supplier | GET | `/api/Supplier/{id}` | route id | ResultObject | GetById | NONE_OBSERVED | 200/400 |
| 62 | Supplier | POST | `/api/Supplier` | CreateSupplierVO | ResultObject | Create | NONE_OBSERVED | 200/400 |
| 63 | Supplier | PUT | `/api/Supplier/{id}` | id + UpdateSupplierVO | ResultObject | Update | NONE_OBSERVED | 400 mismatch; else 200/400 |
| 64 | Supplier | DELETE | `/api/Supplier/{id}` | route id | ResultObject | Delete | NONE_OBSERVED | 200/400 |
| 65 | VoucherType | GET | `/api/VoucherType` | none | ResultObject | GetAll | NONE_OBSERVED | 200/400 |
| 66 | VoucherType | GET | `/api/VoucherType/{id}` | route id | ResultObject | GetById | NONE_OBSERVED | 200/400 |
| 67 | VoucherType | POST | `/api/VoucherType` | CreateVoucherTypeVO | ResultObject | Create | NONE_OBSERVED | 200/400 |
| 68 | VoucherType | PUT | `/api/VoucherType/{id}` | id + UpdateVaucherTypeVO | ResultObject | Update | NONE_OBSERVED | VO name typo; 400 mismatch; else 200/400 |
| 69 | VoucherType | DELETE | `/api/VoucherType/{id}` | route id | ResultObject | Delete | NONE_OBSERVED | 200/400 |

## Empty controller shells

- `SupplierTypeController`: no `[ApiController]`, route or actions.
- `TaxTypeController`: no `[ApiController]`, route or actions.

## Contract observations

- No `/api/v1` versioned boundary exists.
- CRUD route conventions are inconsistent between entity controllers.
- `ResultObject` carries application status while HTTP error mapping is largely collapsed to 400.
- No endpoint-level permission matrix exists in code.
- No CFE/POS/inventory movement/invoice/payment/purchase-order/cash/audit endpoints exist yet despite some corresponding data models.
- Swagger existence does not constitute a reviewed/authoritative OpenAPI contract.
