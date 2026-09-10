using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

public sealed record FiscalSigningPayload(
    Guid FiscalDocumentId,
    CfeFamily Family,
    string FormatVersion,
    string FiscalContentFingerprint,
    string UnsignedContentHash,
    DateTimeOffset SigningTimestamp,
    string Xml,
    string ContentHash);

/// <summary>
/// Builds the deterministic XML payload that crosses the XMLDSig provider boundary.
/// It inserts the already durable DGI TmstFirma before Encabezado, but deliberately does not
/// select certificates, access private keys, create ds:Signature, validate the complete root XSD,
/// persist signed bytes or contact DGI/providers.
/// </summary>
public interface IFiscalSigningPayloadBuilder
{
    FiscalSigningPayload Build(
        Guid fiscalDocumentId,
        UnsignedCfeArtifact unsigned,
        FiscalSigningEvidence signingEvidence);
}

public sealed class DeterministicFiscalSigningPayloadBuilder : IFiscalSigningPayloadBuilder
{
    private static readonly XNamespace CfeNamespace = "http://cfe.dgi.gub.uy";
    private static readonly XNamespace XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

    public FiscalSigningPayload Build(
        Guid fiscalDocumentId,
        UnsignedCfeArtifact unsigned,
        FiscalSigningEvidence signingEvidence)
    {
        ArgumentNullException.ThrowIfNull(unsigned);
        ArgumentNullException.ThrowIfNull(signingEvidence);

        if (fiscalDocumentId == Guid.Empty)
            throw Rule("fiscal.signing_payload.document_id_required", "Fiscal document id is required.");
        if (signingEvidence.FiscalDocumentId != fiscalDocumentId)
            throw Rule(
                "fiscal.signing_payload.document_mismatch",
                "Durable signing evidence belongs to a different fiscal document.");
        if (!string.Equals(
                signingEvidence.FiscalContentFingerprint,
                unsigned.ContentFingerprint,
                StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.signing_payload.snapshot_mismatch",
                "Durable signing evidence does not match the unsigned CFE fiscal-content fingerprint.");
        }

        var unsignedHash = Sha256(unsigned.Xml);
        if (!string.Equals(signingEvidence.UnsignedContentHash, unsignedHash, StringComparison.Ordinal))
        {
            throw Rule(
                "fiscal.signing_payload.unsigned_hash_mismatch",
                "Durable signing evidence does not match the deterministic unsigned CFE content.");
        }

        var document = Parse(unsigned.Xml);
        var root = document.Root
            ?? throw Rule("fiscal.signing_payload.root_required", "Unsigned CFE XML requires a root element.");
        if (root.Name != CfeNamespace + "CFE")
            throw Rule("fiscal.signing_payload.root_invalid", "Unsigned CFE XML must use the official DGI CFE root namespace.");
        if (document.Descendants(XmlDsigNamespace + "Signature").Any())
            throw Rule(
                "fiscal.signing_payload.signature_already_present",
                "Signing payload preparation must not accept an existing ds:Signature.");

        var expectedFamilyName = unsigned.Family switch
        {
            CfeFamily.ETicket or
            CfeFamily.ETicketCreditNote or
            CfeFamily.ETicketDebitNote => "eTck",
            CfeFamily.EFactura or
            CfeFamily.EFacturaCreditNote or
            CfeFamily.EFacturaDebitNote => "eFact",
            CfeFamily.EFacturaExportacion => throw Rule(
                "fiscal.signing_payload.export_not_supported",
                "Release-1 signing payload does not enable export CFE signing."),
            _ => throw Rule(
                "fiscal.signing_payload.family_not_supported",
                "CFE family is not supported by the signing payload builder.")
        };

        var familyElements = root.Elements().ToArray();
        if (familyElements.Length != 1 || familyElements[0].Name != CfeNamespace + expectedFamilyName)
        {
            throw Rule(
                "fiscal.signing_payload.family_invalid",
                "Unsigned CFE XML does not contain exactly the expected DGI family element.");
        }

        var family = familyElements[0];
        if (family.Elements(CfeNamespace + "TmstFirma").Any())
            throw Rule(
                "fiscal.signing_payload.tmstfirma_already_present",
                "Unsigned CFE content must not already contain TmstFirma.");

        family.AddFirst(new XElement(
            CfeNamespace + "TmstFirma",
            signingEvidence.SigningTimestamp.ToString(
                "yyyy-MM-dd'T'HH:mm:sszzz",
                CultureInfo.InvariantCulture)));

        var xml = document.ToString(SaveOptions.DisableFormatting);
        return new FiscalSigningPayload(
            fiscalDocumentId,
            unsigned.Family,
            unsigned.FormatVersion,
            unsigned.ContentFingerprint,
            unsignedHash,
            signingEvidence.SigningTimestamp,
            xml,
            Sha256(xml));
    }

    private static XDocument Parse(string xml)
    {
        try
        {
            return XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            throw Rule(
                "fiscal.signing_payload.xml_invalid",
                $"Unsigned CFE XML is not well formed: {ex.Message}");
        }
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
