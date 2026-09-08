using EFactura.Application.Common.Errors;
using EFactura.Application.Organizations;
using EFactura.Application.Parties;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Parties;
using EFactura.Domain.Sales;

namespace EFactura.Application.Fiscal;

public interface IFiscalContentSnapshotFactory
{
    Task<FiscalContentSnapshot> CreateAsync(
        FiscalizationRequest request,
        Sale confirmedSale,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Freezes mutable issuer/receiver master data and enriches already-frozen fiscal calculation
/// evidence with immutable confirmed-sale line content. It does not build XML, sign artifacts or
/// invoke DGI/provider transports.
/// </summary>
public sealed class FiscalContentSnapshotFactory : IFiscalContentSnapshotFactory
{
    private readonly ICompanyFiscalProfileRepository _companies;
    private readonly IFiscalLocationRepository _locations;
    private readonly IPartyRepository _parties;

    public FiscalContentSnapshotFactory(
        ICompanyFiscalProfileRepository companies,
        IFiscalLocationRepository locations,
        IPartyRepository parties)
    {
        _companies = companies;
        _locations = locations;
        _parties = parties;
    }

    public async Task<FiscalContentSnapshot> CreateAsync(
        FiscalizationRequest request,
        Sale confirmedSale,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(confirmedSale);

        var evidence = request.ConfirmationEvidence
            ?? throw MissingPrerequisite(
                "fiscal.snapshot.confirmation_evidence_missing",
                "Fiscal content snapshot requires fiscal calculation evidence captured at sale confirmation.");
        evidence.EnsureIntegrity();
        EnsureSaleMatches(request, confirmedSale);

        var company = await _companies.GetAsync(request.OrganizationId, cancellationToken)
            ?? throw MissingPrerequisite(
                "fiscal.snapshot.issuer_profile_missing",
                "Fiscal content snapshot requires the organization fiscal issuer profile.");

        if (string.IsNullOrWhiteSpace(request.LocationId))
            throw MissingPrerequisite(
                "fiscal.snapshot.location_required",
                "Fiscal content snapshot requires the authoritative fiscal location used by the sale.");

        var location = await _locations.GetAsync(request.OrganizationId, request.LocationId, cancellationToken)
            ?? throw MissingPrerequisite(
                "fiscal.snapshot.location_missing",
                "The fiscal location referenced by the confirmed sale no longer exists.");

        if (!string.Equals(company.OrganizationId, request.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(location.OrganizationId, request.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(location.Id, request.LocationId, StringComparison.Ordinal))
        {
            throw Inconsistent(
                "fiscal.snapshot.issuer_scope_mismatch",
                "Issuer master data no longer matches the fiscalization organization/location scope.");
        }

        var issuer = new FiscalIssuerContentSnapshot(
            company.Ruc,
            company.LegalName,
            company.CommercialName,
            company.Version,
            location.Id,
            location.DgiBranchCode,
            location.FiscalAddress,
            location.City,
            location.Department,
            location.Version);

        var receiver = await BuildReceiverAsync(request, confirmedSale, cancellationToken);
        var lines = BuildLines(confirmedSale, evidence);

        try
        {
            return FiscalContentSnapshot.Create(
                request.OrganizationId,
                request.SaleId,
                request.CfeFamily,
                request.ReceiverIdentification,
                request.FormatVersion,
                confirmedSale.EffectiveOn,
                request.ConfirmationFingerprint,
                request.SettlementFingerprint,
                issuer,
                receiver,
                lines,
                evidence);
        }
        catch (DomainRuleException ex)
        {
            throw Inconsistent(ex.Code, ex.Message);
        }
    }

    private async Task<FiscalReceiverContentSnapshot?> BuildReceiverAsync(
        FiscalizationRequest request,
        Sale sale,
        CancellationToken cancellationToken)
    {
        if (!sale.CustomerPartyId.HasValue)
        {
            if (request.ReceiverIdentification == ReceiverIdentificationRequirement.Required)
            {
                throw MissingPrerequisite(
                    "fiscal.snapshot.receiver_required",
                    "The selected CFE requires a receiver but the confirmed sale has no customer Party.");
            }

            return null;
        }

        var party = await _parties.GetAsync(request.OrganizationId, sale.CustomerPartyId.Value, cancellationToken)
            ?? throw MissingPrerequisite(
                "fiscal.snapshot.receiver_missing",
                "The Party referenced by the confirmed sale no longer exists.");

        var identificationRequired = request.ReceiverIdentification == ReceiverIdentificationRequirement.Required;
        var identity = SelectIdentity(party, request.CfeFamily, sale.EffectiveOn, identificationRequired);
        if (identificationRequired && identity is null)
        {
            throw MissingPrerequisite(
                "fiscal.snapshot.receiver_identity_missing",
                "The selected CFE requires a compatible receiver fiscal identity.");
        }

        PartyAddress? address = null;
        if (request.CfeFamily is CfeFamily.EFactura or CfeFamily.EFacturaExportacion)
            address = SelectRequiredAddress(party);

        return new FiscalReceiverContentSnapshot(
            party.Id,
            party.Name,
            party.ResidenceCountry,
            party.TaxResidenceCountry,
            party.Version,
            identity is null
                ? null
                : new FiscalReceiverIdentitySnapshot(identity.TypeCode, identity.Number, identity.IssuingCountry),
            address is null
                ? null
                : new FiscalReceiverAddressSnapshot(
                    address.Id,
                    MapAddressKind(address.Kind),
                    address.AddressLine,
                    address.City,
                    address.Region,
                    address.CountryCode,
                    address.PostalCode));
    }

    private static PartyFiscalIdentity? SelectIdentity(
        Party party,
        CfeFamily family,
        DateOnly effectiveOn,
        bool required)
    {
        var candidates = party.FiscalIdentities
            .Where(identity => identity.Active
                               && (!identity.ValidFrom.HasValue || identity.ValidFrom.Value <= effectiveOn)
                               && (!identity.ValidTo.HasValue || effectiveOn <= identity.ValidTo.Value))
            .Where(identity => IsCompatible(identity, family))
            .OrderBy(identity => identity.TypeCode, StringComparer.Ordinal)
            .ThenBy(identity => identity.IssuingCountry, StringComparer.Ordinal)
            .ThenBy(identity => identity.Number, StringComparer.Ordinal)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        if (!required)
            return candidates.Length == 1 ? candidates[0] : null;

        if (candidates.Length > 1)
        {
            throw MissingPrerequisite(
                "fiscal.snapshot.receiver_identity_ambiguous",
                "More than one receiver fiscal identity is eligible and no accepted prior selection identifies one authoritative value.");
        }

        return candidates[0];
    }

    private static bool IsCompatible(PartyFiscalIdentity identity, CfeFamily family) => family switch
    {
        CfeFamily.EFactura => identity.TypeCode == "2" && identity.IssuingCountry == "UY",
        CfeFamily.ETicket => identity.TypeCode switch
        {
            "1" or "2" or "3" => identity.IssuingCountry == "UY",
            "4" or "5" or "7" => true,
            "6" => identity.IssuingCountry is "AR" or "BR" or "CL" or "PY",
            _ => false
        },
        // The accepted export selection path requires identification but does not yet encode a
        // narrower authoritative identity-type policy. Multiple active candidates therefore fail
        // closed instead of inventing a preference.
        CfeFamily.EFacturaExportacion => true,
        _ => false
    };

    private static PartyAddress SelectRequiredAddress(Party party)
    {
        var addresses = party.Addresses.OrderBy(address => address.Id).ToArray();
        if (addresses.Length == 0)
        {
            throw MissingPrerequisite(
                "fiscal.snapshot.receiver_address_missing",
                "The selected CFE requires receiver domicile content but the Party has no address to snapshot.");
        }

        var primaryFiscal = addresses
            .Where(address => address.Primary && address.Kind == PartyAddressKind.Fiscal)
            .ToArray();
        if (primaryFiscal.Length == 1)
            return primaryFiscal[0];
        if (primaryFiscal.Length > 1)
            throw AmbiguousAddress();

        var primary = addresses.Where(address => address.Primary).ToArray();
        if (primary.Length == 1)
            return primary[0];
        if (primary.Length > 1)
            throw AmbiguousAddress();

        var fiscal = addresses.Where(address => address.Kind == PartyAddressKind.Fiscal).ToArray();
        if (fiscal.Length == 1)
            return fiscal[0];
        if (fiscal.Length > 1)
            throw AmbiguousAddress();

        if (addresses.Length == 1)
            return addresses[0];

        throw AmbiguousAddress();
    }

    private static ApplicationProblemException AmbiguousAddress() =>
        MissingPrerequisite(
            "fiscal.snapshot.receiver_address_ambiguous",
            "More than one receiver address could supply domicile content and no accepted prior selection identifies one authoritative value.");

    private static FiscalReceiverAddressKind MapAddressKind(PartyAddressKind kind) => kind switch
    {
        PartyAddressKind.Fiscal => FiscalReceiverAddressKind.Fiscal,
        PartyAddressKind.Delivery => FiscalReceiverAddressKind.Delivery,
        PartyAddressKind.Other => FiscalReceiverAddressKind.Other,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static IReadOnlyCollection<FiscalContentLineSnapshot> BuildLines(
        Sale sale,
        FiscalConfirmationEvidence evidence)
    {
        var fiscalByLine = evidence.Lines.ToDictionary(line => line.LineId);
        var result = new List<FiscalContentLineSnapshot>(sale.Lines.Count);
        var sequence = 0;

        foreach (var saleLine in sale.Lines)
        {
            if (!fiscalByLine.TryGetValue(saleLine.Id, out var fiscal))
            {
                throw Inconsistent(
                    "fiscal.snapshot.line_evidence_missing",
                    "Frozen fiscal calculation evidence is missing a confirmed sale line.");
            }

            result.Add(new FiscalContentLineSnapshot(
                ++sequence,
                saleLine.Id,
                saleLine.ItemId,
                saleLine.ItemCode,
                saleLine.ItemName,
                saleLine.Kind == SaleLineKind.Product ? FiscalContentLineKind.Goods : FiscalContentLineKind.Service,
                saleLine.Quantity,
                saleLine.UnitPrice,
                0m,
                0m,
                fiscal));
        }

        if (fiscalByLine.Count != result.Count)
        {
            throw Inconsistent(
                "fiscal.snapshot.line_evidence_unexpected",
                "Frozen fiscal calculation evidence contains a line that is not present in the confirmed sale.");
        }

        return result;
    }

    private static void EnsureSaleMatches(FiscalizationRequest request, Sale sale)
    {
        if (sale.Id != request.SaleId
            || !string.Equals(sale.OrganizationId, request.OrganizationId, StringComparison.Ordinal)
            || sale.Status != SaleStatus.Confirmed
            || !string.Equals(sale.LocationId, request.LocationId, StringComparison.Ordinal)
            || !string.Equals(sale.ConfirmationFingerprint, request.ConfirmationFingerprint, StringComparison.Ordinal)
            || !string.Equals(sale.SettlementFingerprint, request.SettlementFingerprint, StringComparison.Ordinal))
        {
            throw Inconsistent(
                "fiscal.snapshot.source_sale_mismatch",
                "The source sale no longer matches the immutable fiscalization evidence.");
        }
    }

    private static ApplicationProblemException MissingPrerequisite(string code, string message) =>
        new(
            ApplicationProblemKind.Conflict,
            code,
            message,
            conflictType: "missing_prerequisite");

    private static ApplicationProblemException Inconsistent(string code, string message) =>
        new(
            ApplicationProblemKind.Conflict,
            code,
            message,
            conflictType: "inconsistent_state");
}
