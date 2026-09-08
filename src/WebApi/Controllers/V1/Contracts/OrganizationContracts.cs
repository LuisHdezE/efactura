namespace WebApi.Controllers.V1.Contracts;

public sealed record CompanyFiscalProfileUpdateRequest(string Ruc, string LegalName, string? CommercialName, long? ExpectedVersion = null);
public sealed record CompanyFiscalProfileDto(string OrganizationId, string Ruc, string LegalName, string? CommercialName, long Version);
public sealed record FiscalLocationCreateRequest(string Name, string DgiBranchCode, string FiscalAddress, string City, string Department);
public sealed record FiscalLocationUpdateRequest(string Name, string DgiBranchCode, string FiscalAddress, string City, string Department, bool Active, long ExpectedVersion);
public sealed record FiscalLocationDto(string Id, string OrganizationId, string Name, string DgiBranchCode, string FiscalAddress, string City, string Department, bool Active, long Version);
