# UI-AUTH-001 — Acceso y sesión

Status: `VISUAL_APPROVED / REAL_INTEGRATION_PENDING / BINARY_PRESERVATION_PENDING`

Upstream interface scope ID: `WEB-001`

Reconciliation: `documentation/ui/inventory/UI-AUTH-001_RECONCILIATION.md`

## 1. Objetivo

Ofrecer la entrada segura a la WebApp y establecer, después de una autenticación externa válida, el contexto de aplicación autorizado del usuario sin convertir la API de negocio en emisor de credenciales.

La vista debe separar con claridad:

- autenticación externa;
- resolución del actor/contexto de aplicación;
- navegación hacia las funciones autorizadas.

## 2. Usuarios y roles

Estados de usuario relevantes del upstream:

- `guest`: usuario todavía no autenticado;
- `operator`;
- `administrator`.

Los nombres de rol son agrupaciones/presentación. La autorización real se basa en permisos/policies y alcance de compañía/local/terminal.

## 3. Requisitos de negocio

- `FR-002` — autenticar usuarios y aplicar autorización permission/policy en operaciones protegidas;
- `FR-003` — asociar usuarios con contextos permitidos de organización/local/terminal;
- `NFR-001` — secretos/credenciales fuera de configuración comprometida y mínimo privilegio;
- `NFR-015` — mínimo dato personal/fiscal requerido y secretos/configuración sensible redactados.

## 4. Historias de usuario

No se evidencia un artefacto `US-*` específico para la entrada web de sesión.

La especificación registra el hueco y no inventa un identificador.

## 5. Casos de uso

- `UC-IAM-001 — Authenticate and establish session`.

El caso de uso target incluye validación de identidad, usuario activo, contexto permitido, token corto, refresh-session y auditoría. La arquitectura/API contractual vigente ubica adquisición/refresh/logout del token fuera de la API de negocio, en el proveedor de identidad/deployment lifecycle.

## 6. Entrada y navegación

Entrada principal:

- acceso a la WebApp sin sesión de aplicación resuelta;
- retorno desde el proveedor de identidad cuando exista integración real;
- expiración/rechazo de una sesión previamente activa.

Destino después de una sesión válida:

- la primera vista autorizada definida por navegación/aplicación;
- la demo actual usa `/pos`, pero esa redirección no constituye todavía una regla de producto para autenticación.

No se gobierna aún una ruta definitiva `/login` porque no existe una integración de proveedor aceptada.

## 7. Flujo funcional

### 7.1 Entrada no autenticada

1. Mostrar identidad de producto `eFactura`.
2. Explicar que el acceso es seguro y administrado externamente.
3. Ofrecer una única acción primaria provider-neutral, por ejemplo `Continuar al acceso seguro`.
4. No solicitar contraseña local ni simular un almacén de credenciales propio.
5. Al activar la acción, la integración real futura delegará en el proveedor de identidad.

### 7.2 Retorno de autenticación

1. Mostrar estado de procesamiento.
2. La integración de identidad valida/recupera la sesión externa.
3. La WebApp resuelve contexto de aplicación con `getCurrentActor` cuando ese endpoint sea ejecutable.
4. Si el actor/contexto es válido, la navegación continúa.
5. Si no, mostrar estado 401/403/error sin exponer claims o detalles sensibles.

### 7.3 Sesión lista

Cuando exista contexto autoritativo, la shell podrá mostrar únicamente metadatos seguros:

- nombre/display del actor;
- permisos efectivos relevantes para navegación;
- contexto de compañía/local/terminal permitido/resuelto;
- estado de sesión necesario para UX.

No mostrar token, refresh token, secretos, raw claims ni credenciales.

## 8. Datos mostrados

| Dato | Autoridad/fuente | Requerido | Notas |
| --- | --- | --- | --- |
| Nombre/producto | configuración WebApp | sí | `eFactura` / demo branding según ambiente |
| Estado de autenticación | integración IdP/WebApp | sí | nunca inferido por hardcoded shell |
| Display del actor | `getCurrentActor` | después de auth | contratado, no ejecutable actualmente |
| Permisos efectivos | `getCurrentActor` | después de auth | usar para navegación/UX, API revalida |
| Scopes compañía/local/terminal | `getCurrentActor` | cuando aplique | no inventar selector sin operación autorizada |
| Tema claro/oscuro | preferencia local | no | no es dato de seguridad |

## 9. Acciones

| Acción | Permiso/rol | Backend | Binding | Notas |
| --- | --- | --- | --- | --- |
| Continuar al acceso seguro | guest | `PENDING` | proveedor de identidad/deployment | no API-owned login |
| Resolver actor/contexto | authenticated | `CONTRACTED_NOT_EXECUTABLE` | `API-IAM-001 getCurrentActor` | `GET /api/v1/me` |
| Reintentar resolución | authenticated | `PENDING_RUNTIME` | mismo binding | solo ante fallo recuperable |
| Cerrar sesión | authenticated | `PENDING` | proveedor de identidad/deployment | no inventar endpoint API |
| Cambiar compañía/local/terminal | authenticated | `PENDING` | no operation aceptada para este flujo | no mostrar control activo |

## 10. Reglas visibles

- autenticación no equivale a autorización;
- el backend conserva autoridad final de permisos/scopes;
- la UI no debe mostrar controles activos para capacidades no autorizadas;
- la identidad visual de demo no equivale a sesión real;
- la vista no almacena ni muestra secretos;
- no se presume Auth0, Microsoft, Google u otro proveedor específico.

## 11. Validación

La primera versión visual no valida email/password porque no existe un formulario de credenciales local gobernado.

La integración futura debe mapear de forma segura:

- autenticación cancelada/fallida;
- sesión expirada;
- 401;
- 403;
- 429;
- fallo de red;
- error de resolución de contexto.

Los mensajes deben ser útiles sin revelar detalles de seguridad.

## 12. Estados

- `entry`;
- `redirecting_to_identity_provider`;
- `authentication_return`;
- `resolving_actor_context`;
- `ready`;
- `unauthorized_401`;
- `forbidden_403`;
- `rate_limited_429`;
- `session_expired`;
- `backend_error`;
- `network_unavailable`.

## 13. Errores y confirmaciones

Ejemplos de mensajes UX permitidos:

- `No se pudo completar el acceso. Intenta nuevamente.`
- `Tu sesión expiró. Vuelve a ingresar.`
- `Tu cuenta no tiene acceso a este entorno.`
- `Demasiados intentos. Espera un momento antes de reintentar.`

No mostrar stack traces, claims, IDs internos sensibles ni detalles de validación criptográfica.

## 14. Seguridad y permisos

- ningún secreto/token visible en DOM o mensajes;
- ningún credential field local hasta que exista una decisión explícita de IdP/flow;
- navegación puede ocultarse por permisos como conveniencia, pero la API revalida;
- scopes de organización/local/terminal son autoritativos del servidor/contexto;
- no persistir tokens en `localStorage` por defecto desde esta especificación;
- la estrategia de almacenamiento/sesión del navegador debe gobernarse en la integración real.

## 15. Responsive

### Desktop

- composición centrada y contenida;
- card/panel de acceso con jerarquía simple;
- branding coherente con POS/Clientes.

### Tablet

- mismo flujo, ancho fluido y touch targets amplios.

### Mobile web

- una sola columna;
- CTA principal ancho y accesible;
- contenido crítico visible sin scroll horizontal.

## 16. Accesibilidad

- flujo completamente operable por teclado;
- foco visible;
- acción primaria con nombre accesible;
- cambios de estado anunciados por región live cuando aplique;
- errores programáticos asociados al estado/acción;
- no usar solo color para success/error;
- respetar `prefers-reduced-motion` si se incorporan transiciones.

## 17. Dependencias de otras vistas

Después de sesión válida, la navegación podrá llevar a vistas autorizadas como:

- `UI-POS-001`;
- `UI-CUSTOMER-001`;
- futuras vistas gobernadas.

No se convierte ninguna vista posterior en requisito de autenticación.

## 18. Integración Backend/API

### Contratado

- `API-IAM-001 getCurrentActor` — `GET /api/v1/me` — `AUTHENTICATED`.

### No evidenciado como ejecutable en el baseline actual

- `getCurrentActor` no aparece en el inventario actual de controladores WebApi v1;
- no existe gateway auth/session en la WebApp.

### Externamente resuelto por diseño contractual

- token acquisition;
- refresh;
- logout/revocation;
- proveedor de identidad.

### Simulación temporal permitida para diseño/demo

Puede existir una experiencia visual de entrada en modo mock con estas condiciones:

- debe identificarse como demo/mock;
- no debe aceptar ni almacenar contraseñas reales;
- el CTA de acceso no debe afirmar que autentica contra la API;
- el contexto mostrado después puede ser mock únicamente si se etiqueta como tal;
- la simulación no autoriza cambios de API/backend.

## 19. Referencias visuales

| Versión | Artefacto | Estado | Evidencia |
| --- | --- | --- | --- |
| `v1-provider-neutral` | `../references/approved/UI-AUTH-001/v1-provider-neutral/` | `VISUAL_APPROVED` | Luis: `aprobado`, 2026-09-17 |

La versión aprobada conserva el lenguaje light/dark de POS/Clientes y mantiene la separación IdP/API: no presenta campos locales de usuario/contraseña, recuperación de contraseña ni branding de un proveedor de identidad no seleccionado.

El origen visual exacto está fijado por SHA-256 en el registro de autoridad. La inserción binaria byte-identical del PNG en Git queda pendiente por limitación del canal actual; no se autoriza regenerar o sustituir silenciosamente la imagen aprobada.

## 20. Registro de aprobación

- Approved version: `UI-AUTH-001 v1-provider-neutral`
- Approval date: `2026-09-17`
- Approval statement/reference: `aprobado`
- Approved authority record: `documentation/ui/references/approved/UI-AUTH-001/v1-provider-neutral/README.md`
- Source manifest: `documentation/ui/references/approved/UI-AUTH-001/v1-provider-neutral/visual-source-manifest.json`
- Source PNG SHA-256: `ed2a9039dd60c8eff2398197f48b6cc1d71d8c3457630fedb0998d2615584834`
- Binary preservation: `PENDING_DIRECT_BINARY_TRANSFER`

## 21. Evidencia de implementación

- frontend route/component: `NONE`
- auth/session gateway: `NONE`
- implementation commit/PR: `NONE`
- running capture: `NONE`
- visual review: `NONE`

## 22. Change history

- `v0.1` — `WEB-001` reconciliado como `UI-AUTH-001`; se preserva la separación entre IdP/deployment y API de negocio, y se documentan los huecos de integración reales.
- `v0.2` — Luis aprueba explícitamente `UI-AUTH-001 v1-provider-neutral`; se registra la autoridad visual light/dark, su fingerprint SHA-256 y se mantiene la integración real de identidad como pendiente.
