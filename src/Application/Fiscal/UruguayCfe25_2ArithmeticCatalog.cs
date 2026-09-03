using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;

namespace EFactura.Application.Fiscal;

public static class UruguayCfe25_2ArithmeticCatalog
{
    public const string PackVersion = "UY-CFE-ARITH-25.2-R1-2026.09.03";
    public const string FormatVersion = "25.2";
    public static readonly DateOnly SupportedFrom = new(2026, 6, 30);

    private const string FormatReference =
        "https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=";

    public static readonly RegulatoryRuleEvidence ItemAmountRule = new(
        "UY-CFE-25.2-B-C24-ITEM-AMOUNT",
        "DGI e-Factura - Formato CFE",
        FormatReference,
        "Formato CFE v25.2, reviewed 2026-09-03",
        SupportedFrom,
        clause: "B-C24: monto item = cantidad x precio unitario - descuento + recargo; NUM 17 con 2 decimales");

    public static readonly RegulatoryRuleEvidence HeaderTotalsRule = new(
        "UY-CFE-25.2-A-C116-A-C124-TOTALS",
        "DGI e-Factura - Formato CFE",
        FormatReference,
        "Formato CFE v25.2, reviewed 2026-09-03",
        SupportedFrom,
        clause: "A-C116/A-C117 acumulan netos por indicador; A-C121/A-C122 calculan IVA desde esos netos; A-C124 totaliza encabezado");

    public static readonly RegulatoryRuleEvidence RoundingRule = new(
        "UY-CFE-HOMOLOGATION-MATHEMATICAL-ROUNDING-2",
        "DGI e-Factura - Instructivo de ingreso al regimen CFE",
        "https://www.efactura.dgi.gub.uy/principal/factura-electronica-informacion-general-instructivos",
        "official DGI homologation instruction, reviewed 2026-09-03",
        SupportedFrom,
        clause: "Los resultados de los calculos del set de pruebas consideran redondeo matematico con 2 decimales");

    public static readonly CfeArithmeticRulePack Current = new(
        PackVersion,
        FormatVersion,
        SupportedFrom,
        monetaryScale: 2,
        ItemAmountRule,
        HeaderTotalsRule,
        RoundingRule);
}
