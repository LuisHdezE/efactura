using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EFactura.Domain.Common;
using EFactura.Domain.Taxation;

namespace EFactura.Domain.Fiscal;

public enum FiscalContentLineKind
{
    Goods = 1,
    Service = 2
}

public enum FiscalReceiverAddressKind
{
    Fiscal = 1,
    Delivery = 2,
    Other = 3
}

public enum FiscalSettlementKind
{
    NoCharge = 0,
    ImmediatePayment = 1,
    CreditReceivable = 2,
    Mixed = 3
}

/// <summary>
/// DGI CFE payment-form code. Only mappings already established by authoritative settlement facts
/// are represented here. Ambiguous settlement shapes keep PaymentForm null and later XML BUILD must
/// fail closed instead of inventing a fiscal payment form.
/// </summary>
public enum FiscalPaymentForm
{
    Cash = 1,
    Credit = 2
}

public sealed record FiscalSettlementEvidence(
    FiscalSettlementKind Kind,
    FiscalPaymentForm? PaymentForm,
    DateOnly? DueDate)
{
    public void EnsureValid()
    {
        if (!Enum.IsDefined(Kind))
            throw FiscalConfirmationEvidence.Rule(
                "fiscal.snapshot.settlement_kind_invalid",
                "Fiscal settlement evidence contains an invalid settlement kind.");
        if (PaymentForm.HasValue && !Enum.IsDefined(PaymentForm.Value))
            throw FiscalConfirmationEvidence.Rule(
                "fiscal.snapshot.payment_form_invalid",
                "Fiscal settlement evidence contains an invalid DGI payment form.");

        switch (Kind)
        {
            case FiscalSettlementKind.ImmediatePayment:
                if (PaymentForm != FiscalPaymentForm.Cash || DueDate.HasValue)
                {
                    throw FiscalConfirmationEvidence.Rule(
                        "fiscal.snapshot.payment_form_mismatch",
                        "Immediate-payment settlement must preserve cash payment form without a credit due date.");
                }
                break;

            case FiscalSettlementKind.CreditReceivable:
                if (PaymentForm != FiscalPaymentForm.Credit)
                {
                    throw FiscalConfirmationEvidence.Rule(
                        "fiscal.snapshot.payment_form_mismatch",
                        "Credit settlement must preserve the accepted credit payment-form mapping.");
                }
                if (!DueDate.HasValue)
                {
                    throw FiscalConfirmationEvidence.Rule(
                        "fiscal.snapshot.credit_due_date_required",
                        "Credit settlement evidence requires the authoritative receivable due date.");
                }
                break;

            case FiscalSettlementKind.Mixed:
                if (PaymentForm.HasValue)
                {
                    throw FiscalConfirmationEvidence.Rule(
                        "fiscal.snapshot.payment_form_ambiguous",
                        "Mixed settlement cannot invent a single DGI payment form before an explicit mapping is accepted.");
                }
                if (!DueDate.HasValue)
                {
                    throw FiscalConfirmationEvidence.Rule(
                        "fiscal.snapshot.mixed_due_date_required",
                        "Mixed settlement evidence requires the authoritative residual receivable due date.");
                }
                break;

            case FiscalSettlementKind.NoCharge:
                if (PaymentForm.HasValue || DueDate.HasValue)
                {
                    throw FiscalConfirmationEvidence.Rule(
                        "fiscal.snapshot.no_charge_payment_form_forbidden",
                        "No-charge settlement cannot invent payment-form or due-date evidence.");
                }
                break;
        }
    }
}

public sealed record FiscalRuleEvidenceSnapshot(
    string RuleId,
    string SourceName,
    string SourceReference,
    string SourceVersion,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Clause)
{
    public static FiscalRuleEvidenceSnapshot From(RegulatoryRuleEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        return new(
            evidence.RuleId,
            evidence.SourceName,
            evidence.SourceReference,
            evidence.SourceVersion,
            evidence.EffectiveFrom,
            evidence.EffectiveTo,
            evidence.Clause);
    }

    public void EnsureValid()
    {
        Required(RuleId, 120, "fiscal.snapshot.rule_id_required");
        Required(SourceName, 250, "fiscal.snapshot.rule_source_name_required");
        Required(SourceReference, 1000, "fiscal.snapshot.rule_source_reference_required");
        Required(SourceVersion, 200, "fiscal.snapshot.rule_source_version_required");
        if (EffectiveTo.HasValue && EffectiveTo.Value < EffectiveFrom)
            throw Rule("fiscal.snapshot.rule_effective_range_invalid", "Fiscal rule evidence has an invalid effective range.");
    }

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value)) throw Rule(code, "Required fiscal rule evidence is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max) throw Rule(code, $"Fiscal rule evidence cannot exceed {max} characters.");
        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}

public sealed record FiscalCalculationLineEvidence(
    Guid LineId,
    decimal ItemAmount,
    VatLiabilityKind VatLiability,
    VatRateKind VatRateKind,
    decimal AppliedRatePercent,
    string RateRulePackVersion,
    IReadOnlyCollection<FiscalRuleEvidenceSnapshot> RuleEvidence);

public sealed record FiscalCalculationTotalsEvidence(
    decimal NetAmount,
    decimal MinimumTaxableAmount,
    decimal BasicTaxableAmount,
    decimal ExportAmount,
    decimal MinimumVatAmount,
    decimal BasicVatAmount,
    decimal VatAmount,
    decimal TotalAmount);

/// <summary>
/// Durable fiscal-only evidence captured at Sale confirmation. Inventory evidence deliberately does
/// not live here. This value exists so later fiscal identity creation never has to recompute the
/// tax/arithmetic portion of a confirmation after stock has legitimately changed.
/// </summary>
public sealed record FiscalConfirmationEvidence(
    CfeFamily CfeFamily,
    ReceiverIdentificationRequirement? ReceiverIdentification,
    string FormatVersion,
    string ConfirmationFingerprint,
    string CurrencyCode,
    string ArithmeticRulePackVersion,
    IReadOnlyCollection<FiscalCalculationLineEvidence> Lines,
    FiscalCalculationTotalsEvidence Totals,
    IReadOnlyCollection<FiscalRuleEvidenceSnapshot> SelectionRuleEvidence,
    IReadOnlyCollection<FiscalRuleEvidenceSnapshot> ArithmeticRuleEvidence,
    string EvidenceFingerprint,
    FiscalSettlementEvidence? Settlement = null)
{
    public static FiscalConfirmationEvidence Capture(
        CfeFamily cfeFamily,
        ReceiverIdentificationRequirement? receiverIdentification,
        string formatVersion,
        string confirmationFingerprint,
        CfeArithmeticResult calculation,
        IReadOnlyCollection<RegulatoryRuleEvidence> selectionRuleEvidence,
        FiscalSettlementEvidence? settlement = null)
    {
        ArgumentNullException.ThrowIfNull(calculation);
        ArgumentNullException.ThrowIfNull(selectionRuleEvidence);
        settlement?.EnsureValid();

        var lines = calculation.Lines
            .Select(line => new FiscalCalculationLineEvidence(
                line.LineId,
                line.ItemAmount,
                line.VatLiability,
                line.VatRateKind,
                line.AppliedRatePercent,
                line.RateRulePackVersion,
                line.RuleEvidence.Select(FiscalRuleEvidenceSnapshot.From).ToArray()))
            .ToArray();
        var totals = new FiscalCalculationTotalsEvidence(
            calculation.Totals.NetAmount,
            calculation.Totals.MinimumTaxableAmount,
            calculation.Totals.BasicTaxableAmount,
            calculation.Totals.ExportAmount,
            calculation.Totals.MinimumVatAmount,
            calculation.Totals.BasicVatAmount,
            calculation.Totals.VatAmount,
            calculation.Totals.TotalAmount);
        var selectionEvidence = selectionRuleEvidence.Select(FiscalRuleEvidenceSnapshot.From).ToArray();
        var arithmeticEvidence = calculation.RuleEvidence.Select(FiscalRuleEvidenceSnapshot.From).ToArray();

        var provisional = new FiscalConfirmationEvidence(
            cfeFamily,
            receiverIdentification,
            formatVersion.Trim(),
            confirmationFingerprint.Trim().ToLowerInvariant(),
            calculation.CurrencyCode.Trim().ToUpperInvariant(),
            calculation.ArithmeticRulePackVersion.Trim(),
            lines,
            totals,
            selectionEvidence,
            arithmeticEvidence,
            new string('0', 64),
            settlement);
        var result = provisional with { EvidenceFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    public void EnsureIntegrity()
    {
        if (!Enum.IsDefined(CfeFamily))
            throw Rule("fiscal.snapshot.cfe_family_invalid", "Fiscal confirmation evidence requires a supported CFE family.");
        if (ReceiverIdentification.HasValue && !Enum.IsDefined(ReceiverIdentification.Value))
            throw Rule("fiscal.snapshot.receiver_requirement_invalid", "Fiscal confirmation receiver-identification requirement is invalid.");
        Required(FormatVersion, 40, "fiscal.snapshot.format_version_required");
        EnsureFingerprint(ConfirmationFingerprint, "fiscal.snapshot.confirmation_fingerprint_invalid");
        EnsureCurrency(CurrencyCode);
        Required(ArithmeticRulePackVersion, 200, "fiscal.snapshot.arithmetic_pack_required");
        if (Lines is null || Lines.Count == 0)
            throw Rule("fiscal.snapshot.calculation_lines_required", "Fiscal confirmation evidence requires calculated lines.");
        if (Lines.Any(line => line is null || line.LineId == Guid.Empty))
            throw Rule("fiscal.snapshot.calculation_line_invalid", "Fiscal confirmation evidence contains an invalid line.");
        if (Lines.Select(line => line.LineId).Distinct().Count() != Lines.Count)
            throw Rule("fiscal.snapshot.calculation_line_duplicate", "Fiscal confirmation evidence line identifiers must be unique.");
        if (Totals is null)
            throw Rule("fiscal.snapshot.totals_required", "Fiscal confirmation totals are required.");
        if (SelectionRuleEvidence is null || SelectionRuleEvidence.Count == 0)
            throw Rule("fiscal.snapshot.selection_evidence_required", "CFE selection rule evidence is required.");
        if (ArithmeticRuleEvidence is null || ArithmeticRuleEvidence.Count == 0)
            throw Rule("fiscal.snapshot.arithmetic_evidence_required", "CFE arithmetic rule evidence is required.");

        Settlement?.EnsureValid();

        foreach (var evidence in SelectionRuleEvidence.Concat(ArithmeticRuleEvidence).Concat(Lines.SelectMany(line => line.RuleEvidence)))
            evidence.EnsureValid();
        foreach (var line in Lines)
        {
            if (line.ItemAmount < 0m || line.AppliedRatePercent < 0m)
                throw Rule("fiscal.snapshot.calculation_amount_invalid", "Fiscal calculation line amounts and rates cannot be negative.");
            if (!Enum.IsDefined(line.VatLiability) || !Enum.IsDefined(line.VatRateKind))
                throw Rule("fiscal.snapshot.calculation_tax_kind_invalid", "Fiscal calculation line tax classification is invalid.");
            Required(line.RateRulePackVersion, 200, "fiscal.snapshot.rate_pack_required");
            if (line.RuleEvidence is null || line.RuleEvidence.Count == 0)
                throw Rule("fiscal.snapshot.line_rule_evidence_required", "Fiscal calculation line rule evidence is required.");
        }

        var totals = Totals;
        if (new[] { totals.NetAmount, totals.MinimumTaxableAmount, totals.BasicTaxableAmount, totals.ExportAmount,
                    totals.MinimumVatAmount, totals.BasicVatAmount, totals.VatAmount, totals.TotalAmount }.Any(value => value < 0m))
            throw Rule("fiscal.snapshot.totals_invalid", "Fiscal confirmation totals cannot be negative.");
        if (totals.NetAmount != Lines.Sum(line => line.ItemAmount))
            throw Rule("fiscal.snapshot.net_total_mismatch", "Fiscal confirmation net total does not equal the frozen line total.");
        if (totals.VatAmount != totals.MinimumVatAmount + totals.BasicVatAmount)
            throw Rule("fiscal.snapshot.vat_total_mismatch", "Fiscal confirmation VAT total does not equal its frozen VAT buckets.");
        if (totals.TotalAmount != totals.NetAmount + totals.VatAmount)
            throw Rule("fiscal.snapshot.total_amount_mismatch", "Fiscal confirmation total does not equal net plus VAT.");

        EnsureFingerprint(EvidenceFingerprint, "fiscal.snapshot.evidence_fingerprint_invalid");
        if (!string.Equals(EvidenceFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw Rule("fiscal.snapshot.evidence_fingerprint_mismatch", "Fiscal confirmation evidence fingerprint does not match its immutable content.");
    }

    public string ComputeFingerprint()
    {
        var material = new StringBuilder()
            .Append((int)CfeFamily).Append('|')
            .Append((int?)ReceiverIdentification ?? -1).Append('|')
            .Append(FormatVersion?.Trim()).Append('|')
            .Append(ConfirmationFingerprint?.Trim().ToLowerInvariant()).Append('|')
            .Append(CurrencyCode?.Trim().ToUpperInvariant()).Append('|')
            .Append(ArithmeticRulePackVersion?.Trim()).Append('|')
            .Append(Decimal(Totals.NetAmount)).Append('|')
            .Append(Decimal(Totals.MinimumTaxableAmount)).Append('|')
            .Append(Decimal(Totals.BasicTaxableAmount)).Append('|')
            .Append(Decimal(Totals.ExportAmount)).Append('|')
            .Append(Decimal(Totals.MinimumVatAmount)).Append('|')
            .Append(Decimal(Totals.BasicVatAmount)).Append('|')
            .Append(Decimal(Totals.VatAmount)).Append('|')
            .Append(Decimal(Totals.TotalAmount));

        AppendRules(material, SelectionRuleEvidence, "SEL");
        AppendRules(material, ArithmeticRuleEvidence, "ARITH");
        foreach (var line in Lines.OrderBy(line => line.LineId))
        {
            material.Append('|').Append("LINE:")
                .Append(line.LineId.ToString("N")).Append(':')
                .Append(Decimal(line.ItemAmount)).Append(':')
                .Append((int)line.VatLiability).Append(':')
                .Append((int)line.VatRateKind).Append(':')
                .Append(Decimal(line.AppliedRatePercent)).Append(':')
                .Append(line.RateRulePackVersion);
            AppendRules(material, line.RuleEvidence, "LR");
        }

        if (Settlement is not null)
        {
            material.Append('|').Append("SETTLEMENT:")
                .Append((int)Settlement.Kind).Append(':')
                .Append((int?)Settlement.PaymentForm ?? -1).Append(':')
                .Append(Settlement.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-");
        }

        return Hash(material.ToString());
    }

    internal static void AppendRules(StringBuilder material, IEnumerable<FiscalRuleEvidenceSnapshot> rules, string prefix)
    {
        foreach (var rule in rules.OrderBy(rule => rule.RuleId, StringComparer.Ordinal))
        {
            material.Append('|').Append(prefix).Append(':')
                .Append(rule.RuleId).Append(':')
                .Append(rule.SourceName).Append(':')
                .Append(rule.SourceReference).Append(':')
                .Append(rule.SourceVersion).Append(':')
                .Append(rule.EffectiveFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(':')
                .Append(rule.EffectiveTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-").Append(':')
                .Append(rule.Clause ?? "-");
        }
    }

    internal static string Hash(string material) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();

    internal static string Decimal(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);

    internal static void EnsureFingerprint(string value, string code)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || value.Any(ch => !Uri.IsHexDigit(ch)))
            throw Rule(code, "Fiscal snapshot fingerprint must be a SHA-256 hexadecimal value.");
    }

    internal static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value)) throw Rule(code, "Required fiscal snapshot value is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max) throw Rule(code, $"Fiscal snapshot value cannot exceed {max} characters.");
        return normalized;
    }

    internal static string Country(string value, string code)
    {
        var normalized = Required(value, 2, code).ToUpperInvariant();
        if (normalized.Length != 2 || normalized.Any(ch => ch < 'A' || ch > 'Z'))
            throw Rule(code, "Fiscal snapshot country must be an ISO alpha-2 code.");
        return normalized;
    }

    private static void EnsureCurrency(string value)
    {
        var normalized = Required(value, 3, "fiscal.snapshot.currency_required").ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch < 'A' || ch > 'Z'))
            throw Rule("fiscal.snapshot.currency_invalid", "Fiscal snapshot currency must use ISO alpha-3 form.");
    }

    internal static DomainRuleException Rule(string code, string message) => new(code, message);
}

public sealed record FiscalIssuerContentSnapshot(
    string Ruc,
    string LegalName,
    string? CommercialName,
    long CompanyVersion,
    string LocationId,
    string DgiBranchCode,
    string FiscalAddress,
    string City,
    string Department,
    long LocationVersion);

public sealed record FiscalReceiverIdentitySnapshot(
    string TypeCode,
    string Number,
    string IssuingCountry);

public sealed record FiscalReceiverAddressSnapshot(
    Guid AddressId,
    FiscalReceiverAddressKind Kind,
    string AddressLine,
    string City,
    string? Region,
    string CountryCode,
    string? PostalCode);

public sealed record FiscalReceiverContentSnapshot(
    Guid PartyId,
    string Name,
    string ResidenceCountry,
    string TaxResidenceCountry,
    long PartyVersion,
    FiscalReceiverIdentitySnapshot? Identity,
    FiscalReceiverAddressSnapshot? Address);

public sealed record FiscalContentLineSnapshot(
    int Sequence,
    Guid SaleLineId,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    FiscalContentLineKind Kind,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal SurchargeAmount,
    FiscalCalculationLineEvidence Fiscal,
    string? UnitOfMeasure = null);

/// <summary>
/// Immutable CFE content source associated with a FiscalDocument identity. Later XML construction
/// must consume this snapshot and must not re-read mutable Organization, Party or catalog masters.
/// </summary>
public sealed record FiscalContentSnapshot(
    string OrganizationId,
    Guid SaleId,
    CfeFamily CfeFamily,
    ReceiverIdentificationRequirement? ReceiverIdentification,
    string FormatVersion,
    DateOnly EffectiveOn,
    string ConfirmationFingerprint,
    string SettlementFingerprint,
    FiscalIssuerContentSnapshot Issuer,
    FiscalReceiverContentSnapshot? Receiver,
    IReadOnlyCollection<FiscalContentLineSnapshot> Lines,
    FiscalConfirmationEvidence FiscalEvidence,
    string ContentFingerprint)
{
    public static FiscalContentSnapshot Create(
        string organizationId,
        Guid saleId,
        CfeFamily cfeFamily,
        ReceiverIdentificationRequirement? receiverIdentification,
        string formatVersion,
        DateOnly effectiveOn,
        string confirmationFingerprint,
        string settlementFingerprint,
        FiscalIssuerContentSnapshot issuer,
        FiscalReceiverContentSnapshot? receiver,
        IReadOnlyCollection<FiscalContentLineSnapshot> lines,
        FiscalConfirmationEvidence fiscalEvidence)
    {
        var provisional = new FiscalContentSnapshot(
            organizationId.Trim(),
            saleId,
            cfeFamily,
            receiverIdentification,
            formatVersion.Trim(),
            effectiveOn,
            confirmationFingerprint.Trim().ToLowerInvariant(),
            settlementFingerprint.Trim().ToLowerInvariant(),
            issuer,
            receiver,
            lines,
            fiscalEvidence,
            new string('0', 64));
        var result = provisional with { ContentFingerprint = provisional.ComputeFingerprint() };
        result.EnsureIntegrity();
        return result;
    }

    public void EnsureIntegrity()
    {
        FiscalConfirmationEvidence.Required(OrganizationId, 200, "fiscal.snapshot.organization_required");
        if (SaleId == Guid.Empty) throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.sale_id_required", "Fiscal content snapshot requires a sale id.");
        if (!Enum.IsDefined(CfeFamily)) throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.cfe_family_invalid", "Fiscal content snapshot CFE family is invalid.");
        FiscalConfirmationEvidence.Required(FormatVersion, 40, "fiscal.snapshot.format_version_required");
        FiscalConfirmationEvidence.EnsureFingerprint(ConfirmationFingerprint, "fiscal.snapshot.confirmation_fingerprint_invalid");
        FiscalConfirmationEvidence.EnsureFingerprint(SettlementFingerprint, "fiscal.snapshot.settlement_fingerprint_invalid");
        if (Issuer is null) throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.issuer_required", "Fiscal issuer snapshot is required.");
        if (Issuer.CompanyVersion <= 0 || Issuer.LocationVersion <= 0)
            throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.issuer_version_invalid", "Fiscal issuer master versions must be positive.");
        var ruc = FiscalConfirmationEvidence.Required(Issuer.Ruc, 12, "fiscal.snapshot.issuer_ruc_required");
        if (ruc.Length != 12 || ruc.Any(ch => ch < '0' || ch > '9'))
            throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.issuer_ruc_invalid", "Fiscal issuer RUC must preserve the accepted 12-digit structural form.");
        FiscalConfirmationEvidence.Required(Issuer.LegalName, 150, "fiscal.snapshot.issuer_legal_name_required");
        FiscalConfirmationEvidence.Required(Issuer.LocationId, 200, "fiscal.snapshot.location_required");
        var branch = FiscalConfirmationEvidence.Required(Issuer.DgiBranchCode, 4, "fiscal.snapshot.branch_required");
        if (branch.Length != 4 || branch.Any(ch => ch < '0' || ch > '9'))
            throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.branch_invalid", "Fiscal issuer branch code must preserve four decimal digits.");
        FiscalConfirmationEvidence.Required(Issuer.FiscalAddress, 70, "fiscal.snapshot.issuer_address_required");
        FiscalConfirmationEvidence.Required(Issuer.City, 30, "fiscal.snapshot.issuer_city_required");
        FiscalConfirmationEvidence.Required(Issuer.Department, 30, "fiscal.snapshot.issuer_department_required");

        if (ReceiverIdentification == ReceiverIdentificationRequirement.Required && Receiver is null)
            throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.receiver_required", "This CFE requires a receiver snapshot.");
        if (Receiver is not null)
        {
            if (Receiver.PartyId == Guid.Empty || Receiver.PartyVersion <= 0)
                throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.receiver_identity_invalid", "Receiver master identity/version is invalid.");
            FiscalConfirmationEvidence.Required(Receiver.Name, 250, "fiscal.snapshot.receiver_name_required");
            FiscalConfirmationEvidence.Country(Receiver.ResidenceCountry, "fiscal.snapshot.receiver_residence_invalid");
            FiscalConfirmationEvidence.Country(Receiver.TaxResidenceCountry, "fiscal.snapshot.receiver_tax_residence_invalid");
            if (Receiver.Identity is not null)
            {
                FiscalConfirmationEvidence.Required(Receiver.Identity.TypeCode, 32, "fiscal.snapshot.receiver_identity_type_required");
                FiscalConfirmationEvidence.Required(Receiver.Identity.Number, 80, "fiscal.snapshot.receiver_identity_number_required");
                var country = Receiver.Identity.IssuingCountry.Trim().ToUpperInvariant();
                if (country != "99") FiscalConfirmationEvidence.Country(country, "fiscal.snapshot.receiver_identity_country_invalid");
            }
            if (Receiver.Address is not null)
            {
                if (Receiver.Address.AddressId == Guid.Empty || !Enum.IsDefined(Receiver.Address.Kind))
                    throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.receiver_address_invalid", "Receiver address snapshot identity/kind is invalid.");
                FiscalConfirmationEvidence.Required(Receiver.Address.AddressLine, 255, "fiscal.snapshot.receiver_address_required");
                FiscalConfirmationEvidence.Required(Receiver.Address.City, 80, "fiscal.snapshot.receiver_city_required");
                FiscalConfirmationEvidence.Country(Receiver.Address.CountryCode, "fiscal.snapshot.receiver_address_country_invalid");
            }
        }

        if (Lines is null || Lines.Count == 0)
            throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.lines_required", "Fiscal content snapshot requires sale lines.");
        if (Lines.Select(line => line.Sequence).OrderBy(sequence => sequence).SequenceEqual(Enumerable.Range(1, Lines.Count)) is false)
            throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.line_sequence_invalid", "Fiscal content snapshot line sequence must be contiguous and one-based.");
        if (Lines.Select(line => line.SaleLineId).Distinct().Count() != Lines.Count)
            throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.line_id_duplicate", "Fiscal content snapshot sale-line identifiers must be unique.");
        foreach (var line in Lines)
        {
            if (line.SaleLineId == Guid.Empty || line.ItemId == Guid.Empty || line.Quantity <= 0m || line.UnitPrice < 0m)
                throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.line_invalid", "Fiscal content snapshot contains invalid sale-line values.");
            FiscalConfirmationEvidence.Required(line.ItemCode, 80, "fiscal.snapshot.item_code_required");
            FiscalConfirmationEvidence.Required(line.ItemName, 250, "fiscal.snapshot.item_name_required");
            if (line.UnitOfMeasure is not null)
                FiscalConfirmationEvidence.Required(line.UnitOfMeasure, 4, "fiscal.snapshot.item_unit_invalid");
            if (!Enum.IsDefined(line.Kind))
                throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.line_kind_invalid", "Fiscal content snapshot line kind is invalid.");
        }

        if (FiscalEvidence is null) throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.fiscal_evidence_required", "Frozen fiscal confirmation evidence is required.");
        FiscalEvidence.EnsureIntegrity();
        if (FiscalEvidence.CfeFamily != CfeFamily
            || FiscalEvidence.ReceiverIdentification != ReceiverIdentification
            || !string.Equals(FiscalEvidence.FormatVersion, FormatVersion, StringComparison.Ordinal)
            || !string.Equals(FiscalEvidence.ConfirmationFingerprint, ConfirmationFingerprint, StringComparison.Ordinal))
            throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.fiscal_evidence_mismatch", "Frozen fiscal evidence does not match the fiscal content snapshot header.");
        if (Lines.Count != FiscalEvidence.Lines.Count
            || Lines.Any(line => !FiscalEvidence.Lines.Any(fiscal => SameFiscalLine(fiscal, line.Fiscal))))
            throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.line_fiscal_evidence_mismatch", "Fiscal content lines do not match their frozen calculation evidence.");

        FiscalConfirmationEvidence.EnsureFingerprint(ContentFingerprint, "fiscal.snapshot.content_fingerprint_invalid");
        if (!string.Equals(ContentFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw FiscalConfirmationEvidence.Rule("fiscal.snapshot.content_fingerprint_mismatch", "Fiscal content fingerprint does not match its immutable snapshot.");
    }

    public string ComputeFingerprint()
    {
        var material = new StringBuilder()
            .Append(OrganizationId).Append('|')
            .Append(SaleId.ToString("N")).Append('|')
            .Append((int)CfeFamily).Append('|')
            .Append((int?)ReceiverIdentification ?? -1).Append('|')
            .Append(FormatVersion).Append('|')
            .Append(EffectiveOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('|')
            .Append(ConfirmationFingerprint).Append('|')
            .Append(SettlementFingerprint).Append('|')
            .Append(FiscalEvidence.EvidenceFingerprint).Append('|')
            .Append(Issuer.Ruc).Append('|')
            .Append(Issuer.LegalName).Append('|')
            .Append(Issuer.CommercialName ?? "-").Append('|')
            .Append(Issuer.CompanyVersion).Append('|')
            .Append(Issuer.LocationId).Append('|')
            .Append(Issuer.DgiBranchCode).Append('|')
            .Append(Issuer.FiscalAddress).Append('|')
            .Append(Issuer.City).Append('|')
            .Append(Issuer.Department).Append('|')
            .Append(Issuer.LocationVersion);

        if (Receiver is null)
        {
            material.Append("|RECEIVER:-");
        }
        else
        {
            material.Append("|RECEIVER:")
                .Append(Receiver.PartyId.ToString("N")).Append(':')
                .Append(Receiver.Name).Append(':')
                .Append(Receiver.ResidenceCountry).Append(':')
                .Append(Receiver.TaxResidenceCountry).Append(':')
                .Append(Receiver.PartyVersion);
            if (Receiver.Identity is not null)
                material.Append(":ID:").Append(Receiver.Identity.TypeCode).Append(':').Append(Receiver.Identity.Number).Append(':').Append(Receiver.Identity.IssuingCountry);
            if (Receiver.Address is not null)
                material.Append(":ADDR:").Append(Receiver.Address.AddressId.ToString("N")).Append(':').Append((int)Receiver.Address.Kind).Append(':')
                    .Append(Receiver.Address.AddressLine).Append(':').Append(Receiver.Address.City).Append(':').Append(Receiver.Address.Region ?? "-").Append(':')
                    .Append(Receiver.Address.CountryCode).Append(':').Append(Receiver.Address.PostalCode ?? "-");
        }

        foreach (var line in Lines.OrderBy(line => line.Sequence))
        {
            material.Append("|LINE:")
                .Append(line.Sequence).Append(':')
                .Append(line.SaleLineId.ToString("N")).Append(':')
                .Append(line.ItemId.ToString("N")).Append(':')
                .Append(line.ItemCode).Append(':')
                .Append(line.ItemName).Append(':')
                .Append((int)line.Kind).Append(':')
                .Append(FiscalConfirmationEvidence.Decimal(line.Quantity)).Append(':')
                .Append(FiscalConfirmationEvidence.Decimal(line.UnitPrice)).Append(':')
                .Append(FiscalConfirmationEvidence.Decimal(line.DiscountAmount)).Append(':')
                .Append(FiscalConfirmationEvidence.Decimal(line.SurchargeAmount)).Append(':')
                .Append(line.Fiscal.LineId.ToString("N")).Append(':')
                .Append(FiscalConfirmationEvidence.Decimal(line.Fiscal.ItemAmount)).Append(':')
                .Append((int)line.Fiscal.VatLiability).Append(':')
                .Append((int)line.Fiscal.VatRateKind).Append(':')
                .Append(FiscalConfirmationEvidence.Decimal(line.Fiscal.AppliedRatePercent)).Append(':')
                .Append(line.Fiscal.RateRulePackVersion);
            if (line.UnitOfMeasure is not null)
                material.Append(":UOM:").Append(line.UnitOfMeasure);
            FiscalConfirmationEvidence.AppendRules(material, line.Fiscal.RuleEvidence, "LINE-RULE");
        }

        return FiscalConfirmationEvidence.Hash(material.ToString());
    }

    private static bool SameFiscalLine(FiscalCalculationLineEvidence left, FiscalCalculationLineEvidence right)
    {
        if (left.LineId != right.LineId
            || left.ItemAmount != right.ItemAmount
            || left.VatLiability != right.VatLiability
            || left.VatRateKind != right.VatRateKind
            || left.AppliedRatePercent != right.AppliedRatePercent
            || !string.Equals(left.RateRulePackVersion, right.RateRulePackVersion, StringComparison.Ordinal))
            return false;

        if (left.RuleEvidence.Count != right.RuleEvidence.Count)
            return false;

        var orderedLeft = left.RuleEvidence.OrderBy(rule => rule.RuleId, StringComparer.Ordinal).ToArray();
        var orderedRight = right.RuleEvidence.OrderBy(rule => rule.RuleId, StringComparer.Ordinal).ToArray();
        for (var index = 0; index < orderedLeft.Length; index++)
        {
            if (orderedLeft[index] != orderedRight[index])
                return false;
        }
        return true;
    }
}