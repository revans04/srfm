// AppShell — persistent left drawer with brand lockup + nav list.
// White surface, soft-indigo active pill per the "no-chrome" layout rule.

const NAV_ITEMS = [
  { key: 'dashboard', label: 'Dashboard', icon: 'dashboard' },
  { key: 'budget', label: 'Budget', icon: 'savings' },
  { key: 'transactions', label: 'Transactions', icon: 'format_list_bulleted' },
  { key: 'accounts', label: 'Accounts', icon: 'account_balance' },
  { key: 'reports', label: 'Reports', icon: 'trending_up' },
  { key: 'data', label: 'Data Mgmt', icon: 'dataset' },
  { key: 'settings', label: 'Settings', icon: 'manage_accounts' },
];

function NavItem({ item, active, onClick }) {
  const [hover, setHover] = React.useState(false);
  const bg = active ? 'var(--sr-surface-subtle)' : hover ? '#F8FAFC' : 'transparent';
  const color = active ? 'var(--sr-primary)' : 'var(--sr-fg-1)';
  return (
    <button
      onClick={onClick}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      style={{
        display: 'flex', alignItems: 'center', gap: 12,
        padding: '9px 12px', borderRadius: 12,
        background: bg, color, border: 'none', cursor: 'pointer',
        font: 'inherit', fontSize: 14, fontWeight: active ? 600 : 500,
        textAlign: 'left', transition: 'background 150ms ease, color 150ms ease',
      }}
    >
      <Icon name={item.icon} size={20} color={color} />
      <span>{item.label}</span>
    </button>
  );
}

function AppShell({ active, onNavigate, children }) {
  return (
    <div style={{ display: 'flex', minHeight: '100vh', background: 'var(--sr-page)' }}>
      <aside style={{
        width: 236, flexShrink: 0, background: '#fff',
        borderRight: '1px solid var(--sr-divider)',
        padding: '20px 14px', display: 'flex', flexDirection: 'column', gap: 18,
        position: 'sticky', top: 0, height: '100vh',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '2px 6px' }}>
          <img src="../../assets/logo-sm.png" alt="" style={{ width: 36, height: 36, borderRadius: 8 }} />
          <div>
            <div style={{ fontSize: 15, fontWeight: 600, letterSpacing: '-0.01em' }}>Steady Rise</div>
            <div style={{ fontSize: 11, color: 'var(--sr-fg-3)' }}>Family Money</div>
          </div>
        </div>

        <div style={{
          display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          padding: '8px 12px', border: '1px solid var(--sr-divider)', borderRadius: 12,
          background: '#F8FAFC',
        }}>
          <div>
            <div style={{ fontSize: 11, color: 'var(--sr-fg-3)' }}>Entity</div>
            <div style={{ fontSize: 13, fontWeight: 600 }}>Evans Family</div>
          </div>
          <Icon name="unfold_more" size={18} color="var(--sr-fg-3)" />
        </div>

        <nav style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {NAV_ITEMS.map((it) => (
            <NavItem key={it.key} item={it} active={active === it.key} onClick={() => onNavigate(it.key)} />
          ))}
        </nav>

        <div style={{ marginTop: 'auto', borderTop: '1px solid var(--sr-divider)', paddingTop: 14 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <div style={{ width: 32, height: 32, borderRadius: 999, background: 'var(--sr-primary-soft)', color: 'var(--sr-primary)', display: 'inline-flex', alignItems: 'center', justifyContent: 'center', fontWeight: 600, fontSize: 13 }}>JE</div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ fontSize: 13, fontWeight: 600 }}>Julia Evans</div>
              <div style={{ fontSize: 11, color: 'var(--sr-fg-3)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>julia@evansfamily.com</div>
            </div>
            <IconBtn name="logout" tooltip="Sign out" />
          </div>
        </div>
      </aside>

      <main style={{ flex: 1, minWidth: 0 }}>{children}</main>
    </div>
  );
}

Object.assign(window, { AppShell });
