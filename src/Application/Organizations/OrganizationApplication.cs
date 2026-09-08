using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Security;
using EFactura.Domain.Common;
using EFactura.Domain.Organizations;

namespace EFactura.Application.Organizations;

public sealed record CompanyFiscalProfileView(
    string OrganizationId,
    string Ruc,
    string LegalName,
    string? CommercialName,
    long Version)
{
    public static CompanyFiscalProfileView FromDomain(CompanyFiscalProfile company) =>
        new(company.OrganizationId, company.Ruc, company.LegalName, company.CommercialName, company.Version);
}

public sealed record FiscalLocationView(
    string Id,
    string OrganizationId,
    string Name,
    string DgiBranchCode,
    string FiscalAddress,
    string City,
    string Department,
    bool Active,
    long Version)
{
    public static FiscalLocationView FromDomain(FiscalLocation location) =>
        new(location.Id, location.OrganizationId, location.Name, location.DgiBranchCode, location.FiscalAddress, location.City, location.Department, location.Active, location.Version);
}

public interface ICompanyFiscalProfileRepository
{
    Task<CompanyFiscalProfile?> GetAsync(string organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(CompanyFiscalProfile company, CancellationToken cancellationToken = default);
    Task SaveAsync(CompanyFiscalProfile company, CancellationToken cancellationToken = default);
}

public interface IFiscalLocationRepository
{
    Task<IReadOnlyCollection<FiscalLocation>> ListAsync(string organizationId, bool? active, CancellationToken cancellationToken = default);
    Task<FiscalLocation?> GetAsync(string organizationId, string locationId, CancellationToken cancellationToken = default);
    Task<bool> BranchCodeExistsAsync(string organizationId, string dgiBranchCode, string? excludingLocationId = null, CancellationToken cancellationToken = default);
    Task AddAsync(FiscalLocation location, CancellationToken cancellationToken = default);
    Task SaveAsync(FiscalLocation location, CancellationToken cancellationToken = default);
}

public sealed record UpsertCompanyFiscalProfileCommand(string OrganizationId, string Ruc, string LegalName, string? CommercialName, long? ExpectedVersion, string IdempotencyKey, string RequestHash);
public sealed record CreateFiscalLocationCommand(string OrganizationId, string Name, string DgiBranchCode, string FiscalAddress, string City, string Department, string IdempotencyKey, string RequestHash);
public sealed record UpdateFiscalLocationCommand(string OrganizationId, string LocationId, string Name, string DgiBranchCode, string FiscalAddress, string City, string Department, bool Active, long ExpectedVersion, string IdempotencyKey, string RequestHash);
public sealed record OrganizationMutationResult(string ResourceId, long Version, bool Replayed);

public sealed record CompanyFiscalProfileChangedIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt, string OrganizationId, long Version, string ChangeType) : IIntegrationEvent;
public sealed record FiscalLocationChangedIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt, string OrganizationId, string LocationId, string DgiBranchCode, long Version, string ChangeType) : IIntegrationEvent;

internal static class OrganizationAuthorization
{
    public static void EnsureRead(ActorContext actor, string organizationId)
    {
        if (!actor.IsAuthenticated || !actor.HasPermission(Permissions.OrganizationRead))
            throw Forbidden("The actor is not allowed to read organization configuration.");
        EnsureScope(actor, organizationId);
    }

    public static void EnsureManage(ActorContext actor, string organizationId)
    {
        if (!actor.IsAuthenticated || !actor.HasPermission(Permissions.OrganizationManage))
            throw Forbidden("The actor is not allowed to manage organization configuration.");
        EnsureScope(actor, organizationId);
    }

    private static void EnsureScope(ActorContext actor, string organizationId)
    {
        if (!actor.CompanyScopes.Contains(organizationId))
            throw new ApplicationProblemException(ApplicationProblemKind.Forbidden, "organization_scope_denied", "The actor is not allowed to access this organization.");
    }

    private static ApplicationProblemException Forbidden(string message) => new(ApplicationProblemKind.Forbidden, "permission_denied", message);
}

public sealed class GetCurrentCompanyUseCase
{
    private readonly ICompanyFiscalProfileRepository _companies;
    private readonly IActorContextAccessor _actors;
    public GetCurrentCompanyUseCase(ICompanyFiscalProfileRepository companies, IActorContextAccessor actors) { _companies = companies; _actors = actors; }

    public async Task<CompanyFiscalProfileView> ExecuteAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        OrganizationAuthorization.EnsureRead(_actors.Current, organizationId);
        var company = await _companies.GetAsync(organizationId, cancellationToken)
            ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "organization.company_not_configured", "The current organization does not yet have a fiscal issuer profile.");
        return CompanyFiscalProfileView.FromDomain(company);
    }
}

public sealed class ListFiscalLocationsUseCase
{
    private readonly IFiscalLocationRepository _locations;
    private readonly IActorContextAccessor _actors;
    public ListFiscalLocationsUseCase(IFiscalLocationRepository locations, IActorContextAccessor actors) { _locations = locations; _actors = actors; }

    public async Task<IReadOnlyCollection<FiscalLocationView>> ExecuteAsync(string organizationId, bool? active, CancellationToken cancellationToken = default)
    {
        OrganizationAuthorization.EnsureRead(_actors.Current, organizationId);
        return (await _locations.ListAsync(organizationId, active, cancellationToken)).Select(FiscalLocationView.FromDomain).ToArray();
    }
}

public sealed class GetFiscalLocationUseCase
{
    private readonly IFiscalLocationRepository _locations;
    private readonly IActorContextAccessor _actors;
    public GetFiscalLocationUseCase(IFiscalLocationRepository locations, IActorContextAccessor actors) { _locations = locations; _actors = actors; }

    public async Task<FiscalLocationView> ExecuteAsync(string organizationId, string locationId, CancellationToken cancellationToken = default)
    {
        OrganizationAuthorization.EnsureRead(_actors.Current, organizationId);
        var location = await _locations.GetAsync(organizationId, locationId, cancellationToken)
            ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "organization.location_not_found", "The requested fiscal location was not found.");
        return FiscalLocationView.FromDomain(location);
    }
}

public sealed class UpsertCompanyFiscalProfileUseCase
{
    private readonly ICompanyFiscalProfileRepository _companies;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public UpsertCompanyFiscalProfileUseCase(ICompanyFiscalProfileRepository companies, ITransactionManager transactions, IUnitOfWork unitOfWork, IIdempotencyStore idempotency, IAuditWriter audit, IOutboxWriter outbox, IActorContextAccessor actors, ICorrelationContextAccessor correlations)
    { _companies = companies; _transactions = transactions; _unitOfWork = unitOfWork; _idempotency = idempotency; _audit = audit; _outbox = outbox; _actors = actors; _correlations = correlations; }

    public Task<OrganizationMutationResult> ExecuteAsync(UpsertCompanyFiscalProfileCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        OrganizationAuthorization.EnsureManage(_actors.Current, command.OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);
        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow; var actor = _actors.Current; var correlation = _correlations.Current; var scope = $"organization.company.upsert:{command.OrganizationId}";
            var reservation = await ReserveAsync(_idempotency, scope, command.IdempotencyKey, command.RequestHash, actor.ActorId, correlation.CorrelationId, now, ct);
            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayed = await _companies.GetAsync(command.OrganizationId, ct) ?? throw MissingReplay("company fiscal profile");
                return new OrganizationMutationResult(replayed.OrganizationId, replayed.Version, true);
            }
            EnsureNewReservation(reservation); await _unitOfWork.SaveChangesAsync(ct);
            var company = await _companies.GetAsync(command.OrganizationId, ct); string changeType;
            try
            {
                if (company is null)
                {
                    if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value > 0)
                        throw new ApplicationProblemException(ApplicationProblemKind.Conflict, "concurrency_conflict", "The company fiscal profile does not exist at the expected version.", conflictType: "stale_version", currentVersion: "0");
                    company = CompanyFiscalProfile.Create(command.OrganizationId, command.Ruc, command.LegalName, command.CommercialName);
                    await _companies.AddAsync(company, ct); changeType = "created";
                }
                else
                {
                    if (!command.ExpectedVersion.HasValue || command.ExpectedVersion.Value <= 0)
                        throw new ApplicationProblemException(ApplicationProblemKind.Conflict, "concurrency_conflict", "Updating an existing company fiscal profile requires expectedVersion.", conflictType: "expected_version_required", currentVersion: company.Version.ToString());
                    company.Update(command.Ruc, command.LegalName, command.CommercialName, command.ExpectedVersion.Value);
                    await _companies.SaveAsync(company, ct); changeType = "updated";
                }
            }
            catch (DomainRuleException ex) { throw Map(ex, company?.Version); }

            await _audit.AppendAsync(new AuditEvent(Guid.NewGuid(), now, changeType == "created" ? "ORGANIZATION_FISCAL_PROFILE_CREATED" : "ORGANIZATION_FISCAL_PROFILE_UPDATED", actor.ActorId, command.OrganizationId, null, null, "CompanyFiscalProfile", command.OrganizationId, AuditOutcome.Succeeded, correlation.CorrelationId, correlation.CausationId, new Dictionary<string, string?> { ["version"] = company.Version.ToString(), ["ruc"] = company.Ruc, ["legalName"] = company.LegalName }), ct);
            await _outbox.EnqueueAsync(new CompanyFiscalProfileChangedIntegrationEvent(Guid.NewGuid(), now, command.OrganizationId, company.Version, changeType), new OutboxContext(correlation.CorrelationId, correlation.CausationId, command.OrganizationId, actor.ActorId), ct);
            await _idempotency.CompleteAsync(new IdempotencyCompletion(scope, command.IdempotencyKey, command.RequestHash, changeType, "CompanyFiscalProfile", command.OrganizationId, correlation.CorrelationId, now), ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return new OrganizationMutationResult(command.OrganizationId, company.Version, false);
        }, cancellationToken);
    }

    private static ApplicationProblemException Map(DomainRuleException ex, long? currentVersion) => ex.Code == "concurrency.stale_version" ? new ApplicationProblemException(ApplicationProblemKind.Conflict, "concurrency_conflict", ex.Message, conflictType: "stale_version", currentVersion: currentVersion?.ToString()) : new ApplicationProblemException(ApplicationProblemKind.Validation, ex.Code, ex.Message);
    internal static Task<IdempotencyReservationResult> ReserveAsync(IIdempotencyStore store, string scope, string key, string requestHash, string? actorId, string correlationId, DateTimeOffset now, CancellationToken cancellationToken) => store.TryReserveAsync(new IdempotencyReservation(scope, key, requestHash, actorId, correlationId, now.AddMinutes(10)), cancellationToken);
    internal static void EnsureNewReservation(IdempotencyReservationResult reservation)
    {
        if (reservation.Status == IdempotencyReservationStatus.PayloadMismatch) throw new ApplicationProblemException(ApplicationProblemKind.Conflict, "idempotency_key_reused", "The idempotency key was already used with a different request.", conflictType: "payload_mismatch");
        if (reservation.Status == IdempotencyReservationStatus.ExistingInProgress) throw new ApplicationProblemException(ApplicationProblemKind.Conflict, "idempotency_in_progress", "An operation with this idempotency key is still in progress.", conflictType: "in_progress", retryAfterSeconds: 2);
    }
    internal static ApplicationProblemException MissingReplay(string resource) => new(ApplicationProblemKind.Conflict, "idempotency.missing_completed_resource", $"The prior completed {resource} no longer exists in the authoritative store.");
}

public sealed class CreateFiscalLocationUseCase
{
    private readonly ICompanyFiscalProfileRepository _companies; private readonly IFiscalLocationRepository _locations; private readonly ITransactionManager _transactions; private readonly IUnitOfWork _unitOfWork; private readonly IIdempotencyStore _idempotency; private readonly IAuditWriter _audit; private readonly IOutboxWriter _outbox; private readonly IActorContextAccessor _actors; private readonly ICorrelationContextAccessor _correlations;
    public CreateFiscalLocationUseCase(ICompanyFiscalProfileRepository companies, IFiscalLocationRepository locations, ITransactionManager transactions, IUnitOfWork unitOfWork, IIdempotencyStore idempotency, IAuditWriter audit, IOutboxWriter outbox, IActorContextAccessor actors, ICorrelationContextAccessor correlations)
    { _companies = companies; _locations = locations; _transactions = transactions; _unitOfWork = unitOfWork; _idempotency = idempotency; _audit = audit; _outbox = outbox; _actors = actors; _correlations = correlations; }

    public Task<OrganizationMutationResult> ExecuteAsync(CreateFiscalLocationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command); OrganizationAuthorization.EnsureManage(_actors.Current, command.OrganizationId); ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey); ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);
        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow; var actor = _actors.Current; var correlation = _correlations.Current; var scope = $"organization.location.create:{command.OrganizationId}";
            var reservation = await UpsertCompanyFiscalProfileUseCase.ReserveAsync(_idempotency, scope, command.IdempotencyKey, command.RequestHash, actor.ActorId, correlation.CorrelationId, now, ct);
            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayed = string.IsNullOrWhiteSpace(reservation.ResourceId) ? null : await _locations.GetAsync(command.OrganizationId, reservation.ResourceId, ct);
                if (replayed is null) throw UpsertCompanyFiscalProfileUseCase.MissingReplay("fiscal location");
                return new OrganizationMutationResult(replayed.Id, replayed.Version, true);
            }
            UpsertCompanyFiscalProfileUseCase.EnsureNewReservation(reservation); await _unitOfWork.SaveChangesAsync(ct);
            if (await _companies.GetAsync(command.OrganizationId, ct) is null) throw new ApplicationProblemException(ApplicationProblemKind.Conflict, "organization.company_not_configured", "A fiscal issuer profile must be configured before creating a fiscal location.", conflictType: "missing_prerequisite");
            if (await _locations.BranchCodeExistsAsync(command.OrganizationId, command.DgiBranchCode, null, ct)) throw DuplicateBranch();
            FiscalLocation location;
            try { location = FiscalLocation.Create(Guid.NewGuid().ToString("N"), command.OrganizationId, command.Name, command.DgiBranchCode, command.FiscalAddress, command.City, command.Department); }
            catch (DomainRuleException ex) { throw new ApplicationProblemException(ApplicationProblemKind.Validation, ex.Code, ex.Message); }
            await _locations.AddAsync(location, ct); await AppendEvidenceAsync(location, "ORGANIZATION_LOCATION_CREATED", "created", now, actor, correlation, _audit, _outbox, ct);
            await _idempotency.CompleteAsync(new IdempotencyCompletion(scope, command.IdempotencyKey, command.RequestHash, "created", "FiscalLocation", location.Id, correlation.CorrelationId, now), ct);
            await _unitOfWork.SaveChangesAsync(ct); return new OrganizationMutationResult(location.Id, location.Version, false);
        }, cancellationToken);
    }

    internal static ApplicationProblemException DuplicateBranch() => new(ApplicationProblemKind.Conflict, "organization.location.branch_code_duplicate", "The DGI branch code is already assigned to another location in this organization.", conflictType: "duplicate_branch_code");
    internal static async Task AppendEvidenceAsync(FiscalLocation location, string auditEvent, string changeType, DateTimeOffset now, ActorContext actor, CorrelationContext correlation, IAuditWriter audit, IOutboxWriter outbox, CancellationToken cancellationToken)
    {
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), now, auditEvent, actor.ActorId, location.OrganizationId, location.Id, null, "FiscalLocation", location.Id, AuditOutcome.Succeeded, correlation.CorrelationId, correlation.CausationId, new Dictionary<string, string?> { ["version"] = location.Version.ToString(), ["dgiBranchCode"] = location.DgiBranchCode, ["active"] = location.Active.ToString() }), cancellationToken);
        await outbox.EnqueueAsync(new FiscalLocationChangedIntegrationEvent(Guid.NewGuid(), now, location.OrganizationId, location.Id, location.DgiBranchCode, location.Version, changeType), new OutboxContext(correlation.CorrelationId, correlation.CausationId, location.OrganizationId, actor.ActorId), cancellationToken);
    }
}

public sealed class UpdateFiscalLocationUseCase
{
    private readonly IFiscalLocationRepository _locations; private readonly ITransactionManager _transactions; private readonly IUnitOfWork _unitOfWork; private readonly IIdempotencyStore _idempotency; private readonly IAuditWriter _audit; private readonly IOutboxWriter _outbox; private readonly IActorContextAccessor _actors; private readonly ICorrelationContextAccessor _correlations;
    public UpdateFiscalLocationUseCase(IFiscalLocationRepository locations, ITransactionManager transactions, IUnitOfWork unitOfWork, IIdempotencyStore idempotency, IAuditWriter audit, IOutboxWriter outbox, IActorContextAccessor actors, ICorrelationContextAccessor correlations)
    { _locations = locations; _transactions = transactions; _unitOfWork = unitOfWork; _idempotency = idempotency; _audit = audit; _outbox = outbox; _actors = actors; _correlations = correlations; }

    public Task<OrganizationMutationResult> ExecuteAsync(UpdateFiscalLocationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command); OrganizationAuthorization.EnsureManage(_actors.Current, command.OrganizationId); ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey); ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);
        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow; var actor = _actors.Current; var correlation = _correlations.Current; var scope = $"organization.location.update:{command.OrganizationId}:{command.LocationId}";
            var reservation = await UpsertCompanyFiscalProfileUseCase.ReserveAsync(_idempotency, scope, command.IdempotencyKey, command.RequestHash, actor.ActorId, correlation.CorrelationId, now, ct);
            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayed = await _locations.GetAsync(command.OrganizationId, command.LocationId, ct) ?? throw UpsertCompanyFiscalProfileUseCase.MissingReplay("fiscal location");
                return new OrganizationMutationResult(replayed.Id, replayed.Version, true);
            }
            UpsertCompanyFiscalProfileUseCase.EnsureNewReservation(reservation); await _unitOfWork.SaveChangesAsync(ct);
            var location = await _locations.GetAsync(command.OrganizationId, command.LocationId, ct) ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "organization.location_not_found", "The requested fiscal location was not found.");
            if (await _locations.BranchCodeExistsAsync(command.OrganizationId, command.DgiBranchCode, command.LocationId, ct)) throw CreateFiscalLocationUseCase.DuplicateBranch();
            try { location.Update(command.Name, command.DgiBranchCode, command.FiscalAddress, command.City, command.Department, command.Active, command.ExpectedVersion); }
            catch (DomainRuleException ex)
            {
                if (ex.Code == "concurrency.stale_version") throw new ApplicationProblemException(ApplicationProblemKind.Conflict, "concurrency_conflict", ex.Message, conflictType: "stale_version", currentVersion: location.Version.ToString());
                throw new ApplicationProblemException(ApplicationProblemKind.Validation, ex.Code, ex.Message);
            }
            await _locations.SaveAsync(location, ct); await CreateFiscalLocationUseCase.AppendEvidenceAsync(location, "ORGANIZATION_LOCATION_UPDATED", "updated", now, actor, correlation, _audit, _outbox, ct);
            await _idempotency.CompleteAsync(new IdempotencyCompletion(scope, command.IdempotencyKey, command.RequestHash, "updated", "FiscalLocation", location.Id, correlation.CorrelationId, now), ct);
            await _unitOfWork.SaveChangesAsync(ct); return new OrganizationMutationResult(location.Id, location.Version, false);
        }, cancellationToken);
    }
}
