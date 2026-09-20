# UI-CATALOG-001 — Productos y Servicios

Status: `SPECIFIED / VISUAL_DRAFT_PENDING / TRACEABILITY_GAPS_RECORDED`

Upstream interface scope ID: `WEB-006`

Reconciliation: `documentation/ui/inventory/UI-CATALOG-001_RECONCILIATION.md`

Governed route: `/catalogo`

## 1. Objetivo

Permitir a usuarios autorizados consultar y mantener el maestro comercial unificado de productos y servicios usando el contrato `CommercialItem`, preservando comportamiento de inventario, categorías, unidad de medida y referencia fiscal sin inventar precios, costos, stock disponible o media de producto que el backend actual no expone.

## 2. Principio de producto

Producto y servicio son dos variantes del mismo concepto comercial.

La UI debe reforzar que:

- ambos viven en el mismo catálogo;
- `kind` es `PRODUCT` o `SERVICE`;
- un servicio nunca puede rastrear inventario;
- un producto puede rastrear inventario o no;
- categoría y perfil fiscal son referencias opcionales;
- el backend conserva autoridad sobre validación fiscal, concurrencia e idempotencia.

## 3. Usuarios y permisos

Lectura:

- `catalog.read`.

Mantenimiento:

- `catalog.manage`.

Los nombres de rol del scope inicial son contexto de negocio. La autorización ejecutable se representa por permisos y scopes del backend, no por roles hardcodeados en la UI.

## 4. Requisitos trazados

El alcance upstream referencia:

- `FR-012` — productos y servicios en un concepto comercial común;
- `FR-013` — seguimiento de stock opcional por item;
- `FR-014` — ventas mixtas producto/servicio;
- `FR-015` — asignación fiscal mediante configuración versionada.

## 5. Trazabilidad pendiente

No se evidencia actualmente un lifecycle gobernado `UC-CATALOG-*` ni una historia `US-*` específica del maestro de catálogo.

La especificación registra el hueco y no inventa esas piezas.

## 6. Navegación

Ubicación:

```text
Comercial -> Productos y Servicios
```

Ruta gobernada:

```text
/catalogo
```

Hasta completar baseline visual e implementación, la opción permanece visible pero deshabilitada según la política del shell.

Al activarse, se convierte la misma entrada `planned` en `active`; no se crea una opción duplicada.

Relaciones futuras:

- `WEB-007` Inventario podrá abrir contexto de item para stock/movimientos;
- `WEB-009` Compras podrá relacionar items con procurement;
- `UI-POS-001` seguirá consumiendo el catálogo como selección comercial.

No deben renderizarse enlaces muertos a vistas futuras.

## 7. Estructura funcional

### 7.1 Lista de items

La vista principal debe incluir:

- título `Productos y Servicios`;
- CTA `Nuevo item` cuando la WebApp disponga de escritura real y `catalog.manage`;
- búsqueda por código/nombre;
- filtro de tipo: `Todos`, `Productos`, `Servicios`;
- filtro de estado: `Activos`, `Inactivos`, `Todos`;
- filtro de comportamiento de inventario;
- filtro por categoría;
- paginación/conteo;
- selección de fila para master-detail.

Columnas recomendadas:

- código;
- nombre;
- tipo;
- categoría;
- unidad;
- inventario: `Controla stock` / `No controla stock`;
- perfil fiscal resumido;
- estado.

La lista **no debe mostrar como datos autoritativos**:

- precio de venta;
- costo;
- stock disponible;
- imagen de producto;
- código de barras;
- proveedor principal.

### 7.2 Detalle del item

Panel master-detail con:

- código;
- nombre;
- descripción;
- tipo;
- estado;
- versión;
- unidad;
- comportamiento de inventario;
- categoría;
- perfil fiscal.

Secciones recomendadas:

1. `Información`;
2. `Fiscal y unidad`;
3. `Categoría`.

No mostrar cifras de stock dentro de esta vista. El límite entre catálogo e inventario debe seguir siendo visible.

### 7.3 Crear item

Basado en `API-CAT-002 createItem`.

Campos contractuales:

- código;
- nombre;
- descripción opcional;
- tipo `PRODUCT`/`SERVICE`;
- unidad;
- `trackInventory`;
- perfil fiscal opcional;
- categoría opcional.

La operación requiere `Idempotency-Key`.

Reglas de UX:

- si tipo = `SERVICE`, `trackInventory` se fuerza visualmente a false/deshabilitado;
- código se normaliza conceptualmente como identificador comercial, pero el servidor conserva autoridad;
- categoría inactiva no se ofrece como asignable;
- perfil fiscal se selecciona únicamente desde referencias autoritativas activas/aplicables.

### 7.4 Editar item

Basado en `API-CAT-004 updateItem`.

Soporta mantenimiento de:

- código;
- nombre;
- descripción;
- tipo;
- unidad;
- comportamiento de inventario;
- perfil fiscal;
- categoría;
- `ExpectedVersion`.

Un conflicto de versión debe producir estado de conflicto y no sobrescritura silenciosa.

### 7.5 Desactivar item

Basado en `API-CAT-005 deactivateItem`.

La UI debe:

- pedir confirmación explícita;
- respetar `ExpectedVersion`;
- explicar que la desactivación preserva referencias históricas;
- no ofrecer reactivación porque no existe actualmente un endpoint dedicado de reactivación de item.

### 7.6 Categorías

Operaciones disponibles:

- `API-CAT-006 listItemCategories`;
- `API-CAT-007 createItemCategory`;
- `API-CAT-008 updateItemCategory`.

La misma vista puede incluir un subpanel/drawer `Categorías` para:

- listar categorías;
- crear categoría;
- editar código/nombre;
- activar/desactivar categoría mediante update.

Una categoría asignada a un item nuevo/actualizado debe existir y estar activa.

No se necesita una ruta de navegación global independiente para categorías en v1.

### 7.7 Perfiles fiscales

Fuente:

- `API-CAT-009 listTaxProfiles`.

Mostrar como referencia de selección/lectura:

- código;
- nombre;
- tratamiento;
- tasa cuando exista en el perfil;
- vigencia;
- estado;
- metadata de fuente cuando sea útil en detalle.

La vista no administra definiciones fiscales. No existe en este alcance una operación de create/update de tax profile.

### 7.8 Unidad de medida

Fuente complementaria:

- `API-REF-008 listUnitsOfMeasure`.

La unidad del item sigue siendo texto comercial requerido.

Comportamiento UI:

- input editable con autocomplete/sugerencias de unidades ya configuradas;
- no usar un select cerrado;
- mostrar una ayuda/indicador de compatibilidad CFE 25.2 cuando la referencia lo exponga;
- no presentar esas sugerencias como catálogo oficial de unidades DGI.

## 8. Fuentes de datos

### Commercial Items

`API-CAT-001..005`.

`CommercialItemDto` contiene:

- id;
- version;
- active;
- code;
- name;
- description;
- kind;
- unit;
- trackInventory;
- taxProfileId;
- categoryId.

### Categorías

`API-CAT-006..008`.

`ItemCategoryDto` contiene:

- id;
- version;
- active;
- code;
- name.

### Perfiles fiscales

`API-CAT-009`.

Referencia de solo lectura para la UI de catálogo.

### Unidades

`API-REF-008`.

Proyección runtime de unidades ya configuradas en items activos visibles al actor; no enumeración fiscal cerrada.

## 9. Matriz de acciones

| Acción UI | Permiso | Backend | Disposición |
| --- | --- | --- | --- |
| Listar/buscar items | `catalog.read` | `API-CAT-001` | `SUPPORTED` |
| Ver detalle | `catalog.read` | `API-CAT-003` | `SUPPORTED` |
| Crear item | `catalog.manage` | `API-CAT-002` | `SUPPORTED` |
| Editar item | `catalog.manage` | `API-CAT-004` | `SUPPORTED` |
| Desactivar item | `catalog.manage` | `API-CAT-005` | `SUPPORTED` |
| Listar categorías | `catalog.read` | `API-CAT-006` | `SUPPORTED` |
| Crear categoría | `catalog.manage` | `API-CAT-007` | `SUPPORTED` |
| Editar/activar/desactivar categoría | `catalog.manage` | `API-CAT-008` | `SUPPORTED` |
| Listar perfiles fiscales | `catalog.read` | `API-CAT-009` | `SUPPORTED / READ_ONLY` |
| Sugerir unidades configuradas | `catalog.read` | `API-REF-008` | `SUPPORTED` |
| Gestionar precio/costo | future | no catalog price contract evidenced | `NOT_SUPPORTED_V1` |
| Gestionar imágenes/media | future | no item-media contract evidenced | `NOT_SUPPORTED_V1` |
| Mostrar stock disponible | future `WEB-007` | no stock projection in CommercialItemDto | `OUT_OF_SCOPE` |
| Reactivar item | future | no dedicated executable operation | `NOT_SUPPORTED_V1` |

## 10. Validaciones visibles

- código requerido, máximo 80;
- nombre requerido, máximo 250;
- descripción opcional, máximo 1000;
- unidad requerida, máximo 40;
- tipo solo `PRODUCT`/`SERVICE`;
- servicio no puede rastrear inventario;
- categoría asignable debe estar activa;
- perfil fiscal asignado debe ser válido/aplicable;
- evitar doble submit mediante idempotencia;
- mutaciones respetan `ExpectedVersion`;
- duplicidad de código se presenta como conflicto de negocio, no error genérico.

## 11. Estados UI

La implementación debe contemplar:

- `loading_list`;
- `list_populated`;
- `empty_list`;
- `filtered_empty`;
- `detail_ready`;
- `create_open`;
- `edit_open`;
- `categories_open`;
- `saving`;
- `deactivate_confirm`;
- `validation_error`;
- `duplicate_code_conflict`;
- `stale_version_conflict`;
- `forbidden`;
- `backend_error`;
- `network_unavailable`.

## 12. Estados vacíos

### Sin items

Mensaje claro de que el catálogo no contiene productos/servicios para el filtro actual.

### Filtro sin resultados

Diferenciar de error de backend.

### Sin categoría

Mostrar `Sin categoría`, no inventar `General`.

### Sin perfil fiscal

Mostrar ausencia explícita y no asignar un porcentaje mágico.

### Sin descripción

Mostrar ausencia, no texto placeholder presentado como dato.

## 13. Fronteras de datos no autorizadas

No se deben añadir a fixtures o UI como si fueran contrato:

- precio de lista;
- costo promedio/último costo;
- margen;
- moneda del item;
- barcode/GTIN;
- SKU separado de `code`;
- imagen/media;
- stock actual/reservado/disponible;
- punto de reposición;
- proveedor preferido;
- variantes;
- peso/dimensiones.

El POS puede seguir usando metadata visual local e introducir `unitPrice` al construir una venta, pero esas piezas no cambian la autoridad del maestro de catálogo.

## 14. Dirección visual

La vista reutiliza el shell compacto ya aceptado.

Dirección recomendada:

- desktop light + dark;
- tabla/lista densa como superficie principal;
- master-detail con panel derecho;
- filtros compactos arriba;
- badge `Producto` / `Servicio`;
- badge `Controla stock` únicamente como comportamiento, no cantidad;
- categoría y perfil fiscal legibles sin convertir la tabla en una sábana de texto;
- acciones de mantenimiento concentradas en header/drawer;
- categorías en drawer/subpanel, no navegación paralela;
- sin tarjetas grandes decorativas;
- sin fotografías de producto como eje visual, porque no existe contrato de media.

La jerarquía debe favorecer administración de catálogo, no replicar el grid visual del POS.

## 15. Responsive

### Desktop

- lista dominante + detalle lateral;
- filtros en una línea cuando el ancho lo permita;
- drawer/modal para alta/edición.

### Tablet

- filtros reflow;
- detalle apilable o drawer.

### Mobile

- lista primero;
- detalle a pantalla completa;
- filtros condensados;
- acciones sensibles accesibles sin hover.

## 16. Accesibilidad

- tabla/lista semántica;
- navegación por teclado;
- foco visible;
- labels programáticos;
- estado/tipo/inventario expresados también con texto;
- formularios con errores asociados a campos;
- confirmación accesible para desactivación;
- dialogs/drawers con gestión de foco.

## 17. Estado de implementación

Actualmente:

```text
UI-CATALOG-001 = SPECIFIED / VISUAL_DRAFT_PENDING / TRACEABILITY_GAPS_RECORDED
/catalogo = PLANNED_DISABLED
```

Esta especificación no modifica `capabilities.ts`, `routes.tsx` ni React Router.

Siguiente gate: producir y revisar un baseline visual light/dark antes de cualquier implementación frontend.
