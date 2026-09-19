import { NavLink } from 'react-router-dom';
import type { ShellRouteDefinition } from '../app/routes';

interface MobileNavigationProps {
  routes: ShellRouteDefinition[];
}

export function MobileNavigation({ routes }: MobileNavigationProps) {
  return (
    <div className="ef-mobile-nav border-b px-3 py-2 lg:hidden">
      <nav className="flex gap-2 overflow-x-auto" aria-label="Navegación principal móvil">
        {routes.map(({ capability }) => (
          <NavLink
            key={capability.uiId}
            to={capability.route}
            className={({ isActive }) => `ef-mobile-link ${isActive ? 'is-active' : ''}`}
          >
            {capability.uiId === 'UI-POS-001' ? 'POS' : capability.label}
          </NavLink>
        ))}
      </nav>
    </div>
  );
}
