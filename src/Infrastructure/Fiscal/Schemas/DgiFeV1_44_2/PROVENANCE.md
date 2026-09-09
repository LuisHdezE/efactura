# DGI FE XSD v1.44.2 provenance

This directory contains the minimal schema closure required to validate a signed `CFE` root with `CFEDGI.xsd`.

## Regulatory authority

Dirección General Impositiva (Uruguay), e-Factura **Documentos de interés** registry, publishes `XSDs_FE_V1.44.2` for Testing and Production.

Official registry:
`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

Official archive link listed by DGI:
`https://www.efactura.dgi.gub.uy/files/xsds_fe_1_44_2-zip?es=`

On 2026-09-09 the governed self-hosted runner received HTTP 200, `Content-Type: text/plain`, `Content-Length: 0` from that archive URL. Therefore this repository does **not** claim that the committed bytes were downloaded from DGI in this import run.

## Byte recovery

The schema bytes were recovered from the immutable public mirror `olagopirez/factible@9a24a598e9470c4a1c5c34cb0cb253d08ffa3db3`, path `packages/cfe/spec/xsd`.

The mirror is **not** the regulatory authority. Every recovered file was pinned to a previously observed Git blob identity before commit, and `version.txt` was required to contain exactly `version: 1.44.2`.

Runtime code must verify the committed SHA-256 values recorded in `schema-manifest.json` before compiling the schema set.

Imported closure:

- `CFEDGI.xsd`
- `CFEType.xsd`
- `DGITypes.xsd`
- `xmldsig-core-schema.xsd`
- `xenc-schema.xsd`
- `version.txt`
