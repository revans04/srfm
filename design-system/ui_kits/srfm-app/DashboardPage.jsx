// DashboardPage — Coaching mode. Net worth, income vs spend, account tiles.

const ACCOUNTS = [
  { name: 'Chase Checking', kind: 'Checking', balance: 12402.18 },
  { name: 'Amex Gold', kind: 'Credit card', balance: -2318.54 },
  { name: 'Ally Savings', kind: 'Savings', balance: 28450.00 },
  { name: 'Fidelity Brokerage', kind: 'Investment', balance: 184620.15 },
  { name: 'Vanguard 401(k)', kind: 'Retirement', balance: 412830.22 },
  { name: 'Walton Rental — Ops', kind: 'Business checking', balance: 8912.00 },
];

function fmtUSD(n) {
  const abs = Math.abs(n).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  return (n < 0 ? '-$' : '$') + abs;
}

function DashboardPage() {
  return (
    <div style={{ padding: '28px 40px 60px', maxWidth: 1200, margin: '0 auto' }}>
      <header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 24 }}>
        <div>
          <div style={{ fontSize: 12, color: 'var(--sr-fg-3)', fontWeight: 500, marginBottom: 2 }}>Good morning, Julia</div>
          <h1 style={{ margin: 0, fontSize: 28, fontWeight: 700, letterSpacing: '-0.015em' }}>Dashboard</h1>
        </div>
        <SrButton variant="primary" icon="add">Add Transaction</SrButton>
      </header>

      <section style={{ display: 'grid', gridTemplateColumns: '1.4fr 1fr 1fr', gap: 16, marginBottom: 20 }}>
        <StatCard label="Net Worth" value="$2,070,366.38" delta={{ text: '▲ 2.1% vs last month', tone: 'positive' }} />
        <StatCard label="Income (MTD)" value="$22,500.00" tone="positive" delta={{ text: '94% of expected' }} />
        <StatCard label="Spent (MTD)" value="$6,218.42" delta={{ text: 'Across 6 categories' }} />
      </section>

      <section style={{ display: 'grid', gridTemplateColumns: '1.4fr 1fr', gap: 20 }}>
        <div style={{ background: '#fff', borderRadius: 18, padding: 22, boxShadow: 'var(--sr-shadow-subtle)' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 16 }}>
            <h2 style={{ margin: 0, fontSize: 16, fontWeight: 600 }}>Accounts</h2>
            <SrButton variant="flat" size="sm" icon="refresh">Refresh</SrButton>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column' }}>
            {ACCOUNTS.map((a) => (
              <div key={a.name} style={{
                display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                padding: '14px 4px', borderBottom: '1px solid var(--sr-divider)',
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                  <div style={{
                    width: 34, height: 34, borderRadius: 10, background: 'var(--sr-primary-soft)',
                    color: 'var(--sr-primary)', display: 'flex', alignItems: 'center', justifyContent: 'center',
                  }}>
                    <Icon name="account_balance" size={18} />
                  </div>
                  <div>
                    <div style={{ fontSize: 14, fontWeight: 500 }}>{a.name}</div>
                    <div style={{ fontSize: 11, color: 'var(--sr-fg-3)' }}>{a.kind}</div>
                  </div>
                </div>
                <div style={{
                  fontSize: 15, fontWeight: 600, fontVariantNumeric: 'tabular-nums',
                  color: a.balance < 0 ? 'var(--sr-negative)' : 'var(--sr-fg-1)',
                }}>
                  {fmtUSD(a.balance)}
                </div>
              </div>
            ))}
          </div>
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div style={{ background: '#fff', borderRadius: 18, padding: 22, boxShadow: 'var(--sr-shadow-subtle)' }}>
            <h2 style={{ margin: '0 0 12px', fontSize: 16, fontWeight: 600 }}>Upcoming Bills</h2>
            <div style={{ fontSize: 13, color: 'var(--sr-fg-3)', marginBottom: 10 }}>No upcoming bills yet.</div>
            <a style={{ color: 'var(--sr-primary)', fontSize: 13, fontWeight: 600, textDecoration: 'none', display: 'inline-flex', alignItems: 'center', gap: 4, cursor: 'pointer' }}>
              Import data <Icon name="arrow_forward" size={16} />
            </a>
          </div>

          <div style={{ background: '#fff', borderRadius: 18, padding: 22, boxShadow: 'var(--sr-shadow-subtle)' }}>
            <h2 style={{ margin: '0 0 12px', fontSize: 16, fontWeight: 600 }}>Budget Progress</h2>
            <div style={{ fontSize: 13, color: 'var(--sr-fg-3)', marginBottom: 6 }}>March 2026 — 68% through the month</div>
            <div style={{ display: 'flex', alignItems: 'baseline', gap: 6, marginBottom: 14 }}>
              <span style={{ fontSize: 24, fontWeight: 600, color: 'var(--sr-positive)', fontVariantNumeric: 'tabular-nums' }}>$16,281.58</span>
              <span style={{ fontSize: 12, color: 'var(--sr-fg-3)' }}>left to spend</span>
            </div>
            <div style={{ height: 6, background: '#E2E8F0', borderRadius: 3, overflow: 'hidden' }}>
              <div style={{ height: '100%', width: '26%', background: 'var(--sr-positive)' }} />
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}

Object.assign(window, { DashboardPage });
