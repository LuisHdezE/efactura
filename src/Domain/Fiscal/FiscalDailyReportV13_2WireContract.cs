namespace EFactura.Domain.Fiscal;

/// <summary>
/// Authoritative functional constraints pinned from DGI Reporte Diario v13.2.
///
/// XML root/namespace identities are exposed only after the corresponding Reporte Diario schema bytes
/// were recovered through the governed v1.44.2 byte-recovery process and pinned by hash. Cryptographic
/// algorithms remain intentionally absent because the published historical example is evidence of shape,
/// not a current algorithm policy.
/// </summary>
public static class FiscalDailyReportV13_2WireContract
{
    public const string Version = "13.2";
    public const string SchemaArchiveVersion = "1.44.2";
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

    public const string ReportSchemaFileName = "ReporteDiarioCFE.xsd";
    public const string XmlNamespace = "http://cfe.dgi.gub.uy";
    public const string XmlRootElementName = "Reporte";
    public const string XmlRootTypeName = "ReporteDefType";
    public const string XmlCaratulaElementName = "Caratula";
    public const string XmlCaratulaVersion = "1.0";
    public const string XmlDigitalSignatureNamespace = "http://www.w3.org/2000/09/xmldsig#";
    public const string XmlDigitalSignatureElementName = "Signature";
    public const bool XmlDigitalSignatureIsRequiredFinalChild = true;

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
