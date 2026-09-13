# DGI FE XSD v1.44.2 provenance

This directory contains pinned DGI e-Factura schema artifacts used by the governed fiscal implementation.

Three distinct schema boundaries now live here:

1. the minimal closure required to validate a signed `CFE` root with `CFEDGI.xsd`;
2. the byte-pinned `ReporteDiarioCFE.xsd` plus its already-pinned local dependencies, used as the schema evidence boundary for Reporte Diario;
3. the byte-pinned `EnvioCFE.xsd` plus its already-pinned local dependencies, used as the schema evidence boundary for Sobre v05 packaging.

## Regulatory authority

Dirección General Impositiva (Uruguay), e-Factura **Documentos de interés** registry, publishes `XSDs_FE_V1.44.2` for Testing and Production and separately publishes the Reporte Diario functional format v13.2, the Sobre functional format v05 and examples.

Official registry:
`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

Official archive link listed by DGI:
`https://www.efactura.dgi.gub.uy/files/xsds_fe_1_44_2-zip?es=`

Official Reporte Diario v13.2 format:
`https://www.efactura.dgi.gub.uy/files/formato_reporte_cfe_v13_2-pdf?es=`

Official Sobre v05 format:
`https://www.efactura.dgi.gub.uy/files/Formato_Sobre_v05_pdf?es=`

Official Reporte example link:
`https://www.efactura.dgi.gub.uy/files/ejemplo-de-reporte?es=`

On 2026-09-09 the governed self-hosted runner received HTTP 200, `Content-Type: text/plain`, `Content-Length: 0` from the archive URL. The same retrieval channel later exposed the Reporte example/archive links without usable bytes. Therefore this repository does **not** claim that the committed bytes were downloaded from DGI in these import runs.

## Byte recovery

Schema/example bytes were recovered from the immutable public mirror `olagopirez/factible@9a24a598e9470c4a1c5c34cb0cb253d08ffa3db3`.

The mirror is **not** the regulatory authority. The governed runner downloaded/read the files from that exact commit, verified their Git blob identities before commit, then calculated and recorded SHA-256 values in the repository.

### Signed-CFE closure

Runtime signed-CFE validation continues to use exactly the closure recorded in `schema-manifest.json`:

- `CFEDGI.xsd`
- `CFEType.xsd`
- `DGITypes.xsd`
- `xmldsig-core-schema.xsd`
- `xenc-schema.xsd`
- `version.txt`

### Reporte Diario byte pin

`daily-report-schema-manifest.json` records the exact Reporte artifacts and closure:

- `ReporteDiarioCFE.xsd`
- `DGITypes.xsd`
- `xmldsig-core-schema.xsd`
- `Examples/Rep_219000090011_20120511_01_Ej_Mod_27032014.xml`

The Reporte schema includes `DGITypes.xsd` and imports XMLDSig through `xmldsig-core-schema.xsd`; neither dependency has further schema-location dependencies in this pinned closure.

The Reporte XSD itself carries the historical internal comment `Version: 1.37.6i`, while the currently published functional Reporte format is v13.2. The example XML likewise carries the historical `xsi:schemaLocation` value `ReporteDiarioCFE_v1.15.xsd`. These identifiers are preserved as source evidence and are **not** used to downgrade or rename the current functional v13.2 contract.

The published example uses SHA-1-era XMLDSig algorithms. Those algorithms are historical example evidence only and are **not** promoted to current serializer/signing policy by this byte pin.

### Sobre v05 / EnvioCFE byte pin

`envelope-schema-manifest.json` records the exact Sobre closure:

- `EnvioCFE.xsd`
- `CFEType.xsd`
- `DGITypes.xsd`
- `xmldsig-core-schema.xsd`
- `xenc-schema.xsd`
- `version.txt`

The recovered `EnvioCFE.xsd` is byte-for-byte identical to mirror blob `3209b5b854bcbf23694c8ae2561509b568d5d263`, 3395 bytes, SHA-256 `38b411942eda7c229cf4048965654245ba6218f6079a00acfcbe1804e8daa58f`.

The XSD carries the historical internal comment `Version: 1.31`. That identifier is preserved as byte evidence and is **not** used to downgrade the currently governed `XSDs_FE_V1.44.2` archive identity or the separately published functional Sobre format v05.

The pinned XSD requires `EnvioCFE version="1.0"`, one `Caratula version="1.0"`, `RutReceptor`, `RUCEmisor`, ten-digit-bounded non-negative `Idemisor`, `CantCFE` 1..250, `Fecha`, base64 `X509Certificate`, and up to 250 `CFE` children.

## Dual-gate rule

Functional formats and XSD bytes are independent gates. Where an XSD is more permissive than a current functional format, the implementation must satisfy the stronger functional rule as well.

For Reporte Diario, for example, the pinned XSD allows values such as `Mnts_FyT_Item maxOccurs="2000"` and `RDU_Item maxOccurs="10000"`; this does not override functional limits already pinned from v13.2.

For Sobre packaging, the functional v05 rule that all CFE in one envelope use the same certificate is enforced in addition to XSD validation. Local schema validity does not imply DGI Testing or Production acceptance.

No DGI transport or Production-readiness claim is created by committing these bytes.
