using EFactura.Domain.Taxation;

namespace EFactura.Domain.Fiscal;

public enum CfeSelectionStatus
{
    Selected = 1,
    RequiresReview = 2,
    Ineligible = 3
}

public enum ExportServiceDocumentationStrategy
{
    Unconfigured = 0,
    ExportCombo = 1,
    UsualCfe = 2
}

public sealed record CfeSelectionConfiguration(
    ExportServiceDocumentationStrategy ExportServiceStrategy);

public sealed record CfeSelectionResult(
    CfeSelectionStatus Status,
    CfeFamily? SelectedFamily,
    ReceiverIdentificationRequirement? ReceiverIdentification,
    IReadOnlyCollection<CfeCandidate> Candidates,
    IReadOnlyCollection<string> Reasons,
    IReadOnlyCollection<string> MissingFacts,
    IReadOnlyCollection<RegulatoryRuleEvidence> RuleEvidence,
    string FormatVersion);

public sealed class CfeSelectionPolicy
{
    public CfeSelectionResult Select(
        CfeEligibilityResult eligibility,
        TaxTreatmentDecision treatment,
        CfeSelectionConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(eligibility);
        ArgumentNullException.ThrowIfNull(treatment);
        ArgumentNullException.ThrowIfNull(configuration);

        if (eligibility.Status == CfeEligibilityStatus.Ineligible)
        {
            return new CfeSelectionResult(
                CfeSelectionStatus.Ineligible,
                null,
                null,
                eligibility.Candidates,
                eligibility.Reasons,
                eligibility.MissingFacts,
                eligibility.RuleEvidence,
                eligibility.FormatVersion);
        }

        if (treatment.Status == TaxDecisionStatus.RequiresReview)
        {
            return Review(eligibility, "fiscal.selection.tax_treatment_requires_review", treatment.MissingFacts);
        }

        if (eligibility.Candidates.Count == 0)
        {
            return Review(eligibility, "fiscal.selection.no_eligible_candidate", eligibility.MissingFacts);
        }

        if (treatment.Classification == TaxTreatmentClassification.ExportServices)
        {
            if (configuration.ExportServiceStrategy == ExportServiceDocumentationStrategy.Unconfigured)
            {
                return Review(
                    eligibility,
                    "fiscal.selection.export_service_strategy_unconfigured",
                    new[] { "export_service_documentation_strategy" });
            }

            var desiredFamily = configuration.ExportServiceStrategy == ExportServiceDocumentationStrategy.ExportCombo
                ? CfeFamily.EFacturaExportacion
                : eligibility.Candidates.Any(x => x.Family == CfeFamily.EFactura)
                    ? CfeFamily.EFactura
                    : CfeFamily.ETicket;

            var configured = eligibility.Candidates.SingleOrDefault(x => x.Family == desiredFamily);
            if (configured is null)
            {
                return Review(
                    eligibility,
                    "fiscal.selection.configured_strategy_not_in_candidate_set",
                    new[] { "compatible_export_service_documentation_strategy" });
            }

            return Selected(eligibility, configured, "fiscal.selection.export_service_strategy_selected");
        }

        if (eligibility.Status == CfeEligibilityStatus.RequiresReview)
        {
            return Review(eligibility, "fiscal.selection.eligibility_requires_review", eligibility.MissingFacts);
        }

        if (eligibility.Candidates.Count != 1)
        {
            return Review(
                eligibility,
                "fiscal.selection.multiple_candidates_require_policy",
                new[] { "cfe_selection_policy" });
        }

        return Selected(eligibility, eligibility.Candidates.Single(), "fiscal.selection.single_candidate_selected");
    }

    private static CfeSelectionResult Selected(
        CfeEligibilityResult eligibility,
        CfeCandidate candidate,
        string reason) =>
        new(
            CfeSelectionStatus.Selected,
            candidate.Family,
            candidate.ReceiverIdentification,
            eligibility.Candidates,
            eligibility.Reasons.Append(reason).ToArray(),
            Array.Empty<string>(),
            eligibility.RuleEvidence,
            eligibility.FormatVersion);

    private static CfeSelectionResult Review(
        CfeEligibilityResult eligibility,
        string reason,
        IEnumerable<string> missingFacts) =>
        new(
            CfeSelectionStatus.RequiresReview,
            null,
            null,
            eligibility.Candidates,
            eligibility.Reasons.Append(reason).ToArray(),
            missingFacts.Distinct(StringComparer.Ordinal).ToArray(),
            eligibility.RuleEvidence,
            eligibility.FormatVersion);
}
