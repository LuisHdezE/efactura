import { NavLink } from 'react-router-dom';
import { groupShellRoutes, type ShellRouteDefinition } from '../app/routes';

interface MobileNavigationProps {
  routes: ShellRouteDefinition[];
}

export function MobileNavigation({ routes }: MobileNavigationProps) {
  const groups = groupShellRoutes(routes);

  return (
    <div className="ef-mobile-nav border-b px-3 py-2 lg:hidden">
      <nav className="flex gap-4 overflow-x-auto" aria-label="Navegación principal móvil">
        {groups.map((group) => (
          <div key={group.name} className="flex shrink-0 items-center gap-2">
            <span className="text-[8px] font-extrabold uppercase tracking-[.14em] text-slate-400">
              {group.name}
            </span>
            <div className="flex gap-2">
              {group.routes.map(({ capability, icon }) => (
                <NavLink
                  key={capability.uiId}
                  to={capability.route}
                  className={({ isActive }) => `ef-mobile-link inline-flex items-center gap-1.5 ${isActive ? 'is-active' : ''}`}
                >
                  <span aria-hidden="true">{icon}</span>
                  <span>{capability.label}</span>
                </NavLink>
              ))}
            </div>
          </div>
        ))}
      </nav>
    </div>
  );
}
