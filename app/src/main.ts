/*** main.ts */
import { createApp } from "vue";
import App from "./App.vue";
import router from "./router";
import { createVuetify } from "vuetify";
import "vuetify/styles";
import * as components from "vuetify/components";
import * as directives from "vuetify/directives";
import "@mdi/font/css/materialdesignicons.css";
import { setupAuthListener } from "./firebase/auth";
import { createPinia } from "pinia"; // Import createPinia
import "./styles/budget.css";

const vuetify = createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: "myCustomTheme", // Name of your default theme
    themes: {
      myCustomTheme: {
        dark: false, // Set to true for a dark theme
        colors: {
          primary: "#1976D2", // Main color
          secondary: "#424242", // Secondary color
          accent: "#82B1FF", // Accent color
          error: "#FF5252", // Error color
          info: "#2196F3", // Info color
          success: "#4CAF50", // Success color
          warning: "#FFC107", // Warning color
          light: "#F7F7F7", // Light color
          "primary-light": "#5EA6EA",
        },
      },
    },
  },
});

const pinia = createPinia(); // Create Pinia instance

const app = createApp(App);
setupAuthListener();
app.use(router);
app.use(vuetify);
app.use(pinia); // Use Pinia
app.mount("#app");
