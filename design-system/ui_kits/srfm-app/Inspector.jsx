// Inspector — right-side drawer that replaces modals on desktop for ledger editing.
// On mobile, same component would re-render as a full-screen dialog (not implemented here).

function Inspector({ txn, onClose }) {
  const [form, setForm] = React.useState(() => ({ ...(txn || {}) }));
  React.useEffect(() => { setForm({ ...(txn || {}) }); }, [txn?.id]);

  if (!txn) return null;

  const update = (k) => (e) => setForm({ ...form, [k]: e.target.value });

  return (
    <>
      <div
        onClick={onClose}
        style={{
          position: 'fixed', inset: 0, background: 'rgba(15,23,42,0.08)',
          zIndex: 40, animation: 'srFade 150ms ease-out',
        }}
      />
      <aside style={{
        position: 'fixed', top: 0, right: 0, height: '100vh', width: 420,
        background: '#fff', boxShadow: 'var(--sr-shadow-soft)',
        zIndex: 50, display: 'flex', flexDirection: 'column',
        animation: 'srSlideIn 220ms ease-out',
      }}>
        <header style={{
          padding: '18px 22px', borderBottom: '1px solid var(--sr-divider)',
          display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        }}>
          <div>
            <div style={{ fontSize: 11, color: 'var(--sr-fg-3)', letterSpacing: '0.04em', textTransform: 'uppercase' }}>Edit Transaction</div>
            <h2 style={{ margin: '2px 0 0', fontSize: 18, fontWeight: 600 }}>{txn.payee}</h2>
          </div>
          <IconBtn name="close" tooltip="Close" onClick={onClose} />
        </header>

        <div style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 14 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '10px 14px', background: 'var(--sr-surface-subtle)', borderRadius: 12 }}>
            <StatusBadge code={txn.status} />
            <div style={{ fontSize: 13, color: 'var(--sr-fg-2)' }}>
              {txn.status === 'C' ? 'Cleared' : txn.status === 'U' ? 'Uncleared' : 'Reconciled'}
              {' — '}
              <a style={{ color: 'var(--sr-primary)', fontWeight: 600, textDecoration: 'none', cursor: 'pointer' }}>Change</a>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <SrField label="Date">
              <SrInput value={form.date || ''} onChange={update('date')} />
            </SrField>
            <SrField label="Amount">
              <SrInput numeric value={form.amount ?? ''} onChange={update('amount')} />
            </SrField>
          </div>

          <SrField label="Payee">
            <SrInput value={form.payee || ''} onChange={update('payee')} />
          </SrField>

          <SrField label="Category">
            <SrInput value={form.category || ''} onChange={update('category')} />
          </SrField>

          <SrField label="Account">
            <SrInput value={form.account || ''} onChange={update('account')} />
          </SrField>

          <SrField label="Memo">
            <textarea
              value={form.memo || ''}
              onChange={update('memo')}
              rows={3}
              style={{
                font: 'inherit', fontSize: 14, padding: '9px 12px', borderRadius: 12,
                border: '1px solid #CBD5E1', background: '#fff', resize: 'vertical',
                outline: 'none',
              }}
            />
          </SrField>

          <div style={{ borderTop: '1px solid var(--sr-divider)', paddingTop: 14, marginTop: 6 }}>
            <div style={{ fontSize: 12, color: 'var(--sr-fg-3)', fontWeight: 500, marginBottom: 8 }}>LINKED</div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, padding: '8px 12px', background: '#F8FAFC', borderRadius: 10 }}>
              <Icon name="link" size={16} color="var(--sr-positive)" />
              <span style={{ flex: 1 }}>Matched to bank transaction</span>
              <a style={{ color: 'var(--sr-primary)', fontWeight: 600, textDecoration: 'none', cursor: 'pointer', fontSize: 12 }}>View</a>
            </div>
          </div>
        </div>

        <footer style={{
          padding: '14px 22px', borderTop: '1px solid var(--sr-divider)',
          display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 8,
        }}>
          <SrButton variant="negative" size="sm" icon="delete_outline">Mark as deleted</SrButton>
          <div style={{ display: 'flex', gap: 8 }}>
            <SrButton variant="flat" size="sm" onClick={onClose}>Cancel</SrButton>
            <SrButton variant="primary" size="sm">Save Transaction</SrButton>
          </div>
        </footer>
      </aside>
    </>
  );
}

Object.assign(window, { Inspector });
