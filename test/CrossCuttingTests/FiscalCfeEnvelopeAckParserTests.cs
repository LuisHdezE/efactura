using EFactura.Application.Fiscal;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalCfeEnvelopeAckParserTests
{
    private const string DgiNamespace = "http://cfe.dgi.gub.uy";
    private readonly DgiFiscalCfeEnvelopeAckParser _parser = new();

    [Fact]
    public void AS_preserves_DGI_receiver_and_consultation_parameters_without_CFE_acceptance_semantics()
    {
        var result = _parser.Parse(Ack("AS", reasons: null, includeConsultation: true));

        Assert.True(result.IsValid);
        Assert.Equal(FiscalCfeEnvelopeAckState.Received, result.State);
        Assert.Equal("219999830019", result.ReceiverRut);
        Assert.Equal("214748364700", result.IssuerRuc);
        Assert.Equal(5001, result.DgiResponseId);
        Assert.Equal(4101, result.SenderEnvelopeId);
        Assert.Equal(9001, result.DgiReceiverId);
        Assert.Equal(1, result.CfeCount);
        Assert.Equal("2026-09-13T03:15:00", result.ReceptionTimestampText);
        Assert.Equal("2026-09-13T03:15:01", result.SigningTimestampText);
        Assert.Equal("dGVzdC10b2tlbg==", result.ConsultationToken);
        Assert.Equal("2026-09-13T03:20:00", result.ConsultationAvailableAtText);
        Assert.Empty(result.RejectionReasons);
    }

    [Fact]
    public void BS_preserves_S08_as_rejection_evidence_without_recovery_semantics()
    {
        var result = _parser.Parse(Ack(
            "BS",
            "<MotivosRechazo><Motivo>S08</Motivo><Glosa>Sobre enviado ya existe</Glosa><Detalle>Duplicado DGI</Detalle></MotivosRechazo>",
            includeConsultation: false));

        Assert.True(result.IsValid);
        Assert.Equal(FiscalCfeEnvelopeAckState.Rejected, result.State);
        var reason = Assert.Single(result.RejectionReasons);
        Assert.Equal("S08", reason.Code);
        Assert.Equal("Sobre enviado ya existe", reason.Glosa);
        Assert.Equal("Duplicado DGI", reason.Detail);
    }

    [Fact]
    public void DGI_boundary_rejects_receiver_only_S20_reason()
    {
        var result = _parser.Parse(Ack(
            "BS",
            "<MotivosRechazo><Motivo>S20</Motivo><Glosa>Duplicado receptor</Glosa></MotivosRechazo>",
            includeConsultation: false));

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.envelope.ack.reasons_invalid", result.FailureCode);
    }

    [Fact]
    public void AS_with_rejection_reasons_fails_closed()
    {
        var result = _parser.Parse(Ack(
            "AS",
            "<MotivosRechazo><Motivo>S01</Motivo><Glosa>Formato</Glosa></MotivosRechazo>",
            includeConsultation: true));

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.envelope.ack.reasons_invalid", result.FailureCode);
    }

    [Fact]
    public void Missing_DGI_signature_fails_structural_validation_without_claiming_crypto_validation()
    {
        var xml = Ack("AS", null, includeConsultation: true)
            .Replace("<ds:Signature xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\"><ds:SignedInfo /></ds:Signature>", string.Empty, StringComparison.Ordinal);

        var result = _parser.Parse(xml);

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.envelope.ack.structure_invalid", result.FailureCode);
    }

    [Fact]
    public void DTD_is_prohibited()
    {
        var xml = $"<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///etc/passwd'>]><ACKSobre xmlns=\"{DgiNamespace}\"><Caratula>&e;</Caratula></ACKSobre>";

        var result = _parser.Parse(xml);

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.envelope.ack.xml_invalid", result.FailureCode);
    }

    private static string Ack(string state, string? reasons, bool includeConsultation)
    {
        var consultation = includeConsultation
            ? "<ParamConsulta><Token>dGVzdC10b2tlbg==</Token><FechaHora>2026-09-13T03:20:00</FechaHora></ParamConsulta>"
            : string.Empty;
        return $"<ACKSobre xmlns=\"{DgiNamespace}\"><Caratula><RUCReceptor>219999830019</RUCReceptor><RUCEmisor>214748364700</RUCEmisor><IDRespuesta>5001</IDRespuesta><NomArch/><FecHRecibido>2026-09-13T03:15:00</FecHRecibido><IdEmisor>4101</IdEmisor><IDReceptor>9001</IDReceptor><CantidadCFE>1</CantidadCFE><Tmst>2026-09-13T03:15:01</Tmst></Caratula><Detalle><Estado>{state}</Estado>{consultation}{reasons}</Detalle><ds:Signature xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\"><ds:SignedInfo /></ds:Signature></ACKSobre>";
    }
}
