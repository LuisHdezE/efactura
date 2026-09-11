# 42 — Fiscal Daily Report v13.2 schema byte pin

**Status:** IMPLEMENTATION CANDIDATE  
**Baseline:** `main@f25feacf924847032418a740aa1b086663b09356`  
**Formal DGI Testing readiness:** **BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Purpose

This increment closes the byte-identity gap left intentionally open by the functional Reporte Diario v13.2 wire-contract slice.

It does **not** implement a Reporte XML serializer. It pins the schema/example evidence needed before a serializer may be designed without guessing XML names, namespaces or signature placement.

## Regulatory authority

DGI remains the regulatory authority.

Current DGI `Documentos de interés` publishes:

- Reporte Diario functional format **v13.2**;
- `XSDs_FE_V1.44.2`;
- `Ejemplo de Reporte` XML.

Official sources:

- registry: `https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`
- Reporte v13.2 format: `https://www.efactura.dgi.gub.uy/files/formato_reporte_cfe_v13_2-pdf?es=`
- example XML: `https://www.efactura.dgi.gub.uy/files/ejemplo-de-reporte?es=`
- XSD archive: `https://www.efactura.dgi.gub.uy/files/xsds_fe_1_44_2-zip?es=`

The available automated retrieval channel did not expose usable bytes from the DGI file endpoints. This increment therefore follows the already-governed v1.44.2 recovery method: regulatory/version identity comes from DGI, while exact bytes are recovered from an immutable public mirror.

## Immutable byte-recovery source

Mirror only, not regulatory authority:

- repository: `olagopirez/factible`
- commit: `9a24a598e9470c4a1c5c34cb0cb253d08ffa3db3`
- schema path: `packages/cfe/spec/xsd/ReporteDiarioCFE.xsd`
- example path: `packages/cfe/spec/Rep_219000090011_20120511_01_Ej_Mod_27032014.xml`

A one-shot branch-local GitHub Actions workflow downloaded from that exact commit, verified Git blob identities **before commit**, calculated SHA-256, wrote `daily-report-schema-manifest.json`, committed the bytes, and removed itself from the candidate tree.

## Pinned artifacts

### `ReporteDiarioCFE.xsd`

- byte length: `65284`
- Git blob SHA-1: `eaa0d0b59f689af7c91e28f4c0882c9212e25ae9`
- SHA-256: `0c87a0d533025d48680da966bb007967c16ff44ab76ba65ac8089d7452fbb06b`

### Published example Reporte XML

`Rep_219000090011_20120511_01_Ej_Mod_27032014.xml`

- byte length: `13630`
- Git blob SHA-1: `0e92d35b2ee12fa6010275dc43ffcc9665d04e7a`
- SHA-256: `713f38f78fabb39c4559caae8aa89efdc251157d459782c652a8152fc59d7657`

### Local XSD closure

`ReporteDiarioCFE.xsd` has exactly these schema-location dependencies:

1. `DGITypes.xsd`
2. `xmldsig-core-schema.xsd`

Both were already pinned by the v1.44.2 signed-CFE schema increment and are re-attested in the daily-report manifest.

No external schema resolution is required for this closure.

## Exact schema identities now safe to expose

The pinned XSD proves:

- target namespace: `http://cfe.dgi.gub.uy`
- root element: `Reporte`
- root type: `ReporteDefType`
- first root child: `Caratula`
- `Caratula/@version`: required, fixed `1.0`
- XMLDSig namespace: `http://www.w3.org/2000/09/xmldsig#`
- `ds:Signature`: required and the final child of `ReporteDefType`

Those identities are now exposed by `FiscalDailyReportV13_2WireContract`.

## Critical version distinction

Three different version labels coexist and must not be collapsed:

- current **functional Reporte format**: `13.2`
- current published **FE XSD archive**: `1.44.2`
- embedded historical comment inside `ReporteDiarioCFE.xsd`: `1.37.6i`

The published example additionally carries historical `xsi:schemaLocation="... ReporteDiarioCFE_v1.15.xsd"`.

The latter two historical identifiers are source evidence. They do not downgrade the current functional format from v13.2.

## Functional format vs XSD is a dual gate

The XSD is not a replacement for the v13.2 functional document. The implementation must satisfy **both**.

Examples of deliberate divergence preserved by this increment:

| Concern | Functional v13.2 contract already pinned | Pinned XSD |
| --- | ---: | ---: |
| Report amount rows enforced by current domain boundary | `1000` | `Mnts_FyT_Item maxOccurs="2000"` |
| Number-range functional repetition guard | `50000` | `RDU_Item maxOccurs="10000"` per XSD container |

These values are not silently reconciled. Their scopes must be understood when the serializer/mapping layer is implemented. A permissive XSD must never relax a stronger functional rule.

## Historical signature example boundary

The example XML contains SHA-1-era XMLDSig algorithms, including `rsa-sha1` and SHA-1 digest.

That proves the historical example's signature shape only. This increment intentionally does **not** make those algorithms the current signing policy. The production serializer/signing contract must be resolved independently from current authoritative DGI requirements and the signing policy already used by the application.

## Automated guards

`FiscalDailyReportSchemaArchitectureTests` protects:

- SHA-256 and Git blob identity for every pinned Reporte artifact;
- immutable mirror commit identity;
- exact local schema closure;
- root/namespace/Caratula/signature placement;
- functional-vs-XSD divergence remaining explicit;
- historical SHA-1 example algorithms not leaking into the domain wire contract;
- absence of the one-shot vendor workflow from the candidate tree.

## Explicit exclusions

This increment does not implement:

- Reporte Diario XML serialization;
- runtime Reporte XSD validator/service;
- XMLDSig generation for Reporte;
- Reporte persistence/replay/sequence lifecycle;
- DGI `EFACRECEPCIONREPORTE` transport;
- ACK Reporte parsing/lifecycle;
- Sobre v05;
- Testing certification evidence;
- Production transport/readiness;
- missing monetary/source-fact mappings such as retained/perceived/credit-fiscal concepts;
- any automatic resolution of the known functional-vs-XSD limit differences.

The next technical slice may now build a **local-only Reporte schema validator and deterministic wire mapping preconditions** from byte-pinned evidence instead of assumptions.
