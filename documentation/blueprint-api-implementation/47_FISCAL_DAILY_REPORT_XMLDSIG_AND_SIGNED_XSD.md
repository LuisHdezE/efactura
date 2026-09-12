# 47 — Reporte Diario XMLDSig and signed XSD validation

## Decision

This increment closes the next bounded Reporte Diario v13.2 gate after deterministic pre-signature XML:

1. append one real enveloped XML Digital Signature as the final child of `Reporte`;
2. verify the produced signature locally with the signing certificate;
3. validate the complete signed XML against the **untouched** byte-pinned `ReporteDiarioCFE.xsd` closure.

It does not persist or submit reports and it does not claim DGI certification readiness.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Authority rechecked 2026-09-11

Current DGI material continues to state that the daily report contains a mandatory advanced electronic signature over the complete report data. The current Reporte CFE v13.2 format remains the functional basis, and the repository continues to use the already governed FE v1.44.2 schema byte pin.

Official references rechecked:

- DGI functional definitions document, section `IX. FORMATO DEL REPORTE DIARIO`;
- `Formato Reporte CFE v13 2.pdf`;
- DGI e-Factura `Documentos de interés` publication registry.

The pinned `ReporteDiarioCFE.xsd` requires `ds:Signature` as the final child of `ReporteDefType` with `minOccurs="1"`.

The current public DGI material checked for this increment does not provide evidence sufficient to claim one unique modern XMLDSig algorithm tuple as a universal normative rule. Therefore this increment preserves the same conservative posture already accepted for CFE signing: an explicit SHA-256 compatibility profile whose status is recorded as evidence-backed implementation policy, not as a claim that DGI mandates only that tuple.

## Report-specific signing contract

Reporte Diario does not reuse `FiscalSignatureRequest` by pretending that a report is a CFE.

Application now owns separate contracts:

- `FiscalDailyReportSignatureRequest`;
- `FiscalDailyReportSignatureResult`;
- `IFiscalDailyReportSignatureProvider`;
- `IFiscalDailyReportSignedSchemaValidator`.

The request binds:

- organization id;
- functional format version;
- semantic projection fingerprint;
- exact unsigned XML;
- SHA-256 of that unsigned XML;
- the already frozen `TmstFirmaEnv` timestamp.

Certificate and private-key APIs remain Infrastructure-only.

## XMLDSig profile

`XmlDsigFiscalDailyReportSignatureProvider` exposes profile id:

`dgi-daily-report-sha256-evidence-backed-v1`

Current compatibility tuple:

- canonicalization: inclusive XML C14N 1.0;
- signature method: RSA-SHA256;
- digest method: SHA-256;
- reference URI: empty string, covering the whole document;
- transform: enveloped-signature;
- KeyInfo: X509 certificate plus issuer/serial evidence.

SHA-1 profiles fail closed. The historical DGI example is not promoted into current cryptographic policy.

## Pre-key validation

Before certificate access, the provider verifies:

- organization id exists;
- functional version is exactly Reporte Diario v13.2;
- projection fingerprint is present;
- unsigned XML and its SHA-256 still match;
- signing timestamp is frozen at whole-second precision;
- root is `Reporte` in `http://cfe.dgi.gub.uy`;
- no prior `ds:Signature` exists;
- first root child is pinned `Caratula` v1.0;
- `Caratula` contains exactly one `TmstFirmaEnv`;
- `TmstFirmaEnv` matches the supplied frozen signing timestamp exactly.

This keeps replay-sensitive input checks ahead of the private-key boundary.

## Certificate policy

The existing organization-scoped `PfxFiscalSigningCertificateSource` now serves both CFE and Reporte Diario through separate interfaces while loading the configured PKCS#12 only once in normal dependency-injection composition.

Existing safeguards remain:

- PFX path/password are external configuration;
- SHA-256 certificate fingerprint pin;
- `EphemeralKeySet`;
- private key required;
- certificate validity checked at the frozen signing instant;
- recognized SHA-2 certificate signature algorithm required;
- secrets and PFX files are not committed.

## Produced signature guarantees

The Reporte signer:

1. signs the exact deterministic unsigned XML;
2. appends exactly one `ds:Signature` to `Reporte`;
3. requires that signature to be the final child;
4. verifies the produced XMLDSig using the signing certificate;
5. returns signed XML, signed-content SHA-256, profile id, certificate thumbprint and certificate serial number.

No transport or persistence is performed by the signer.

## Untouched signed-root XSD validation

`DgiFeV1_44_2SignedDailyReportSchemaValidator` is deliberately different from the pre-signature validator introduced in record #46.

It:

1. verifies the embedded `daily-report-schema-manifest.json` identity;
2. recomputes SHA-256 for `ReporteDiarioCFE.xsd`, `DGITypes.xsd` and `xmldsig-core-schema.xsd`;
3. checks that the manifest still records the signature as a required final child;
4. compiles the original `ReporteDiarioCFE.xsd` bytes unchanged;
5. performs only the local security normalization required to compile the historical W3C XMLDSig dependency without DTD/network resolution;
6. requires exactly one `ds:Signature` and requires it to be the final child before schema validation;
7. validates the complete signed report against that untouched report schema closure.

Unlike unsigned structural validation, there is no `minOccurs=1 -> 0` relaxation in this path.

## Security boundary

The signed-report path:

- prohibits DTD processing for report XML parsing;
- performs no HTTP schema resolution;
- refuses schema dependencies outside the byte-pinned closure;
- rejects SHA-1 profile selection;
- verifies payload hash before certificate access;
- verifies the generated XMLDSig before returning it;
- validates the signed root against the original mandatory-signature XSD.

## Explicit non-scope

This increment does **not** add:

- durable Reporte signing timestamp / signed artifact persistence and replay;
- report sequence lifecycle or reliquidation orchestration;
- live BCU acquisition;
- foreign-currency B-C27 threshold semantics;
- DGI `EFACRECEPCIONREPORTE` transport;
- Reporte acknowledgement / `Reporte Procesado` lifecycle;
- Sobre v05 packaging;
- certificate revocation / DGI habilitation online checks;
- DGI Testing certification or Production readiness.

## Next bounded gate

The next safe increment is **durable Reporte Diario lifecycle evidence** before transport:

- persist/replay the frozen signing timestamp and signed artifact hash;
- bind report identity (`RUC + FechaResumen + SecEnvio`) to immutable projection/signature evidence;
- make retries reproduce/reuse the same signed artifact rather than re-signing mutable input;
- define controlled sequence/reliquidation transitions.

Only after that durable lifecycle gate should the project approach `EFACRECEPCIONREPORTE` transport and response processing.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
