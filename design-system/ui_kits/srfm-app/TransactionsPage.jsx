// TransactionsPage — Auditing mode. Compact density.
// Tabs (Budget / Account / Match) + sticky filter bar + ledger table.
// Rows open Inspector for editing (never a modal on desktop).

const TABS = [
  { key: 'budget', label: 'BUDGET REGISTER', tip: 'Transactions matched to your budget categories' },
  { key: 'account', label: 'ACCOUNT REGISTER', tip: 'Transactions imported from your bank account' },
  { key: 'match', label: 'MATCH BANK TRANSACTIONS', tip: 'Link imported bank transactions to your budget entries' },
];

const TXNS = [
  { id: 1, date: '2026-03-15', payee: 'Kroger', category: 'Food › Groceries', account: 'Chase Checking', memo: 'Weekly trip', amount: -42.19, status: 'C' },
  { id: 2, date: '2026-03-15', payee: 'Shell', category: 'Auto › Gas', account: 'Chase Checking', memo: '', amount: -38.02, status: 'C' },
  { id: 3, date: '2026-03-14', payee: 'Evans & Co. Payroll', category: 'Income › Salary', account: 'Chase Checking', memo: 'Bi-weekly', amount: 4215.00, status: 'R' },
  { id: 4, date: '2026-03-14', payee: 'Netflix', category: 'Entertainment › Streaming', account: 'Amex Gold', memo: '', amount: -22.99, status: 'C' },
  { id: 5, date: '2026-03-13', payee: 'Target', category: 'Home › Supplies', account: 'Amex Gold', memo: 'Paper towels, batteries', amount: -87.43, status: 'C' },
  { id: 6, date: '2026-03-12', payee: 'Duke Energy', category: 'Utilities › Electric', account: 'Chase Checking', memo: '', amount: -142.18, status: 'R' },
  { id: 7, date: '2026-03-12', payee: "Lucia's Piano Lessons", category: 'Kids › Activities', account: 'Chase Checking', memo: 'Monthly', amount: -120.00, status: 'C' },
  { id: 8, date: '2026-03-11', payee: 'Venmo — Sam Chen', category: 'Transfer', account: 'Chase Checking', memo: 'Soccer carpool', amount: 40.00, status: 'U' },
  { id: 9, date: '2026-03-10', payee: 'Costco', category: 'Food › Groceries', account: 'Amex Gold', memo: '', amount: -312.87, status: 'C' },
  { id: 10, date: '2026-03-10', payee: 'Shell', category: 'Auto › Gas', account: 'Amex Gold', memo: '', amount: -52.10, status: 'C' },
  { id: 11, date: '2026-03-09', payee: 'Spotify Family', category: 'Entertainment › Streaming', account: 'Amex Gold', memo: '', amount: -16.99, status: 'C' },
  { id: 12, date: '2026-03-08', payee: 'ATM Withdrawal', category: 'Uncategorized', account: 'Chase Checking', memo: 'Main St branch', amount: -200.00, status: 'U' },
];

function fmtMoney(n) {
  const abs = Math.abs(n).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  return (n < 0 ? '-$' : '$') + abs;
}
function fmtDate(iso) {
  const d = new Date(iso + 'T00:00:00');
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

function TransactionsPage({ onSelect, selectedId }) {
  const [tab, setTab] = React.useState('budget');
  const [filters, setFilters] = React.useState(['March 2026', 'Cleared']);
  const [search, setSearch] = React.useState('');

  return (
    <div style={{ padding: '28px 40px 60px', maxWidth: 1400, margin: '0 auto' }}>
      <header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 18 }}>
        <div>
          <div style={{ fontSize: 12, color: 'var(--sr-fg-3)', fontWeight: 500, marginBottom: 2 }}>All accounts</div>
          <h1 style={{ margin: 0, fontSize: 28, fontWeight: 700, letterSpacing: '-0.015em' }}>Transactions</h1>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <SrButton variant="outline" icon="file_upload">Import</SrButton>
          <SrButton variant="outline" icon="download">Export</SrButton>
          <SrButton variant="primary" icon="add">Add Transaction</SrButton>
        </div>
      </header>

      {/* Tabs */}
      <div style={{
        display: 'flex', gap: 0, borderBottom: '1px solid var(--sr-divider)',
        marginBottom: 16,
      }}>
        {TABS.map((t) => (
          <button
            key={t.key}
            title={t.tip}
            onClick={() => setTab(t.key)}
            style={{
              background: 'transparent', border: 'none', cursor: 'pointer',
              padding: '12px 18px', font: 'inherit', fontSize: 12, fontWeight: 600,
              letterSpacing: '0.04em', color: tab === t.key ? 'var(--sr-primary)' : 'var(--sr-fg-2)',
              borderBottom: `2px solid ${tab === t.key ? 'var(--sr-primary)' : 'transparent'}`,
              marginBottom: -1, transition: 'color 150ms, border-color 150ms',
            }}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* Sticky filter bar */}
      <div style={{
        background: '#fff', border: '1px solid var(--sr-divider)', borderRadius: 12,
        padding: '10px 14px', display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap',
        marginBottom: 14,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6, flex: '1 1 280px', maxWidth: 360 }}>
          <Icon name="search" size={18} color="var(--sr-fg-3)" />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search payee, memo, amount"
            style={{ border: 'none', outline: 'none', background: 'transparent', flex: 1, font: 'inherit', fontSize: 13 }}
          />
        </div>
        <div style={{ flex: 1 }} />
        <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}>
          {filters.map((f) => (
            <SrChip key={f} active removable onRemove={() => setFilters(filters.filter(x => x !== f))}>{f}</SrChip>
          ))}
          <SrChip outline onClick={() => {}}>+ Add filter</SrChip>
        </div>
        <div style={{ width: 1, height: 24, background: 'var(--sr-divider)', margin: '0 4px' }} />
        <IconBtn name="refresh" tooltip="Refresh" />
        <IconBtn name="tune" tooltip="Columns" />
      </div>

      {/* Ledger table */}
      <div style={{
        background: '#fff', borderRadius: 12, border: '1px solid var(--sr-divider)',
        overflow: 'hidden',
      }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
          <thead>
            <tr style={{ background: '#F8FAFC' }}>
              {['Status', 'Date', 'Payee', 'Category', 'Account', 'Memo', 'Amount', ''].map((h, i) => (
                <th key={i} style={{
                  textAlign: i === 6 ? 'right' : 'left', fontWeight: 500, color: 'var(--sr-fg-2)',
                  fontSize: 11, letterSpacing: '0.04em', textTransform: 'uppercase',
                  padding: '8px 12px', borderBottom: '1px solid var(--sr-divider)',
                }}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {TXNS.map((t) => {
              const selected = t.id === selectedId;
              return (
                <tr
                  key={t.id}
                  onClick={() => onSelect(t)}
                  style={{
                    cursor: 'pointer',
                    background: selected ? 'var(--sr-surface-subtle)' : 'transparent',
                    transition: 'background 150ms ease',
                  }}
                  onMouseEnter={(e) => { if (!selected) e.currentTarget.style.background = '#F8FAFC'; }}
                  onMouseLeave={(e) => { if (!selected) e.currentTarget.style.background = 'transparent'; }}
                >
                  <td style={td(0)}><StatusBadge code={t.status} /></td>
                  <td style={td()}>{fmtDate(t.date)}</td>
                  <td style={{ ...td(), fontWeight: 500 }}>{t.payee}</td>
                  <td style={{ ...td(), color: 'var(--sr-fg-2)' }}>{t.category}</td>
                  <td style={{ ...td(), color: 'var(--sr-fg-2)' }}>{t.account}</td>
                  <td style={{ ...td(), color: 'var(--sr-fg-3)', maxWidth: 160, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{t.memo || '—'}</td>
                  <td style={{ ...td(), textAlign: 'right', fontVariantNumeric: 'tabular-nums', fontWeight: 500, color: t.amount < 0 ? 'var(--sr-negative)' : 'var(--sr-positive)' }}>
                    {fmtMoney(t.amount)}
                  </td>
                  <td style={{ ...td(), width: 44, textAlign: 'right' }}>
                    <Icon name="chevron_right" size={18} color="var(--sr-fg-3)" />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>

        <div style={{
          padding: '10px 14px', display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          borderTop: '1px solid var(--sr-divider)', fontSize: 12, color: 'var(--sr-fg-3)',
        }}>
          <div>Showing 12 of 183 transactions</div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
            <IconBtn name="chevron_left" tooltip="Previous" />
            <span>Page 1 of 16</span>
            <IconBtn name="chevron_right" tooltip="Next" />
          </div>
        </div>
      </div>
    </div>
  );
}

function td() { return { padding: '9px 12px', borderBottom: '1px solid var(--sr-divider)', verticalAlign: 'middle' }; }

Object.assign(window, { TransactionsPage, TXNS });
