# UI-SUPPLIER-001 — Proveedores

Status: `VISUAL_APPROVED / IMPLEMENTATION_READY / TRACEABILITY_GAPS_RECORDED`

Upstream interface scope ID: `WEB-005`

Reconciliation: `documentation/ui/inventory/UI-SUPPLIER-001_RECONCILIATION.md`

Governed route: `/proveedores`

## 1. Objetivo

Permitir a usuarios autorizados buscar, consultar, crear y mantener proveedores sobre el modelo unificado `Party`, usando el rol `SUPPLIER`, conservando identidades fiscales, domicilios, contactos y coexistencia de roles sin inventar saldos, compras o evidencia documental que todavía no tenga una proyección ejecutable.

## 2. Principio de producto

`Proveedor` no es un maestro independiente del cliente.

Una misma Party puede tener:

- `SUPPLIER`;
- `CUSTOMER`;
- ambos roles.

La vista debe reforzar esa realidad y evitar duplicación de personas/organizaciones cuando un mismo tercero compra y vende a la organización.

## 3. Usuarios y permisos

Lectura:

- `parties.read`.

Mantenimiento general:

- `parties.manage`.

Mantenimiento de identidad fiscal:

- `parties.fiscal.manage`.

Los roles de negocio del Interface Scope Baseline incluyen purchaser, treasury, accountant y administrator, pero la autorización ejecutable se representa por permisos/policies del backend, no por nombres de rol hardcodeados en la UI.

## 4. Requisitos trazados

El alcance upstream referencia:

- `FR-010`;
- `FR-011`;
- `FR-067`;
- `FR-071`;
- `FR-081`.

La primera versión implementable cubre directamente el mantenimiento maestro soportado por `FR-010` y `FR-011`.

`FR-067`, `FR-071` y `FR-081` permanecen como trazabilidad de alcance futuro, pero la vista no debe fingir que existen compras, cuentas por pagar o evidencia CFE enlazada mientras sus proyecciones no estén disponibles.

## 5. Trazabilidad pendiente

No se evidencia actualmente un lifecycle gobernado `UC-SUPPLIER-*` ni una historia `US-*` específica del maestro de proveedores.

La especificación registra el hueco y no inventa esas piezas.

## 6. Navegación

Ubicación:

```text
Comercial -> Proveedores
```

Ruta gobernada:

```text
/proveedores
```

Mientras la implementación no exista, la opción permanece visible pero deshabilitada según la política del shell.

Al activarse, reemplaza esa misma entrada `planned` por `active`; no se agrega una segunda opción.

Relaciones futuras:

- `WEB-009` Órdenes de compra y Recepciones podrá abrir contexto de proveedor;
- `WEB-011` Cuentas por pagar podrá enlazar al proveedor;
- `WEB-016` CFE recibidos podrá relacionar evidencia externa cuando exista el contrato correspondiente.

Esos destinos futuros no deben renderizar enlaces muertos.

## 7. Estructura funcional

### 7.1 Lista de proveedores

La vista principal debe incluir:

- título `Proveedores`;
- CTA `Nuevo proveedor` cuando exista capacidad de escritura en el gateway de la WebApp y `parties.manage`;
- búsqueda por texto;
- filtro de estado activo;
- lectura de Party filtrada por `role=SUPPLIER`;
- nombre;
- tipo `PERSON`/`ORGANIZATION`;
- identidad fiscal principal/visible cuando exista;
- contacto primario cuando exista;
- país de residencia fiscal;
- badge de roles, mostrando `CUSTOMER + SUPPLIER` cuando corresponda;
- estado activo.

La lista no debe mostrar:

- deuda pendiente;
- aging;
- monto comprado;
- órdenes abiertas;
- comprobantes recibidos;
- última compra;

salvo que una futura proyección autoritativa los exponga.

### 7.2 Detalle de proveedor

Al seleccionar un proveedor, mostrar un panel master-detail con:

- nombre;
- tipo de Party;
- estado activo;
- versión;
- país de residencia;
- país de residencia fiscal;
- roles;
- identidades fiscales;
- direcciones;
- contactos.

Secciones recomendadas:

1. `Resumen`;
2. `Identidades fiscales`;
3. `Direcciones y contactos`.

Puede existir un bloque visual `Compras y obligaciones` únicamente como estado explícitamente no disponible/futuro, sin cifras mock presentadas como reales. Para la primera implementación se prefiere omitirlo y mantener la pantalla enfocada.

### 7.3 Crear proveedor

Basado en `API-PTY-002 createParty`.

Campos soportados:

- `kind`: `PERSON` o `ORGANIZATION`;
- nombre;
- país de residencia;
- país de residencia fiscal;
- roles, incluyendo obligatoriamente `SUPPLIER` en el contexto de esta vista;
- identidades fiscales;
- direcciones;
- contactos.

La operación requiere `Idempotency-Key` según contrato.

### 7.4 Editar proveedor

`API-PTY-004 updateParty` soporta mantenimiento de:

- kind cuando el servidor lo admita;
- nombre;
- residencia;
- residencia fiscal;
- direcciones;
- contactos;
- `ExpectedVersion`.

Conflictos de concurrencia no deben sobrescribirse silenciosamente.

### 7.5 Identidades fiscales

Operaciones disponibles:

- `API-PTY-005 addPartyFiscalIdentity`;
- `API-PTY-006 updatePartyFiscalIdentity`.

Mostrar:

- tipo;
- número;
- país emisor;
- vigencia desde/hasta;
- estado activo.

La API actual ya dispone de:

- `API-REF-001 listCountries`;
- `API-REF-003 listFiscalIdentityTypes`.

Por tanto, el diseño puede contemplar selectores alimentados por contratos reales. La implementación WebApp deberá respetar su modo de datos vigente y no fingir integración live si todavía opera con fixtures locales.

### 7.6 Roles

`API-PTY-007 setPartyRoles` mantiene `CUSTOMER`/`SUPPLIER`.

Desde Proveedores:

- `SUPPLIER` es el rol de contexto;
- `CUSTOMER` adicional se muestra como badge/información;
- no se crea una Party duplicada;
- quitar el rol `SUPPLIER` requiere una acción explícita y gobernada cuando la WebApp habilite escritura.

## 8. Fuentes de datos

### Party

Fuente contractual:

- `API-PTY-001 listParties`;
- `API-PTY-003 getParty`;
- `API-PTY-002/004/005/006/007` para mutaciones.

`PartyDto` contiene:

- id;
- version;
- active;
- kind;
- name;
- residenceCountry;
- taxResidenceCountry;
- roles;
- fiscalIdentities[];
- addresses[];
- contacts[].

### Referencias

Fuentes disponibles:

- países: `API-REF-001`;
- departamentos de Uruguay: `API-REF-002`;
- tipos de identidad fiscal: `API-REF-003`.

`API-REF-004` monedas existe, pero no es necesaria para el maestro v1 mientras no se muestre información financiera.

`API-REF-005` tipos de documento fiscal y `API-REF-006` indicadores de factura existen en el baseline actual, pero pertenecen al alcance fiscal y no son necesarios para el maestro de proveedores v1.

### Resumen financiero/procurement

No disponible para v1:

- `API-PTY-008 getPartyAccountSummary` continúa sin HTTP ejecutable;
- no existe una proyección supplier-payables usable por esta vista;
- no se evidencia un endpoint HTTP de purchase-order summary para este contexto;
- no existe una proyección Party de received-CFE/source evidence.

## 9. Matriz de acciones

| Acción UI | Permiso | Backend | Disposición |
| --- | --- | --- | --- |
| Listar/buscar proveedores | `parties.read` | `API-PTY-001` con `role=SUPPLIER` | `SUPPORTED` |
| Ver detalle | `parties.read` | `API-PTY-003` | `SUPPORTED` |
| Crear proveedor | `parties.manage` | `API-PTY-002` | `SUPPORTED` |
| Editar datos generales | `parties.manage` | `API-PTY-004` | `SUPPORTED` |
| Agregar identidad fiscal | `parties.fiscal.manage` | `API-PTY-005` | `SUPPORTED` |
| Actualizar identidad fiscal | `parties.fiscal.manage` | `API-PTY-006` | `SUPPORTED` |
| Gestionar roles | `parties.manage` | `API-PTY-007` | `SUPPORTED` |
| Cargar países | authenticated | `API-REF-001` | `SUPPORTED` |
| Cargar tipos de identidad | authenticated | `API-REF-003` | `SUPPORTED` |
| Ver saldo/aging proveedor | `parties.read` | `API-PTY-008` | `PENDING` |
| Ver compras/recepciones del proveedor | future | no projection evidenced | `PENDING` |
| Ver CFE recibidos asociados | future | no Party projection evidenced | `PENDING` |

## 10. Validaciones visibles

- `kind` solo `PERSON`/`ORGANIZATION`;
- la creación desde esta vista incluye `SUPPLIER`;
- al menos un rol debe permanecer;
- direcciones solo `FISCAL`, `DELIVERY` u `OTHER`;
- identidades conservan type code, number, issuing country y vigencia;
- evitar doble submit mediante idempotencia;
- mutaciones respetan `ExpectedVersion`;
- el catálogo de tipos fiscales ayuda a presentar opciones, pero el backend conserva autoridad de validación.

## 11. Estados UI

La implementación debe contemplar:

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

## 12. Estados vacíos

### Sin proveedores

Mensaje claro de que no existen Parties con rol `SUPPLIER` en el filtro actual.

### Búsqueda sin resultados

Diferenciar de error de backend.

### Sin identidad fiscal

Mostrar ausencia explícita, no inventar un RUT genérico.

### Sin dirección/contacto

Mostrar estado vacío y acción de edición solo cuando corresponda.

## 13. Dirección visual

La vista reutiliza el lenguaje aprobado del producto y especialmente la familia visual de Clientes:

- mismo `AppShell`;
- Sidebar/Topbar/BottomBar reutilizables;
- light/dark mediante el theme global existente;
- composición master-detail compacta;
- tabla/lista densa, profesional y legible;
- panel derecho para detalle;
- badges pequeños para estado/tipo/roles;
- iconografía subordinada al contenido;
- sin grandes tarjetas decorativas que desperdicien espacio.

La vista debe sentirse claramente `Proveedores`, no una copia textual de Clientes. El énfasis visual recae en:

- organización/persona;
- identidad fiscal;
- país fiscal;
- contacto principal;
- rol dual cuando exista.

No usar cifras financieras mock como decoración.

## 14. Responsive

### Desktop

Master-detail recomendado:

- área de listado dominante;
- panel derecho de detalle;
- creación/edición mediante drawer/modal.

### Tablet

Reflow lista/detalle, conservando búsqueda y CTA.

### Mobile

Lista primero; detalle a pantalla completa; acciones sensibles accesibles sin hover.

## 15. Accesibilidad

- navegación por teclado;
- foco visible;
- labels programáticos;
- tabla/lista semántica;
- badges con texto, no solo color;
- errores asociados a campos;
- dialogs con gestión de foco;
- estados disabled distinguibles sin depender solo de opacidad.

## 16. Baseline visual aprobado

Luis aprobó explícitamente `UI-SUPPLIER-001 v1-theme-pair` el 2026-09-19 con la instrucción:

> Úsalo como baseline visual UI-SUPPLIER-001.

Registro de autoridad:

```text
documentation/ui/references/approved/UI-SUPPLIER-001/v1-theme-pair/README.md
```

Manifest de fuente:

```text
documentation/ui/references/approved/UI-SUPPLIER-001/v1-theme-pair/visual-source-manifest.md
```

La composición aprobada establece para la implementación:

- desktop light + dark;
- shell compacto completo;
- `Proveedores` activo visualmente;
- tabla/lista de proveedores densa;
- búsqueda y filtros visibles;
- CTA `Nuevo proveedor`;
- panel master-detail a la derecha;
- información de identidad fiscal/contacto/estado;
- jerarquía tipográfica y espacial coherente con Dashboard y Clientes.

Los datos de ejemplo del mockup no son contractuales.

El PNG fuente exacto está fingerprinted, pero su preservación binaria en Git permanece pendiente por limitación del conector actual. Esto no autoriza sustituirlo por una regeneración distinta.

## 17. Estado de implementación

Actualmente:

```text
UI-SUPPLIER-001 = VISUAL_APPROVED / IMPLEMENTATION_READY
/proveedores = PLANNED_DISABLED
```

La aprobación visual por sí sola no modifica `capabilities.ts`, `routes.tsx` ni React Router. La ruta se activa únicamente en el incremento de implementación, después de preservar los contratos funcionales y el shell compartido.

## 18. Registro de aprobación visual

- Approved version: `UI-SUPPLIER-001 v1-theme-pair`
- Approval date: `2026-09-19`
- Approval statement: `Úsalo como baseline visual UI-SUPPLIER-001.`
- Generation id: `742b47d8-fc9b-47ef-b30c-08af759d453e`
- Source SHA-256: `5eba4cf03854c7804bacd1ab31186a1a0817a92f0730d3679a814538f0c26f8b`
- Source dimensions: `1536 x 1024`
- Binary preservation: `PENDING`

Siguiente gate: implementación frontend de la vista aprobada dentro del shell existente, CI, autorización explícita de merge, deployment y runtime visual review.
