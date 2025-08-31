import { readdirSync, statSync, copyFileSync } from 'fs';
import { join } from 'path';

function copyRecursive(dir) {
  for (const f of readdirSync(dir)) {
    const p = join(dir, f);
    const st = statSync(p);
    if (st.isDirectory()) copyRecursive(p);
    else if (p.endsWith('.js')) {
      copyFileSync(p, p.slice(0, -3));
    }
  }
}

try {
  copyRecursive('dist-tests/src');
} catch {}
