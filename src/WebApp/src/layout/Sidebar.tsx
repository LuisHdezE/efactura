import { NavLink } from 'react-router-dom';
import type { ShellRouteDefinition } from '../app/routes';

interface SidebarProps {
  routes: ShellRouteDefinition[];
}

export function Sidebar({ routes }: SidebarProps) {
  return (
    <aside className="ef-sidebar hidden lg:flex lg:flex-col">
      <nav className="space-y-1" aria-label="Navegación principal">
        {routes.map(({ capability, icon }) => (
          <NavLink
            key={capability.uiId}
            to={capability.route}
            className={({ isActive }) => `ef-nav-link ${isActive ? 'is-active' : ''}`}
          >
            <span className="ef-nav-icon" aria-hidden="true">{icon}</span>
            <span>{capability.uiId === 'UI-POS-001' ? 'POS' : capability.label}</span>
          </NavLink>
        ))}
      </nav>

      <div className="ef-sidebar-footer">
        <span>🇺🇾</span>
        <strong>UY</strong>
      </div>
    </aside>
  );
}
