# Deployment: Render API and Vercel client

## 1. Prepare the SQL Server database

The API currently uses EF Core SQL Server. Create a hosted SQL Server database before deploying the API; Render's managed PostgreSQL is not compatible with the current provider. Keep the database private and allow network access only from the Render service's outbound addresses where the database host supports that restriction.

Use a dedicated application login and an encrypted connection string. The API currently applies EF migrations at startup, so that login needs schema-change permissions during deployment. Test this behavior against staging and take a database backup before production schema changes.

Resume files are currently stored under the API content root at `App_Data/resumes`. That container filesystem is not durable across Render redeploys, and the upload path is not configurable yet. Do not accept production resumes until the application is changed to use a Render persistent disk or durable object storage, and a redeploy test confirms files remain downloadable.

## 2. Deploy the API to Render

Create a Render Blueprint from this repository and use `render.yaml`. Supply these prompted values:

- `ConnectionStrings__DefaultConnection`: hosted SQL Server connection string.
- `Cors__AllowedOrigins__0`: the production Vercel origin, such as `https://student-job-hub.vercel.app`.
- `Client__BaseUrl`: the same Vercel origin, without a trailing slash.

Render generates `Jwt__Key`. Keep it in Render's environment settings and do not reuse the development key. The service exposes `/health` for Render's health check.

The Blueprint enables ASP.NET Core forwarded headers so HTTPS redirection recognizes the original HTTPS request after Render's proxy terminates TLS.

## 3. Deploy the client to Vercel

Import the repository into Vercel with the repository root as the project root. `vercel.json` runs the Blazor WebAssembly Release publish and serves the published `wwwroot` files with SPA route fallback. Set the Vercel environment variable `API_BASE_URL` to the Render API's HTTPS base URL, including the trailing slash, for both Production and Preview as appropriate.

The build script installs the .NET 10 SDK when needed, publishes the client, and writes `API_BASE_URL` into the generated public `appsettings.json`. This value is public application configuration, not a secret.

## 4. Verify staging before production

Confirm Render reports `/health` as healthy, then open the Vercel deployment and test registration, login, browsing jobs and services, applications, bookings, uploads, notifications, exports, and admin permissions. Check browser network errors and Render logs. Verify CORS uses the exact Vercel origin and that production API traffic is HTTPS.

Before public launch, document how the first production Admin account will be created. The current automatic admin bootstrap is Development-only.