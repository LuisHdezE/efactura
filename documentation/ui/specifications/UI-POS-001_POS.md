# UI-POS-001 — Punto de Venta

Status: `SPECIFIED / NOT_VISUALLY_APPROVED`

Upstream interface scope ID: `WEB-003`

Reconciliation: `documentation/ui/inventory/UI-POS-001_RECONCILIATION.md`

## 1. Objetivo

Permitir que un cajero o vendedor autorizado prepare una venta de productos, servicios o líneas mixtas, seleccione cuando corresponda un cliente, capture cantidades y precios unitarios, obtenga validación y preview fiscal calculados por el servidor y ejecute únicamente las transiciones que el backend actual soporta.

La vista debe optimizar el flujo operativo del POS sin convertir al cliente web en autoridad fiscal, tributaria, de inventario o financiera.

## 2. Problema que resuelve

El operador necesita construir una venta rápidamente y conocer antes de confirmarla:

- qué artículos/servicios forman la venta;
- qué cliente se vincula, cuando corresponde;
- cantidades y precios ingresados;
- importes comerciales calculados;
- resultado de validación;
- preview tributario/fiscal;
- si el servidor considera la venta lista para confirmación;
- resultado durable de la confirmación local cuando esta se ejecuta.

La vista no debe confundir confirmación comercial local con aceptación fiscal final por DGI.

## 3. Usuarios y autorización

Actores de negocio relacionados:

- `Cashier`;
- `Seller/Operator` autorizado.

La autorización real es por permisos/policies, no por el nombre del rol. La vista necesita, según la acción:

- `catalog.read`;
- `parties.read`;
- `sales.read`;
- `sales.create`;
- `sales.confirm`.

La ausencia de `sales.confirm` debe impedir visual y funcionalmente la confirmación aunque el usuario pueda crear o editar borradores.

## 4. Requisitos de negocio

Trazabilidad primaria heredada de `WEB-003`:

- `FR-012` — productos y servicios bajo concepto comercial común;
- `FR-013` — inventario opcional por artículo;
- `FR-014` — venta mixta producto + servicio;
- `FR-020` — borrador, validación, confirmación e historia comercial inmutable;
- `FR-021` — cálculo exacto de importes y reglas fiscales;
- `FR-022` — venta contado/crédito y medios de pago extensibles;
- `FR-024` — confirmación idempotente;
- `FR-030` — selección de familia fiscal mediante política versionada;
- `FR-050` — separar offline cliente/API de indisponibilidad DGI/proveedor;
- `FR-052` — futuros clientes offline con identidad de operación única y replay idempotente.

Trazabilidad adicional de la rama de crédito soportada por la confirmación actual:

- `FR-023` — venta a crédito crea/vincula una cuenta por cobrar después del límite comercial/fiscal aplicable.

Reglas de negocio especialmente visibles:

- `BR-005` — una misma identidad idempotente no puede duplicar efectos;
- `BR-007` — producto/servicio cambia comportamiento de inventario, no vendibilidad/fiscalización;
- `BR-015` — el frontend no puede elegir arbitrariamente un tipo de documento fiscal para saltarse la decisión del servidor.

## 5. Historias de usuario

No existe actualmente en el repositorio un identificador `US-*` gobernado para este flujo.

Esta especificación no inventa uno. Antes de que `UI-POS-001` pueda pasar al estado final `ACCEPTED`, debe existir un artefacto de historia de usuario gobernado y quedar vinculado aquí.

El intent de usuario ya está expresado por `UC-SALE-001`/`002` y por los requisitos anteriores, pero eso no sustituye el artefacto de User Story exigido por la gobernanza visual.

## 6. Casos de uso relacionados

Identificadores reales existentes:

- `UC-SALE-001` — Create and validate sale draft;
- `UC-SALE-002` — Confirm cash sale and initiate fiscalization;
- `UC-SALE-003` — Confirm credit sale and create receivable;
- `UC-SALE-004` — Sell services only;
- `UC-SALE-005` — Mixed product + service sale.

Dependencia contextual futura/condicional:

- `UC-CASH-001` — Open POS/cash shift, cuando el modo POS requiera turno de caja. La primera vista no puede afirmar que el control de turno está plenamente integrado si la superficie correspondiente no está disponible.

## 7. Entrada y navegación

### Entrada

La vista se abre dentro de un contexto autenticado y autorizado.

Debe contar con `locationId` y `terminalId` válidos para crear una venta. El contrato futuro `getPosBootstrap` debería simplificar esta inicialización, pero no está actualmente evidenciado como endpoint implementado.

Por tanto, el primer diseño no debe representar mágicamente un terminal/ubicación autoconfigurado sin una fuente real documentada.

### Destinos relacionados

Sin asignar nuevos IDs de UI todavía:

- upstream `WEB-004` — Customers and Parties, para administración ampliada del cliente;
- upstream `WEB-006` — Products and Services Catalog, para administración ampliada del catálogo;
- futura vista de documento/estado fiscal basada en `WEB-013` cuando las rutas estén disponibles.

## 8. Flujo funcional

### 8.1 Preparación

1. Resolver el contexto operativo permitido.
2. Permitir búsqueda de artículos/servicios mediante `listItems`.
3. Permitir búsqueda opcional de cliente mediante `listParties`, normalmente filtrado a rol `CUSTOMER` cuando el contrato/consulta lo permita.
4. Construir las líneas de venta.
5. Para cada línea, capturar cantidad y precio unitario explícito.
6. Crear el borrador con `createSale`, o actualizarlo con `updateSaleDraft`.

### 8.2 Validación

1. Ejecutar `validateSale`.
2. Mostrar resultado válido/no válido.
3. Mostrar hallazgos relevantes.
4. Mostrar la venta devuelta por el servidor.
5. Mostrar el preview fiscal asociado.
6. No convertir una validación local de formulario en equivalente de validación fiscal del servidor.

### 8.3 Preview fiscal

La vista puede consultar `getSaleFiscalPreview` y debe mostrar de forma comprensible:

- neto;
- impuestos;
- total;
- tratamiento tributario;
- estado de elegibilidad/selección CFE;
- razones y datos faltantes;
- si el servidor informa `ReadyForConfirmation`;
- hallazgos y evidencia de reglas cuando sea útil para operación/soporte.

Debe conservarse la semántica explícita de que es un preview y no el documento fiscal final.

### 8.4 Confirmación

1. Solo habilitar cuando el actor posea `sales.confirm` y el estado/preview permita continuar.
2. Exigir confirmación humana antes de ejecutar la transición irreversible del flujo comercial actual.
3. Enviar `ExpectedVersion` y una nueva clave de idempotencia conforme al contrato.
4. Enviar settlement intent únicamente cuando exista una fuente válida para los datos requeridos.
5. Mostrar el recibo durable local devuelto por `confirmSale`.
6. Presentar `FiscalizationRequestId` como trabajo fiscal iniciado, no como CFE aceptado.

### 8.5 Después de confirmar

La primera versión no puede consultar autoritativamente el estado fiscal posterior porque `getSaleFiscalizationStatus` no está actualmente expuesto.

Debe finalizar con un estado del tipo:

```text
Venta confirmada. Fiscalización iniciada.
```

Nunca con una afirmación como:

```text
CFE aceptado por DGI
```

salvo que una futura versión consulte evidencia autoritativa que lo pruebe.

## 9. Datos mostrados

### 9.1 Artículo/servicio

Fuente: `API-CAT-001 listItems`.

Datos actualmente disponibles para búsqueda/selección:

- id;
- código;
- nombre;
- descripción;
- tipo `PRODUCT`/`SERVICE`;
- unidad;
- `TrackInventory`;
- `TaxProfileId`;
- `CategoryId`;
- estado activo/version.

### 9.2 Precio

El catálogo actual no expone precio de venta.

`UnitPrice` es entrada de la línea de venta. Por ello:

- el primer UI debe tratar el precio como dato introducido por el operador;
- no debe mostrar un supuesto precio de catálogo;
- no debe inventar listas de precios, descuentos automáticos o promociones.

Si posteriormente se incorpora un pricing authority, esta sección deberá versionarse.

### 9.3 Cliente

Fuente: `API-PTY-001 listParties`.

El selector puede mostrar los datos necesarios para identificar al cliente sin exponer información adicional no necesaria. La identidad fiscal exacta sigue siendo dominio autoritativo del servidor.

### 9.4 Venta

Fuente: `API-SAL-002..006`.

Datos relevantes disponibles en la proyección actual:

- Sale id;
- version;
- status;
- organization/location/terminal;
- customer party id;
- commercial intent;
- currency;
- effective date;
- delivery country;
- net amount;
- validation fingerprint/time;
- líneas con item, código, nombre, tipo, cantidad, precio unitario, neto y tax profile.

### 9.5 Preview fiscal

Datos relevantes:

- net amount;
- tax amount;
- total amount;
- tratamiento/impuesto por línea;
- tasa aplicada cuando resuelta;
- razones;
- datos faltantes;
- evidencia de reglas;
- clasificación global;
- candidatos/selección CFE;
- `ReadyForConfirmation`;
- findings;
- validation fingerprint.

### 9.6 Resultado de confirmación

Fuente: `API-SAL-007 confirmSale`.

Mostrar:

- Sale id;
- versión confirmada;
- confirmation fingerprint;
- settlement fingerprint;
- `FiscalizationRequestId`;
- payment count;
- receivable id, si existe;
- timestamp de confirmación;
- indicador de replay cuando corresponda.

No presentar esta respuesta como documento fiscal.

## 10. Acciones

| Acción UI | Permiso | Backend | Binding | Regla de diseño |
| --- | --- | --- | --- | --- |
| Buscar artículo/servicio | `catalog.read` | `SUPPORTED` | `API-CAT-001 listItems` | Control activo |
| Buscar cliente | `parties.read` | `SUPPORTED` | `API-PTY-001 listParties` | Control activo |
| Crear borrador | `sales.create` | `SUPPORTED` | `API-SAL-002 createSale` | Idempotente |
| Editar borrador | `sales.create` | `SUPPORTED` | `API-SAL-004 updateSaleDraft` | Solo estado permitido por servidor |
| Validar venta | `sales.create` | `SUPPORTED` | `API-SAL-005 validateSale` | Control activo |
| Ver preview fiscal | `sales.read` | `SUPPORTED` | `API-SAL-006 getSaleFiscalPreview` | Mostrar semántica preview |
| Confirmar venta | `sales.confirm` | `SUPPORTED_WITH_SETTLEMENT_LIMITS` | `API-SAL-007 confirmSale` | No afirmar aceptación fiscal |
| Seleccionar medio de pago desde catálogo | `payments.read` | `PENDING` | `API-PMT-001 listPaymentMethods` | No presentar como live en v1 |
| Cancelar venta | `sales.cancel` | `PENDING` | `API-SAL-008 cancelSale` | No presentar como live en v1 |
| Consultar fiscalización posterior | `sales.read` | `PENDING` | `API-SAL-009 getSaleFiscalizationStatus` | No presentar como live en v1 |
| Imprimir/descargar representación fiscal | `fiscal.read` | `PENDING_FOR_POS_FLOW` | `API-FIS-004 downloadFiscalRepresentation` | No presentar como live en v1 |
| Ejecutar venta offline autoritativa | varios | `PENDING` | futuro sync/client architecture | Prohibido inventar |

## 11. Reglas visibles

- Una línea puede ser producto o servicio.
- Un servicio no debe mostrar controles de stock como requisito para poder venderse.
- Una venta mixta es válida como concepto.
- El operador no selecciona arbitrariamente la familia CFE final.
- Cambiar un borrador previamente validado puede invalidar la validación anterior; la UI debe exigir nueva validación cuando corresponda.
- La versión de la venta importa para evitar actualizaciones/confirmaciones sobre estado obsoleto.
- Confirmar dos veces con la misma identidad idempotente no debe interpretarse como dos ventas.
- Confirmación local y resultado fiscal externo son estados diferentes.

## 12. Validaciones

### Cliente

La UI puede validar formato/presencia básica de campos que ella captura, pero la identidad fiscal autoritativa pertenece al backend.

### Línea

La UI debe impedir inputs obviamente inválidos antes de enviar cuando el contrato lo permite, pero no debe duplicar reglas fiscales complejas como autoridad propia.

Como mínimo debe tratar cantidad y precio unitario como valores numéricos explícitos y enviar los datos esperados por el contrato.

### Estado/version

Los errores de concurrencia/versionado deben provocar recarga/reconciliación, no sobrescritura silenciosa.

### Idempotencia

Cada comando retry-sensitive debe utilizar la identidad idempotente requerida. Un retry de red no debe fabricar una nueva operación de negocio.

## 13. Estados de la vista

La referencia visual debe contemplar al menos:

- `initial`;
- `loading`;
- `draft_empty`;
- `draft_populated`;
- `validating`;
- `validation_failed`;
- `validated_ready`;
- `preview_requires_review`;
- `confirming`;
- `confirmed_local`;
- `backend_error`;
- `forbidden`;
- `conflict`;
- `rate_limited`;
- `network_unavailable`.

`network_unavailable` no debe mutar automáticamente a un supuesto modo offline autoritativo.

## 14. Estados vacíos

### Sin líneas

Debe explicar que se busque/agregue al menos un artículo o servicio. La CTA primaria es búsqueda/agregado, no confirmación.

### Búsqueda sin resultados

Distinguir búsqueda vacía de error de API.

### Sin cliente

Puede ser un estado válido según el commercial intent y las reglas que el servidor evalúe; la UI no debe forzar un cliente universalmente.

## 15. Loading

Evitar bloquear toda la vista cuando solo se actualiza un subrecurso.

Estados de carga independientes recomendados para:

- búsqueda de artículos;
- búsqueda de clientes;
- guardar borrador;
- validar;
- cargar preview;
- confirmar.

La acción de confirmación sí debe impedir doble envío interactivo mientras está en progreso.

## 16. Errores

Mapear RFC 9457/Problem Details y conservar el código estable del backend cuando sea útil para soporte.

Clases mínimas a representar:

- validación (`422` o equivalente del contrato aplicado);
- forbidden (`403`);
- conflicto/versionado (`409`);
- rate limit (`429`);
- error inesperado;
- pérdida de red.

No reemplazar un error fiscal/regulatorio específico por un genérico “algo salió mal” cuando el backend devuelve razones útiles y seguras.

## 17. Confirmaciones

### Confirmar venta

Debe existir confirmación clara inmediatamente antes de `confirmSale` porque la operación crea efectos comerciales durables y puede crear pago/receivable, consumo de stock rastreado y trabajo de fiscalización.

La confirmación debe resumir al menos:

- total de la venta según preview autoritativo;
- cliente/contexto relevante;
- forma de settlement cuando esté disponible;
- advertencia de que se inicia el procesamiento fiscal.

### Cancelar

No existe acción funcional de cancelación en esta versión.

## 18. Seguridad

La UI debe derivar visibilidad/habilitación de acciones a partir de permisos efectivos, pero el backend conserva la autoridad final.

No mostrar controles administrativos de catálogo/cliente dentro del POS como si `sales.*` otorgara automáticamente `catalog.manage` o `parties.manage`.

No exponer:

- secretos;
- certificados/clave privada;
- datos de infraestructura fiscal;
- campos fiscales server-owned que el contrato no acepte.

## 19. Responsive

### Desktop

Es el objetivo primario del POS web. Debe permitir flujo rápido con búsqueda, detalle de líneas y resumen/preview visibles sin navegación excesiva.

### Tablet

Debe conservar la operación completa con reflow de paneles y controles táctiles utilizables.

### Mobile web estrecho

Debe seguir siendo funcional para inspección/operación básica, pero no se declara equivalente al cliente Android ni se inventan capacidades offline.

## 20. Accesibilidad

La referencia y posterior implementación deben considerar:

- flujo operable por teclado;
- foco visible;
- labels programáticos;
- búsqueda accesible;
- tabla/lista de líneas con semántica comprensible;
- errores asociados al campo/acción;
- estados fiscales/tributarios no comunicados solo mediante color;
- confirmación con foco controlado;
- cambios async anunciables mediante semántica apropiada.

## 21. Dependencias de otras vistas

- gestión detallada de clientes: upstream `WEB-004`;
- gestión detallada de catálogo: upstream `WEB-006`;
- estado/documento fiscal: upstream `WEB-013` cuando esté implementado;
- turno/caja: upstream `WEB-012` cuando la política POS lo requiera y su implementación esté disponible.

No se asignan IDs `UI-*` a esas vistas dentro de esta especificación.

## 22. Integración backend/API

### Implementado

- `API-CAT-001 listItems`;
- `API-PTY-001 listParties`;
- `API-SAL-002 createSale`;
- `API-SAL-003 getSale`;
- `API-SAL-004 updateSaleDraft`;
- `API-SAL-005 validateSale`;
- `API-SAL-006 getSaleFiscalPreview`;
- `API-SAL-007 confirmSale`.

### Pendiente para el POS objetivo completo

- `API-POS-001 getPosBootstrap`;
- `API-PMT-001 listPaymentMethods`;
- `API-SAL-008 cancelSale`;
- `API-SAL-009 getSaleFiscalizationStatus`;
- `API-FIS-004 downloadFiscalRepresentation` dentro del flujo POS;
- fuente autoritativa de precio de venta si se pretende precargar precios desde catálogo;
- bootstrap/selección completa de terminal y contexto POS;
- Client Architecture aprobada para ejecución offline.

### Simulación temporal

Ninguna simulación está aprobada por esta especificación.

Una futura referencia visual puede mostrar áreas conceptuales pendientes únicamente si quedan inequívocamente identificadas como no operativas y ello aporta valor de diseño. La implementación no debe conectar mocks silenciosos a una apariencia de producción.

## 23. Referencias visuales

Todavía no existe una referencia visual gobernada.

Próximo artefacto esperado, después de aceptar esta especificación:

```text
documentation/ui/references/drafts/UI-POS-001/v1-desktop.png
```

Variantes adicionales se crearán solo cuando aporten información relevante, por ejemplo tablet o estados de validación/confirmación.

## 24. Registro de aprobación visual

- Approved version: `NONE`
- Approval date: `NONE`
- Approval statement/reference: `NONE`
- Approved artifact: `NONE`

`adelante`, `continúa`, `seguimos` o `avanza` no convierten una futura imagen draft en baseline visual aprobado.

## 25. Evidencia de implementación frontend

- route/component: `NONE`
- implementation commit/PR: `NONE`
- running capture: `NONE`
- visual review: `NONE`

No debe iniciarse implementación visual definitiva antes de que exista una referencia visual explícitamente aprobada.

## 26. Criterio para producir el DRAFT visual v1

El draft v1 podrá diseñarse sobre esta frontera:

- búsqueda de productos/servicios;
- búsqueda/selección opcional de cliente;
- líneas de venta;
- cantidad;
- precio unitario ingresado por operador;
- neto/preview/total autoritativos después de validación;
- findings y readiness;
- acción de guardar/validar;
- confirmación únicamente bajo las restricciones documentadas;
- resultado `confirmed_local` claramente separado del resultado fiscal final.

No deberá representar como live:

- catálogo de medios de pago aún no disponible;
- cancelación;
- fiscal-status polling;
- impresión fiscal;
- offline autoritativo;
- precio de catálogo inexistente.

## 27. Change history

- `v0.1` — primera especificación gobernada derivada de `WEB-003` y reconciliada contra el backend/API actual. Sin referencia visual aprobada.
