using EFactura.Application.Fiscal;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportBrAckEvidenceTests
{
    private readonly DgiFiscalDailyReportBrAckEvidenceParser _parser = new();

    [Fact]
    public void BR_rejection_reasons_are_preserved_as_typed_evidence()
    {
        var result = _parser.Parse(Ack("R04", "No cumple validaciones según Formato de Reporte", "Detalle fiscal"));

        Assert.True(result.IsValid);
        var reason = Assert.Single(result.Reasons);
        Assert.Equal("R04", reason.Code);
        Assert.Equal("No cumple validaciones según Formato de Reporte", reason.Glosa);
        Assert.Equal("Detalle fiscal", reason.Detail);
        Assert.False(FiscalDailyReportRejectionReasonEvidence.ContainsR05(result.Reasons));
    }

    [Fact]
    public void R05_is_recognized_as_sequence_reconciliation_evidence()
    {
        var result = _parser.Parse(Ack("R05", "La secuencia indicada en el reporte no es correcta", null));

        Assert.True(result.IsValid);
        Assert.True(FiscalDailyReportRejectionReasonEvidence.ContainsR05(result.Reasons));
    }

    [Fact]
    public void BR_without_rejection_reasons_fails_closed()
    {
        var result = _parser.Parse(
            "<ACKRepDiario xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><IDReceptor>1</IDReceptor></Caratula><Detalle><Estado>BR</Estado></Detalle></ACKRepDiario>");

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.daily_report.br_ack.reasons_required", result.FailureCode);
    }

    [Theory]
    [InlineData("R00")]
    [InlineData("R07")]
    [InlineData("r04")]
    public void Unknown_or_noncanonical_rejection_code_fails_closed(string code)
    {
        var result = _parser.Parse(Ack(code, "glosa", null));

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.daily_report.br_ack.reasons_invalid", result.FailureCode);
    }

    private static string Ack(string code, string glosa, string? detail)
    {
        var detailXml = detail is null ? string.Empty : $"<Detalle>{detail}</Detalle>";
        return $"<ACKRepDiario xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><IDReceptor>receiver</IDReceptor></Caratula><Detalle><Estado>BR</Estado><MotivosRechazo><Motivo>{code}</Motivo><Glosa>{glosa}</Glosa>{detailXml}</MotivosRechazo></Detalle></ACKRepDiario>";
    }
}
