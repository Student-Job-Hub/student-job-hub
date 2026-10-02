import { readFile, writeFile } from "node:fs/promises";

const [settingsPath, apiBaseUrl] = process.argv.slice(2);

if (!settingsPath || !apiBaseUrl) {
  throw new Error("Usage: node set-client-api-url.mjs <settings-path> <api-base-url>");
}

const parsedUrl = new URL(apiBaseUrl);
if (parsedUrl.protocol !== "https:") {
  throw new Error("The deployed API URL must use HTTPS.");
}

const settings = JSON.parse(await readFile(settingsPath, "utf8"));
settings.ApiBaseUrl = parsedUrl.toString();
await writeFile(settingsPath, `${JSON.stringify(settings, null, 2)}\n`);