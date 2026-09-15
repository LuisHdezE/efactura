import { NavLink, Outlet } from 'react-router-dom';
import { uiCapabilities } from '../app/capabilities';

export function AppShell() {
  return (
    <div className="min-h-screen bg-slate-50 lg:grid lg:grid-cols-[248px_1fr]">
      <aside className="hidden min-h-screen bg-slate-950 px-5 py-6 text-slate-100 lg:flex lg:flex-col">
        <div className="mb-8 flex items-center gap-3">
          <div className="grid h-10 w-10 place-items-center rounded-xl bg-blue-600 text-2xl font-bold">e</div>
          <div><div className="text-xl font-semibold">eFactura</div><div className="text-xs text-slate-400">Uruguay · Demo</div></div>
        </div>
        <nav className="space-y-2">
          {uiCapabilities.map((item) => (
            <NavLink key={item.uiId} to={item.route} className={({ isActive }) => `block rounded-xl px-4 py-3 text-sm font-medium transition ${isActive ? 'bg-blue-600 text-white' : 'text-slate-300 hover:bg-slate-900 hover:text-white'}`}>
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="mt-auto rounded-xl border border-slate-800 bg-slate-900/70 p-4 text-xs leading-5 text-slate-400">
          <div className="mb-1 font-semibold text-slate-200">Datos simulados</div>
          La navegación y los campos visibles se limitan a capacidades respaldadas por la API actual.
        </div>
      </aside>

      <main className="min-w-0">
        <header className="sticky top-0 z-20 flex h-16 items-center justify-between border-b border-slate-200 bg-white/95 px-4 backdrop-blur sm:px-6 lg:px-8">
          <div className="flex items-center gap-3 lg:hidden">
            <div className="grid h-9 w-9 place-items-center rounded-lg bg-blue-600 text-xl font-bold text-white">e</div>
            <span className="font-semibold">eFactura</span>
          </div>
          <div className="hidden text-sm text-slate-500 lg:block">Tienda Central</div>
          <div className="flex items-center gap-3">
            <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-bold tracking-wide text-amber-800">DEMO</span>
            <div className="text-right"><div className="text-sm font-semibold">Carlos A.</div><div className="text-xs text-slate-500">Operador</div></div>
          </div>
        </header>
        <div className="border-b border-slate-200 bg-white px-4 py-2 lg:hidden">
          <nav className="flex gap-2 overflow-x-auto">
            {uiCapabilities.map((item) => (
              <NavLink key={item.uiId} to={item.route} className={({ isActive }) => `whitespace-nowrap rounded-lg px-3 py-2 text-sm font-medium ${isActive ? 'bg-blue-600 text-white' : 'bg-slate-100 text-slate-700'}`}>{item.label}</NavLink>
            ))}
          </nav>
        </div>
        <Outlet />
      </main>
    </div>
  );
}
