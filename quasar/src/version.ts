// src/version.js
let version = "0.0.0";
try {
  version =
    process.env.VUE_APP_VERSION ||
    require("../package.json").version ||
    "0.0.0";
} catch (error) {
  console.error("Error loading package.json version:", error);
  version = process.env.VUE_APP_VERSION || "0.0.0";
}
export default version;
