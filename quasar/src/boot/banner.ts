import { boot } from 'quasar/wrappers';

export default boot(() => {
  const name = 'quasar';
  const mode = import.meta.env.MODE;
  const base = import.meta.env.BASE_URL;
  // eslint-disable-next-line no-console
  console.log(`\n=============================\n  Starting ${name} (${mode})\n  Version: ${import.meta.env.VITE_APP_VERSION ?? 'n/a'}\n  Base URL: ${base}\n=============================\n`);
});

