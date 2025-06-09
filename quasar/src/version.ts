// src/version.ts
import pkg from '../package.json';

let version = '0.0.0';

try {
  version =
    import.meta.env.VITE_APP_VERSION ||
    (pkg as { version?: string }).version ||
    '0.0.0';
} catch (error) {
  console.error('Error loading package.json version:', error);
  version = import.meta.env.VITE_APP_VERSION || '0.0.0';
}

export default version;
