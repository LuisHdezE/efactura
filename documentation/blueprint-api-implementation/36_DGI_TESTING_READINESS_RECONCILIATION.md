# DGI Testing readiness reconciliation

## Purpose

This record reconciles the post-signing roadmap with the current official DGI Testing requirements before any claim of external acceptance is made.

Accepted baseline before this slice:

- `main@dd379ea000a5cf55673659b74c241901bf3e52db`;
- PR #60 merged with organization-scoped PFX signing composition;
- post-merge `Clean Architecture Guard` run #219 (`34429049916`) completed successfully;
- Release-1 signed CFE support covers e-Ticket (101) and e-Factura (111);
- signed artifacts are replay-safe, locally XMLDSig-verified and validated against the pinned DGI FE XSD v1.44.2 set.

This is a readiness/governance reconciliation only. It does not claim that any CFE has been received or accepted by DGI.

## Official DGI evidence rechecked on 2026-09-10

The DGI e-Factura `Documentos de interés` registry currently publishes for both Testing and Production:

- `Formato CFE v25.2`;
- `Formato_Sobre_v05`;
- `Formato Reporte CFE v13 2`;
- `Formato Mensajes Respuesta v19`;
- `XSDs_FE_V1.44.2`.

Official registry:
`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

The current DGI ingress/testing instructive states that Testing provides free-form facilities for sending documents, envelopes and reports representative of real operation, and that automated reception validates formats and electronic signatures.

Official current instructive endpoint:
`https://www.efactura.dgi.gub.uy/files/instructivo-ingreso-al-regimen-cfe-archivo-pdf?es=`

The DGI FAQ also distinguishes Testing, Homologation and Production and states that Testing access requires an enabled user.

Official FAQ endpoint:
`https://www.efactura.dgi.gub.uy/files/descargar-todas-las-preguntas-frecuentes?es=`

## Testing is not one single acceptance concept

Two different concepts must remain separate in this project.

### 1. Free technical validation in Testing

Testing can be used to exercise representative documents and validate, through DGI reception mechanisms, document/envelope/report formats and electronic signatures.

A successful individual reception is useful external technical evidence, but it does **not** by itself prove completion of the formal `Prueba de Testing` required for the traditional onboarding path.

### 2. Formal `Prueba de Testing` for traditional onboarding

The current DGI instructive requires, for the traditional ingress path:

- envelopes may contain one or multiple CFE;
- all CFE participating in the test use the same issue/signature date;
- at least **50 distinct documents for each CFE type in the minimum combo** must satisfy DGI validations and reach status `Recibido`;
- the minimum combo is:
  - 101 e-Ticket;
  - 102 Nota de Crédito de e-Ticket;
  - 103 Nota de Débito de e-Ticket;
  - 111 e-Factura;
  - 112 Nota de Crédito de e-Factura;
  - 113 Nota de Débito de e-Factura;
- rejected CFE do not count toward the 50-per-type minimum;
- both received and rejected CFE participating in the test must be included in the corresponding Reporte Diario;
- the Reporte Diario must itself reach status `Recibido`;
- the taxpayer then processes the report and the successful terminal state is `Reporte Procesado`.

The same instructive states that Testing is optional for the simplified ingress path and for certain already-authorized issuers testing changes/new CFE. Therefore this repository must not hard-code or assume the taxpayer onboarding mode without explicit operational evidence.

## Current product capability versus DGI formal Testing

### Accepted capabilities

The accepted codebase can currently produce, sign and validate locally:

- 101 e-Ticket;
- 111 e-Factura.

It also has:

- immutable fiscal content evidence;
- durable signing-time evidence;
- deterministic `TmstFirma` payload construction;
- XMLDSig signing;
- organization-scoped externally configured PFX certificate selection;
- durable signed artifact persistence;
- signed-root validation against DGI FE XSD v1.44.2.

### Missing capabilities that block a formal traditional `Prueba de Testing`

The accepted baseline does not yet implement:

- 102 Nota de Crédito de e-Ticket;
- 103 Nota de Débito de e-Ticket;
- 112 Nota de Crédito de e-Factura;
- 113 Nota de Débito de e-Factura;
- DGI Sobre v05 packaging/submission evidence;
- Reporte Diario v13.2 generation and lifecycle;
- DGI Mensaje de Respuesta v19 interpretation;
- a Testing submission mechanism or operator-assisted export package;
- authoritative DGI receipt/status capture;
- Testing credentials or a legitimate project-specific DGI test identity in repository-controlled configuration.

No credential, certificate, private key or DGI secret belongs in Git.

## Correct gate status

At this baseline:

- **local signed-CFE readiness for 101/111:** READY;
- **free external DGI Testing validation:** NOT YET EVIDENCED;
- **formal traditional `Prueba de Testing`:** BLOCKED BY MISSING PRODUCT CAPABILITIES;
- **DGI production transport/readiness:** OUT OF SCOPE AND NOT EVIDENCED.

A future DGI response must be treated as authoritative external evidence. Repository tests, local XSD validation and self-verified XMLDSig cannot be relabeled as DGI acceptance.

## Evidence rules for a future Testing run

When real Testing execution becomes possible, evidence should record at minimum:

- environment (`Testing`);
- execution date/time and repository commit used;
- organization/taxpayer identifier in a non-secret operational form appropriate for the evidence policy;
- CFE type and fiscal identity;
- immutable signed-content SHA-256;
- signature profile and non-secret certificate identifiers already persisted by the application;
- envelope/submission identifier when applicable;
- DGI reception status and timestamp;
- DGI response/consultation identifier or other authoritative correlation evidence;
- for formal traditional testing, per-type `Recibido` counts and Reporte Diario status including `Reporte Procesado`.

Passwords, private keys, PFX bytes and authentication secrets must never be captured as acceptance evidence.

## Reconciled next sequence

The previous shorthand expectation of proving Testing with only one signed e-Ticket and one signed e-Factura is insufficient for the formal traditional DGI test and is superseded by this record.

The bounded sequence is now:

1. **Domestic credit/debit note foundation**
   - add the missing 102/103/112/113 fiscal families with official reference/correction semantics and immutable evidence;
   - keep export/contingency families fail-closed.
2. **Daily Report foundation**
   - implement the currently published Reporte Diario v13.2 contract and durable evidence needed to reconcile all CFE participating in a test day.
3. **Testing package/submission contract**
   - evidence Sobre v05 and Mensaje de Respuesta v19 contracts;
   - choose an isolated Testing submission mechanism or operator-assisted export path;
   - keep Production transport separately gated.
4. **External DGI Testing evidence**
   - execute against DGI only with legitimate credentials/certificate material supplied outside source control;
   - capture authoritative DGI statuses without manufacturing success.
5. **Production transport and operational lifecycle**
   - only after the required external evidence and transport contract are separately reviewed and accepted.

If the actual taxpayer uses the simplified onboarding path, the formal 50-per-type test may not be mandatory, but that operational fact must be established explicitly rather than inferred by the codebase.

## Non-scope

This reconciliation does not implement:

- CFE notes;
- Reporte Diario;
- Sobre;
- DGI Web Service calls;
- response parsing;
- OCSP/CRL;
- DGI certificate habilitation checks;
- Homologation or Production workflows;
- any secret provisioning.
