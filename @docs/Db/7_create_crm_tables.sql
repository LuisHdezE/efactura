-- Secuencias para ContactTypes
DROP SEQUENCE IF EXISTS public."ContactTypesIdSeq" CASCADE;
CREATE SEQUENCE public."ContactTypesIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para Countries
DROP SEQUENCE IF EXISTS public."CountriesIdSeq" CASCADE;
CREATE SEQUENCE public."CountriesIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para ContactDetails
DROP SEQUENCE IF EXISTS public."ContactDetailsIdSeq" CASCADE;
CREATE SEQUENCE public."ContactDetailsIdSeq"
    INCREMENT 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1;

-- Secuencias para Departments
DROP SEQUENCE IF EXISTS public."DepartmentsIdSeq" CASCADE;
CREATE SEQUENCE public."DepartmentsIdSeq"
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

DROP TABLE IF EXISTS public."Countries";
CREATE TABLE "public"."Countries" (
    "Id" int8 NOT NULL DEFAULT nextval('"CountriesIdSeq"'::regclass),
    "Name" VARCHAR(150),
    "Code" VARCHAR(2),
    "CreatedAt" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp(6) NULL,
    PRIMARY KEY ("Id")
);

DROP TABLE IF EXISTS public."Departments";
CREATE TABLE "public"."Departments" (
    "Id" int8 NOT NULL DEFAULT nextval('"DepartmentsIdSeq"'::regclass),
    "Name" VARCHAR(100) NOT NULL,
    "CountryId" INT NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_department_country
        FOREIGN KEY("CountryId")
        REFERENCES "public"."Countries"("Id")
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    PRIMARY KEY ("Id")
);



ALTER TABLE "public"."Customers"
    DROP COLUMN "Email",
    DROP COLUMN "Phone";

ALTER TABLE "public"."Customers"
    ADD COLUMN "UniqueCode" VARCHAR(100) NOT NULL DEFAULT '00',
    ADD COLUMN "DepartmentId" INT,
    ADD COLUMN "City" VARCHAR(80),
    ADD COLUMN "ZipCode" VARCHAR(5);

   
ALTER TABLE "public"."Customers"
    ADD CONSTRAINT fk_customer_customerType
        FOREIGN KEY("CustomerTypeId")
        REFERENCES "public"."CustomerTypes"("Id")
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    ADD CONSTRAINT fk_customer_department
        FOREIGN KEY("DepartmentId")
        REFERENCES "public"."Departments"("Id")
        ON DELETE CASCADE
        ON UPDATE CASCADE;

DROP TABLE IF EXISTS public."ContactTypes";
CREATE TABLE public."ContactTypes" (
    "Id" int8 NOT NULL DEFAULT nextval('"ContactTypesIdSeq"'::regclass),
    "Name" varchar(100) COLLATE "pg_catalog"."default" NOT NULL,
    "CreatedAt" timestamp(6) DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp(6) NULL,
    PRIMARY KEY ("Id")
);

DROP TABLE IF EXISTS public."ContactDetails";
CREATE TABLE "public"."ContactDetails" (
    "Id" int8 NOT NULL DEFAULT nextval('"ContactDetailsIdSeq"'::regclass),
    "CustomerId" BIGINT,
    "ContactTypeId" BIGINT,
    "Value" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_contact_detail_customer
        FOREIGN KEY("CustomerId")
        REFERENCES "public"."Customers"("Id"),
    CONSTRAINT fk_contactDetail_contactType
        FOREIGN KEY("ContactTypeId")
        REFERENCES "public"."ContactTypes"("Id"),
    PRIMARY KEY ("Id")
);

DROP TABLE IF EXISTS public."TaxTypes";
CREATE TABLE public."TaxTypes" (
    "Id" int8 NOT NULL DEFAULT nextval('"TaxTypesIdSeq"'::regclass),
    "Name" varchar(100) COLLATE "pg_catalog"."default" NOT NULL,
    "Rate" numeric(5,2) NOT NULL,
    PRIMARY KEY ("Id")
);


-- Insertar datos en la tabla Countries
INSERT INTO "public"."Countries" ("Name","Code") VALUES ('United States', 'US');
INSERT INTO "public"."Countries" ("Name","Code") VALUES ('Canada', 'CA');
INSERT INTO "public"."Countries" ("Name","Code") VALUES ('Mexico', 'MX');
INSERT INTO "public"."Countries" ("Name","Code") VALUES ('Germany', 'DE');
INSERT INTO "public"."Countries" ("Name","Code") VALUES ('Brazil', 'BR');
INSERT INTO "public"."Countries" ("Name","Code") VALUES ('Uruguay', 'UY');

-- Insertar datos en la tabla Departments
INSERT INTO "public"."Departments" ("Name", "CountryId", "CreatedAt", "UpdatedAt")
VALUES
('Artigas', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Canelones', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Cerro Largo', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Colonia', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Durazno', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Flores', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Florida', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Lavalleja', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Maldonado', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Montevideo', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Paysandú', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Río Negro', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Rivera', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Rocha', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Salto', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('San José', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Soriano', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Tacuarembó', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Treinta y Tres', 6, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insertar datos en la tabla ContactTypes
INSERT INTO "public"."ContactTypes" ("Name")
VALUES 
('Phone'),
('Mobile'),
('Email'),
('Fax');

-- ----------------------------
-- Records of tax_type
-- ----------------------------
INSERT INTO "public"."TaxTypes" ("Name", "Rate") VALUES 
('VAT', 10.00),
('Sales Tax', 7.50),
('VAT', 10.00),
('Sales Tax', 7.50),
('Import Duty', 5.00),
('Luxury Tax', 15.00);