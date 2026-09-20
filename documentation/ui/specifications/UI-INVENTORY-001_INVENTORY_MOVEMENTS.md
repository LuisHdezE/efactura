# UI-INVENTORY-001 — Inventario y Movimientos

Status: `SPECIFIED / VISUAL_BASELINE_PENDING / EXECUTION_GAPS_RECORDED / TRACEABILITY_GAP_RECORDED`

Upstream interface scope ID: `WEB-007`

Reconciliation: `documentation/ui/inventory/UI-INVENTORY-001_RECONCILIATION.md`

Governed route: `/inventario`

## 1. Objetivo

Permitir a operadores autorizados consultar posiciones de inventario por artículo y ubicación, inspeccionar historial de movimientos inmutables y registrar ajustes manuales auditables sin mezclar transferencias, compras, reposición, costeo ni datos que el backend actual no expone.

## 2. Principio de producto

La vista representa dos conceptos distintos pero relacionados:

- `InventoryPosition`: estado actual autoritativo de cantidad para `item + location`, con versión optimista;
- `StockMovement`: hecho inmutable que explica un cambio de cantidad.

La UI nunca debe reescribir historial para “corregir” stock. Una corrección se expresa mediante un nuevo movimiento autorizado.

## 3. Usuarios y permisos

Lectura:

- `inventory.read`.

Ajuste manual:

- `inventory.adjust`.

Los roles del scope (`inventory_operator`, `administrator`) son contexto de negocio. La autorización ejecutable depende de permisos y scopes de organización/ubicación del actor.

La vista no debe asumir que `inventory.read` implica automáticamente `catalog.read` u `organization.read`.

## 4. Requisitos trazados

El alcance upstream referencia:

- `FR-060` — movimientos inmutables y posiciones actuales por ubicación;
- `FR-061` — venta, recepción, transferencia y ajuste como fuentes explícitas de movimiento;
- `FR-062` — ajuste manual con permiso, razón y contexto antes/después;
- `FR-064` — alertas de mínimo/reposición como capacidad objetivo;
- `FR-065` — EOQ/ROP basado en inputs reales, no demanda mock;
- `FR-066` — EOQ/ROP no muta stock directamente.

## 5. Trazabilidad

Existe lifecycle gobernado:

- `UC-INV-001 — Manual stock adjustment`.

No debe absorberse `UC-INV-002 — Transfer stock between locations`, porque corresponde a `WEB-008 / Stock Transfers`.

`UC-PROC-001 — Generate replenishment/EOQ proposal` queda como dependencia futura porque `API-RPL-001/002` no están implementadas.

No se encontró un artefacto gobernado `US-INV-*`; se registra ese hueco sin inventarlo.

## 6. Navegación

Ubicación:

```text
Inventario y Compras -> Inventario
```

Ruta reservada:

```text
/inventario
```

La entrada permanece `PLANNED_DISABLED` hasta completar baseline visual aprobado e implementación.

`Transferencias` sigue siendo una entrada separada y futura (`WEB-008`).

## 7. Estructura funcional

### 7.1 Lista de posiciones

Superficie principal con:

- título `Inventario`;
- filtro por artículo mediante identificador/referencia autorizada;
- filtro por ubicación;
- paginación;
- selección de posición para detalle;
- cantidad actual;
- versión de posición.

Columnas contractuales mínimas:

- artículo;
- ubicación;
- cantidad;
- versión.

Cuando exista permiso y fuente auxiliar disponible, la UI puede enriquecer:

- `itemId` con código/nombre de catálogo;
- `locationId` con etiqueta de ubicación.

Ese enriquecimiento nunca reemplaza los IDs autoritativos ni debe romper la vista si Catalog/Organization no pueden leerse.

No mostrar como autoridad:

- reservado;
- disponible para venta separado de `quantity`;
- mínimo/reorder point;
- EOQ;
- stock en tránsito;
- valorización monetaria.

### 7.2 Detalle de posición

Panel master-detail con:

- `positionId`;
- artículo;
- ubicación;
- cantidad actual;
- versión actual;
- acción `Ajustar stock` únicamente cuando la integración de escritura esté habilitada y el actor posea `inventory.adjust`.

El detalle debe ofrecer acceso al historial de movimientos de la posición seleccionada.

### 7.3 Historial de movimientos

Basado en `API-INV-003 listStockMovements`.

Mostrar únicamente campos contractuales:

- fecha/hora (`occurredAtUtc`);
- tipo (`kind`);
- cantidad antes;
- delta;
- cantidad después;
- razón;
- explicación;
- artículo/ubicación cuando ayuden al contexto.

El historial es inmutable: no se ofrece editar, borrar ni “corregir” un movimiento existente.

Filtros server-side disponibles:

- `itemId`;
- `locationId`;
- `positionId`;
- paginación.

No presentar filtros de fecha, razón, tipo o signo del delta como si fueran filtros exhaustivos del servidor mientras el contrato no los soporte.

### 7.4 Ajuste manual

Basado en `API-INV-004 createStockAdjustment` y `UC-INV-001`.

Campos:

- artículo stock-tracked;
- ubicación;
- cantidad actual;
- `quantityDelta` positivo o negativo;
- razón requerida;
- explicación opcional;
- versión esperada.

La UI debe mostrar una previsualización explícita:

```text
Cantidad actual + Delta = Cantidad resultante
```

Reglas:

- delta cero no permitido;
- razón requerida, máximo 80 caracteres;
- explicación opcional, máximo 1000 caracteres;
- ubicación requerida;
- solo producto activo con `TrackInventory=true`;
- mutación con `Idempotency-Key`;
- conflicto de versión no puede sobrescribirse silenciosamente.

Si la posición aún no existe, el backend admite el primer ajuste con `expectedVersion = 0`. La UI no debe inventar una posición previa distinta de cero.

### 7.5 Conflicto de concurrencia

Ante `stale_version` / `concurrency_conflict`:

- no reintentar silenciosamente con nueva versión;
- informar que la cantidad cambió;
- recargar la posición;
- recalcular la previsualización antes de solicitar una nueva confirmación.

### 7.6 Reposición / EOQ

El scope upstream menciona reposición y EOQ/ROP, pero `API-RPL-001` y `API-RPL-002` continúan `MISSING_HTTP`.

Por tanto v1 puede, como máximo, reservar espacio visual futuro o mostrar una indicación de capacidad no disponible, pero no debe:

- ejecutar simulaciones locales con fórmulas/inputs inventados;
- presentar recomendaciones mock como autoridad;
- convertir una simulación en movimiento de stock;
- habilitar una acción ejecutable sin contrato real.

### 7.7 Transferencias

No forman parte de `UI-INVENTORY-001`.

Toda UX de crear, aprobar, despachar, recibir o reconciliar transferencias pertenece a `WEB-008` y permanecerá fuera de `/inventario`.

## 8. Fuentes de datos

### Posiciones

`API-INV-001..002`.

`InventoryPositionDto`:

- id;
- version;
- itemId;
- locationId;
- quantity.

### Movimientos

`API-INV-003`.

`StockMovementDto`:

- id;
- positionId;
- itemId;
- locationId;
- kind;
- quantityBefore;
- quantityDelta;
- quantityAfter;
- reasonCode;
- explanation;
- occurredAtUtc.

### Ajuste

`API-INV-004`.

Request:

- itemId;
- locationId;
- quantityDelta;
- reasonCode;
- expectedVersion;
- explanation opcional.

Response:

- positionId;
- movementId;
- version;
- quantity;
- replayed.

### Enriquecimiento opcional

- Catalog para código/nombre de item cuando `catalog.read` esté disponible;
- Locations para etiqueta de ubicación cuando `organization.read` esté disponible.

Esas fuentes son auxiliares y no cambian la autoridad de Inventory.

## 9. Matriz de acciones

| Acción UI | Permiso | Backend | Disposición |
| --- | --- | --- | --- |
| Listar posiciones | `inventory.read` | `API-INV-001` | `SUPPORTED` |
| Ver posición | `inventory.read` | `API-INV-002` | `SUPPORTED` |
| Listar movimientos | `inventory.read` | `API-INV-003` | `SUPPORTED_WITH_PROJECTION_GAP` |
| Crear ajuste manual | `inventory.adjust` | `API-INV-004` | `SUPPORTED` |
| Simular EOQ/ROP | `inventory.read` | `API-RPL-001` | `MISSING_HTTP / NOT_EXECUTABLE_V1` |
| Listar recomendaciones | `inventory.read` | `API-RPL-002` | `MISSING_HTTP / NOT_EXECUTABLE_V1` |
| Transferir stock | future `WEB-008` | `API-TRF-*` | `OUT_OF_SCOPE` |
| Valorar inventario | future/reporting-costing | no executable v1 projection here | `OUT_OF_SCOPE` |
| Editar/borrar movimiento | none | immutable history | `FORBIDDEN_BY_MODEL` |

## 10. Gap actual de proyección de movimientos

El dominio ya define movimientos `Adjustment` y `SaleConsumption`, y el flujo de venta puede persistir consumos de stock.

Sin embargo, el mapper actual de `InventoryController` solo convierte `Adjustment` a `ADJUSTMENT` y rechaza otros tipos con `inventory.unsupported_movement_kind`.

Consecuencia para esta UI:

- no afirmar todavía que el historial HTTP representa todos los movimientos de venta/recepción/transferencia;
- tratar el fallo como dependencia backend visible;
- no inventar `SALE_CONSUMPTION` en fixtures como si fuera hoy una proyección HTTP soportada;
- el futuro baseline visual puede reservar una columna `Tipo`, pero el dato runtime seguirá la respuesta real del backend.

## 11. Estados UI

La implementación debe contemplar:

- `loading_positions`;
- `positions_populated`;
- `positions_empty`;
- `detail_ready`;
- `loading_movements`;
- `movements_populated`;
- `movements_empty`;
- `adjustment_open`;
- `adjustment_submitting`;
- `adjustment_success`;
- `validation_error`;
- `stale_version_conflict`;
- `idempotency_in_progress`;
- `idempotency_payload_mismatch`;
- `forbidden`;
- `location_scope_denied`;
- `backend_error`;
- `network_unavailable`;
- `movement_projection_unavailable` cuando el backend rechace un tipo almacenado que su mapper no soporte.

## 12. Estados vacíos

### Sin posiciones

Indicar que no existen posiciones visibles para el scope/filtro actual. No convertir esto en “stock cero global”.

### Sin movimientos

Indicar que no hay movimientos visibles para la posición/filtro actual.

### Sin enriquecimiento de catálogo/ubicación

Mostrar IDs autoritativos y no inventar etiquetas.

## 13. Fronteras de datos no autorizadas

No añadir como si fueran contrato:

- stock reservado;
- disponible para venta separado de `quantity`;
- mínimo/reorder point;
- EOQ/ROP calculado localmente;
- lead time;
- safety stock;
- stock en tránsito;
- supplier principal;
- lotes/series/vencimientos;
- costo unitario/PPP/FIFO;
- valorización total;
- ubicación jerárquica tipo pasillo/bin;
- movimientos editables;
- política de stock negativo no aprobada.

Tampoco calcular un “stock total de la empresa” sumando una sola página paginada de posiciones.

## 14. Dirección visual

La vista debe reutilizar el shell compacto aceptado y preservar alta densidad operativa.

Dirección candidata para baseline:

- desktop light + dark;
- tabla/lista dominante de posiciones;
- master-detail lateral;
- pestañas o secciones `Posición` / `Movimientos`;
- acción `Ajustar stock` concentrada y claramente gobernada por permiso;
- formulario/drawer de ajuste con before/delta/after destacado;
- estados positivos/negativos expresados con signo y texto, no solo color;
- ninguna tarjeta KPI que implique agregados globales no soportados.

La vista debe sentirse operacional, no un dashboard analítico ni un módulo de compras.

## 15. Responsive

### Desktop

- lista de posiciones + detalle lateral;
- historial visible en panel/tab;
- ajuste en drawer/modal.

### Tablet

- filtros reflow;
- detalle apilable/drawer;
- tabla con columnas prioritarias.

### Mobile

- lista primero;
- detalle y movimientos en pantalla completa;
- ajuste accesible sin hover;
- cantidades y signos legibles.

## 16. Accesibilidad

- tabla/lista semántica;
- navegación por teclado;
- foco visible;
- labels programáticos;
- cantidad/delta expresados textualmente además de color;
- errores asociados a campos;
- diálogo de ajuste con gestión de foco;
- conflicto de versión anunciado claramente;
- razones y explicaciones legibles por tecnologías asistivas.

## 17. Estado de implementación

Actualmente:

```text
UI-INVENTORY-001 = SPECIFIED / VISUAL_BASELINE_PENDING / EXECUTION_GAPS_RECORDED / TRACEABILITY_GAP_RECORDED
/inventario = PLANNED_DISABLED
```

No se modifica todavía `capabilities.ts`, `routes.tsx` ni React Router.

Siguiente gate: producir un baseline visual light/dark fiel a esta especificación, someterlo a aprobación explícita y solo entonces preparar la implementación frontend.
