# DGI FE XSD v1.44.2 provenance

This directory contains pinned DGI e-Factura schema artifacts used by the governed fiscal implementation.

Two distinct schema boundaries now live here:

1. the minimal closure required to validate a signed `CFE` root with `CFEDGI.xsd`;
2. the byte-pinned `ReporteDiarioCFE.xsd` plus its already-pinned local dependencies, used as the schema evidence boundary for Reporte Diario.

## Regulatory authority

Dirección General Impositiva (Uruguay), e-Factura **Documentos de interés** registry, publishes `XSDs_FE_V1.44.2` for Testing and Production and separately publishes the Reporte Diario functional format v13.2 and an example Reporte XML.

Official registry:
`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

Official archive link listed by DGI:
`https://www.efactura.dgi.gub.uy/files/xsds_fe_1_44_2-zip?es=`

Official Reporte Diario v13.2 format:
`https://www.efactura.dgi.gub.uy/files/formato_reporte_cfe_v13_2-pdf?es=`

Official Reporte example link:
`https://www.efactura.dgi.gub.uy/files/ejemplo-de-reporte?es=`

On 2026-09-09 the governed self-hosted runner received HTTP 200, `Content-Type: text/plain`, `Content-Length: 0` from the archive URL. The same retrieval channel later exposed the Reporte example/archive links without usable bytes. Therefore this repository does **not** claim that the committed bytes were downloaded from DGI in these import runs.

## Byte recovery

Schema/example bytes were recovered from the immutable public mirror `olagopirez/factible@9a24a598e9470c4a1c5c34cb0cb253d08ffa3db3`.

The mirror is **not** the regulatory authority. The governed runner downloaded the files from that exact commit, verified their Git blob identities before commit, then calculated and recorded SHA-256 values in the repository.

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

## Dual-gate rule

The functional Reporte Diario v13.2 specification and the XSD are independent gates. Where the XSD is more permissive than the functional format, the implementation must satisfy the stronger functional rule as well. For example, the pinned XSD allows values such as `Mnts_FyT_Item maxOccurs="2000"` and `RDU_Item maxOccurs="10000"`; this does not override functional limits already pinned from v13.2.

No serializer, DGI transport or Production-readiness claim is created by committing these bytes.
