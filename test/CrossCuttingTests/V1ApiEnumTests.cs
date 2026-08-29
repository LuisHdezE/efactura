using EFactura.Application.Common.Errors;
using EFactura.Domain.Sales;
using WebApi.CrossCutting.Requests;
using Xunit;

namespace CrossCuttingTests;

public sealed class V1ApiEnumTests
{
    [Theory]
    [InlineData("CONSUMER_FINAL", SaleCommercialIntent.ConsumerFinal)]
    [InlineData("TAXPAYER_INVOICE", SaleCommercialIntent.TaxpayerInvoice)]
    [InlineData("EXPORT", SaleCommercialIntent.Export)]
    public void Sales_intents_accept_the_public_upper_snake_case_contract(
        string value,
        SaleCommercialIntent expected)
    {
        var parsed = V1ApiEnum.Parse<SaleCommercialIntent>(
            value,
            "sales.invalid_intent",
            "Invalid sale intent.");

        Assert.Equal(expected, parsed);
    }

    [Theory]
    [InlineData("ENTIRELY_IN_URUGUAY", SaleServicePerformanceScope.EntirelyInUruguay)]
    [InlineData("ENTIRELY_OUTSIDE_URUGUAY", SaleServicePerformanceScope.EntirelyOutsideUruguay)]
    [InlineData("UNKNOWN_OR_MIXED", SaleServicePerformanceScope.UnknownOrMixed)]
    public void Service_scope_accepts_the_public_upper_snake_case_contract(
        string value,
        SaleServicePerformanceScope expected)
    {
        var parsed = V1ApiEnum.Parse<SaleServicePerformanceScope>(
            value,
            "sales.invalid_service_performance_scope",
            "Invalid scope.");

        Assert.Equal(expected, parsed);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("CONSUMER-FINAL")]
    [InlineData("CONSUMER FINAL")]
    [InlineData("NOT_A_REAL_VALUE")]
    public void Public_enum_parser_rejects_numeric_and_non_contract_values(string value)
    {
        var problem = Assert.Throws<ApplicationProblemException>(() =>
            V1ApiEnum.Parse<SaleCommercialIntent>(
                value,
                "sales.invalid_intent",
                "Invalid sale intent."));

        Assert.Equal(ApplicationProblemKind.Validation, problem.Kind);
        Assert.Equal("sales.invalid_intent", problem.Code);
    }
}
