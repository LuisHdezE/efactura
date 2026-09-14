# Fiscal CFE document-response certificate trust

## Scope

This increment adds a bounded, append-only PKI Uruguay certificate-trust boundary for the X.509 certificate already extracted from a cryptographically verified durable `ACKCFE`.

The source chain remains:

`Sobre -> transport submission -> ACKSobre observation -> ACKCFE consultation -> ACKCFE XMLDSig verification -> ACKCFE certificate trust`

No earlier record is mutated when trust validation succeeds.

## Accepted prerequisites

Trust validation starts only after all of the following are already durable and internally consistent:

1. an `ACKCFE` consultation identified by `OrganizationId + ConsultationOperationId`;
2. the exact stored `ResponseXml` and its SHA-256 continuity;
3. a successful append-only `StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification` for that exact consultation;
4. the certificate SHA-256 produced by the accepted ACKCFE XMLDSig verification boundary.

A missing or mismatched prerequisite fails closed.

## PKI policy

The Infrastructure adapter reuses the already-governed PKI Uruguay chain policy used by ACKSobre certificate trust:

- SHA-256-pinned configured trust roots;
- `X509ChainTrustMode.CustomRootTrust`;
- configured intermediate certificates;
- online revocation checking;
- revocation across the entire chain;
- no verification-flag relaxation;
- bounded URL-retrieval timeout;
- exact embedded certificate continuity through SHA-256.

Reusing the cryptographic policy does not reuse or rewrite ACKSobre evidence. ACKCFE trust is persisted independently.

## Persisted evidence

New table:

`v1_fiscal_cfe_document_response_certificate_trust_validations`

Each record stores:

- source signature-verification, consultation, ACKSobre observation, submission and Sobre identifiers;
- organization and operation id;
- exact ACKCFE response SHA-256;
- validation profile id;
- end-entity certificate SHA-256;
- trusted-root SHA-256;
- full chain certificate SHA-256 list;
- revocation mode;
- `PkiUruguayTrustValidated = true`;
- `DgiIdentityValidated = false`;
- whole-second validation timestamp.

`OrganizationId + OperationId` is unique for idempotent replay. Source relationships use `Restrict` delete behavior.

## Explicit trust boundary

This increment proves only that the certificate embedded in the already verified ACKCFE chains to the configured PKI Uruguay trust material under the governed revocation policy at validation time.

It does **not** prove that the end-entity certificate is legally controlled by DGI or that the signer is authorized for the specific fiscal message. That requires separate authoritative identity/habilitation evidence.

## Explicit non-scope

This increment does **not**:

- assert DGI legal signer identity or habilitation;
- infer `ACKCFE_det/Estado` or `EstadoCFE` lifecycle transitions;
- infer that one ACKCFE completely resolves the Sobre;
- poll or reconsult automatically;
- recover ambiguous Sobre `Unknown` or S08 states;
- mutate `FiscalDocument`, sale, accounting, inventory, Sobre, ACKSobre, ACKCFE consultation or signature-verification evidence;
- expose a public REST endpoint;
- change formal DGI Testing readiness.

Formal DGI Testing readiness remains:

`BLOCKED BY MISSING PRODUCT CAPABILITIES`

## Governance

The implementation must remain independently reviewable, provider-portable across PostgreSQL and MySQL, exact-head CI green before Ready for review, and subject to explicit human merge approval.
