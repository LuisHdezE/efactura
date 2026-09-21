using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Domain.Common;
using EFactura.Domain.Organizations;

namespace EFactura.Application.Organizations;

public sealed record TerminalView(
    string Id,
    string OrganizationId,
    string Code,
    string Name,
    string LocationId,
    bool Active,
    long Version)
{
    public static TerminalView FromDomain(Terminal terminal) =>
        new(
            terminal.Id,
            terminal.OrganizationId,
            terminal.Code,
            terminal.Name,
            terminal.LocationId,
            terminal.Active,
            terminal.Version);
}

public interface ITerminalRepository
{
    Task<IReadOnlyCollection<Terminal>> ListAsync(
        string organizationId,
        bool? active,
        string? locationId,
        CancellationToken cancellationToken = default);

    Task<Terminal?> GetAsync(
        string organizationId,
        string terminalId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        string organizationId,
        string normalizedCode,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveAtLocationAsync(
        string organizationId,
        string locationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Terminal terminal, CancellationToken cancellationToken = default);
    Task SaveAsync(Terminal terminal, CancellationToken cancellationToken = default);
}

public sealed record RegisterTerminalCommand(
    string OrganizationId,
    string Code,
    string Name,
    string LocationId,
    string IdempotencyKey,
    string RequestHash);

public sealed record UpdateTerminalCommand(
    string OrganizationId,
    string TerminalId,
    string Name,
    string LocationId,
    bool Active,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed record TerminalChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string OrganizationId,
    string TerminalId,
    string Code,
    string LocationId,
    bool Active,
    long Version,
    string ChangeType) : IIntegrationEvent;

public sealed class ListTerminalsUseCase
{
    private readonly ITerminalRepository _terminals;
    private readonly IActorContextAccessor _actors;

    public ListTerminalsUseCase(ITerminalRepository terminals, IActorContextAccessor actors)
    {
        _terminals = terminals;
        _actors = actors;
    }

    public async Task<IReadOnlyCollection<TerminalView>> ExecuteAsync(
        string organizationId,
        bool? active,
        string? locationId,
        CancellationToken cancellationToken = default)
    {
        OrganizationAuthorization.EnsureRead(_actors.Current, organizationId);
        return (await _terminals.ListAsync(organizationId, active, NormalizeOptional(locationId), cancellationToken))
            .Select(TerminalView.FromDomain)
            .ToArray();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class GetTerminalUseCase
{
    private readonly ITerminalRepository _terminals;
    private readonly IActorContextAccessor _actors;

    public GetTerminalUseCase(ITerminalRepository terminals, IActorContextAccessor actors)
    {
        _terminals = terminals;
        _actors = actors;
    }

    public async Task<TerminalView> ExecuteAsync(
        string organizationId,
        string terminalId,
        CancellationToken cancellationToken = default)
    {
        OrganizationAuthorization.EnsureRead(_actors.Current, organizationId);
        var terminal = await _terminals.GetAsync(organizationId, terminalId, cancellationToken)
            ?? throw TerminalProblems.NotFound();
        return TerminalView.FromDomain(terminal);
    }
}

public sealed class RegisterTerminalUseCase
{
    private readonly ITerminalRepository _terminals;
    private readonly IFiscalLocationRepository _locations;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public RegisterTerminalUseCase(
        ITerminalRepository terminals,
        IFiscalLocationRepository locations,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _terminals = terminals;
        _locations = locations;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<OrganizationMutationResult> ExecuteAsync(
        RegisterTerminalCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        OrganizationAuthorization.EnsureManage(_actors.Current, command.OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var actor = _actors.Current;
            var correlation = _correlations.Current;
            var scope = $"organization.terminal.register:{command.OrganizationId}";

            var reservation = await UpsertCompanyFiscalProfileUseCase.ReserveAsync(
                _idempotency,
                scope,
                command.IdempotencyKey,
                command.RequestHash,
                actor.ActorId,
                correlation.CorrelationId,
                now,
                ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayed = string.IsNullOrWhiteSpace(reservation.ResourceId)
                    ? null
                    : await _terminals.GetAsync(command.OrganizationId, reservation.ResourceId, ct);
                if (replayed is null)
                    throw UpsertCompanyFiscalProfileUseCase.MissingReplay("terminal");

                return new OrganizationMutationResult(replayed.Id, replayed.Version, true);
            }

            UpsertCompanyFiscalProfileUseCase.EnsureNewReservation(reservation);
            await _unitOfWork.SaveChangesAsync(ct);

            var location = await _locations.GetAsync(command.OrganizationId, command.LocationId, ct)
                ?? throw TerminalProblems.LocationNotFound();
            if (!location.Active)
                throw TerminalProblems.InactiveLocation();

            Terminal terminal;
            try
            {
                terminal = Terminal.Create(
                    Guid.NewGuid().ToString("N"),
                    command.OrganizationId,
                    command.Code,
                    command.Name,
                    command.LocationId);
            }
            catch (DomainRuleException ex)
            {
                throw TerminalProblems.MapDomain(ex);
            }

            if (await _terminals.CodeExistsAsync(command.OrganizationId, terminal.Code, ct))
                throw TerminalProblems.DuplicateCode();

            await _terminals.AddAsync(terminal, ct);
            await TerminalEvidence.AppendAsync(
                terminal,
                "ORGANIZATION_TERMINAL_REGISTERED",
                "registered",
                now,
                actor,
                correlation,
                _audit,
                _outbox,
                ct);

            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    "registered",
                    "Terminal",
                    terminal.Id,
                    correlation.CorrelationId,
                    now),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return new OrganizationMutationResult(terminal.Id, terminal.Version, false);
        }, cancellationToken);
    }
}

public sealed class UpdateTerminalUseCase
{
    private readonly ITerminalRepository _terminals;
    private readonly IFiscalLocationRepository _locations;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public UpdateTerminalUseCase(
        ITerminalRepository terminals,
        IFiscalLocationRepository locations,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _terminals = terminals;
        _locations = locations;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<OrganizationMutationResult> ExecuteAsync(
        UpdateTerminalCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        OrganizationAuthorization.EnsureManage(_actors.Current, command.OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var actor = _actors.Current;
            var correlation = _correlations.Current;
            var scope = $"organization.terminal.update:{command.OrganizationId}:{command.TerminalId}";

            var reservation = await UpsertCompanyFiscalProfileUseCase.ReserveAsync(
                _idempotency,
                scope,
                command.IdempotencyKey,
                command.RequestHash,
                actor.ActorId,
                correlation.CorrelationId,
                now,
                ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayed = await _terminals.GetAsync(command.OrganizationId, command.TerminalId, ct)
                    ?? throw UpsertCompanyFiscalProfileUseCase.MissingReplay("terminal");
                return new OrganizationMutationResult(replayed.Id, replayed.Version, true);
            }

            UpsertCompanyFiscalProfileUseCase.EnsureNewReservation(reservation);
            await _unitOfWork.SaveChangesAsync(ct);

            var terminal = await _terminals.GetAsync(command.OrganizationId, command.TerminalId, ct)
                ?? throw TerminalProblems.NotFound();
            var location = await _locations.GetAsync(command.OrganizationId, command.LocationId, ct)
                ?? throw TerminalProblems.LocationNotFound();

            var isReassignment = !string.Equals(terminal.LocationId, location.Id, StringComparison.Ordinal);
            if ((isReassignment || command.Active) && !location.Active)
                throw TerminalProblems.InactiveLocation();

            try
            {
                terminal.Update(command.Name, command.LocationId, command.Active, command.ExpectedVersion);
            }
            catch (DomainRuleException ex)
            {
                throw TerminalProblems.MapDomain(ex, terminal.Version);
            }

            await _terminals.SaveAsync(terminal, ct);
            await TerminalEvidence.AppendAsync(
                terminal,
                "ORGANIZATION_TERMINAL_UPDATED",
                "updated",
                now,
                actor,
                correlation,
                _audit,
                _outbox,
                ct);

            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    "updated",
                    "Terminal",
                    terminal.Id,
                    correlation.CorrelationId,
                    now),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return new OrganizationMutationResult(terminal.Id, terminal.Version, false);
        }, cancellationToken);
    }
}

internal static class TerminalProblems
{
    public static ApplicationProblemException NotFound() =>
        new(
            ApplicationProblemKind.NotFound,
            "organization.terminal_not_found",
            "The requested terminal was not found.");

    public static ApplicationProblemException LocationNotFound() =>
        new(
            ApplicationProblemKind.NotFound,
            "organization.location_not_found",
            "The requested fiscal location was not found.");

    public static ApplicationProblemException InactiveLocation() =>
        new(
            ApplicationProblemKind.Conflict,
            "organization.terminal.location_inactive",
            "An active terminal binding requires an active fiscal location.",
            conflictType: "inactive_location");

    public static ApplicationProblemException DuplicateCode() =>
        new(
            ApplicationProblemKind.Conflict,
            "organization.terminal.code_duplicate",
            "The terminal code is already assigned inside this organization.",
            conflictType: "duplicate_terminal_code");

    public static ApplicationProblemException MapDomain(DomainRuleException ex, long? currentVersion = null) =>
        ex.Code == "concurrency.stale_version"
            ? new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                ex.Message,
                conflictType: "stale_version",
                currentVersion: currentVersion?.ToString())
            : new ApplicationProblemException(ApplicationProblemKind.Validation, ex.Code, ex.Message);
}

internal static class TerminalEvidence
{
    public static async Task AppendAsync(
        Terminal terminal,
        string auditEvent,
        string changeType,
        DateTimeOffset now,
        ActorContext actor,
        CorrelationContext correlation,
        IAuditWriter audit,
        IOutboxWriter outbox,
        CancellationToken cancellationToken)
    {
        await audit.AppendAsync(
            new AuditEvent(
                Guid.NewGuid(),
                now,
                auditEvent,
                actor.ActorId,
                terminal.OrganizationId,
                terminal.LocationId,
                terminal.Id,
                "Terminal",
                terminal.Id,
                AuditOutcome.Succeeded,
                correlation.CorrelationId,
                correlation.CausationId,
                new Dictionary<string, string?>
                {
                    ["version"] = terminal.Version.ToString(),
                    ["code"] = terminal.Code,
                    ["locationId"] = terminal.LocationId,
                    ["active"] = terminal.Active.ToString()
                }),
            cancellationToken);

        await outbox.EnqueueAsync(
            new TerminalChangedIntegrationEvent(
                Guid.NewGuid(),
                now,
                terminal.OrganizationId,
                terminal.Id,
                terminal.Code,
                terminal.LocationId,
                terminal.Active,
                terminal.Version,
                changeType),
            new OutboxContext(
                correlation.CorrelationId,
                correlation.CausationId,
                terminal.OrganizationId,
                actor.ActorId),
            cancellationToken);
    }
}
