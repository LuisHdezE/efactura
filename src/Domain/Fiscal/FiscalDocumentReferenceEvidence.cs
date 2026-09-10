using System.Globalization;
using System.Text;

namespace EFactura.Domain.Fiscal;

/// <summary>
/// Immutable evidence identifying one previously issued CFE referenced by a domestic correction note.
/// Global-reference semantics are intentionally outside this foundation: this value represents only
/// an identifiable CFE and never invents an IndGlobal fallback.
/// </summary>
public sealed record FiscalDocumentReferenceEvidence(
    int Sequence,
    string OrganizationId,
    CfeFamily ReferencedCfeType,
    string Series,
    long Number,
    DateOnly? FiscalDate,
    decimal? Amount,
    string? CurrencyCode,
    decimal? ExchangeRate,
    string? Reason,
    string EvidenceFingerprint)
{
    public static FiscalDocumentReferenceEvidence Capture(
        int sequence,
        string organizationId,
        CfeFamily referencedCfeType,
        string series,
        long number,
        DateOnly? fiscalDate = null,
        decimal? amount = null,
        string? currencyCode = null,
        decimal? exchangeRate = null,
        string? reason = null)
    {
        var provisional = new FiscalDocumentReferenceEvidence(
            sequence,
            organizationId?.Trim() ?? string.Empty,
            referencedCfeType,
            series?.Trim().ToUpperInvariant() ?? string.Empty,
            number,
            fiscalDate,
            amount,
            NormalizeCurrency(currencyCode),
            exchangeRate,
            NormalizeOptional(reason),
            new string('0', 64));

        var result = provisional with { EvidenceFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    public void EnsureIntegrity()
    {
        if (Sequence is < 1 or > 40)
            throw FiscalConfirmationEvidence.Rule(
                "fiscal.snapshot.reference_sequence_invalid",
                "Fiscal reference sequence must be between 1 and 40.");

        FiscalConfirmationEvidence.Required(
            OrganizationId,
            200,
            "fiscal.snapshot.reference_organization_required");

        if (!IsDomesticReferenceType(ReferencedCfeType))
            throw FiscalConfirmationEvidence.Rule(
                "fiscal.snapshot.reference_type_not_supported",
                "This domestic correction-note foundation only accepts identifiable domestic CFE reference types 101/102/103/111/112/113.");

        var series = FiscalConfirmationEvidence.Required(
            Series,
            2,
            "fiscal.snapshot.reference_series_required");
        if (!string.Equals(series, series.ToUpperInvariant(), StringComparison.Ordinal)
            || series.Any(ch => ch is < 'A' or > 'Z'))
        {
            throw FiscalConfirmationEvidence.Rule(
                "fiscal.snapshot.reference_series_invalid",
                "Referenced CFE series must contain one or two uppercase letters.");
        }

        if (Number is < 1 or > 9_999_999)
            throw FiscalConfirmationEvidence.Rule(
                "fiscal.snapshot.reference_number_invalid",
                "Referenced CFE number is outside the supported DGI structural range.");

        if (FiscalDate.HasValue
            && (FiscalDate.Value < new DateOnly(2011, 10, 1)
                || FiscalDate.Value > new DateOnly(2050, 12, 31)))
        {
            throw FiscalConfirmationEvidence.Rule(
                "fiscal.snapshot.reference_date_invalid",
                "Referenced CFE fiscal date is outside the supported DGI structural range.");
        }

        if (Amount is < 0m)
            throw FiscalConfirmationEvidence.Rule(
                "fiscal.snapshot.reference_amount_invalid",
                "Referenced CFE amount cannot be negative.");

        if (CurrencyCode is not null)
        {
            var currency = FiscalConfirmationEvidence.Required(
                CurrencyCode,
                3,
                "fiscal.snapshot.reference_currency_invalid");
            if (currency.Length != 3
                || !string.Equals(currency, currency.ToUpperInvariant(), StringComparison.Ordinal)
                || currency.Any(ch => ch is < 'A' or > 'Z'))
            {
                throw FiscalConfirmationEvidence.Rule(
                    "fiscal.snapshot.reference_currency_invalid",
                    "Referenced CFE currency must use ISO alpha-3 form.");
            }

            if (!string.Equals(currency, "UYU", StringComparison.Ordinal) && ExchangeRate is null)
                throw FiscalConfirmationEvidence.Rule(
                    "fiscal.snapshot.reference_exchange_rate_required",
                    "Referenced CFE exchange rate is required when the referenced currency is not UYU.");
        }
        else if (ExchangeRate is not null)
        {
            throw FiscalConfirmationEvidence.Rule(
                "fiscal.snapshot.reference_currency_required_for_exchange_rate",
                "Referenced CFE currency is required when an exchange rate is preserved.");
        }

        if (ExchangeRate is <= 0m)
            throw FiscalConfirmationEvidence.Rule(
                "fiscal.snapshot.reference_exchange_rate_invalid",
                "Referenced CFE exchange rate must be positive.");

        if (Reason is not null)
            FiscalConfirmationEvidence.Required(Reason, 90, "fiscal.snapshot.reference_reason_invalid");

        FiscalConfirmationEvidence.EnsureFingerprint(
            EvidenceFingerprint,
            "fiscal.snapshot.reference_fingerprint_invalid");
        if (!string.Equals(EvidenceFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw FiscalConfirmationEvidence.Rule(
                "fiscal.snapshot.reference_fingerprint_mismatch",
                "Fiscal reference fingerprint does not match its immutable content.");
    }

    public string ComputeFingerprint()
    {
        var material = new StringBuilder()
            .Append(Sequence).Append('|')
            .Append(OrganizationId).Append('|')
            .Append((int)ReferencedCfeType).Append('|')
            .Append(Series).Append('|')
            .Append(Number).Append('|')
            .Append(FiscalDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-").Append('|')
            .Append(Amount.HasValue ? FiscalConfirmationEvidence.Decimal(Amount.Value) : "-").Append('|')
            .Append(CurrencyCode ?? "-").Append('|')
            .Append(ExchangeRate.HasValue ? FiscalConfirmationEvidence.Decimal(ExchangeRate.Value) : "-").Append('|')
            .Append(Reason ?? "-");

        return FiscalConfirmationEvidence.Hash(material.ToString());
    }

    public static bool IsDomesticReferenceType(CfeFamily type) => type is
        CfeFamily.ETicket or
        CfeFamily.ETicketCreditNote or
        CfeFamily.ETicketDebitNote or
        CfeFamily.EFactura or
        CfeFamily.EFacturaCreditNote or
        CfeFamily.EFacturaDebitNote;

    private static string? NormalizeCurrency(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
