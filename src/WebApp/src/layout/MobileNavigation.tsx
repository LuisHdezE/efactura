import { NavLink } from 'react-router-dom';
import {
  groupShellNavigationItems,
  navigationLabel,
  plannedNavigationProgressLabel,
  type ShellNavigationItem
} from '../app/routes';

interface MobileNavigationProps {
  items: ShellNavigationItem[];
}

export function MobileNavigation({ items }: MobileNavigationProps) {
  const groups = groupShellNavigationItems(items);

  return (
    <div className="ef-mobile-nav border-b px-3 py-2 lg:hidden">
      <nav className="flex gap-4 overflow-x-auto" aria-label="Navegación principal móvil">
        {groups.map((group) => (
          <div key={group.name} className="flex shrink-0 items-center gap-2">
            <span className="text-[8px] font-extrabold uppercase tracking-[.14em] text-slate-400">{group.name}</span>
            <div className="flex gap-2">
              {group.items.map((item) => {
                const label = navigationLabel(item);

                if (item.state === 'active') {
                  return (
                    <NavLink
                      key={item.webId}
                      to={item.capability.route}
                      className={({ isActive }) => `ef-mobile-link inline-flex items-center gap-1.5 ${isActive ? 'is-active' : ''}`}
                    >
                      <span aria-hidden="true">{item.icon}</span>
                      <span>{label}</span>
                    </NavLink>
                  );
                }

                const progressLabel = plannedNavigationProgressLabel(item);

                return (
                  <span
                    key={item.webId}
                    className="ef-mobile-link is-disabled inline-flex items-center gap-1.5"
                    data-progress={item.progress ?? 'roadmap'}
                    aria-disabled="true"
                    title={`${label} · ${progressLabel ?? 'En preparación'} · No disponible todavía`}
                  >
                    <span aria-hidden="true">{item.icon}</span>
                    <span>{label}</span>
                    {progressLabel ? <span className="ef-mobile-progress">{progressLabel}</span> : null}
                  </span>
                );
              })}
            </div>
          </div>
        ))}
      </nav>
    </div>
  );
}
