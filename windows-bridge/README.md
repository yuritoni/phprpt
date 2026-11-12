# Windows Crystal Reports Bridge

ASP.NET Web API 2 / .NET Framework 4.8 host for the PHP 8.4 client.

## Important prerequisites

1. Windows Server 2022 x64.
2. IIS with ASP.NET 4.x enabled and .NET Framework 4.8.
3. A matching SAP Crystal Reports for Visual Studio .NET SDK/runtime installation (64-bit) and its in-process RAS assemblies. Crystal Reports 2020 Designer by itself is not the deployable runtime for this service.
4. The same SAP Crystal Reports support pack/runtime build on the development and deployment machines. Install the SAP SDK using SAP's installer; do not copy individual CrystalDecisions DLLs from an arbitrary workstation.
5. Install the correct 64-bit database client/ODBC/native drivers for every database used by the reports.

The service uses the in-process `ReportDocument.ReportClientDocument` model. It does not require a separately deployed RAS Server for the operations implemented here. Advanced design operations beyond formulas/page settings (for example arbitrary report-object placement, adding/removing database fields, and section-controller edits) require additional RAS controller implementations and must be validated against the exact installed SDK build.

## Build and deploy

1. Open `CrystalReportsBridge.csproj` in Visual Studio on a Windows build machine with .NET Framework 4.8 targeting pack.
2. Add references from the installed SAP SDK if MSBuild cannot resolve them automatically:
   - `CrystalDecisions.CrystalReports.Engine`
   - `CrystalDecisions.Shared`
   - `CrystalDecisions.ReportAppServer.ClientDoc`
   - `CrystalDecisions.ReportAppServer.DataDefModel`
   - `CrystalDecisions.ReportAppServer.ReportDefModel`
   - `CrystalDecisions.ReportAppServer.Controllers`
3. Build x64 / Release. Set `Copy Local = false` for SAP assemblies; install the matching runtime on the server.
4. Publish to an IIS application configured for .NET CLR v4.0, Integrated pipeline, and 64-bit worker process (`Enable 32-Bit Applications = False`).
5. Set `ApiKey`, `ReportRoot`, and `OutputRoot` in `Web.config`. Use a long random API key and restrict inbound network access to the PHP host.
6. Grant the IIS App Pool identity read access to `ReportRoot`, and modify access to `OutputRoot`.
7. Configure PHP `.env` with `CRYSTAL_BRIDGE_URL` and the same API key.

## Supported API in this starter

- `GET /api/v1/health`
- `POST /api/v1/reports/open`
- `GET /api/v1/reports/{id}/metadata|parameters|tables|fields|formulas|subreports`
- `PUT /api/v1/reports/{id}/parameters`
- `PUT /api/v1/reports/{id}/database-connection`
- `PUT /api/v1/reports/{id}/formulas`
- `PUT /api/v1/reports/{id}/page`
- `POST /api/v1/reports/{id}/save`
- `POST /api/v1/reports/{id}/export` (binary response)
- `DELETE /api/v1/reports/{id}`

All routes except health require `X-API-Key`.

## Operational notes

- Sessions are held in the worker process. Do not run multiple IIS workers/web-garden for this app. An app-pool recycle invalidates open report IDs; clients must reopen the report.
- Reports are opened read-only in memory. `save` writes to the configured output directory; it never overwrites the source report.
- Crystal Reports is stateful and native-resource intensive. Use a small IIS queue, monitor private bytes/handles, and recycle during a maintenance window. This starter intentionally serializes operations per report session.
- Do not expose this service publicly. Put it on a private network behind a firewall/VPN and use HTTPS.
- SAP licensing and redistribution terms must be confirmed for the intended server and number of concurrent users.

Run `powershell -ExecutionPolicy Bypass -File .\scripts\verify-runtime.ps1` on the Windows host to perform a basic .NET and SAP assembly presence check before building/deploying.
