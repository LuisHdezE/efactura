using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Security;
using EFactura.Domain.Common;
using EFactura.Domain.Sales;

namespace EFactura.Application.Sales;

public sealed record CancelSaleCommand(
    string OrganizationId,
    Guid SaleId,
    long ExpectedVersion,
    string OperatorReason,
    string? OperatorContext,
    string IdempotencyKey,
    string RequestHash);

public sealed record SaleCancellationResult(
    Guid SaleId,
    long Version,
    SaleStatus Status,
    bool Replayed);

public sealed record SaleCancelledIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SaleId,
    string OrganizationId,
    SaleStatus PreviousStatus,
    long Version) : IIntegrationEvent;

/// <summary>
/// Owns the complete local atomic boundary for cancelling a pre-confirmation Sale.
/// Cancellation is terminal but is not a reversal workflow. Confirmed financial,
/// stock and fiscal effects are deliberately outside this use case.
/// </summary>
public sealed class CancelSaleUseCase
{
    private readonly ISaleRepository _sales;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public CancelSaleUseCase(
        ISaleRepository sales,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _sales = sales;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<SaleCancellationResult> ExecuteAsync(
        CancelSaleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        SalesAuthorization.Ensure(_actors, command.OrganizationId, Permissions.SalesCancel);
        ValidateCommand(command);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var scope = $"sales.cancel:{command.OrganizationId}:{command.SaleId}";
            var reservation = await _idempotency.TryReserveAsync(
                new IdempotencyReservation(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    _actors.Current.ActorId,
                    _correlations.Current.CorrelationId,
                    now.AddMinutes(10)),
                ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
                return await ReplayAsync(command.OrganizationId, command.SaleId, ct);

            if (reservation.Status != IdempotencyReservationStatus.Acquired)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    reservation.Status == IdempotencyReservationStatus.PayloadMismatch
                        ? "idempotency_key_reused"
                        : "idempotency_in_progress",
                    "The sale-cancellation idempotency reservation could not be acquired.",
                    conflictType: reservation.Status == IdempotencyReservationStatus.ExistingInProgress
                        ? "in_progress"
                        : null,
                    retryAfterSeconds: reservation.Status == IdempotencyReservationStatus.ExistingInProgress
                        ? 2
                        : null);
            }

            // Persist the reservation inside the same local transaction. Any later exception rolls it back.
            await _unitOfWork.SaveChangesAsync(ct);

            var sale = await _sales.GetAsync(command.OrganizationId, command.SaleId, ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.NotFound,
                    "sales.not_found",
                    "Sale was not found.");
            EnsureSaleScope(_actors.Current, sale);

            // Locked conflict precedence: terminal/irreversible state wins over stale-version hints.
            if (sale.Status == SaleStatus.Confirmed)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    "sales.cancellation.irreversible_boundary_crossed",
                    "A confirmed sale cannot be cancelled through the pre-confirmation cancellation workflow.",
                    conflictType: "irreversible_boundary",
                    currentVersion: sale.Version.ToString());
            }
            if (sale.Status == SaleStatus.Cancelled)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    "sales.already_cancelled",
                    "The sale is already cancelled and is terminal.",
                    conflictType: "invalid_state",
                    currentVersion: sale.Version.ToString());
            }
            if (sale.Version != command.ExpectedVersion)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    "concurrency.stale_version",
                    "The sale changed before cancellation could start.",
                    conflictType: "stale_version",
                    currentVersion: sale.Version.ToString());
            }

            var previousStatus = sale.Status;
            try
            {
                sale.MarkCancelled(command.ExpectedVersion);
            }
            catch (DomainRuleException ex)
            {
                throw DomainProblem(ex, sale.Version);
            }

            await _sales.SaveAsync(sale, ct);

            var reason = command.OperatorReason.Trim();
            var operatorContext = Optional(command.OperatorContext, 1000);
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(),
                now,
                "SALE_CANCELLED",
                _actors.Current.ActorId,
                sale.OrganizationId,
                sale.LocationId,
                sale.TerminalId,
                "Sale",
                sale.Id.ToString(),
                AuditOutcome.Succeeded,
                _correlations.Current.CorrelationId,
                null,
                new Dictionary<string, string?>
                {
                    ["previousStatus"] = previousStatus.ToString(),
                    ["newStatus"] = sale.Status.ToString(),
                    ["operatorReason"] = reason,
                    ["operatorContext"] = operatorContext,
                    ["version"] = sale.Version.ToString()
                }), ct);

            await _outbox.EnqueueAsync(
                new SaleCancelledIntegrationEvent(
                    Guid.NewGuid(),
                    now,
                    sale.Id,
                    sale.OrganizationId,
                    previousStatus,
                    sale.Version),
                new OutboxContext(
                    _correlations.Current.CorrelationId,
                    OrganizationId: sale.OrganizationId,
                    ActorId: _actors.Current.ActorId),
                ct);

            await _idempotency.CompleteAsync(new IdempotencyCompletion(
                scope,
                command.IdempotencyKey,
                command.RequestHash,
                "sale_cancelled",
                "Sale",
                sale.Id.ToString(),
                _correlations.Current.CorrelationId,
                now), ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return new SaleCancellationResult(
                sale.Id,
                sale.Version,
                sale.Status,
                false);
        }, cancellationToken);
    }

    private async Task<SaleCancellationResult> ReplayAsync(
        string organizationId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        var sale = await _sales.GetAsync(organizationId, saleId, cancellationToken)
            ?? throw ReplayConflict("The completed cancellation sale no longer exists.");
        EnsureSaleScope(_actors.Current, sale);
        if (sale.Status != SaleStatus.Cancelled)
            throw ReplayConflict("The completed cancellation idempotency record no longer matches a cancelled sale.");

        return new SaleCancellationResult(sale.Id, sale.Version, sale.Status, true);
    }

    private static void ValidateCommand(CancelSaleCommand command)
    {
        if (command.SaleId == Guid.Empty)
            throw Validation("sales.sale_id_required", "Sale id is required.");
        if (command.ExpectedVersion <= 0)
            throw Validation("sales.expected_version_invalid", "Expected sale version must be positive.");
        if (string.IsNullOrWhiteSpace(command.OperatorReason))
            throw Validation("sales.cancellation.operator_reason_required", "Operator reason is required for cancellation auditability.");
        if (command.OperatorReason.Trim().Length > 500)
            throw Validation("sales.cancellation.operator_reason_too_long", "Operator reason cannot exceed 500 characters.");
        _ = Optional(command.OperatorContext, 1000);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);
    }

    private static void EnsureSaleScope(ActorContext actor, Sale sale)
    {
        if (!string.IsNullOrWhiteSpace(sale.LocationId)
            && !actor.LocationScopes.Contains(sale.LocationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "location_scope_denied",
                "The actor is outside the sale location scope.");
        }

        if (!string.IsNullOrWhiteSpace(sale.TerminalId)
            && actor.TerminalScopes.Count > 0
            && !actor.TerminalScopes.Contains(sale.TerminalId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "terminal_scope_denied",
                "The actor is outside the sale terminal scope.");
        }
    }

    private static ApplicationProblemException DomainProblem(DomainRuleException ex, long currentVersion) =>
        ex.Code switch
        {
            "sales.cancellation.irreversible_boundary_crossed" => new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                ex.Code,
                ex.Message,
                conflictType: "irreversible_boundary",
                currentVersion: currentVersion.ToString()),
            "sales.already_cancelled" => new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                ex.Code,
                ex.Message,
                conflictType: "invalid_state",
                currentVersion: currentVersion.ToString()),
            "concurrency.stale_version" => new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                ex.Code,
                ex.Message,
                conflictType: "stale_version",
                currentVersion: currentVersion.ToString()),
            _ => new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                ex.Code,
                ex.Message)
        };

    private static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    private static ApplicationProblemException ReplayConflict(string message) =>
        new(
            ApplicationProblemKind.Conflict,
            "idempotency.missing_completed_resource",
            message,
            conflictType: "inconsistent_replay");

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > max)
        {
            throw Validation(
                "sales.cancellation.operator_context_too_long",
                $"Operator context cannot exceed {max} characters.");
        }
        return normalized;
    }
}
