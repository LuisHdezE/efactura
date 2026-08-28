
using ApplicationCore.Entites;
using ApplicationCore.Entities;
using ApplicationCore.ValueObjects.ContactDetail;
using ApplicationCore.ValueObjects.ContactType;
using ApplicationCore.ValueObjects.Country;
using ApplicationCore.ValueObjects.CustomerType;
using ApplicationCore.ValueObjects.DocumentType;
using ApplicationCore.ValueObjects.InvoiceIndicator;
using ApplicationCore.ValueObjects.PaymentMethod;
using ApplicationCore.ValueObjects.ProductCategory;
using ApplicationCore.ValueObjects.Supplier;
using ApplicationCore.ValueObjects.SupplierType;
using ApplicationCore.ValueObjects.TaxType;
using ApplicationCore.ValueObjects.VoucherType;
using AutoMapper;

namespace ApplicationCore.Automapper
{
    public class AutomapperProfiles : Profile
    {
        public AutomapperProfiles()
        {
            CreateMap<CreateCustomerTypeVO, CustomerTypes>();
            CreateMap<UpdateCustomerTypeVO, CustomerTypes>();
            CreateMap<CustomerTypes, ListCustomerTypeVO>();

            CreateMap<CreateCountryVO, Country>();
            CreateMap<UpdateCountryVO, Country>();
            CreateMap<Country, ListCountryVO>();
            CreateMap<Country, GetCountryByIdVO>();

            CreateMap<CreateDocumentTypeVO, DocumentType>();
            CreateMap<UpdateDocumentTypeVO, DocumentType>();
            CreateMap<DocumentType, ListDocumentTypeVO>();
            CreateMap<DocumentType, GetDocumentTypeVO>();

            CreateMap<CreateInvoiceIndicatorVO, InvoiceIndicator>();
            CreateMap<UpdateInvoiceIndicatorVO, InvoiceIndicator>();
            CreateMap<InvoiceIndicator, ListInvoiceIndicatorVO>();
            CreateMap<InvoiceIndicator, GetInvoiceIndicatorVO>();

            CreateMap<CreateContactTypeVO, ContactType>();
            CreateMap<UpdateContactTypeVO, ContactType>();
            CreateMap<ContactType, ListContactTypeVO>();
            CreateMap<ContactType, GetContactTypeVO>();

            CreateMap<CreateContactDetailVO, ContactDetail>();
            CreateMap<UpdateContactDetailVO, ContactDetail>();
            CreateMap<ContactDetail, ListContactDetailVO>();
            CreateMap<ContactDetail, GetContactDetailVO>();

            CreateMap<CreatePaymentMethodVO, PaymentMethod>();
            CreateMap<UpdatePaymentMethodVO, PaymentMethod>();
            CreateMap<PaymentMethod, ListPaymentMethodVO>();
            CreateMap<PaymentMethod, GetPaymentMethodVO>();

            CreateMap<CreateProductCategoryVO, ProductCategory>();
            CreateMap<UpdateProductCategoryVO, ProductCategory>();
            CreateMap<ProductCategory, ListProductCategoryVO>();
            CreateMap<ProductCategory, GetProductCategoryVO>();

            CreateMap<CreateSupplierVO, Supplier>();
            CreateMap<UpdateSupplierVO, Supplier>();
            CreateMap<Supplier, ListSupplierVO>();
            CreateMap<Supplier, GetSupplierVO>();

            CreateMap<CreateSupplierTypeVO, SupplierTypes>();
            CreateMap<UpdateSupplierTypeVO, SupplierTypes>();
            CreateMap<SupplierTypes, ListSupplierTypeVO>();
            CreateMap<SupplierTypes, GetSupplierTypeVO>();


            CreateMap<CreateTaxTypeVO, TaxTypes>();
            CreateMap<UpdateTaxTypeVO, TaxTypes>();
            CreateMap<TaxTypes, ListTaxTypeVO>();
            CreateMap<TaxTypes, GetTaxTypeVO>();

            CreateMap<CreateVoucherTypeVO, VoucherType>();
            CreateMap<UpdateVaucherTypeVO, VoucherType>();
            CreateMap<VoucherType, ListVoucherTypeVO>();
            CreateMap<VoucherType, GetVoucherTypeVO>();




        }
    }
}