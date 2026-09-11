namespace EFactura.Domain.Fiscal;

/// <summary>
/// Authoritative functional constraints pinned from DGI Reporte Diario v13.2.
///
/// This descriptor intentionally excludes XML element names, namespaces, signature algorithms and
/// schema identities until the current DGI XML/XSD bytes are separately recovered and pinned. It is
/// therefore a functional wire-contract boundary, not a serializer contract.
/// </summary>
public static class FiscalDailyReportV13_2WireContract
{
    public const string Version = "13.2";
    public const string ReportingCurrencyCode = "UYU";

    public const int MaximumSummaryZoneOccurrences = 1_000;
    public const int MaximumAmountRowsPerSummary = 1_000;
    public const int MaximumNumberRangeRepetitions = 50_000;

    public const int MonetaryIntegerDigits = 15;
    public const int MonetaryDecimalDigits = 2;
    public const int VatRateIntegerDigits = 3;
    public const int VatRateDecimalDigits = 3;
    public const int CounterDigits = 10;
    public const int BranchCodeDigits = 4;
    public const int SeriesMaximumLength = 2;

    public const long MinimumDocumentNumber = 1;
    public const long MaximumDocumentNumber = 9_999_999;
    public const int ThirdPartyPaymentIndicator = 1;
    public const int HighValueTicketCounterMinimum = 0;

    public const string XmlDigitalSignatureStandard = "XML Digital Signature";

    public const string OfficialRegistryUrl =
        "https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=";
    public const string OfficialFormatUrl =
        "https://www.efactura.dgi.gub.uy/files/formato_reporte_cfe_v13_2-pdf?es=";
    public const string OfficialExampleUrl =
        "https://www.efactura.dgi.gub.uy/files/ejemplo-de-reporte?es=";
    public const string OfficialSchemaArchiveUrl =
        "https://www.efactura.dgi.gub.uy/files/xsds_fe_1_44_2-zip?es=";
}
