# UI-SUPPLIER-001 — Proveedores

Status: `SPECIFIED / NOT_VISUALLY_APPROVED / DEPENDENCY_GAPS_RECORDED`

Upstream interface scope ID: `WEB-005`

Reconciliation: `documentation/ui/inventory/UI-SUPPLIER-001_RECONCILIATION.md`

## 1. Objetivo

Permitir buscar, consultar, crear y mantener proveedores sobre el agregado unificado `Party`, preservando identidades fiscales, direcciones, contactos y coexistencia de roles sin inventar compras, cuentas por pagar ni relaciones de documentos recibidos que aún no estén expuestas como proyección autoritativa.

## 2. Usuarios y permisos

Actores upstream: purchaser, treasury, accountant, administrator.

Permisos efectivos requeridos según acción:

- `parties.read`;
- `parties.manage`;
- `parties.fiscal.manage`.

Los roles de negocio no sustituyen las policies reales del backend.

## 3. Requisitos

Trazabilidad upstream:

- `FR-010`;
- `FR-011`;
- `FR-067`;
- `FR-071`;
- `FR-081`.

`FR-067`, `FR-071` y `FR-081` relacionan al proveedor con compras, obligaciones y evidencia recibida. En v1 esos dominios son dependencias futuras y no se convierten en métricas falsas dentro del maestro Party.

## 4. Historias de usuario / casos de uso

No existe todavía un `US-*` ni `UC-SUPPLIER-*` gobernado para este lifecycle. La vista se especifica con requirements + API real y conserva el gap para cierre posterior.

## 5. Navegación

Entrada principal desde `Proveedores`.

Destinos futuros relacionados:

- `WEB-009` Purchase Orders and Receipts;
- `WEB-011` Accounts Payable and Supplier Payments;
- received CFE/source-document view cuando tenga UI gobernada.

Esos destinos pueden aparecer como navegación conceptual, pero no como tarjetas con datos live en v1.

## 6. Lista de proveedores

La lista usa `API-PTY-001 listParties` con `role=SUPPLIER`.

Mostrar:

- nombre;
- `PERSON` / `ORGANIZATION`;
- identidad fiscal visible cuando exista;
- contacto primario cuando exista;
- estado activo;
- badges de roles, incluyendo `CUSTOMER + SUPPLIER`;
- acciones contextuales según permisos.

Filtros mínimos:

- búsqueda textual;
- activos/inactivos.

No mostrar:

- saldo pendiente;
- aging;
- compras acumuladas;
- última compra;
- facturas pendientes;
- órdenes abiertas;
- recepción pendiente.

## 7. Detalle de proveedor

Estructura recomendada master-detail:

### Resumen

- nombre;
- kind;
- residencia;
- residencia fiscal;
- estado;
- versión;
- roles.

### Identidades fiscales

- type code;
- número;
- país emisor;
- vigencia;
- activo.

### Direcciones y contactos

Direcciones:

- `FISCAL`;
- `DELIVERY`;
- `OTHER`.

Contactos:

- type code;
- value;
- primary.

### Relaciones operativas futuras

Reservar como navegación secundaria, sin valores live:

- `Compras`;
- `Cuentas por pagar`;
- `Documentos recibidos`.

Estas áreas deben mostrar semántica de destino/futuro módulo, nunca un resumen numérico inventado.

## 8. Crear proveedor

Fuente: `API-PTY-002 createParty`.

Campos soportados:

- kind;
- name;
- residenceCountry;
- taxResidenceCountry;
- roles, incluyendo `SUPPLIER`;
- fiscalIdentities[];
- addresses[];
- contacts[].

El comando requiere idempotencia.

## 9. Editar proveedor

Datos generales y canales por `API-PTY-004 updateParty` con `ExpectedVersion`.

Roles mediante `API-PTY-007 setPartyRoles`.

Identidades fiscales mediante `API-PTY-005/006`.

La coexistencia con `CUSTOMER` debe conservarse si el operador autorizado no la elimina explícitamente.

## 10. Acciones

| Acción | Permiso | Backend | Estado |
| --- | --- | --- | --- |
| Listar/buscar proveedores | `parties.read` | `API-PTY-001` | `SUPPORTED` |
| Ver detalle | `parties.read` | `API-PTY-003` | `SUPPORTED` |
| Crear proveedor | `parties.manage` | `API-PTY-002` | `SUPPORTED` |
| Editar maestro | `parties.manage` | `API-PTY-004` | `SUPPORTED` |
| Agregar identidad fiscal | `parties.fiscal.manage` | `API-PTY-005` | `SUPPORTED` |
| Actualizar identidad fiscal | `parties.fiscal.manage` | `API-PTY-006` | `SUPPORTED` |
| Gestionar roles | `parties.manage` | `API-PTY-007` | `SUPPORTED` |
| Ver payable/account summary | `parties.read` | `API-PTY-008` | `PENDING` |
| Ver compras/recepciones | varios | futuro `WEB-009` | `PENDING_UI_DEPENDENCY` |
| Ver cuentas por pagar | varios | futuro `WEB-011` | `PENDING_UI_DEPENDENCY` |
| Ver documentos recibidos | fiscal/procurement | received-CFE domain | `PENDING_UI_DEPENDENCY` |

## 11. Reglas visibles

- `SUPPLIER` es un rol de Party;
- un proveedor puede ser también cliente;
- cambiar el maestro no altera snapshots/documentos históricos;
- identidad fiscal, país emisor, residencia y residencia fiscal son facts distintos;
- no existe un `isForeign` universal;
- mutaciones respetan versión e idempotencia;
- la ausencia de resumen financiero no implica saldo cero.

## 12. Estados

La referencia debe contemplar:

- `loading_list`;
- `list_populated`;
- `empty_list`;
- `search_empty`;
- `detail_ready`;
- `create_open`;
- `edit_open`;
- `saving`;
- `validation_error`;
- `conflict`;
- `forbidden`;
- `backend_error`;
- `network_unavailable`.

## 13. Estados vacíos

- sin proveedores;
- búsqueda sin resultados;
- sin identidades fiscales;
- sin direcciones;
- sin contactos.

El CTA `Nuevo proveedor` aparece solo con `parties.manage`.

## 14. Errores y concurrencia

Usar Problem Details y no ocultar códigos útiles.

Un `409` por versión debe forzar reconciliación/recarga antes de guardar nuevamente.

## 15. Seguridad y privacidad

- mínimo dato personal necesario en listas;
- permisos separados para identidad fiscal;
- no exponer secretos;
- aislamiento por organización verificado por backend;
- navegación a módulos financieros/compra debe volver a verificar sus propios permisos.

## 16. Responsive

### Desktop

Master-detail como Clientes, con identidad visual consistente pero semántica de proveedor.

### Tablet

Lista + detalle 40/60 o navegación de panel.

### Mobile

Lista primero y detalle en pantalla completa.

## 17. Primera referencia visual v1

Objetivo: `list_populated + detail_ready` desktop.

Debe incluir:

- navegación eFactura;
- título `Proveedores`;
- CTA `Nuevo proveedor`;
- búsqueda + filtro de activos;
- lista de Party con `SUPPLIER`;
- detalle de proveedor seleccionado;
- identidades fiscales;
- direcciones/contactos;
- badges de roles;
- enlaces secundarios `Compras`, `Cuentas por pagar`, `Documentos recibidos` claramente tratados como navegación, sin métricas.

No deberá incluir como live:

- saldo pendiente;
- deuda vencida;
- aging;
- total comprado;
- última factura;
- órdenes abiertas;
- recepciones pendientes.

## 18. Referencias visuales

- Draft: `NONE` todavía.
- Approved: `NONE`.

## 19. Evidencia frontend

- implementation: `NONE`;
- running capture: `NONE`;
- visual review: `NONE`.

## 20. Change history

- `v0.1` — primera especificación gobernada de `UI-SUPPLIER-001` reconciliada contra `main@c59e053d40a78704035e559368f4857f587c04e7`.
