# Fiscal Daily Report durable signing evidence and replay

## Scope

This increment gives the signed Reporte Diario its own durable identity and replay boundary. It does not reuse CFE signed-artifact tables or pretend a Reporte is a CFE.

The durable business identity is the tuple **RUC + FechaResumen + SecEnvio**, scoped additionally by the internal organization id. The current slice does not allocate `SecEnvio`; it persists and protects the sequence already present in the accepted wire projection.

## Before the private key

`PrepareFiscalDailyReportSigningEvidenceUseCase` runs before any certificate/private key boundary. On first execution it:

1. receives the deterministic `FiscalDailyReportWireProjection`;
2. freezes the Uruguay signing timestamp at whole-second precision;
3. builds the unsigned Reporte Diario including `TmstFirmaEnv`;
4. persists format version, projection fingerprint, unsigned-content SHA-256 and the frozen timestamp;
5. emits audit/outbox evidence in the same local transaction.

On replay, durable evidence is resolved before the clock. The unsigned report is rebuilt with the stored timestamp and must match the stored projection fingerprint and unsigned hash. The clock is not consulted again.

## Signed artifact replay

`SignFiscalDailyReportUseCase` requires the durable evidence above. It rebuilds the exact unsigned payload with the persisted timestamp and resolves the signed artifact before calling `IFiscalDailyReportSignatureProvider`.

When an artifact already exists, replay verifies:

- report identity and signing-evidence association;
- functional format and projection fingerprint;
- unsigned and signed content hashes;
- frozen signing timestamp;
- persisted signature/certificate metadata;
- current validation against the untouched byte-pinned Reporte schema closure;
- persisted schema evidence against the current pinned validator baseline.

A valid replay returns the stored artifact without crossing the private key boundary again. Any mismatch fails closed as inconsistent replay.

## Persistence

Two provider-neutral V1 tables are introduced:

- `v1_fiscal_daily_report_signing_evidence`;
- `v1_fiscal_daily_report_signed_artifacts`.

Both enforce unique report identity by organization, issuer RUC, summary date and sequence. The signed artifact also has a one-to-one unique reference to its signing evidence.

## Deliberate non-scope

This increment still does **not** implement:

- automatic `SecEnvio` allocation or reliquidation orchestration;
- DGI `EFACRECEPCIONREPORTE` transport;
- acknowledgement lifecycle from `Recibido` to `Reporte Procesado`;
- Sobre v05 packaging;
- live BCU acquisition or unresolved foreign-currency B-C27 semantics;
- online certificate revocation / DGI habilitation checks;
- formal DGI Testing certification or Production readiness.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
