using EFactura.Application.ReferenceData;

namespace Infrastructure.ReferenceData;

internal static class Release1FiscalReferenceCatalog
{
    public static IReadOnlyList<FiscalDocumentTypeReference> DocumentTypes { get; } =
        new FiscalDocumentTypeReference[]
        {
            new(101, "e-Ticket", "ETICKET", "ORIGINAL", 201, true),
            new(102, "Nota de Crédito de e-Ticket", "ETICKET", "CREDIT_NOTE", 202, true),
            new(103, "Nota de Débito de e-Ticket", "ETICKET", "DEBIT_NOTE", 203, true),
            new(111, "e-Factura", "EFACTURA", "ORIGINAL", 211, true),
            new(112, "Nota de Crédito de e-Factura", "EFACTURA", "CREDIT_NOTE", 212, true),
            new(113, "Nota de Débito de e-Factura", "EFACTURA", "DEBIT_NOTE", 213, true)
        };

    public static IReadOnlyList<InvoiceIndicatorReference> InvoiceIndicators { get; } =
        new InvoiceIndicatorReference[]
        {
            new(1, "Exento de IVA", "EXEMPT"),
            new(2, "Gravado a Tasa Mínima", "MINIMUM"),
            new(3, "Gravado a Tasa Básica", "BASIC"),
            new(10, "Exportación y asimiladas", "EXPORT")
        };
}
