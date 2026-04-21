// BudgetPage — Coaching mode. Comfortable density.
// Hero "Left to Budget", summary cards, category progress bars, savings goals.

function BudgetPage() {
  const categories = [
    { label: 'Housing', spent: 2200, budget: 2400 },
    { label: 'Groceries', spent: 420, budget: 600 },
    { label: 'Utilities', spent: 180, budget: 250 },
    { label: 'Gas', spent: 82, budget: 200 },
    { label: 'Dining out', spent: 312, budget: 250 },
    { label: 'Kids activities', spent: 145, budget: 200 },
  ];
  const goals = [
    { label: 'Emergency fund', saved: 8400, target: 15000 },
    { label: 'Family vacation — Italy', saved: 2150, target: 6000 },
    { label: 'New roof', saved: 900, target: 12000 },
  ];

  return (
    <div style={{ padding: '28px 40px 60px', maxWidth: 1200, margin: '0 auto' }}>
      <header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 24 }}>
        <div>
          <div style={{ fontSize: 12, color: 'var(--sr-fg-3)', fontWeight: 500, marginBottom: 2 }}>March 2026</div>
          <h1 style={{ margin: 0, fontSize: 28, fontWeight: 700, letterSpacing: '-0.015em' }}>Budget</h1>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <SrButton variant="flat" icon="event">March 2026</SrButton>
          <SrButton variant="primary" icon="add">Add Transaction</SrButton>
        </div>
      </header>

      <section style={{
        background: 'linear-gradient(180deg, #fff 0%, #F8FAFF 100%)',
        borderRadius: 22, padding: 28,
        boxShadow: 'var(--sr-shadow-subtle)',
        display: 'grid', gridTemplateColumns: '1.3fr 1fr 1fr', gap: 32,
        alignItems: 'center', marginBottom: 24,
      }}>
        <div>
          <div style={{ fontSize: 13, color: 'var(--sr-fg-3)', fontWeight: 500 }}>Left to Budget</div>
          <div style={{ fontSize: 44, fontWeight: 700, color: 'var(--sr-positive)', fontVariantNumeric: 'tabular-nums', letterSpacing: '-0.02em', marginTop: 4 }}>
            $16,281.58
          </div>
          <div style={{ fontSize: 12, color: 'var(--sr-fg-3)', marginTop: 4 }}>of $24,000 monthly income</div>
        </div>
        <div style={{ borderLeft: '1px solid var(--sr-divider)', paddingLeft: 24 }}>
          <div style={{ fontSize: 12, color: 'var(--sr-fg-3)' }}>Income Received</div>
          <div style={{ fontSize: 22, fontWeight: 600, fontVariantNumeric: 'tabular-nums', marginTop: 2 }}>$22,500.00</div>
          <div style={{ fontSize: 11, color: 'var(--sr-fg-3)' }}>94% of expected</div>
        </div>
        <div style={{ borderLeft: '1px solid var(--sr-divider)', paddingLeft: 24 }}>
          <div style={{ fontSize: 12, color: 'var(--sr-fg-3)' }}>Spent this month</div>
          <div style={{ fontSize: 22, fontWeight: 600, fontVariantNumeric: 'tabular-nums', marginTop: 2 }}>$6,218.42</div>
          <div style={{ fontSize: 11, color: 'var(--sr-fg-3)' }}>across 6 categories</div>
        </div>
      </section>

      <section style={{ display: 'grid', gridTemplateColumns: '1.4fr 1fr', gap: 20 }}>
        <div style={{ background: '#fff', borderRadius: 18, padding: 22, boxShadow: 'var(--sr-shadow-subtle)' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 18 }}>
            <h2 style={{ margin: 0, fontSize: 16, fontWeight: 600 }}>Category Progress</h2>
            <SrButton variant="flat" size="sm" icon="edit">Edit budget</SrButton>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
            {categories.map((c) => <ProgressRow key={c.label} {...c} />)}
          </div>
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          <div style={{ background: '#fff', borderRadius: 18, padding: 22, boxShadow: 'var(--sr-shadow-subtle)' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 14 }}>
              <h2 style={{ margin: 0, fontSize: 16, fontWeight: 600 }}>Savings Goals</h2>
              <SrButton variant="flat" size="sm" icon="add">New</SrButton>
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
              {goals.map((g) => {
                const pct = Math.round((g.saved / g.target) * 100);
                return (
                  <div key={g.label} style={{ display: 'flex', flexDirection: 'column', gap: 6, fontVariantNumeric: 'tabular-nums' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13 }}>
                      <span style={{ fontWeight: 500 }}>{g.label}</span>
                      <span style={{ color: 'var(--sr-fg-3)' }}>{pct}%</span>
                    </div>
                    <div style={{ height: 6, background: '#E2E8F0', borderRadius: 3, overflow: 'hidden' }}>
                      <div style={{ height: '100%', width: `${pct}%`, background: 'var(--sr-primary)' }} />
                    </div>
                    <div style={{ fontSize: 11, color: 'var(--sr-fg-3)' }}>
                      ${g.saved.toLocaleString()} of ${g.target.toLocaleString()}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          <div style={{
            background: 'var(--sr-primary-soft)', borderRadius: 18, padding: 18,
            display: 'flex', gap: 12, alignItems: 'flex-start',
          }}>
            <Icon name="info" color="var(--sr-primary)" size={20} />
            <div style={{ fontSize: 13, lineHeight: 1.5 }}>
              We detected legacy Savings categories. <a style={{ color: 'var(--sr-primary)', fontWeight: 600, textDecoration: 'none' }}>Convert them to the new Savings Goals feature →</a>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}

Object.assign(window, { BudgetPage });
