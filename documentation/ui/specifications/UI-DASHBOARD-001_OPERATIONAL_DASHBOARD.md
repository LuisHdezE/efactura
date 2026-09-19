# UI-DASHBOARD-001 — Dashboard operativo

Status: `SPECIFIED / VISUAL_BASELINE_PENDING / API_MOCK_DATA_ONLY`

Upstream interface scope ID: `WEB-002`

Reconciliation: `documentation/ui/inventory/UI-DASHBOARD-001_RECONCILIATION.md`

Shell policy: `documentation/ui/WEBAPP_SHELL_POLICY.md`

## 1. Objetivo

Ofrecer una vista operativa de entrada que permita detectar rápidamente qué requiere atención en ventas, fiscalidad, caja/flujo financiero, inventario y obligaciones, manteniendo una separación explícita entre datos demo y datos autoritativos del servidor.

La vista será el primer gran consumidor del shell reusable y no recreará Sidebar, Topbar, BottomBar ni navegación móvil.

## 2. Ruta y navegación

Ruta objetivo:

```text
/dashboard
```

Cuando se implemente deberá registrarse en `src/WebApp/src/app/routes.tsx` mediante `shellRoutes`.

Eso debe producir simultáneamente:

- ruta ejecutable;
- enlace `Dashboard` en Sidebar;
- enlace equivalente en navegación móvil;
- estado activo correcto;
- acceso directo por URL.

Mientras no exista aprobación explícita post-runtime, `defaultShellRoute` permanece `/pos`.

## 3. Usuarios objetivo

El baseline upstream identifica:

- administrator;
- seller;
- treasury;
- accountant.

La composición final será permission-aware. Los nombres de rol ayudan a entender perfiles de uso, pero la autorización efectiva dependerá de permisos/policies del backend cuando la integración real exista.

## 4. Requisitos trazados

- `FR-033` — alertas de vigencia/rango/consumo de CAE;
- `FR-064` — alertas de stock mínimo/reposición;
- `FR-074` — proyección de flujo de caja desde CxC/CxP y eventos aprobados;
- `FR-082` — datos estructurados para vistas de ventas, impuestos, inventario, finanzas y fiscalidad;
- `FR-083` — calendario fiscal con fuente/proveniencia y ciclo de actualización.

Caso de uso relacionado:

- `UC-MON-001` — monitoreo de CAE/certificado/integración fiscal con alertas durables, severidad, deduplicación y acknowledgement.

No existe actualmente un `UC-DAS-*` ni un `US-*` dedicado al Dashboard. Ese hueco de trazabilidad no se rellena artificialmente.

## 5. Contratos backend relevantes

| API ID | operationId | Route | Permission | Estado actual |
| --- | --- | --- | --- | --- |
| `API-DAS-001` | `getDashboardSummary` | `GET /api/v1/dashboard` | `dashboard.read` | `MISSING_HTTP` |
| `API-ALT-001` | `listAlerts` | `GET /api/v1/alerts` | `alerts.read` | `MISSING_HTTP` |
| `API-MON-001` | `getIntegrationStatus` | `GET /api/v1/operations/integrations` | `operations.read` | `CONTRACT_COLLISION` |

Por tanto la primera implementación WebApp usa únicamente fixtures demo para el plano de datos específico del Dashboard.

No se implementará cliente HTTP falso contra rutas inexistentes ni se resolverá desde frontend la colisión de `API-MON-001` con `API-180`.

## 6. Regla de autoridad de datos

Hasta que existan endpoints ejecutables:

- números, montos, porcentajes, tendencias, fechas y alertas del Dashboard son demo fixtures;
- la presentación debe conservar el contexto `Demo / Mock local` del shell;
- ninguna tarjeta podrá afirmar `en vivo`, `sincronizado`, `actualizado desde servidor` o equivalente;
- la estructura visual puede anticipar futuros datos autoritativos sin fijar un DTO no documentado.

Mock data no amplía capability.

## 7. Arquitectura visual dentro del shell

La vista debe ocupar únicamente el área de contenido provista por `AppShell`.

Orden funcional recomendado:

1. encabezado de página `Dashboard` con contexto temporal demo;
2. fila de indicadores operativos principales;
3. zona de tendencia/resumen comercial;
4. panel de alertas y obligaciones;
5. panel de inventario/reposición;
6. bloque fiscal/CAE;
7. bloque de flujo financiero/obligaciones;
8. actividad o accesos rápidos solo cuando apunten a rutas implementadas.

El diseño puede reorganizar estos bloques visualmente, pero no inventar módulos fuera del alcance gobernado.

## 8. Indicadores permitidos en el mock visual

El mock puede representar, como datos de demostración:

- ventas del período;
- cantidad de operaciones/ventas;
- estado fiscal agregado;
- CAE próximo a vencer/agotar;
- alertas de stock/reposición;
- saldo/flujo proyectado de corto plazo como concepto de `FR-074`;
- obligaciones o vencimientos próximos;
- cantidad de alertas por severidad.

Estos valores no definen el futuro schema de `getDashboardSummary`.

La UI debe evitar métricas no respaldadas por requisitos/contratos, por ejemplo scoring comercial, predicción de churn, rentabilidad calculada no gobernada o métricas de marketing.

## 9. Alertas

La sección `Alertas` debe soportar visualmente:

- severidad textual y visual;
- título/resumen;
- contexto de negocio;
- estado pendiente/atendida cuando exista esa semántica;
- CTA contextual solo cuando haya destino real implementado.

Ejemplos de categorías conceptualmente válidas:

- CAE;
- stock/reposición;
- fiscal;
- integración.

No mostrar secretos, certificados privados ni detalles internos sensibles.

En la primera implementación, cualquier acción `Abrir contexto` deberá limitarse a rutas que realmente existan. Si el destino futuro aún no está implementado, se presenta como información no interactiva o acción deshabilitada claramente explicada.

## 10. Navegación y accesos rápidos

Accesos permitidos inicialmente porque ya existen en el shell:

- `Punto de Venta` → `/pos`;
- `Clientes` → `/clientes`.

A medida que nuevas vistas sean implementadas y registradas en `shellRoutes`, el Dashboard podrá enlazarlas sin crear rutas paralelas o hardcodeadas fuera de la política del shell.

Los accesos rápidos no reemplazan el Sidebar.

## 11. Sección comercial

Puede visualizar una tendencia o resumen de ventas demo.

Debe:

- mostrar unidades y moneda claramente;
- distinguir monto de cantidad de operaciones;
- ofrecer alternativa textual a gráficos;
- no afirmar que el gráfico proviene de `getSalesReport` mientras dicho endpoint no esté implementado.

## 12. Inventario y reposición

La sección puede mostrar:

- productos con stock bajo;
- cantidad de alertas de reposición;
- estados tipo `Bajo`, `Crítico` o equivalentes, siempre acompañados de texto y no solo color.

La visualización debe quedar preparada para enlazar a una futura UI de inventario, pero no crear ese destino antes de que sea gobernado e implementado.

## 13. Fiscal / CAE

Puede presentar:

- estado general demo;
- CAE con vencimiento/consumo próximo;
- pendientes fiscales conceptuales;
- obligaciones/calendario próximas.

No debe:

- revelar secretos/certificados privados;
- mostrar salud DGI/proveedor como dato live mientras `getIntegrationStatus` permanezca sin resolver;
- fabricar un estado fiscal definitivo que implique comunicación real con DGI.

## 14. Finanzas y obligaciones

La vista puede representar un resumen demo de flujo proyectado y próximos vencimientos conforme `FR-074`.

No debe derivar o recalcular autoridad financiera en cliente. Cuando las proyecciones reales existan, vendrán de endpoints autoritativos.

La presentación debe diferenciar claramente:

- entradas proyectadas;
- salidas proyectadas;
- horizonte temporal.

## 15. Estados de la vista

La referencia visual y futura implementación deben contemplar al menos:

- `loading`;
- `ready`;
- `empty`;
- `partial_permission`;
- `forbidden`;
- `backend_error`;
- `offline`;
- `mock_demo`.

### `mock_demo`

Es el estado inicial de la implementación: estructura funcional completa con fixtures locales y sin afirmación de integración live.

### `partial_permission`

La composición debe poder omitir módulos para los cuales el actor no tenga permiso, sin dejar huecos visuales rotos.

## 16. Responsive

### Desktop

Dashboard multipanel con jerarquía clara, Sidebar persistente y ancho útil para tarjetas, gráfico/resumen y paneles secundarios.

### Tablet

Tarjetas en 2 columnas y paneles apilables sin perder orden semántico.

### Mobile

Una columna, navegación provista por `MobileNavigation`, tarjetas y alertas ordenadas por prioridad.

La página no crea una navegación móvil propia.

## 17. Accesibilidad

- encabezados jerárquicos;
- foco visible;
- navegación por teclado;
- tarjetas con nombres accesibles;
- estados/severidad con texto además de color;
- gráficos con resumen textual o datos equivalentes;
- montos/porcentajes con etiquetas inequívocas;
- skeleton/loading que no genere anuncios excesivos;
- CTAs deshabilitados con razón comprensible.

## 18. Tema claro/oscuro

La vista debe soportar los dos temas heredados del shell.

El feature no implementa un segundo selector de tema ni redefine globalmente tokens de Sidebar/Topbar/BottomBar.

La propuesta visual debe revisarse en pareja claro/oscuro antes de implementación.

## 19. Referencias visuales existentes

Auditoría realizada antes de autorizar un nuevo mock:

- `documentation/ui/references/approved/`: sin Dashboard;
- `documentation/ui/references/drafts/`: sin Dashboard;
- historial de repositorio/PRs: sin baseline visual aprobado de Dashboard localizado.

Por ello la siguiente etapa puede crear `UI-DASHBOARD-001 v1-theme-pair` como nuevo draft.

El draft debe mantener el shell actual como marco visual, no rediseñarlo.

## 20. Definition of Done de implementación

`UI-DASHBOARD-001` no se considera implementada hasta que:

1. exista visual aprobado explícitamente;
2. la vista React use el `AppShell` existente;
3. exista su `UiCapability` gobernada;
4. esté registrada en `shellRoutes`;
5. aparezca automáticamente en Sidebar y navegación móvil;
6. `/dashboard` funcione por URL directa;
7. soporte claro/oscuro;
8. los datos demo estén identificados como mock, sin fake live API;
9. Frontend Demo CI y gates del repositorio estén verdes;
10. exista deploy y revisión runtime.

Cambiar `defaultShellRoute` a `/dashboard` es una decisión posterior y explícita, no parte automática de este Definition of Done.

## 21. Próxima etapa

Crear y revisar el baseline visual `UI-DASHBOARD-001 v1-theme-pair` sobre esta especificación.

No debe iniciarse la implementación React antes de aprobación visual explícita.

## 22. Change history

- `v0.1` — primera especificación gobernada de `UI-DASHBOARD-001`, reconciliada contra `main@bd51dd53c7d44644fd0e5bcdee4f8de7cb89eea4`; API específica del Dashboard clasificada como mock-only hasta implementación backend; shell reusable declarado obligatorio.
