using System.Globalization;
using System.Xml.Linq;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;

namespace EFactura.Application.Fiscal;

public sealed record UnsignedCfeArtifact(
    CfeFamily Family,
    string FormatVersion,
    string ContentFingerprint,
    string Xml);

public interface IFiscalXmlBuilder
{
    UnsignedCfeArtifact Build(FiscalDocument document, FiscalContentSnapshot snapshot);
}

/// <summary>
/// Deterministically builds the unsigned CFE business artifact from immutable fiscal evidence.
/// It deliberately does not create TmstFirma, ds:Signature, access certificates, read mutable
/// masters, validate the complete CFEDGI.xsd root, persist artifacts or contact DGI/providers.
/// </summary>
public sealed class DeterministicUnsignedCfeBuilder : IFiscalXmlBuilder
{
    private static readonly XNamespace CfeNamespace = "http://cfe.dgi.gub.uy";

    public UnsignedCfeArtifact Build(FiscalDocument document, FiscalContentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(snapshot);
        snapshot.EnsureIntegrity();
        EnsureConsistent(document, snapshot);

        var familyElementName = document.CfeType switch
        {
            CfeFamily.ETicket or
            CfeFamily.ETicketCreditNote or
            CfeFamily.ETicketDebitNote => "eTck",
            CfeFamily.EFactura or
            CfeFamily.EFacturaCreditNote or
            CfeFamily.EFacturaDebitNote => "eFact",
            CfeFamily.EFacturaExportacion => throw Rule(
                "fiscal.cfe_builder.export_not_supported",
                "Release-1 unsigned CFE BUILD does not invent export-specific evidence."),
            _ => throw Rule("fiscal.cfe_builder.family_not_supported", "CFE family is not supported by the unsigned builder.")
        };

        var settlement = snapshot.FiscalEvidence.Settlement
            ?? throw Rule("fiscal.cfe_builder.settlement_required", "Unsigned CFE BUILD requires frozen settlement evidence.");
        if (!settlement.PaymentForm.HasValue)
            throw Rule("fiscal.cfe_builder.payment_form_ambiguous", "Unsigned CFE BUILD cannot invent an ambiguous DGI payment form.");

        foreach (var line in snapshot.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.UnitOfMeasure))
                throw Rule("fiscal.cfe_builder.unit_required", "Unsigned CFE BUILD requires frozen DGI unit-of-measure evidence for every line.");
        }

        var family = new XElement(
            CfeNamespace + familyElementName,
            BuildHeader(document, snapshot, settlement),
            BuildDetail(snapshot));

        if (snapshot.References is { Count: > 0 })
            family.Add(BuildReferences(snapshot.References));

        family.Add(new XElement(CfeNamespace + "CAEData",
            Element("CAE_ID", document.CaeAuthorizationNumber),
            Element("DNro", document.CaeRangeFrom),
            Element("HNro", document.CaeRangeTo),
            Element("FecVenc", Date(document.CaeValidTo))));

        var root = new XElement(CfeNamespace + "CFE",
            new XAttribute("version", "1.0"),
            family);
        var xml = new XDocument(new XDeclaration("1.0", "utf-8", null), root)
            .ToString(SaveOptions.DisableFormatting);

        return new UnsignedCfeArtifact(document.CfeType, snapshot.FormatVersion, snapshot.ContentFingerprint, xml);
    }

    private static XElement BuildHeader(
        FiscalDocument document,
        FiscalContentSnapshot snapshot,
        FiscalSettlementEvidence settlement)
    {
        var idDoc = new XElement(CfeNamespace + "IdDoc",
            Element("TipoCFE", (int)document.CfeType),
            Element("Serie", document.Series),
            Element("Nro", document.Number),
            Element("FchEmis", Date(document.FiscalDate)),
            Element("FmaPago", (int)settlement.PaymentForm!.Value));
        if (settlement.DueDate.HasValue)
            idDoc.Add(Element("FchVenc", Date(settlement.DueDate.Value)));

        var issuer = new XElement(CfeNamespace + "Emisor",
            Element("RUCEmisor", snapshot.Issuer.Ruc),
            Element("RznSoc", snapshot.Issuer.LegalName));
        if (!string.IsNullOrWhiteSpace(snapshot.Issuer.CommercialName))
            issuer.Add(Element("NomComercial", snapshot.Issuer.CommercialName!));
        issuer.Add(
            Element("CdgDGISucur", ParseBranch(snapshot.Issuer.DgiBranchCode)),
            Element("DomFiscal", snapshot.Issuer.FiscalAddress),
            Element("Ciudad", snapshot.Issuer.City),
            Element("Departamento", snapshot.Issuer.Department));

        var header = new XElement(CfeNamespace + "Encabezado", idDoc, issuer);
        if (snapshot.Receiver is not null)
            header.Add(BuildReceiver(snapshot.Receiver));
        header.Add(BuildTotals(snapshot));
        return header;
    }

    private static XElement BuildReceiver(FiscalReceiverContentSnapshot receiver)
    {
        var result = new XElement(CfeNamespace + "Receptor");
        if (receiver.Identity is not null)
        {
            result.Add(
                Element("TipoDocRecep", receiver.Identity.TypeCode),
                Element("CodPaisRecep", receiver.Identity.IssuingCountry),
                Element("DocRecep", receiver.Identity.Number));
        }
        result.Add(Element("RznSocRecep", receiver.Name));
        if (receiver.Address is not null)
        {
            result.Add(
                Element("DirRecep", receiver.Address.AddressLine),
                Element("CiudadRecep", receiver.Address.City));
            if (!string.IsNullOrWhiteSpace(receiver.Address.Region))
                result.Add(Element("DeptoRecep", receiver.Address.Region!));
            if (!string.IsNullOrWhiteSpace(receiver.Address.PostalCode))
                result.Add(Element("CP", receiver.Address.PostalCode!));
        }
        return result;
    }

    private static XElement BuildTotals(FiscalContentSnapshot snapshot)
    {
        var totals = snapshot.FiscalEvidence.Totals;
        var result = new XElement(CfeNamespace + "Totales",
            Element("TpoMoneda", snapshot.FiscalEvidence.CurrencyCode));
        if (totals.ExportAmount != 0m) result.Add(Element("MntExpoyAsim", totals.ExportAmount));
        if (totals.MinimumTaxableAmount != 0m) result.Add(Element("MntNetoIvaTasaMin", totals.MinimumTaxableAmount));
        if (totals.BasicTaxableAmount != 0m) result.Add(Element("MntNetoIVATasaBasica", totals.BasicTaxableAmount));

        var minimumRate = Rate(snapshot, VatRateKind.Minimum);
        var basicRate = Rate(snapshot, VatRateKind.Basic);
        if (minimumRate.HasValue) result.Add(Element("IVATasaMin", minimumRate.Value));
        if (basicRate.HasValue) result.Add(Element("IVATasaBasica", basicRate.Value));
        if (totals.MinimumVatAmount != 0m) result.Add(Element("MntIVATasaMin", totals.MinimumVatAmount));
        if (totals.BasicVatAmount != 0m) result.Add(Element("MntIVATasaBasica", totals.BasicVatAmount));
        result.Add(
            Element("MntTotal", totals.TotalAmount),
            Element("CantLinDet", snapshot.Lines.Count),
            Element("MntPagar", totals.TotalAmount));
        return result;
    }

    private static XElement BuildDetail(FiscalContentSnapshot snapshot) =>
        new(CfeNamespace + "Detalle",
            snapshot.Lines.OrderBy(line => line.Sequence).Select(line =>
            {
                var item = new XElement(CfeNamespace + "Item",
                    Element("NroLinDet", line.Sequence),
                    new XElement(CfeNamespace + "CodItem",
                        Element("TpoCod", "INT1"),
                        Element("Cod", line.ItemCode)),
                    Element("IndFact", Indicator(line.Fiscal.VatRateKind)),
                    Element("NomItem", line.ItemName),
                    Element("Cantidad", line.Quantity),
                    Element("UniMed", line.UnitOfMeasure!),
                    Element("PrecioUnitario", line.UnitPrice));
                if (line.DiscountAmount != 0m) item.Add(Element("DescuentoMonto", line.DiscountAmount));
                if (line.SurchargeAmount != 0m) item.Add(Element("RecargoMnt", line.SurchargeAmount));
                item.Add(Element("MontoItem", line.Fiscal.ItemAmount));
                return item;
            }));

    private static XElement BuildReferences(IEnumerable<FiscalDocumentReferenceEvidence> references) =>
        new(CfeNamespace + "Referencia",
            references.OrderBy(reference => reference.Sequence).Select(reference =>
            {
                var item = new XElement(CfeNamespace + "Referencia",
                    Element("NroLinRef", reference.Sequence),
                    Element("TpoDocRef", (int)reference.ReferencedCfeType),
                    Element("Serie", reference.Series),
                    Element("NroCFERef", reference.Number));
                if (!string.IsNullOrWhiteSpace(reference.Reason))
                    item.Add(Element("RazonRef", reference.Reason!));
                if (reference.FiscalDate.HasValue)
                    item.Add(Element("FechaCFEref", Date(reference.FiscalDate.Value)));
                if (reference.Amount.HasValue)
                    item.Add(Element("MntCFEref", reference.Amount.Value));
                if (!string.IsNullOrWhiteSpace(reference.CurrencyCode))
                    item.Add(Element("TpoMonedaRef", reference.CurrencyCode!));
                if (reference.ExchangeRate.HasValue)
                    item.Add(Element("TpoCambioRef", reference.ExchangeRate.Value));
                return item;
            }));

    private static int Indicator(VatRateKind kind) => kind switch
    {
        VatRateKind.Exempt => 1,
        VatRateKind.Minimum => 2,
        VatRateKind.Basic => 3,
        VatRateKind.Export => 10,
        _ => throw Rule("fiscal.cfe_builder.indicator_not_supported", "Frozen tax evidence cannot be mapped safely to a Release-1 DGI billing indicator.")
    };

    private static decimal? Rate(FiscalContentSnapshot snapshot, VatRateKind kind)
    {
        var rates = snapshot.FiscalEvidence.Lines
            .Where(line => line.VatRateKind == kind)
            .Select(line => line.AppliedRatePercent)
            .Distinct()
            .ToArray();
        if (rates.Length > 1)
            throw Rule("fiscal.cfe_builder.rate_ambiguous", "Frozen tax evidence contains multiple rates for one DGI tax bucket.");
        return rates.Length == 1 ? rates[0] : null;
    }

    private static void EnsureConsistent(FiscalDocument document, FiscalContentSnapshot snapshot)
    {
        if (document.Status != FiscalDocumentStatus.IdentityCreated
            || document.OrganizationId != snapshot.OrganizationId
            || document.SaleId != snapshot.SaleId
            || document.CfeType != snapshot.CfeFamily
            || document.FormatVersion != snapshot.FormatVersion
            || document.ConfirmationFingerprint != snapshot.ConfirmationFingerprint
            || document.SettlementFingerprint != snapshot.SettlementFingerprint
            || document.CurrencyCode != snapshot.FiscalEvidence.CurrencyCode
            || document.NetAmount != snapshot.FiscalEvidence.Totals.NetAmount
            || document.VatAmount != snapshot.FiscalEvidence.Totals.VatAmount
            || document.TotalAmount != snapshot.FiscalEvidence.Totals.TotalAmount)
        {
            throw Rule("fiscal.cfe_builder.identity_snapshot_mismatch", "Fiscal document identity and immutable content snapshot do not match.");
        }

        if (snapshot.References?.Any(reference =>
                reference.ReferencedCfeType == document.CfeType
                && string.Equals(reference.Series, document.Series, StringComparison.Ordinal)
                && reference.Number == document.Number) == true)
        {
            throw Rule("fiscal.cfe_builder.reference_self_forbidden", "A CFE cannot reference its own fiscal identity.");
        }
    }

    private static int ParseBranch(string value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var branch)
            ? branch
            : throw Rule("fiscal.cfe_builder.branch_invalid", "Frozen DGI branch code is not numeric.");

    private static XElement Element(string name, object value) =>
        new(CfeNamespace + name, value is decimal number ? Decimal(number) : value);

    private static string Decimal(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);
    private static string Date(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
