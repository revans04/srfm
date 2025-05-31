// scripts/updateVersionAndDeploy.js
const { execSync } = require("child_process");
const fs = require("fs");
const path = require("path");

// Get the version type (patch, minor, major) from command-line arguments
const versionType = process.argv[2] || "patch"; // Default to "patch" if not specified

try {
  // Increment the version in package.json
  console.log(`Incrementing ${versionType} version...`);
  execSync(`npm run version-${versionType}`, { stdio: "inherit" });

  // Read the updated version from package.json
  const packageJsonPath = path.resolve(__dirname, "../package.json");
  const packageJson = JSON.parse(fs.readFileSync(packageJsonPath, "utf8"));
  const newVersion = packageJson.version;
  console.log(`New version: ${newVersion}`);

  // Set the environment variable for the build
  process.env.VUE_APP_VERSION = newVersion;

  // Build and deploy
  console.log("Building and deploying...");
  execSync("npm run predeploy", { stdio: "inherit" });
  execSync("npm run deploy", { stdio: "inherit" });

  console.log("Deployment successful!");
} catch (error) {
  console.error("Error during version update and deployment:", error.message);
  process.exit(1);
}
