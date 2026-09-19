import { NavLink } from 'react-router-dom';
import {
  groupShellNavigationItems,
  navigationLabel,
  type ShellNavigationItem
} from '../app/routes';

interface SidebarProps {
  items: ShellNavigationItem[];
}

export function Sidebar({ items }: SidebarProps) {
  const groups = groupShellNavigationItems(items);

  return (
    <aside className="ef-sidebar hidden lg:flex lg:flex-col">
      <nav className="ef-sidebar-nav" aria-label="Navegación principal">
        {groups.map((group) => (
          <div key={group.name} className="ef-nav-group">
            <div className="ef-nav-group-title">{group.name}</div>
            {group.items.map((item) => {
              const label = navigationLabel(item);

              if (item.state === 'active') {
                return (
                  <NavLink
                    key={item.webId}
                    to={item.capability.route}
                    className={({ isActive }) => `ef-nav-link ${isActive ? 'is-active' : ''}`}
                  >
                    <span className="ef-nav-icon" aria-hidden="true">{item.icon}</span>
                    <span className="ef-nav-label">{label}</span>
                  </NavLink>
                );
              }

              return (
                <div
                  key={item.webId}
                  className="ef-nav-item-disabled"
                  aria-disabled="true"
                  title={`${label} · En preparación`}
                >
                  <span className="ef-nav-icon" aria-hidden="true">{item.icon}</span>
                  <span className="ef-nav-label">{label}</span>
                </div>
              );
            })}
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
