# UI-CUSTOMER-001 — Clientes

Status: `VISUAL_APPROVED / TRACEABILITY_GAPS_RECORDED / NOT_IMPLEMENTED`

Upstream interface scope ID: `WEB-004`

Reconciliation: `documentation/ui/inventory/UI-CUSTOMER-001_RECONCILIATION.md`

## 1. Objetivo

Permitir a usuarios autorizados buscar, consultar, crear y mantener clientes sobre el modelo unificado `Party`, conservando identidades fiscales, domicilios, contactos y roles sin inventar información financiera ni reescribir evidencia fiscal histórica.

## 2. Problema que resuelve

El operador necesita identificar rápidamente a una persona u organización, verificar sus datos fiscales y de contacto, crearla cuando no existe y corregir datos maestros de forma auditada y versionada.

La vista debe evitar dos errores conceptuales:

- tratar `Customer` y `Supplier` como maestros duplicados cuando un mismo Party puede tener ambos roles;
- confundir cambios del maestro actual con los snapshots históricos ya usados en CFE emitidos.

## 3. Usuarios y permisos

Acceso de lectura:

- `parties.read`.

Mantenimiento general:

- `parties.manage`.

Mantenimiento de identidad fiscal:

- `parties.fiscal.manage`.

La UI puede ocultar/deshabilitar acciones según permisos efectivos, pero el backend conserva autoridad final.

## 4. Requisitos de negocio

Trazabilidad principal:

- `FR-010`;
- `FR-011`;
- `FR-016`;
- `FR-017`;
- `FR-018`;
- `FR-019`.

Reglas especialmente visibles:

- nacionalidad/residencia, residencia fiscal, identidad fiscal y país emisor son hechos distintos;
- una Party puede tener varias identidades fiscales cuando sea válido;
- cambiar el maestro no modifica snapshots fiscales históricos;
- la autorización es por permisos/policies, no por nombre de rol de usuario.

## 5. Historias de usuario y casos de uso

No existe actualmente un `US-*` gobernado ni un lifecycle `UC-PTY-*`/`UC-CUSTOMER-*` dedicado al mantenimiento maestro.

La especificación registra ese hueco y no lo inventa. Debe resolverse antes del estado final `ACCEPTED`.

## 6. Navegación

Entrada principal desde `Clientes` en navegación web.

Relaciones futuras:

- POS `UI-POS-001` puede abrir selector/búsqueda de cliente;
- cuentas por cobrar `WEB-010` podrá enlazar desde un cliente cuando exista su UI gobernada;
- proveedores `WEB-005` reutilizan el mismo Party con rol `SUPPLIER`.

## 7. Estructura funcional de la vista

### 7.1 Lista de clientes

Panel principal con:

- título `Clientes`;
- CTA `Nuevo cliente`, solo con `parties.manage`;
- buscador por texto;
- filtro `Activos`;
- lista/paginación de `Party` filtrada por `role=CUSTOMER`;
- tipo `PERSON`/`ORGANIZATION`;
- nombre;
- identidad fiscal principal/visible cuando exista;
- contacto primario cuando exista;
- estado activo;
- indicador de roles, incluyendo coexistencia `CUSTOMER + SUPPLIER` cuando corresponda.

No mostrar saldo, deuda, aging ni límite de crédito como si fueran parte de esta proyección.

### 7.2 Detalle de cliente

Al seleccionar un registro mostrar un panel/detail route con:

- nombre;
- tipo de Party;
- estado activo;
- versión;
- residencia;
- residencia fiscal;
- roles;
- identidades fiscales;
- direcciones;
- contactos.

Secciones sugeridas:

1. `Resumen`;
2. `Identidades fiscales`;
3. `Direcciones y contactos`.

No existe una sección financiera live en v1.

### 7.3 Crear cliente

Formulario/drawer/modal gobernado por `API-PTY-002 createParty`.

Campos soportados:

- kind `PERSON`/`ORGANIZATION`;
- nombre;
- país de residencia;
- país de residencia fiscal;
- roles, con `CUSTOMER` requerido por esta vista;
- cero o más identidades fiscales;
- cero o más direcciones;
- cero o más contactos.

Cada operación create usa `Idempotency-Key` conforme al contrato.

### 7.4 Editar datos generales

`API-PTY-004 updateParty` permite modificar:

- kind cuando el contrato/servidor lo permita;
- nombre;
- residencia;
- residencia fiscal;
- direcciones;
- contactos;
- `ExpectedVersion`.

La UI debe tratar conflicto/versionado como `409`/concurrency y no sobrescribir silenciosamente.

### 7.5 Identidades fiscales

Agregar identidad mediante `API-PTY-005`.

Actualizar/activar-expirar identidad mediante `API-PTY-006`.

Mostrar por identidad:

- type code;
- number;
- issuing country;
- valid from;
- valid to;
- active.

La primera versión no debe fabricar nombres amigables de tipos fiscales a partir de un catálogo no implementado. Puede mostrar el código autoritativo y texto auxiliar claro hasta que `listFiscalIdentityTypes` exista.

### 7.6 Roles

`API-PTY-007 setPartyRoles` puede mantener `CUSTOMER`/`SUPPLIER`.

Desde la vista Clientes:

- `CUSTOMER` debe permanecer como rol de contexto;
- si además existe `SUPPLIER`, mostrar badge informativo;
- la edición de roles requiere `parties.manage`;
- no crear un registro duplicado para el proveedor.

## 8. Datos y autoridad

### Lista/detalle

Fuente: `API-PTY-001` y `API-PTY-003`.

`PartyDto` actual incluye:

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

### Identidad fiscal

Fuente: Party projection y comandos `API-PTY-005/006`.

La UI no determina por sí sola validez fiscal definitiva; el servidor valida los facts permitidos.

### Países/tipos de identidad

`API-REF-001 listCountries` y `API-REF-003 listFiscalIdentityTypes` están contratados pero no evidenciados como endpoints implementados en el baseline actual.

Por tanto no son fuente live para v1.

### Resumen de cuenta

`API-PTY-008 getPartyAccountSummary` permanece diferido. No hay datos financieros autorizados en esta vista v1.

## 9. Acciones

| Acción UI | Permiso | Backend | Disposición |
| --- | --- | --- | --- |
| Buscar/listar clientes | `parties.read` | `API-PTY-001` | `SUPPORTED` |
| Ver detalle | `parties.read` | `API-PTY-003` | `SUPPORTED` |
| Crear cliente | `parties.manage` | `API-PTY-002` | `SUPPORTED` |
| Editar datos generales | `parties.manage` | `API-PTY-004` | `SUPPORTED` |
| Agregar identidad fiscal | `parties.fiscal.manage` | `API-PTY-005` | `SUPPORTED` |
| Actualizar identidad fiscal | `parties.fiscal.manage` | `API-PTY-006` | `SUPPORTED` |
| Gestionar roles | `parties.manage` | `API-PTY-007` | `SUPPORTED` |
| Ver resumen/saldo/aging | `parties.read` | `API-PTY-008` | `PENDING` |
| Cargar países desde catálogo API | authenticated | `API-REF-001` | `PENDING` |
| Cargar tipos de identidad desde catálogo API | authenticated | `API-REF-003` | `PENDING` |

## 10. Validaciones visibles

- kind solo `PERSON` o `ORGANIZATION`;
- al menos un rol al crear Party, y esta vista crea contexto `CUSTOMER`;
- dirección kind solo `FISCAL`, `DELIVERY` u `OTHER`;
- identidad fiscal mantiene type code, number, issuing country y vigencia;
- no asumir que cliente extranjero carece de identidad uruguaya;
- no derivar `isForeign` genérico;
- evitar doble submit mediante idempotencia;
- respetar `ExpectedVersion` en mutaciones.

## 11. Estados

La referencia visual debe contemplar al menos:

- `loading_list`;
- `list_populated`;
- `empty_list`;
- `search_empty`;
- `detail_loading`;
- `detail_ready`;
- `create_open`;
- `create_saving`;
- `edit_saving`;
- `fiscal_identity_edit`;
- `forbidden`;
- `validation_error`;
- `conflict`;
- `backend_error`;
- `network_unavailable`.

## 12. Estados vacíos

### Sin clientes

Mensaje: no existen clientes registrados en el filtro actual.

CTA `Nuevo cliente` solo si `parties.manage`.

### Búsqueda sin resultados

Distinguir claramente de error de backend.

### Sin identidades fiscales

Mostrar estado vacío, no inventar documento genérico.

### Sin dirección/contacto

Mostrar `Sin direcciones registradas` / `Sin contactos registrados` y CTA de edición cuando corresponda.

## 13. Errores y confirmaciones

Usar Problem Details del backend y conservar códigos de validación útiles.

Mutaciones de roles/identidades y cambios sensibles deben presentar confirmación cuando puedan afectar futura facturación, pero nunca afirmar que modifican CFE históricos.

## 14. Seguridad y privacidad

- mínimo dato personal necesario en listados;
- detalle solo para usuarios autorizados;
- acciones fiscalmente sensibles separadas por `parties.fiscal.manage`;
- no mostrar secretos ni integración interna;
- backend vuelve a verificar siempre permisos y organización.

## 15. Responsive

### Desktop

Diseño recomendado master-detail:

- columna izquierda/central con tabla/lista;
- panel derecho de detalle;
- drawer/modal para crear/editar.

### Tablet

Lista y detalle pueden reflow a 40/60 o navegación entre paneles.

### Mobile

Lista primero; detail en ruta/pantalla completa; acciones principales sticky en footer cuando corresponda.

## 16. Accesibilidad

- navegación por teclado;
- foco visible;
- tabla/lista semántica;
- labels reales;
- badges con texto, no solo color;
- errores asociados a campos;
- dialogs con focus trap y retorno de foco;
- acciones destructivas/sensibles con texto explícito.

## 17. Referencias visuales

### 17.1 Histórico v1-desktop

El primer draft se conserva en:

```text
documentation/ui/references/drafts/UI-CUSTOMER-001/v1-desktop.jpg
```

Ese artefacto es histórico y no constituye el baseline aprobado.

Hallazgo que sigue vigente:

- la línea visual `última actualización 12/06/2024` no tiene fuente en el `PartyDto` actual. La implementación debe omitir ese timestamp salvo que una futura proyección lo exponga de forma autoritativa.

### 17.2 Baseline aprobado v2-theme-pair

Luis aprobó explícitamente `UI-CUSTOMER-001 v2-theme-pair` el 2026-09-16.

El artefacto aprobado está preservado en:

```text
documentation/ui/references/approved/UI-CUSTOMER-001/v2-theme-pair/customer-theme-pair.png
```

Registro de autoridad:

```text
documentation/ui/references/approved/UI-CUSTOMER-001/v2-theme-pair/README.md
```

La composición aprobada establece para la implementación:

- shell visual coherente con la experiencia POS;
- versión clara y oscura;
- lista de clientes compacta;
- master-detail con panel derecho;
- búsqueda visible;
- CTA `Nuevo cliente`;
- avatar/identificador visual no fotográfico;
- badges de estado y tipo;
- acciones de edición compatibles con las capacidades Party existentes.

Los datos de ejemplo del mockup no son contractuales. La implementación debe seguir usando únicamente campos y operaciones autorizados por el API vigente.

## 18. Registro de aprobación visual

- Approved version: `UI-CUSTOMER-001 v2-theme-pair`
- Approval date: `2026-09-16`
- Approval statement/reference: `Aprueba UI-CUSTOMER-001 v2-theme-pair para guardarla`
- Approved artifact: `documentation/ui/references/approved/UI-CUSTOMER-001/v2-theme-pair/customer-theme-pair.png`
- Approval record: `documentation/ui/references/approved/UI-CUSTOMER-001/v2-theme-pair/README.md`

Esta aprobación es exclusivamente visual. No implica que la vista esté implementada, revisada en runtime ni aceptada funcionalmente.

## 19. Evidencia frontend

- implementation: `NONE`
- running capture: `NONE`
- visual review: `NONE`

## 20. Change history

- `v0.1` — primera especificación gobernada de `UI-CUSTOMER-001`, reconciliada contra `main@c59e053d40a78704035e559368f4857f587c04e7`.
- `v0.2` — se incorpora `v1-desktop.jpg` como referencia `VISUAL_DRAFT` y se documenta el único dato visual sin fuente actual (`last updated`).
- `v0.3` — Luis aprueba explícitamente `UI-CUSTOMER-001 v2-theme-pair`; se preserva el PNG exacto bajo `references/approved` y se mantiene la implementación como pendiente.
