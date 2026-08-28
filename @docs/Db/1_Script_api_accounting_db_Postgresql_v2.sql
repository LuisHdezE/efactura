-- ----------------------------
-- 1. Secuencias (CamelCase con Mayúsculas Iniciales)
-- ----------------------------

-- (Las secuencias permanecen sin cambios)

-- Secuencias para CustomerTypes
DROP SEQUENCE IF EXISTS public."CustomerTypesIdSeq" CASCADE;
CREATE SEQUENCE public."CustomerTypesIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para SupplierTypes
DROP SEQUENCE IF EXISTS public."SupplierTypesIdSeq" CASCADE;
CREATE SEQUENCE public."SupplierTypesIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para PaymentMethods
DROP SEQUENCE IF EXISTS public."PaymentMethodsIdSeq" CASCADE;
CREATE SEQUENCE public."PaymentMethodsIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para ProductCategories
DROP SEQUENCE IF EXISTS public."ProductCategoriesIdSeq" CASCADE;
CREATE SEQUENCE public."ProductCategoriesIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para Customers
DROP SEQUENCE IF EXISTS public."CustomersIdSeq" CASCADE;
CREATE SEQUENCE public."CustomersIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para Suppliers
DROP SEQUENCE IF EXISTS public."SuppliersIdSeq" CASCADE;
CREATE SEQUENCE public."SuppliersIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para PurchaseOrders
DROP SEQUENCE IF EXISTS public."PurchaseOrdersIdSeq" CASCADE;
CREATE SEQUENCE public."PurchaseOrdersIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para Invoices
DROP SEQUENCE IF EXISTS public."InvoicesIdSeq" CASCADE; 
CREATE SEQUENCE public."InvoicesIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para Payments
DROP SEQUENCE IF EXISTS public."PaymentsIdSeq" CASCADE;
CREATE SEQUENCE public."PaymentsIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para CashTransactions
DROP SEQUENCE IF EXISTS public."CashTransactionsIdSeq" CASCADE;
CREATE SEQUENCE public."CashTransactionsIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para Products
DROP SEQUENCE IF EXISTS public."ProductsIdSeq" CASCADE;
CREATE SEQUENCE public."ProductsIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para TaxTypes
DROP SEQUENCE IF EXISTS public."TaxTypesIdSeq" CASCADE;
CREATE SEQUENCE public."TaxTypesIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- ----------------------------
-- 2. Tablas Base (Con Baja Lógica y Campos de Auditoría)
-- ----------------------------

-- Tabla CustomerTypes
DROP TABLE IF EXISTS public."CustomerTypes";
CREATE TABLE public."CustomerTypes" (
    "Id" int8 NOT NULL DEFAULT nextval('"CustomerTypesIdSeq"'::regclass),
    "Name" varchar(100) COLLATE "pg_catalog"."default" NOT NULL,
    "CreatedAt" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" int8 NOT NULL,
    "UpdatedAt" timestamp(6) NULL,
    "UpdatedBy" int8 NULL,
    "DeletedAt" timestamp(6) NULL,
    "DeletedBy" int8 NULL,
    PRIMARY KEY ("Id")
);

-- Tabla SupplierTypes
DROP TABLE IF EXISTS public."SupplierTypes";
CREATE TABLE public."SupplierTypes" (
    "Id" int8 NOT NULL DEFAULT nextval('"SupplierTypesIdSeq"'::regclass),
    "Name" varchar(100) COLLATE "pg_catalog"."default" NOT NULL,
    "CreatedAt" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" int8 NOT NULL,
    "UpdatedAt" timestamp(6) NULL,
    "UpdatedBy" int8 NULL,
    "DeletedAt" timestamp(6) NULL,
    "DeletedBy" int8 NULL,
    PRIMARY KEY ("Id")
);

-- Tabla PaymentMethods
DROP TABLE IF EXISTS public."PaymentMethods";
CREATE TABLE public."PaymentMethods" (
    "Id" int8 NOT NULL DEFAULT nextval('"PaymentMethodsIdSeq"'::regclass),
    "Name" varchar(100) COLLATE "pg_catalog"."default" NOT NULL,
    "CreatedAt" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" int8 NOT NULL,
    "UpdatedAt" timestamp(6) NULL,
    "UpdatedBy" int8 NULL,
    "DeletedAt" timestamp(6) NULL,
    "DeletedBy" int8 NULL,
    PRIMARY KEY ("Id")
);

-- Tabla ProductCategories
DROP TABLE IF EXISTS public."ProductCategories";
CREATE TABLE public."ProductCategories" (
    "Id" int8 NOT NULL DEFAULT nextval('"ProductCategoriesIdSeq"'::regclass),
    "Name" varchar(255) COLLATE "pg_catalog"."default" NOT NULL,
    "CreatedAt" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" int8 NOT NULL,
    "UpdatedAt" timestamp(6) NULL,
    "UpdatedBy" int8 NULL,
    "DeletedAt" timestamp(6) NULL,
    "DeletedBy" int8 NULL,
    PRIMARY KEY ("Id")
);

-- Tabla Customers
DROP TABLE IF EXISTS public."Customers";
CREATE TABLE public."Customers" (
    "Id" int8 NOT NULL DEFAULT nextval('"CustomersIdSeq"'::regclass),
    "Name" varchar(255) COLLATE "pg_catalog"."default" NOT NULL,
    "Email" varchar(255) COLLATE "pg_catalog"."default",
    "Phone" varchar(50) COLLATE "pg_catalog"."default",
    "Address" varchar(500) COLLATE "pg_catalog"."default",
    "CustomerTypeId" int8,
    "CreatedAt" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" int8 NOT NULL,
    "UpdatedAt" timestamp(6) NULL,
    "UpdatedBy" int8 NULL,
    "DeletedAt" timestamp(6) NULL,
    "DeletedBy" int8 NULL,
    PRIMARY KEY ("Id")
);

-- Tabla Suppliers
DROP TABLE IF EXISTS public."Suppliers";
CREATE TABLE public."Suppliers" (
    "Id" int8 NOT NULL DEFAULT nextval('"SuppliersIdSeq"'::regclass),
    "Name" varchar(255) COLLATE "pg_catalog"."default" NOT NULL,
    "ContactName" varchar(255) COLLATE "pg_catalog"."default",
    "Phone" varchar(50) COLLATE "pg_catalog"."default",
    "Email" varchar(255) COLLATE "pg_catalog"."default",
    "Address" varchar(500) COLLATE "pg_catalog"."default",
    "SupplierTypeId" int8,
    "CreatedAt" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" int8 NOT NULL,
    "UpdatedAt" timestamp(6) NULL,
    "UpdatedBy" int8 NULL,
    "DeletedAt" timestamp(6) NULL,
    "DeletedBy" int8 NULL,
    PRIMARY KEY ("Id")
);

-- Tabla PurchaseOrders
DROP TABLE IF EXISTS public."PurchaseOrders";
CREATE TABLE public."PurchaseOrders" (
    "Id" int8 NOT NULL DEFAULT nextval('"PurchaseOrdersIdSeq"'::regclass),
    "CustomerId" int8,
    "OrderDate" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "TotalAmount" numeric(10,2) NOT NULL,
    "Status" varchar(50) COLLATE "pg_catalog"."default" NOT NULL,
    "CreatedBy" int8 NOT NULL,
    "UpdatedAt" timestamp(6) NULL,
    "UpdatedBy" int8 NULL,
    "DeletedAt" timestamp(6) NULL,
    "DeletedBy" int8 NULL,
    PRIMARY KEY ("Id")
);

-- Tabla Invoices
DROP TABLE IF EXISTS public."Invoices";
CREATE TABLE public."Invoices" (
    "Id" int8 NOT NULL DEFAULT nextval('"InvoicesIdSeq"'::regclass),
    "OrderId" int8,
    "InvoiceDate" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "AmountDue" numeric(10,2) NOT NULL,
    "AmountPaid" numeric(10,2),
    "DueDate" date,
    "CreatedBy" int8 NOT NULL,
    "UpdatedAt" timestamp(6) NULL,
    "UpdatedBy" int8 NULL,
    "DeletedAt" timestamp(6) NULL,
    "DeletedBy" int8 NULL,
    PRIMARY KEY ("Id")
);

-- Tabla Payments
DROP TABLE IF EXISTS public."Payments";
CREATE TABLE public."Payments" (
    "Id" int8 NOT NULL DEFAULT nextval('"PaymentsIdSeq"'::regclass),
    "InvoiceId" int8,
    "PaymentDate" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "Amount" numeric(10,2) NOT NULL,
    "PaymentMethodId" int8,
    "CreatedBy" int8 NOT NULL,
    "UpdatedAt" timestamp(6) NULL,
    "UpdatedBy" int8 NULL,
    "DeletedAt" timestamp(6) NULL,
    "DeletedBy" int8 NULL,
    PRIMARY KEY ("Id")
);

-- Tabla CashTransactions
DROP TABLE IF EXISTS public."CashTransactions";
CREATE TABLE public."CashTransactions" (
    "Id" int8 NOT NULL DEFAULT nextval('"CashTransactionsIdSeq"'::regclass),
    "TransactionDate" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "Amount" numeric(10,2) NOT NULL,
    "TransactionType" varchar(50) COLLATE "pg_catalog"."default" NOT NULL,
    "Description" text COLLATE "pg_catalog"."default",
    "RelatedInvoiceId" int8,
    "CreatedBy" int8 NOT NULL,
    "UpdatedAt" timestamp(6) NULL,
    "UpdatedBy" int8 NULL,
    "DeletedAt" timestamp(6) NULL,
    "DeletedBy" int8 NULL,
    PRIMARY KEY ("Id")
);

-- Tabla Products
DROP TABLE IF EXISTS public."Products";
CREATE TABLE public."Products" (
    "Id" int8 NOT NULL DEFAULT nextval('"ProductsIdSeq"'::regclass),
    "Name" varchar(255) COLLATE "pg_catalog"."default" NOT NULL,
    "Description" text COLLATE "pg_catalog"."default",
    "Price" numeric(10,2) NOT NULL,
    "Stock" int4 NOT NULL,
    "ProductCategoryId" int8,
    "CreatedAt" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" int8 NOT NULL,
    "UpdatedAt" timestamp(6) NULL,
    "UpdatedBy" int8 NULL,
    "DeletedAt" timestamp(6) NULL,
    "DeletedBy" int8 NULL,
    PRIMARY KEY ("Id")
);

-- Tabla TaxTypes
DROP TABLE IF EXISTS public."TaxTypes";
CREATE TABLE public."TaxTypes" (
    "Id" int8 NOT NULL DEFAULT nextval('"TaxTypesIdSeq"'::regclass),
    "Name" varchar(100) COLLATE "pg_catalog"."default" NOT NULL,
    "Rate" numeric(5,2) NOT NULL,
    "CreatedAt" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" int8 NOT NULL,
    "UpdatedAt" timestamp(6) NULL,
    "UpdatedBy" int8 NULL,
    "DeletedAt" timestamp(6) NULL,
    "DeletedBy" int8 NULL,
    PRIMARY KEY ("Id")
);

-- ----------------------------
-- 3. Poblar Base de Datos con Datos de Prueba
-- ----------------------------

-- Insertar datos en CustomerTypes
INSERT INTO public."CustomerTypes" ("Name", "CreatedBy") VALUES
('Regular', 1),
('Premium', 1),
('VIP', 1);

-- Insertar datos en SupplierTypes
INSERT INTO public."SupplierTypes" ("Name", "CreatedBy") VALUES
('Local', 1),
('International', 1);

-- Insertar datos en PaymentMethods
INSERT INTO public."PaymentMethods" ("Name", "CreatedBy") VALUES
('Credit Card', 1),
('Bank Transfer', 1),
('Cash', 1);

-- Insertar datos en ProductCategories
INSERT INTO public."ProductCategories" ("Name", "CreatedBy") VALUES
('Electronics', 1),
('Furniture', 1),
('Clothing', 1);

-- Insertar datos en TaxTypes
INSERT INTO public."TaxTypes" ("Name", "Rate", "CreatedBy") VALUES
('IVA', 19.00, 1),
('Exento', 0.00, 1);

-- Insertar datos en Customers
INSERT INTO public."Customers" (
    "Name",
    "Email",
    "Phone",
    "Address",
    "CustomerTypeId",
    "CreatedBy"
) VALUES
('John Doe', 'johndoe@example.com', '555-1234', '123 Main St', 1, 1),
('Jane Smith', 'janesmith@example.com', '555-5678', '456 Elm St', 2, 1),
('Alice Johnson', 'alicej@example.com', '555-9012', '789 Oak St', 3, 1);

-- Insertar datos en Suppliers
INSERT INTO public."Suppliers" (
    "Name",
    "ContactName",
    "Phone",
    "Email",
    "Address",
    "SupplierTypeId",
    "CreatedBy"
) VALUES
('Tech Supplies Co.', 'Bob Brown', '555-3456', 'contact@techsupplies.com', '321 Tech Ave', 2, 1),
('Local Furniture Inc.', 'Carol White', '555-7890', 'sales@localfurniture.com', '654 Furniture Rd', 1, 1),
('Fashion Hub', 'Dave Green', '555-2345', 'info@fashionhub.com', '987 Style Blvd', 2, 1);

-- Insertar datos en Products
INSERT INTO public."Products" (
    "Name",
    "Description",
    "Price",
    "Stock",
    "ProductCategoryId",
    "CreatedBy"
) VALUES
('Smartphone', 'Latest model smartphone with advanced features', 699.99, 50, 1, 1),
('Office Chair', 'Ergonomic office chair with lumbar support', 149.99, 20, 2, 1),
('Jeans', 'Comfortable denim jeans available in various sizes', 49.99, 100, 3, 1);

-- Insertar datos en PurchaseOrders
INSERT INTO public."PurchaseOrders" (
    "CustomerId",
    "OrderDate",
    "TotalAmount",
    "Status",
    "CreatedBy"
) VALUES
(1, '2024-01-15 10:30:00', 849.98, 'Completed', 1),
(2, '2024-02-20 14:45:00', 149.99, 'Pending', 1),
(3, '2024-03-05 09:15:00', 49.99, 'Completed', 1);

-- Insertar datos en Invoices
INSERT INTO public."Invoices" (
    "OrderId",
    "InvoiceDate",
    "AmountDue",
    "AmountPaid",
    "DueDate",
    "CreatedBy"
) VALUES
(1, '2024-01-16 12:00:00', 849.98, 849.98, '2024-02-16', 1),
(2, '2024-02-21 16:00:00', 149.99, NULL, '2024-03-21', 1),
(3, '2024-03-06 11:00:00', 49.99, 49.99, '2024-04-06', 1);

-- Insertar datos en Payments
INSERT INTO public."Payments" (
    "InvoiceId",
    "PaymentDate",
    "Amount",
    "PaymentMethodId",
    "CreatedBy"
) VALUES
(1, '2024-01-17 13:00:00', 849.98, 1, 1),
(3, '2024-03-07 10:00:00', 49.99, 2, 1);

-- Insertar datos en CashTransactions
INSERT INTO public."CashTransactions" (
    "TransactionDate",
    "Amount",
    "TransactionType",
    "Description",
    "RelatedInvoiceId",
    "CreatedBy"
) VALUES
('2024-01-17 13:30:00', 849.98, 'Ingreso', 'Pago completo de factura 1', 1, 1),
('2024-03-07 10:30:00', 49.99, 'Ingreso', 'Pago completo de factura 3', 3, 1);

-- Nota: Si existen otras tablas relacionadas (como "Users"), asegúrate de insertar también datos en ellas antes de referenciarlas aquí.

-- ----------------------------
-- 4. Ejemplos de Operaciones con los Nuevos Campos
-- ----------------------------

-- Actualizar un registro (ejemplo para la tabla Customers)
UPDATE public."Customers"
SET
    "Email" = 'newemail@example.com',
    "UpdatedAt" = CURRENT_TIMESTAMP,
    "UpdatedBy" = 2
WHERE "Id" = 1;

-- Baja lógica de un registro (ejemplo para la tabla Products)
UPDATE public."Products"
SET
    "DeletedAt" = CURRENT_TIMESTAMP,
    "DeletedBy" = 2
WHERE "Id" = 1;

-- Restaurar un registro eliminado lógicamente
UPDATE public."Products"
SET
    "DeletedAt" = NULL,
    "DeletedBy" = NULL
WHERE "Id" = 1;

-- Consultar registros activos (ejemplo para la tabla Products)
SELECT * FROM public."Products"
WHERE "DeletedAt" IS NULL;

-- Consultar registros eliminados lógicamente
SELECT * FROM public."Products"
WHERE "DeletedAt" IS NOT NULL;

-- ----------------------------
-- 5. Consideraciones Adicionales
-- ----------------------------

-- **Integridad Referencial**: Si tienes claves foráneas, asegúrate de manejar adecuadamente las relaciones al realizar bajas lógicas.
-- **Triggers (Opcional)**: Puedes implementar triggers para automatizar el llenado de `"UpdatedAt"` y `"UpdatedBy"` en actualizaciones.
-- **Índices (Opcional)**: Considera crear índices en las columnas `"DeletedAt"`, `"DeletedBy"`, `"UpdatedAt"` y `"UpdatedBy"` para mejorar el rendimiento de las consultas.
-- **Control de Acceso**: Asegúrate de que solo usuarios autorizados puedan crear, modificar o eliminar registros, y que los campos de auditoría se establezcan correctamente.

