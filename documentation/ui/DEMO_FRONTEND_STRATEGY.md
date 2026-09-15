# eFactura Demo Frontend Strategy

Status: `ACTIVE / D0 FOUNDATION`

Baseline for this increment: `main@8201b915dd08a6a6167773bc20db98029fbcb898`.

## Principle

**Mock data, real contracts. Mock data MUST NOT become mock capability.**

A demo view may use local fixtures, but every visible field, state, action and navigation target must be traceable to the current eFactura domain model, public DTOs, implemented Application behavior or implemented WebApi route.

A contracted-but-not-implemented endpoint does not authorize an apparently functional control.

## Initial runtime scope

The first navigable demo exposes only:

- `UI-POS-001` at `/pos`;
- `UI-CUSTOMER-001` at `/clientes`.

The shell deliberately does not publish aspirational menu entries for views that have not been reconciled and implemented.

## Data architecture

UI components depend on TypeScript gateway interfaces.

Current adapter:

- `mock` — contract-shaped fixtures stored locally.

Future adapter:

- `api` — HTTP implementation over the same gateway contracts after the integration lane is authorized.

Runtime switch:

- `VITE_DATA_MODE=mock` today;
- `VITE_DATA_MODE=api` later.

API mode intentionally fails closed until an HTTP adapter is explicitly implemented and reviewed.

## Capability registry

`src/WebApp/src/app/capabilities.ts` records the implemented API operations that authorize each published view. Navigation is derived from this governed view set rather than from the complete aspirational interface inventory.

## POS rules preserved

- item catalog data follows `CommercialItemDto`;
- catalog cards do not expose a selling price because `listItems` does not supply one;
- `UnitPrice` is captured only on the sale line;
- client-side code does not calculate authoritative tax treatment;
- confirmation is not presented as DGI acceptance;
- pending payment-method discovery, cancellation, printing, fiscalization-status and offline behavior are not exposed as live demo actions.

## Customer rules preserved

- customer is a `Party` with role `CUSTOMER`;
- a Party may also have role `SUPPLIER`;
- visible fields are limited to current `PartyDto` data;
- no debt, aging, credit limit or account summary is fabricated;
- identities, addresses and contacts retain their actual API projection semantics.

## Deployment target

Target public URL: `https://efactura.eliasworks.uy`.

Expected document root: `public_html/efactura/`.

GitHub Actions builds `src/WebApp` and uploads only the static `dist/` output over FTP, following the deployment pattern already used by `erp_eliasworks`.

Repository prerequisite before the first successful automatic deploy:

- GitHub Actions secret `FTP_PASSWORD` available to this repository;
- DNS/subdomain `efactura.eliasworks.uy` configured to the expected hosting document root.

The bundled `.htaccess` preserves SPA routes on Apache.

## Governance flow from D0 onward

```text
API/domain evidence
  -> governed UI specification
  -> visual reference
  -> React implementation using mock gateway
  -> deployed demo
  -> running capture
  -> visual/functional review
  -> later HTTP gateway integration
```

A mock implementation is therefore executable presentation evidence, not a replacement for backend capability.
