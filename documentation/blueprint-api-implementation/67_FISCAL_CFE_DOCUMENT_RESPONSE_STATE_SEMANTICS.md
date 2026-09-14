# 67. ACKCFE authoritative document-state semantics

## Status

Implemented as a read-only Application boundary after the accepted ACKCFE consultation, XMLDSig verification and PKI Uruguay certificate-trust chain.

This increment does **not** mutate `FiscalDocument`, sales, accounting, inventory, fiscalization work items or any other local lifecycle state.

Formal DGI Testing readiness remains **BLOCKED BY MISSING PRODUCT CAPABILITIES**.

## Authoritative evidence used

DGI's current response-format documentation, `Formato_Mensaje_de_Respuesta_v19` (printed 2025-11-25), states that the response to `Consulta de Comprobantes` reports the review result for each CFE/CFC as received, rejected or observed. The same document explicitly states that the per-document result may be produced in one response or in multiple messages, depending on the receiver process.

Source:

- https://www.efactura.dgi.gub.uy/files/formato_mensajes_respuesta_v19-pdf?es=

DGI's `Formato_Mensaje_de_Respuesta_v17` (printed 2024-09-26) publishes the exact field-26 `Estado Recepción del Comprobante` codes used by the governed semantic classifier:

- `AE` = `Comprobante Recibido`;
- `BE` = `Comprobante Rechazado (CFE)`;
- `CE` = `Comprobante Observado (CFC)` and is identified by the document as DGI-only semantics for contingency documents.

Source:

- https://www.efactura.dgi.gub.uy/files/formato_mensajes_respuesta_v17-pdf?es=

The existing consultation contract remains the DGI `WS_eFactura.EFACCONSULTARESTADOENVIO` / `ConsultaCFE(IdReceptor + Token)` boundary already governed by implementation document 64.

## Implemented boundary

`InterpretFiscalCfeEnvelopeDocumentResponseStateUseCase` performs semantic interpretation only after all of the following durable evidence is present and internally correlated:

1. the exact append-only `StoredFiscalCfeEnvelopeDocumentResponseConsultation`;
2. successful durable ACKCFE XMLDSig verification for the same response hash;
3. successful durable PKI Uruguay trust validation for the certificate used by that verification;
4. exact consultation, ACK observation, submission, envelope, organization, response-hash and certificate-hash continuity across those records.

The use case then deserializes the already durable `ACKCFE_det` evidence and maps only the published taxonomy:

| Raw DGI code | Governed semantic state |
|---|---|
| `AE` | `Received` |
| `BE` | `Rejected` |
| `CE` | `ObservedContingency` |

Any other state code fails closed as `fiscal.envelope.document_response.semantics.state_code_unsupported`.

## Counter consistency

Semantic interpretation also verifies that the exact per-document classifications agree with the durable ACKCFE `Caratula` counters:

- number of `AE` details must equal `CantCFEAceptados`;
- number of `BE` details must equal `CantCFERechazados`;
- number of `CE` details must equal `CantCFEObservados`.

A mismatch fails closed as `fiscal.envelope.document_response.semantics.count_mismatch`.

`CantOtrosRechazados` remains preserved as source evidence but is not invented into a per-CFE semantic state because this increment has no authoritative one-to-one detail mapping for that counter.

## Security and trust boundary

This semantic layer requires successful XMLDSig mathematics and PKI Uruguay chain trust before interpreting the response bytes. It deliberately preserves the previously accepted distinction:

- `PkiUruguayTrustValidated = true`;
- `DgiIdentityValidated = false`.

Therefore this increment does not claim DGI legal signer identity, signer habilitation, accreditation or production authorization.

## No completeness or finality inference

DGI explicitly permits the result for the CFE/CFC contained in a Sobre to be emitted in one or multiple response messages. Consequently this increment:

- does **not** claim that one ACKCFE resolves every CFE in the Sobre;
- does **not** infer final token completeness;
- does **not** schedule token polling or reconsultation;
- does **not** convert `AE`, `BE` or `CE` into a local fiscal-document lifecycle transition;
- does **not** change the accepted transport `Unknown` policy;
- does **not** implement S08 recovery.

The next lifecycle-mutating boundary must first define an authoritative multi-message completeness/reconsultation policy and must remain fail-closed under partial or ambiguous external evidence.

## Tests

The increment adds cross-cutting tests proving that:

- trusted `AE`, `BE` and `CE` evidence maps to the governed semantic states;
- the historical/raw one-character fixture value `A` is no longer accepted by the semantic layer;
- ACKCFE Caratula/detail counter mismatch fails closed;
- missing PKI trust evidence blocks interpretation;
- trust evidence belonging to a different consultation fails closed.

Architecture tests additionally protect the boundary from persistence writes, transaction side effects, Infrastructure dependencies and local lifecycle mutation claims.
