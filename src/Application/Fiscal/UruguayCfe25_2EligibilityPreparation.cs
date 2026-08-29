using EFactura.Application.Common.Errors;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;

namespace EFactura.Application.Fiscal;

public interface ICfeEligibilityRulePackProvider
{
    Task<CfeEligibilityRulePack> GetAsync(
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default);
}

public sealed class UruguayCfe25_2EligibilityRulePackProvider : ICfeEligibilityRulePackProvider
{
    public static readonly DateOnly SupportedFrom = new(2026, 6, 30);
    public const string FormatVersion = "CFE-25.2";

    private static readonly CfeEligibilityRulePack Pack = new(
        FormatVersion,
        SupportedFrom,
        5000m,
        exportServiceStrategyVerifiedCurrent: false,
        new RegulatoryRuleEvidence(
            "UY-CFE-FORMAT-25.2",
            "DGI - Formato CFE v25.2",
            "https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=",
            "Formato_CFE_v25-2, reviewed 2026-08-29",
            SupportedFrom,
            clause: "Formato habilitado en Producción desde 30/06/2026"),
        new RegulatoryRuleEvidence(
            "UY-CFE-25.2-RECEIVER-IDENTITY",
            "DGI - Formato CFE v25.2",
            "https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=",
            "Formato_CFE_v25-2, reviewed 2026-08-29",
            SupportedFrom,
            clause: "A-C60/A-C61/A-C62/A-C62.1 - e-Factura requiere RUC UY; e-Ticket admite documentos tipados"),
        new RegulatoryRuleEvidence(
            "UY-CFE-ETICKET-ID-5000-UI",
            "DGI - Formato CFE v25.2 Tabla E",
            "https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=",
            "Formato_CFE_v25-2, reviewed 2026-08-29",
            new DateOnly(2022, 11, 1),
            clause: "Identificación y envío obligatorio de e-Tickets cuando monto neto supera 5.000 UI o existen retenciones/percepciones"),
        new RegulatoryRuleEvidence(
            "UY-CFE-FAQ-EXPORT-SERVICES-STRATEGY",
            "DGI - Preguntas Frecuentes CFE",
            "https://www.efactura.dgi.gub.uy/files/descargar-todas-las-preguntas-frecuentes?es=",
            "v27 indexed; portal announced newer FAQ 2026-06-25; currentness pending",
            new DateOnly(2024, 11, 29),
            clause: "Exportación de servicios: combo exportación opcional; CFE habitual según RUC, subject to latest-FAQ revalidation"));

    public Task<CfeEligibilityRulePack> GetAsync(
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (effectiveOn < SupportedFrom)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "fiscal.cfe_format_date_unsupported",
                $"The Release-1 CFE eligibility pack supports operations on or after {SupportedFrom:yyyy-MM-dd}. Historical operations require the applicable historical CFE format pack.");
        }

        return Task.FromResult(Pack);
    }
}

public sealed record PrepareCfeEligibilityRequest(
    DateOnly EffectiveOn,
    TaxTreatmentDecision TaxTreatment,
    ReceiverTaxFacts Receiver,
    FiscalOperationIntent OperationIntent,
    decimal? NetAmountUi,
    bool HasRetentionsOrPerceptions);

public sealed class PrepareCfeEligibilityUseCase
{
    private readonly ICfeEligibilityRulePackProvider _rulePacks;
    private readonly CfeEligibilityPolicy _policy;

    public PrepareCfeEligibilityUseCase(
        ICfeEligibilityRulePackProvider rulePacks,
        CfeEligibilityPolicy policy)
    {
        _rulePacks = rulePacks;
        _policy = policy;
    }

    public async Task<CfeEligibilityResult> ExecuteAsync(
        PrepareCfeEligibilityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.TaxTreatment);
        ArgumentNullException.ThrowIfNull(request.Receiver);

        var pack = await _rulePacks.GetAsync(request.EffectiveOn, cancellationToken);

        return _policy.Prepare(
            new CfeEligibilityFacts(
                request.EffectiveOn,
                request.TaxTreatment,
                request.Receiver,
                request.OperationIntent,
                request.NetAmountUi,
                request.HasRetentionsOrPerceptions),
            pack);
    }
}
