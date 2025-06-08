// src/boot/notify.ts
import { boot } from "quasar/wrappers";
import { Notify } from "quasar";

export default boot(({ app }) => {
  app.config.globalProperties.$q.notify = Notify;
});
