# UI-SUPPLIER-001 Runtime Polish

Status: `APPLIED / PENDING REDEPLOY REVIEW`

## Runtime review findings

The first deployed implementation matched the approved structure in light and dark modes, with three minor presentation issues remaining:

1. `Nuevo proveedor` was functionally disabled but still looked too similar to an enabled primary action.
2. The supplier detail tabs exposed an unnecessary visible scrollbar artifact.
3. Planned Sidebar options in dark mode were too low-contrast to read comfortably.

## Applied correction

- Disabled primary action now uses neutral surface/border/text styling and no longer looks executable.
- Tab overflow behavior remains available when needed, but its scrollbar chrome is hidden.
- Dark-mode planned Sidebar options receive a modest opacity increase while remaining visibly disabled.

## Scope protection

No backend, API, route, fixture, gateway, feature behavior, capability mapping, or navigation activation state changes are included in this polish.
