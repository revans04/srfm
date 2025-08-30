import { boot } from 'quasar/wrappers';
import version from 'src/version';

export default boot(() => {
  const name = 'q-srfm';
  const mode = import.meta.env.MODE;
  const base = import.meta.env.BASE_URL;
  console.log(`\n=============================\n  Starting ${name} (${mode})\n  Version: ${version}\n  Base URL: ${base}\n=============================\n`);
});

