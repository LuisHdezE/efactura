using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Fiscal;
using EFactura.Application.Inventory;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Sales;
using EFactura.Domain.Taxation;

namespace EFactura.Application.Sales;

public sealed record SaleConfirmationPlanningLine(
    Guid LineId,
    Guid ItemId,
    SaleLineKind Kind,
    decimal Quantity,
    decimal UnitPrice,
    TaxRateResolution TaxRate,
    decimal DiscountAmount = 0m,
    decimal SurchargeAmount = 0m);

public sealed record SaleConfirmationPlanningRequest(
    Guid SaleId,
    long SaleVersion,
    SaleStatus Status,
    DateOnly EffectiveOn,
    string CurrencyCode,
    string ValidationFingerprint,
    CfeSelectionResult Selection,
    IReadOnlyCollection<SaleConfirmationPlanningLine> Lines,
    InventoryAvailabilityResult Inventory);

public sealed record SaleConfirmationPlan(
    Guid SaleId,
    long SaleVersion,
    string ValidationFingerprint,
    string ConfirmationFingerprint,
    CfeSelectionResult Selection,
    CfeArithmeticResult FiscalCalculation,
    InventoryAvailabilityResult Inventory);

/// <summary>
/// Creates the immutable, side-effect-free plan that a future confirmSale transaction must consume.
/// This planner never mutates Sale/Inventory, reserves CAE, writes persistence, creates money effects,
/// emits outbox events or calls an external fiscal provider.
/// </summary>
public sealed class SaleConfirmationPlanner
{
    private static readonly CfeArithmeticCalculator Calculator = new();

    public SaleConfirmationPlan Prepare(SaleConfirmationPlanningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.SaleId == Guid.Empty)
            throw Rule("sales.confirmation.sale_id_required", "Sale confirmation planning requires a sale identifier.");
        if (request.SaleVersion <= 0)
            throw Rule("sales.confirmation.sale_version_invalid", "Sale confirmation planning requires a positive sale version.");
        if (request.Status != SaleStatus.Validated)
            throw Rule("sales.confirmation.validation_required", "Only a validated sale can produce a confirmation plan.");
        if (string.IsNullOrWhiteSpace(request.ValidationFingerprint))
            throw Rule("sales.confirmation.validation_fingerprint_required", "A validated sale fingerprint is required before confirmation planning.");

        ValidateSelection(request.Selection, request.EffectiveOn);

        if (request.Lines is null || request.Lines.Count == 0)
            throw Rule("sales.confirmation.lines_required", "Sale confirmation planning requires at least one line.");
        if (request.Lines.Any(line => line is null))
            throw Rule("sales.confirmation.line_required", "Sale confirmation planning cannot contain null lines.");
        if (request.Lines.Select(line => line.LineId).Distinct().Count() != request.Lines.Count)
            throw Rule("sales.confirmation.line_id_duplicate", "Sale confirmation line identifiers must be unique.");
        if (request.Lines.Any(line => line.LineId == Guid.Empty || line.ItemId == Guid.Empty))
            throw Rule("sales.confirmation.line_identity_required", "Sale confirmation lines require both line and item identifiers.");

        ValidateInventoryExpectations(request.Lines, request.Inventory);

        var calculation = Calculator.Calculate(
            new CfeArithmeticRequest(
                request.EffectiveOn,
                request.CurrencyCode,
                request.Lines.Select(line => new CfeArithmeticLineInput(
                    line.LineId,
                    line.Quantity,
                    line.UnitPrice,
                    line.DiscountAmount,
                    line.SurchargeAmount,
                    line.TaxRate)).ToArray()),
            UruguayCfe25_2ArithmeticCatalog.Current);

        var validationFingerprint = request.ValidationFingerprint.Trim().ToLowerInvariant();
        var confirmationFingerprint = BuildConfirmationFingerprint(
            request,
            validationFingerprint,
            calculation);

        return new SaleConfirmationPlan(
            request.SaleId,
            request.SaleVersion,
            validationFingerprint,
            confirmationFingerprint,
            request.Selection,
            calculation,
            request.Inventory);
    }

    private static void ValidateSelection(CfeSelectionResult selection, DateOnly effectiveOn)
    {
        if (selection is null)
            throw Rule("sales.confirmation.selection_required", "Server-side CFE selection evidence is required before confirmation planning.");
        if (selection.Status != CfeSelectionStatus.Selected)
            throw Rule("sales.confirmation.selection_not_final", "CFE selection must be final before confirmation planning.");
        if (!selection.SelectedFamily.HasValue || !Enum.IsDefined(selection.SelectedFamily.Value))
            throw Rule("sales.confirmation.cfe_family_invalid", "The selected CFE family is not supported by the current domain catalog.");
        if (string.IsNullOrWhiteSpace(selection.FormatVersion))
            throw Rule("sales.confirmation.selection_format_required", "CFE selection format provenance is required before confirmation planning.");
        if (!string.Equals(
                selection.FormatVersion,
                UruguayCfe25_2ArithmeticCatalog.FormatVersion,
                StringComparison.Ordinal))
        {
            throw Rule(
                "sales.confirmation.selection_format_mismatch",
                "CFE selection and arithmetic must use the same supported format version.");
        }
        if (selection.RuleEvidence is null || selection.RuleEvidence.Count == 0)
            throw Rule("sales.confirmation.selection_rule_evidence_required", "CFE selection requires regulatory rule evidence before confirmation planning.");
        if (selection.MissingFacts is not null && selection.MissingFacts.Count > 0)
            throw Rule("sales.confirmation.selection_missing_facts_present", "A final CFE selection cannot retain unresolved required facts.");

        foreach (var evidence in selection.RuleEvidence)
        {
            if (!evidence.Covers(effectiveOn))
            {
                throw Rule(
                    "sales.confirmation.selection_rule_not_effective",
                    $"CFE selection rule evidence '{evidence.RuleId}' does not cover the requested fiscal date.");
            }
        }

        var candidates = selection.Candidates?
            .Where(candidate => candidate.Family == selection.SelectedFamily.Value)
            .ToArray() ?? Array.Empty<CfeCandidate>();
        if (candidates.Length != 1)
            throw Rule("sales.confirmation.selection_candidate_missing", "Selected CFE family must correspond to exactly one eligible candidate.");
        if (selection.ReceiverIdentification != candidates[0].ReceiverIdentification)
            throw Rule("sales.confirmation.selection_receiver_identity_mismatch", "Selected CFE receiver-identification requirement does not match its eligible candidate.");
    }

    private static void ValidateInventoryExpectations(
        IReadOnlyCollection<SaleConfirmationPlanningLine> lines,
        InventoryAvailabilityResult inventory)
    {
        if (inventory is null)
            throw Rule("sales.confirmation.inventory_evidence_required", "Inventory availability evidence is required before confirmation planning.");
        if (!inventory.Ready)
            throw Rule("sales.confirmation.inventory_not_ready", "Inventory availability is no longer ready for confirmation.");
        if (inventory.Lines is null)
            throw Rule("sales.confirmation.inventory_lines_required", "Inventory availability lines are required before confirmation planning.");

        var productRequirements = lines
            .Where(line => line.Kind == SaleLineKind.Product)
            .GroupBy(line => line.ItemId)
            .ToDictionary(group => group.Key, group => group.Sum(line => line.Quantity));

        if (inventory.Lines.Select(line => line.ItemId).Distinct().Count() != inventory.Lines.Count)
            throw Rule("sales.confirmation.inventory_item_duplicate", "Inventory availability evidence must contain one row per product item.");

        foreach (var requirement in productRequirements)
        {
            var evidence = inventory.Lines.SingleOrDefault(line => line.ItemId == requirement.Key)
                ?? throw Rule(
                    "sales.confirmation.inventory_item_missing",
                    "Inventory availability evidence is missing a product from the sale confirmation plan.");

            if (evidence.RequiredQuantity != requirement.Value)
                throw Rule(
                    "sales.confirmation.inventory_quantity_mismatch",
                    "Inventory availability evidence does not match the product quantity in the sale confirmation plan.");

            if (evidence.TracksInventory
                && (!evidence.Sufficient || !evidence.AvailableQuantity.HasValue || !evidence.PositionVersion.HasValue))
            {
                throw Rule(
                    "sales.confirmation.inventory_position_unresolved",
                    "Tracked inventory requires sufficient quantity and an authoritative position version before confirmation.");
            }
        }

        if (inventory.Lines.Any(line => !productRequirements.ContainsKey(line.ItemId)))
            throw Rule(
                "sales.confirmation.inventory_evidence_unexpected",
                "Inventory availability evidence contains an item that is not part of the sale product requirements.");
    }

    private static string BuildConfirmationFingerprint(
        SaleConfirmationPlanningRequest request,
        string validationFingerprint,
        CfeArithmeticResult calculation)
    {
        var selectedFamily = request.Selection.SelectedFamily!.Value;
        var material = new StringBuilder()
            .Append(request.SaleId.ToString("N")).Append('|')
            .Append(request.SaleVersion).Append('|')
            .Append(validationFingerprint).Append('|')
            .Append((int)selectedFamily).Append('|')
            .Append((int?)request.Selection.ReceiverIdentification ?? -1).Append('|')
            .Append(request.Selection.FormatVersion).Append('|')
            .Append(request.EffectiveOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('|')
            .Append(calculation.CurrencyCode).Append('|')
            .Append(calculation.FormatVersion).Append('|')
            .Append(calculation.ArithmeticRulePackVersion).Append('|')
            .Append(Decimal(calculation.Totals.NetAmount)).Append('|')
            .Append(Decimal(calculation.Totals.VatAmount)).Append('|')
            .Append(Decimal(calculation.Totals.TotalAmount));

        foreach (var evidence in request.Selection.RuleEvidence.OrderBy(item => item.RuleId, StringComparer.Ordinal))
        {
            material.Append('|')
                .Append(evidence.RuleId).Append(':')
                .Append(evidence.SourceVersion).Append(':')
                .Append(evidence.EffectiveFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(':')
                .Append(evidence.EffectiveTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-");
        }

        foreach (var line in calculation.Lines.OrderBy(line => line.LineId))
        {
            material.Append('|')
                .Append(line.LineId.ToString("N")).Append(':')
                .Append(Decimal(line.ItemAmount)).Append(':')
                .Append((int)line.VatLiability).Append(':')
                .Append((int)line.VatRateKind).Append(':')
                .Append(Decimal(line.AppliedRatePercent)).Append(':')
                .Append(line.RateRulePackVersion);
        }

        foreach (var item in request.Inventory.Lines.OrderBy(line => line.ItemId))
        {
            material.Append('|')
                .Append(item.ItemId.ToString("N")).Append(':')
                .Append(item.TracksInventory).Append(':')
                .Append(Decimal(item.RequiredQuantity)).Append(':')
                .Append(item.AvailableQuantity.HasValue ? Decimal(item.AvailableQuantity.Value) : "-").Append(':')
                .Append(item.PositionVersion?.ToString(CultureInfo.InvariantCulture) ?? "-").Append(':')
                .Append(item.Sufficient);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material.ToString())))
            .ToLowerInvariant();
    }

    private static string Decimal(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
