using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Persistence;
using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

public sealed record FiscalCfeStateConsultationRequest(
    string OrganizationId,
    CfeFamily CfeType,
    string Series,
    long Number);

public sealed record FiscalCfeStateConsultationResponse(
    string StateCode,
    string DgiSenderId,
    string DgiReceiverId,
    string? ConsultationToken,
    string? ConsultationAvailableAtText,
    string ResponseXml);

public interface IFiscalCfeStateConsultationGateway
{
    Task<FiscalCfeStateConsultationResponse> QueryAsync(
        FiscalCfeStateConsultationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record StoredFiscalCfeStateConsultation(
    Guid Id,
    Guid FiscalDocumentId,
    string OrganizationId,
    CfeFamily CfeType,
    string Series,
    long Number,
    string OperationId,
    string StateCode,
    string DgiSenderId,
    string DgiReceiverId,
    string? ConsultationToken,
    string? ConsultationAvailableAtText,
    string ResponseXml,
    string ResponseSha256,
    DateTimeOffset ConsultedAtUtc);

public interface IFiscalCfeStateConsultationRepository
{
    Task<StoredFiscalCfeStateConsultation?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalCfeStateConsultation consultation,
        CancellationToken cancellationToken = default);
}

public sealed record ConsultFiscalCfeStateCommand(
    string OrganizationId,
    Guid FiscalDocumentId,
    string OperationId);

public sealed record FiscalCfeStateConsultationResult(
    Guid ConsultationId,
    Guid FiscalDocumentId,
    string OrganizationId,
    CfeFamily CfeType,
    string Series,
    long Number,
    string StateCode,
    string DgiSenderId,
    string DgiReceiverId,
    string? ConsultationToken,
    string? ConsultationAvailableAtText,
    string ResponseXml,
    string ResponseSha256,
    DateTimeOffset ConsultedAtUtc,
    bool Replayed);

/// <summary>
/// Queries DGI ws_consultas / EFACCONSULTARESTADOCFE for one already durable FiscalDocument identity
/// and stores the returned evidence append-only. The DGI state code is preserved as external evidence
/// and is deliberately not translated into a local lifecycle semantic by this slice. The returned
/// Token/FechaHora pair is preserved only as consultation evidence; this use case does not invoke a
/// token-input second-response operation because that transport contract has not been proven.
/// </summary>
public sealed class ConsultFiscalCfeStateUseCase
{
    private readonly IFiscalDocumentRepository _documents;
    private readonly IFiscalCfeStateConsultationRepository _consultations;
    private readonly IFiscalCfeStateConsultationGateway _gateway;
    private readonly IFiscalDailyReportTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public ConsultFiscalCfeStateUseCase(
        IFiscalDocumentRepository documents,
        IFiscalCfeStateConsultationRepository consultations,
        IFiscalCfeStateConsultationGateway gateway,
        IFiscalDailyReportTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork)
    {
        _documents = documents;
        _consultations = consultations;
        _gateway = gateway;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<FiscalCfeStateConsultationResult> ExecuteAsync(
        ConsultFiscalCfeStateCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var organizationId = command.OrganizationId.Trim();
        var operationId = command.OperationId.Trim();

        var replay = await _consultations.GetByOperationIdAsync(
            organizationId,
            operationId,
            cancellationToken);
        if (replay is not null)
        {
            EnsureReplayMatches(replay, command.FiscalDocumentId);
            return Result(replay, true);
        }

        var document = await _documents.GetAsync(
            organizationId,
            command.FiscalDocumentId,
            cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "fiscal.cfe_state_consultation.document_not_found",
                "Fiscal document was not found.");

        var response = await _gateway.QueryAsync(
            new FiscalCfeStateConsultationRequest(
                organizationId,
                document.CfeType,
                document.Series,
                document.Number),
            cancellationToken);

        ValidateResponse(response);

        var responseHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(response.ResponseXml))).ToLowerInvariant();
        var now = PrepareFiscalDailyReportSubmissionUseCase.WholeSecond(_clock.UtcNow);
        var consultation = new StoredFiscalCfeStateConsultation(
            Guid.NewGuid(),
            document.Id,
            document.OrganizationId,
            document.CfeType,
            document.Series,
            document.Number,
            operationId,
            response.StateCode.Trim(),
            response.DgiSenderId.Trim(),
            response.DgiReceiverId.Trim(),
            NormalizeOptional(response.ConsultationToken),
            NormalizeOptional(response.ConsultationAvailableAtText),
            response.ResponseXml,
            responseHash,
            now);

        return await _transactions.ExecuteAsync(async ct =>
        {
            var concurrentReplay = await _consultations.GetByOperationIdAsync(
                organizationId,
                operationId,
                ct);
            if (concurrentReplay is not null)
            {
                EnsureReplayMatches(concurrentReplay, command.FiscalDocumentId);
                return Result(concurrentReplay, true);
            }

            await _consultations.AddAsync(consultation, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result(consultation, false);
        }, cancellationToken);
    }

    private static void Validate(ConsultFiscalCfeStateCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId) || command.OrganizationId.Trim().Length > 200)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.cfe_state_consultation.organization_invalid",
                "Organization id is required and must not exceed 200 characters.");
        if (command.FiscalDocumentId == Guid.Empty)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.cfe_state_consultation.document_id_invalid",
                "Fiscal document id is required.");
        if (string.IsNullOrWhiteSpace(command.OperationId) || command.OperationId.Trim().Length > 120)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.cfe_state_consultation.operation_id_invalid",
                "Operation id is required and must not exceed 120 characters.");
    }

    private static void ValidateResponse(FiscalCfeStateConsultationResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (string.IsNullOrWhiteSpace(response.StateCode) || response.StateCode.Trim().Length > 40)
            throw ExternalEvidenceInvalid("DGI CFE state response does not contain a bounded EstadoCFE value.");
        if (string.IsNullOrWhiteSpace(response.DgiSenderId) || response.DgiSenderId.Trim().Length > 120)
            throw ExternalEvidenceInvalid("DGI CFE state response does not contain IdEmisor.");
        if (string.IsNullOrWhiteSpace(response.DgiReceiverId) || response.DgiReceiverId.Trim().Length > 120)
            throw ExternalEvidenceInvalid("DGI CFE state response does not contain IdReceptor.");
        if (string.IsNullOrWhiteSpace(response.ResponseXml))
            throw ExternalEvidenceInvalid("DGI CFE state response XML is required as durable evidence.");

        var hasToken = !string.IsNullOrWhiteSpace(response.ConsultationToken);
        var hasAvailableAt = !string.IsNullOrWhiteSpace(response.ConsultationAvailableAtText);
        if (hasToken != hasAvailableAt)
            throw ExternalEvidenceInvalid("DGI CFE state consultation parameters must preserve Token and FechaHora together.");
        if (response.ConsultationAvailableAtText?.Trim().Length > 80)
            throw ExternalEvidenceInvalid("DGI CFE state consultation FechaHora exceeds the accepted evidence bound.");
    }

    private static void EnsureReplayMatches(
        StoredFiscalCfeStateConsultation consultation,
        Guid fiscalDocumentId)
    {
        if (consultation.FiscalDocumentId != fiscalDocumentId)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.cfe_state_consultation.operation_replay_mismatch",
                "The consultation operation id was already used for a different fiscal document.",
                "inconsistent_replay");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ApplicationProblemException ExternalEvidenceInvalid(string message) =>
        PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
            "fiscal.cfe_state_consultation.external_evidence_invalid",
            message,
            "external_evidence_invalid");

    private static FiscalCfeStateConsultationResult Result(
        StoredFiscalCfeStateConsultation value,
        bool replayed) => new(
        value.Id,
        value.FiscalDocumentId,
        value.OrganizationId,
        value.CfeType,
        value.Series,
        value.Number,
        value.StateCode,
        value.DgiSenderId,
        value.DgiReceiverId,
        value.ConsultationToken,
        value.ConsultationAvailableAtText,
        value.ResponseXml,
        value.ResponseSha256,
        value.ConsultedAtUtc,
        replayed);
}
