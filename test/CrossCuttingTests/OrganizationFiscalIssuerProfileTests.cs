using EFactura.Domain.Common;
using EFactura.Domain.Organizations;
using Xunit;

namespace CrossCuttingTests;

public sealed class OrganizationFiscalIssuerProfileTests
{
    [Fact]
    public void Company_profile_preserves_structural_RUC_and_uses_optimistic_version()
    {
        var company = CompanyFiscalProfile.Create("company-1", "211234560019", "Empresa de Prueba S.A.", "Empresa");
        Assert.Equal("211234560019", company.Ruc); Assert.Equal(1, company.Version);
        company.Update("211234560019", "Empresa de Prueba S.A.", "Empresa Uy", 1);
        Assert.Equal(2, company.Version); Assert.Equal("Empresa Uy", company.CommercialName);
        var stale = Assert.Throws<DomainRuleException>(() => company.Update("211234560019", "Empresa de Prueba S.A.", "Otro", 1));
        Assert.Equal("concurrency.stale_version", stale.Code);
    }

    [Theory]
    [InlineData("21123456001")]
    [InlineData("21123456001X")]
    [InlineData("")]
    public void Company_profile_rejects_non_structural_RUC(string ruc)
    {
        var error = Assert.Throws<DomainRuleException>(() => CompanyFiscalProfile.Create("company-1", ruc, "Empresa de Prueba S.A.", null));
        Assert.Contains("ruc", error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void Fiscal_location_preserves_four_digit_DGI_branch_code_and_version()
    {
        var location = FiscalLocation.Create("loc-1", "company-1", "Casa Central", "0001", "Av. 18 de Julio 1234", "Montevideo", "Montevideo");
        Assert.Equal("0001", location.DgiBranchCode); Assert.True(location.Active); Assert.Equal(1, location.Version);
        location.Update("Casa Central", "0001", "Av. 18 de Julio 1234", "Montevideo", "Montevideo", false, 1);
        Assert.False(location.Active); Assert.Equal(2, location.Version);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("001")]
    [InlineData("00001")]
    [InlineData("00A1")]
    public void Fiscal_location_rejects_invalid_DGI_branch_code(string branchCode)
    {
        var error = Assert.Throws<DomainRuleException>(() => FiscalLocation.Create("loc-1", "company-1", "Casa Central", branchCode, "Av. 18 de Julio 1234", "Montevideo", "Montevideo"));
        Assert.Equal("organization.location.branch_code_invalid", error.Code);
    }
}
