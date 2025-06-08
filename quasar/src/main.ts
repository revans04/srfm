/*** main.ts */
import { createApp } from "vue";
import { createPinia } from "pinia";
import { Quasar } from "quasar";
import quasarIconSet from "quasar/icon-set/mdi-v7"; // Use mdi-v7 for Material Design Icons
import App from "./App.vue";
import router from "./router";
import persistedState from "./store/plugins/persistedState";
import { setupAuthListener } from "./firebase/auth";

// Import Quasar styles
import "quasar/src/css/index.sass";

// Import Material Design Icons
import "@quasar/extras/mdi-v7/mdi-v7.css";

// Import custom styles
import "./styles/budget.css";

const pinia = createPinia();
pinia.use(persistedState);

const app = createApp(App);

// Setup Quasar
app.use(Quasar, {
  plugins: {}, // Add Quasar plugins here if needed (e.g., Notify, Dialog)
  iconSet: quasarIconSet, // Use Material Design Icons
  config: {
    brand: {
      primary: "#1976D2",
      secondary: "#424242",
      accent: "#82B1FF",
      error: "#FF5252",
      info: "#2196F3",
      success: "#4CAF50",
      warning: "#FFC107",
      light: "#F7F7F7",
      "primary-light": "#5EA6EA",
    },
  },
});

setupAuthListener();
app.use(router);
app.use(pinia);
app.mount("#app");
