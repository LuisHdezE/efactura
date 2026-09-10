# 37 — Fiscal Domestic Credit/Debit Note Foundation

Date: 2026-09-10

## Baseline

This slice starts from the accepted eFactura baseline:

- `main@808f0e70da6c2a39a3380bcb0068f5a66c6e754a`
- merge of PR #61, `docs(fiscal): reconcile DGI Testing readiness gate`
- post-merge `Clean Architecture Guard` run #221 / ID `34438269480`: `success`

The working branch is:

`blueprint/fiscal-domestic-credit-debit-note-foundation`

This slice does not modify the Blueprint Master and does not create `.blueprint/` adoption state.

## Authoritative DGI evidence revalidated before implementation

The official DGI eFactura registry was rechecked on 2026-09-10 before product changes.

Current published baselines used by this slice:

- `Formato CFE v25.2`
- `XSDs FE v1.44.2`

Official registry:

`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

Official DGI FAQ and current CFE format identify the domestic correction-note types used here:

- `101` e-Ticket
- `102` Nota de Crédito de e-Ticket
- `103` Nota de Débito de e-Ticket
- `111` e-Factura
- `112` Nota de Crédito de e-Factura
- `113` Nota de Débito de e-Factura

The pinned `CFEType.xsd` already permits `101/102/103` under the `eTck` XML family and `111/112/113` under the `eFact` XML family. No new XML root/family is invented.

The current DGI reference zone is represented by `Referencia`, with up to 40 reference lines. For an identifiable referenced CFE, this foundation preserves the evidence needed to emit:

- `NroLinRef`
- `TpoDocRef`
- `Serie`
- `NroCFERef`
- optional `RazonRef`
- optional `FechaCFEref`
- optional `MntCFEref`
- optional `TpoMonedaRef`
- `TpoCambioRef` when a preserved referenced currency is not UYU

The pinned `SerieType` accepts the DGI structural forms `A`, `AB` and `1A` through `9Z`; the domain evidence validates those forms without widening them.

The v25/v25.2 evidence was explicitly rechecked because amount, currency and exchange-rate reference fields were added to the current format line.

## Scope

This slice introduces the reusable fiscal foundation for domestic credit/debit notes:

- `102` Nota de Crédito de e-Ticket
- `103` Nota de Débito de e-Ticket
- `112` Nota de Crédito de e-Factura
- `113` Nota de Débito de e-Factura

It deliberately does not expose an operational note endpoint or connect notes to normal confirmed-sale selection.

A normal sale continues to select the previously accepted `101` or `111` flow. Correction notes require a separate operational boundary in a later slice.

## Immutable referenced-CFE evidence

`FiscalDocumentReferenceEvidence` is a Domain value that freezes one identifiable referenced CFE.

It captures:

- sequential reference number;
- organization id;
- referenced CFE type;
- series;
- number;
- optional fiscal date;
- optional referenced amount;
- optional referenced currency;
- optional referenced exchange rate;
- optional reason;
- deterministic SHA-256 evidence fingerprint.

Validation is fail-closed. This bounded foundation accepts only identifiable domestic CFE reference types `101/102/103/111/112/113`.

It does not implement or synthesize `IndGlobal=1`. DGI supports global references when the prior CFE cannot be identified, but that is a distinct semantic case and is intentionally deferred rather than inferred from incomplete evidence.

## Snapshot integration and replay

`FiscalContentSnapshot` now has optional immutable `References` evidence.

For domestic notes `102/103/112/113`:

- at least one reference is mandatory;
- at most 40 references are accepted;
- sequence must be contiguous and one-based;
- each reference must belong to the same organization as the snapshot;
- duplicate referenced fiscal identities fail closed;
- `102/103` references stay inside the domestic e-Ticket family (`101/102/103`);
- `112/113` references stay inside the domestic e-Factura family (`111/112/113`).

The reference evidence fingerprint is part of `FiscalContentSnapshot.ComputeFingerprint()` whenever references exist. Therefore changing the referenced type, series, number, date, amount, currency, exchange rate or reason changes the immutable content fingerprint.

Compatibility rule: snapshots without references append no new fingerprint material. Existing `101/111` snapshot fingerprints therefore remain unchanged solely because this property was introduced.

Persistence remains the existing JSON snapshot contract (`V1FiscalContentSnapshotRecord.SnapshotJson`). No relational migration is introduced by this slice. JSON round-trip/integrity tests protect replay.

## Deterministic unsigned CFE BUILD

`DeterministicUnsignedCfeBuilder` maps:

- `101/102/103 -> eTck`
- `111/112/113 -> eFact`
- `121 -> fail closed`

For snapshots containing reference evidence, the builder emits DGI `Referencia` after `Detalle` and before `CAEData`, preserving XSD order.

It emits only identified-CFE reference evidence. No `IndGlobal` is manufactured.

The builder also rejects self-reference when the referenced CFE type/series/number is the same fiscal identity being built.

The unsigned artifact remains deterministic and still contains neither `TmstFirma` nor `ds:Signature`.

## Signing payload and XMLDSig

The deterministic signing payload and Infrastructure XMLDSig adapter now recognize the same domestic XML-family mapping:

- `102/103 -> eTck`
- `112/113 -> eFact`

All existing protections remain in place:

- durable signing evidence must match the immutable snapshot and unsigned hash;
- `TmstFirma` is deterministic from persisted signing evidence and is inserted first in the family element;
- an existing XMLDSig signature is rejected;
- Adenda is not accepted in the signable payload;
- certificate/private-key access remains Infrastructure-only;
- certificate validity is checked at `TmstFirma`;
- RSA private key and SHA-2 certificate requirements remain;
- the generated XMLDSig is self-verified before return.

`121` export signing remains fail-closed.

## Pinned DGI XSD validation

The existing `DgiFeV1_44_2SignedCfeSchemaValidator` remains unchanged because the pinned schema already supports the domestic note type codes and reference zone.

Tests cover the complete local chain for all four notes:

`immutable reference evidence`
`-> FiscalContentSnapshot fingerprint`
`-> deterministic unsigned XML`
`-> deterministic TmstFirma payload`
`-> XMLDSig`
`-> signed-root DGI XSD v1.44.2 validation`

This is local structural/cryptographic evidence only. It is not a claim of external DGI acceptance.

## Preserved behavior

- normal `101` e-Ticket snapshots do not require references;
- normal `111` e-Factura snapshots do not require references;
- existing sale eligibility continues to select `101/111` and does not start issuing notes;
- `121` e-Factura de Exportación remains fail-closed in BUILD/signing;
- no mutable master is re-read during BUILD;
- no certificate/private-key concern enters Domain or Application;
- no DGI transport is introduced.

## Explicit exclusions

This PR does not implement:

- an operational note command/API endpoint;
- global-reference (`IndGlobal`) semantics;
- export notes `122/123`;
- contingencies;
- Adenda;
- Reporte Diario v13.2;
- Sobre v05;
- Mensajes de Respuesta v19;
- WSDL/Web Service Testing;
- HTTP transport to DGI;
- DGI credentials;
- real PFX material;
- OCSP/CRL;
- Production transport/lifecycle;
- broad POS refactors;
- accounts receivable/payable changes;
- Blueprint Master changes;
- `.blueprint/` adoption.

## Readiness statement

This slice does **not** make formal DGI Testing ready.

The accepted readiness status remains:

`BLOCKED BY MISSING PRODUCT CAPABILITIES`

No external DGI Testing success is claimed or fabricated.

## Next gate after acceptance

After this note foundation is reviewed, CI passes and the PR is explicitly approved and merged, the accepted roadmap continues with:

`Domestic notes 102/103/112/113`
`-> Reporte Diario foundation`
`-> Testing package/submission contract`
`-> authoritative external DGI Testing evidence`
`-> Production transport and operational lifecycle`
