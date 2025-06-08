// src/boot/notify.ts
import { boot } from 'quasar/wrappers';
import { Notify } from 'quasar';
import type { QNotifyCreateOptions, QNotifyUpdateOptions } from 'quasar';

// Define the type for the notify function
type NotifyFunction = (opts: string | QNotifyCreateOptions) => (props?: QNotifyUpdateOptions) => void;

// Extend the Vue app's global properties to include $q.notify
declare module '@vue/runtime-core' {
  interface ComponentCustomProperties {
    $q: {
      notify: NotifyFunction;
    };
  }
}

export default boot(({ app }) => {
  // Assign Notify to $q.notify
  app.config.globalProperties.$q = app.config.globalProperties.$q || {};
  app.config.globalProperties.$q.notify = Notify.create as NotifyFunction;
});
