# Roadmap

## Phase 1 — Starter bridge (included)
- PHP 8.4 Composer client.
- Windows ASP.NET Web API 2 bridge skeleton targeting .NET Framework 4.8 / x64.
- In-process ReportDocument load, metadata, parameter/formula updates, DB logon, page setup, save, export, and close.
- API-key and path restrictions.

## Phase 2 — Complete designer operations
- Inspect report object types and section hierarchy.
- Move/resize text, field, picture, line, box, and chart objects through RAS ReportObjectController.
- Add/remove/modify formula fields through DataDefController.
- Modify section formatting and conditional formulas through ReportSectionController.
- Add/remove fields and tables with DataDefController / DatabaseController.
- Edit subreport links and layouts.
- Add validation and integration tests using representative `.rpt` files.

## Phase 3 — Production hardening
- Replace in-memory report sessions with a controlled job/session store or per-request execution model.
- Add async job status for long exports, concurrency limits, health/diagnostic endpoints, structured redacted logs, and metrics.
- Add Windows service/IIS deployment automation and runtime compatibility checks.
- Add optional mutual TLS or private network identity in addition to API keys.

## Compatibility gate

Before implementing Phase 2, record the installed SAP Crystal Reports for Visual Studio SDK version/support pack and confirm the in-process RAS assemblies are present. Do not mix CrystalDecisions assemblies from different support packs.
