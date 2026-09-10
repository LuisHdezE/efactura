# Fiscal PFX certificate composition

## Purpose

This slice composes the already accepted fiscal signing boundaries with an explicit production-oriented PKCS#12/PFX source without introducing DGI transport, certificate revocation services, or DGI habilitation claims.

Accepted baseline before this slice:

- `main@aaef1f512fefde9fa46bea387c06acb90f5747f4`;
- signed CFE artifacts are replay-safe and durable;
- XMLDSig is validated locally;
- signed roots pass the pinned DGI FE XSD v1.44.2 set;
- no production certificate source was previously registered.

## Composition

Infrastructure now provides:

- `PfxFiscalSigningCertificateSource : IFiscalSigningCertificateSource`;
- `UruguayFiscalSigningTimeSource : IFiscalSigningTimeSource`;
- `XmlDsigFiscalSignatureProvider : IFiscalSignatureProvider`.

`V1PersistenceServiceCollectionExtensions` composes these dependencies explicitly. No signing endpoint is added by this slice.

The Application-owned `FiscalSignatureRequest` carries `OrganizationId` so Infrastructure can select the correct signing identity without leaking PFX paths, passwords, X509 types, or key-storage details into Application or Domain.

## External configuration contract

Certificates are configured outside source control under:

`FiscalSigning:Certificates:<organizationId>`

Required values per organization:

- `Path`: absolute path to the `.pfx`/PKCS#12 file;
- `Password`: non-empty PKCS#12 password;
- `ExpectedSha256Thumbprint`: 64-hex SHA-256 certificate fingerprint.

Equivalent environment-variable keys can be supplied by the deployment environment, for example:

```text
FiscalSigning__Certificates__org-a__Path=/run/secrets/efactura/org-a-signing.pfx
FiscalSigning__Certificates__org-a__Password=<external-secret>
FiscalSigning__Certificates__org-a__ExpectedSha256Thumbprint=<64-hex-sha256>
```

These values are examples of key names only. No real path, password, certificate, fingerprint, or private key is committed by this slice.

## Fail-closed rules

A configured certificate is rejected when:

- the organization configuration is incomplete;
- the certificate path is relative;
- the file is missing or unreadable;
- the PKCS#12 password is incorrect;
- the loaded certificate does not expose a private key;
- the configured SHA-256 fingerprint is malformed;
- the loaded certificate fingerprint does not match the configured fingerprint.

A signing request for an organization without a configured certificate fails with `fiscal.signature.certificate_not_configured`.

## Private-key handling

PKCS#12 files are loaded through `X509CertificateLoader.LoadPkcs12FromFile` using only `X509KeyStorageFlags.EphemeralKeySet`.

The source deliberately does not request `PersistKeySet` or `Exportable`. Configured certificates are cached for the application-process lifetime and disposed when the singleton source is disposed. Certificate rotation therefore requires a controlled process restart/redeploy, which keeps the active signing identity stable and auditable during a process lifetime.

The repository already ignores `*.pfx`; no PKCS#12 bytes or passwords belong in Git.

## Signing clock

`UruguayFiscalSigningTimeSource` derives the signing instant from the host UTC clock and converts it to the Uruguay time zone (`America/Montevideo`, with Windows fallback `Montevideo Standard Time`).

Application persists that offset-aware timestamp before the private-key boundary. Replays continue to use durable `TmstFirma` evidence rather than consulting the clock again.

## Regulatory boundary

This slice proves local certificate loading, private-key availability, certificate identity pinning, existing certificate validity-at-`TmstFirma` checks, XMLDSig generation, and XSD validation.

It does **not** prove:

- certificate revocation status (OCSP/CRL);
- DGI-specific electronic-invoicing habilitation;
- DGI Testing acceptance;
- DGI transport/envelope/acknowledgement behavior;
- production secret-store implementation beyond external configuration and mounted PKCS#12 material.

Those remain explicit subsequent gates. A locally loadable and cryptographically valid certificate must not be described as DGI-enabled until external evidence confirms the remaining DGI requirements.

## Next gate

After this composition is accepted, the next bounded slice should prepare **DGI Testing acceptance evidence** for at least one signed e-Ticket and one signed e-Factura using a legitimately configured test signing identity, while keeping transport implementation separate until the acceptance contract is evidenced.
