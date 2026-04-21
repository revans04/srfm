// Primitives — small, neat building blocks used across SRFM surfaces.
// Everything reads tokens from colors_and_type.css.

const { useState } = React;

function Icon({ name, size = 18, color, style }) {
  return (
    <span
      className="material-icons"
      style={{ fontSize: size, color, lineHeight: 1, verticalAlign: 'middle', ...style }}
    >
      {name}
    </span>
  );
}

function SrButton({ variant = 'primary', icon, children, onClick, disabled, size = 'md', style }) {
  const base = {
    fontFamily: 'inherit',
    fontWeight: 600,
    fontSize: size === 'sm' ? 13 : 14,
    letterSpacing: '0.01em',
    borderRadius: 999,
    padding: size === 'sm' ? '6px 14px' : '9px 18px',
    cursor: disabled ? 'not-allowed' : 'pointer',
    display: 'inline-flex',
    alignItems: 'center',
    gap: 6,
    transition: 'background 150ms ease, color 150ms ease',
    opacity: disabled ? 0.5 : 1,
    ...style,
  };
  const variants = {
    primary: { background: 'var(--sr-primary)', color: '#fff', border: 'none' },
    outline: { background: '#fff', color: 'var(--sr-primary)', border: '1px solid var(--sr-primary)' },
    flat: { background: 'transparent', color: 'var(--sr-primary)', border: 'none' },
    negative: { background: '#fff', color: 'var(--sr-negative)', border: '1px solid var(--sr-negative)' },
    'negative-solid': { background: 'var(--sr-negative)', color: '#fff', border: 'none' },
  };
  return (
    <button style={{ ...base, ...variants[variant] }} onClick={disabled ? undefined : onClick} disabled={disabled}>
      {icon && <Icon name={icon} size={16} />}
      {children}
    </button>
  );
}

function IconBtn({ name, tooltip, onClick, primary }) {
  const [hover, setHover] = useState(false);
  return (
    <button
      title={tooltip}
      onClick={onClick}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      style={{
        width: 32, height: 32, borderRadius: 999, border: 'none',
        background: primary ? 'var(--sr-primary)' : hover ? '#F1F5F9' : 'transparent',
        color: primary ? '#fff' : 'var(--sr-fg-2)',
        cursor: 'pointer', display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
        transition: 'background 150ms ease',
      }}
    >
      <Icon name={name} size={18} />
    </button>
  );
}

function SrChip({ active, outline, removable, onClick, onRemove, children }) {
  const style = {
    display: 'inline-flex', alignItems: 'center', gap: 6,
    fontSize: 12, fontWeight: 500, padding: '5px 12px', borderRadius: 999,
    cursor: onClick ? 'pointer' : 'default',
    transition: 'background 150ms ease, color 150ms ease',
    border: outline ? '1px solid var(--sr-primary)' : '1px solid transparent',
    background: active ? 'var(--sr-primary)' : outline ? '#fff' : 'rgba(37,99,235,0.08)',
    color: active ? '#fff' : outline ? 'var(--sr-primary)' : 'var(--sr-fg-1)',
  };
  return (
    <span style={style} onClick={onClick}>
      {children}
      {removable && (
        <Icon name="close" size={14} style={{ cursor: 'pointer', opacity: 0.7 }} onClick={(e) => { e.stopPropagation(); onRemove?.(); }} />
      )}
    </span>
  );
}

function SrField({ label, children, style }) {
  return (
    <label style={{ display: 'flex', flexDirection: 'column', gap: 4, ...style }}>
      <span style={{ fontSize: 12, color: 'var(--sr-fg-2)', fontWeight: 500 }}>{label}</span>
      {children}
    </label>
  );
}

function SrInput({ value, onChange, numeric, ...rest }) {
  const [focus, setFocus] = useState(false);
  return (
    <input
      value={value}
      onChange={onChange}
      onFocus={() => setFocus(true)}
      onBlur={() => setFocus(false)}
      style={{
        font: 'inherit', fontSize: 14,
        padding: '9px 12px', borderRadius: 12,
        border: `1px solid ${focus ? 'var(--sr-primary)' : '#CBD5E1'}`,
        boxShadow: focus ? '0 0 0 3px rgba(29,78,216,0.12)' : 'none',
        background: '#fff', outline: 'none',
        textAlign: numeric ? 'right' : 'left',
        fontVariantNumeric: numeric ? 'tabular-nums' : 'normal',
        transition: 'border-color 150ms ease, box-shadow 150ms ease',
      }}
      {...rest}
    />
  );
}

function StatusBadge({ code }) {
  const map = {
    C: { bg: 'var(--sr-positive)', tip: 'Cleared — transaction has posted to your account' },
    U: { bg: 'var(--sr-warning)', tip: 'Uncleared — transaction hasn’t posted yet' },
    R: { bg: 'var(--sr-primary)', tip: 'Reconciled — verified against your bank statement' },
  };
  const m = map[code];
  return (
    <span
      title={m.tip}
      style={{
        display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
        width: 22, height: 22, borderRadius: 999,
        background: m.bg, color: '#fff', fontSize: 11, fontWeight: 600, cursor: 'help',
      }}
    >{code}</span>
  );
}

function ProgressRow({ label, spent, budget }) {
  const pct = Math.min(100, Math.round((spent / budget) * 100));
  const over = spent > budget;
  const color = over ? 'var(--sr-negative)' : pct >= 80 ? 'var(--sr-warning)' : 'var(--sr-positive)';
  const fmt = (n) => `$${n.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 6, fontVariantNumeric: 'tabular-nums' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13 }}>
        <span>{label}</span>
        <span style={{ color: over ? 'var(--sr-negative)' : 'var(--sr-fg-3)' }}>
          {fmt(spent)} / {fmt(budget)}
        </span>
      </div>
      <div style={{ height: 6, background: '#E2E8F0', borderRadius: 3, overflow: 'hidden' }}>
        <div style={{ height: '100%', width: `${pct}%`, background: color, transition: 'width 300ms ease' }} />
      </div>
    </div>
  );
}

function StatCard({ label, value, delta, tone = 'neutral' }) {
  const valColor = tone === 'positive' ? 'var(--sr-positive)' : tone === 'negative' ? 'var(--sr-negative)' : 'var(--sr-fg-1)';
  return (
    <div style={{
      background: '#fff', borderRadius: 18, padding: 18,
      boxShadow: 'var(--sr-shadow-subtle)', display: 'flex', flexDirection: 'column', gap: 4,
    }}>
      <div style={{ fontSize: 12, color: 'var(--sr-fg-3)', fontWeight: 500 }}>{label}</div>
      <div style={{ fontSize: 26, fontWeight: 600, color: valColor, fontVariantNumeric: 'tabular-nums', letterSpacing: '-0.01em' }}>{value}</div>
      {delta && <div style={{ fontSize: 12, color: delta.tone === 'positive' ? 'var(--sr-positive)' : 'var(--sr-fg-3)' }}>{delta.text}</div>}
    </div>
  );
}

Object.assign(window, { Icon, SrButton, IconBtn, SrChip, SrField, SrInput, StatusBadge, ProgressRow, StatCard });
