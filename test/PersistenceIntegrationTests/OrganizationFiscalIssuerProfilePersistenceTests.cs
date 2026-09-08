using EFactura.Application.Common.Errors;
using EFactura.Domain.Organizations;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write.Repositories;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class OrganizationFiscalIssuerProfilePersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Company_and_fiscal_location_round_trip_with_leading_zero_branch_code(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider); if (database is null) return;
        await using (var context = database.CreateContext())
        {
            var repository = new EfOrganizationRepository(context); var unitOfWork = new EfUnitOfWork(context);
            await repository.AddAsync(CompanyFiscalProfile.Create("company-1", "211234560019", "Empresa de Prueba S.A.", "Empresa"));
            await repository.AddAsync(FiscalLocation.Create("loc-1", "company-1", "Casa Central", "0001", "Av. 18 de Julio 1234", "Montevideo", "Montevideo"));
            await unitOfWork.SaveChangesAsync();
        }
        await using (var context = database.CreateContext())
        {
            var repository = new EfOrganizationRepository(context); var company = await repository.GetAsync("company-1"); var location = await repository.GetAsync("company-1", "loc-1");
            Assert.NotNull(company); Assert.Equal("211234560019", company!.Ruc); Assert.Equal(1, company.Version); Assert.NotNull(location); Assert.Equal("0001", location!.DgiBranchCode); Assert.True(location.Active); Assert.Equal(1, location.Version);
        }
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Stale_company_update_is_translated_to_portable_concurrency_conflict(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider); if (database is null) return;
        await using (var seedContext = database.CreateContext())
        {
            var seedRepository = new EfOrganizationRepository(seedContext); await seedRepository.AddAsync(CompanyFiscalProfile.Create("company-1", "211234560019", "Empresa de Prueba S.A.", null)); await new EfUnitOfWork(seedContext).SaveChangesAsync();
        }
        CompanyFiscalProfile first; CompanyFiscalProfile second;
        await using (var firstReadContext = database.CreateContext()) await using (var secondReadContext = database.CreateContext())
        {
            first = (await new EfOrganizationRepository(firstReadContext).GetAsync("company-1"))!; second = (await new EfOrganizationRepository(secondReadContext).GetAsync("company-1"))!;
        }
        first.Update("211234560019", "Empresa Uno S.A.", null, 1); second.Update("211234560019", "Empresa Dos S.A.", null, 1);
        await using (var firstWriteContext = database.CreateContext())
        {
            var repository = new EfOrganizationRepository(firstWriteContext); await repository.SaveAsync(first); await new EfUnitOfWork(firstWriteContext).SaveChangesAsync();
        }
        await using (var secondWriteContext = database.CreateContext())
        {
            var repository = new EfOrganizationRepository(secondWriteContext);
            var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => repository.SaveAsync(second));
            Assert.Equal(ApplicationProblemKind.Conflict, error.Kind); Assert.Equal("concurrency_conflict", error.Code); Assert.Equal("stale_version", error.ConflictType); Assert.Equal("2", error.CurrentVersion);
        }
        await using (var verifyContext = database.CreateContext())
        {
            var persisted = await new EfOrganizationRepository(verifyContext).GetAsync("company-1");
            Assert.NotNull(persisted); Assert.Equal("Empresa Uno S.A.", persisted!.LegalName); Assert.Equal(2, persisted.Version);
        }
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Duplicate_DGI_branch_code_is_rejected_portably(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider); if (database is null) return;
        await using (var seedContext = database.CreateContext())
        {
            var repository = new EfOrganizationRepository(seedContext); await repository.AddAsync(CompanyFiscalProfile.Create("company-1", "211234560019", "Empresa de Prueba S.A.", null)); await repository.AddAsync(Location("loc-1", "0001")); await new EfUnitOfWork(seedContext).SaveChangesAsync();
        }
        await using (var context = database.CreateContext())
        {
            var repository = new EfOrganizationRepository(context); await repository.AddAsync(Location("loc-2", "0001"));
            var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => new EfUnitOfWork(context).SaveChangesAsync());
            Assert.Equal(ApplicationProblemKind.Conflict, error.Kind); Assert.Equal("organization.location.branch_code_duplicate", error.Code); Assert.Equal("duplicate_branch_code", error.ConflictType);
        }
    }

    private static FiscalLocation Location(string id, string branchCode) => FiscalLocation.Create(id, "company-1", $"Sucursal {id}", branchCode, "Av. 18 de Julio 1234", "Montevideo", "Montevideo");
}
