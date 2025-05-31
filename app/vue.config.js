const { defineConfig } = require("@vue/cli-service");
module.exports = defineConfig({
  transpileDependencies: true,
  devServer: {
    host: 'family-budget.local', // Bind to this host
    port: 8081,
    allowedHosts: [
      'family-budget.local'
    ]
  },
});
