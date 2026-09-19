import { NavLink } from 'react-router-dom';
import { groupShellRoutes, type ShellRouteDefinition } from '../app/routes';

interface SidebarProps {
  routes: ShellRouteDefinition[];
}

export function Sidebar({ routes }: SidebarProps) {
  const groups = groupShellRoutes(routes);

  return (
    <aside className="ef-sidebar hidden lg:flex lg:flex-col">
      <nav className="space-y-3" aria-label="Navegación principal">
        {groups.map((group) => (
          <div key={group.name} className="space-y-1">
            <div className="px-1 pt-1 text-center text-[8px] font-extrabold uppercase tracking-[.14em] text-white/35">
              {group.name}
            </div>
            {group.routes.map(({ capability, icon }) => (
              <NavLink
                key={capability.uiId}
                to={capability.route}
                className={({ isActive }) => `ef-nav-link ${isActive ? 'is-active' : ''}`}
              >
                <span className="ef-nav-icon" aria-hidden="true">{icon}</span>
                <span className="text-center leading-tight">{capability.label}</span>
              </NavLink>
            ))}
          </div>
        ))}
      </nav>

      <div className="ef-sidebar-footer">
        <span>🇺🇾</span>
        <strong>UY</strong>
      </div>
    </aside>
  );
}
