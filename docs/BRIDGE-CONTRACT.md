# Windows Bridge API Contract (v1)

Base path: `/api/v1`. All routes except `/health` require `X-API-Key`.

| Method | Path | Request / response |
|---|---|---|
| GET | `/health` | Service health |
| POST | `/reports/open` | `{ "path": "report.rpt" }` -> `{ "reportId": "..." }` |
| GET | `/reports/{id}/metadata` | Report metadata |
| GET | `/reports/{id}/parameters` | Parameter definitions |
| PUT | `/reports/{id}/parameters` | `{ "name": "Period", "value": "2026-10" }` |
| GET | `/reports/{id}/tables` | Database table metadata |
| GET | `/reports/{id}/fields` | Database field metadata |
| PUT | `/reports/{id}/database-connection` | `{ "server": "...", "database": "...", "username": "...", "password": "..." }` |
| GET | `/reports/{id}/formulas` | Formula definitions |
| PUT | `/reports/{id}/formulas` | `{ "name": "FormulaName", "formula": "..." }` |
| GET | `/reports/{id}/subreports` | Subreport list |
| PUT | `/reports/{id}/page` | `{ "orientation": "Landscape", "paperSize": "PaperA4" }` |
| POST | `/reports/{id}/save` | `{ "destination": "edited-report.rpt" }` |
| POST | `/reports/{id}/export` | `{ "format": "pdf" }` -> binary response |
| DELETE | `/reports/{id}` | Close and release report session |

Report paths are relative to `ReportRoot` (or absolute paths inside that root). Save destinations are file names only and are written under `OutputRoot`.

## Implementation boundaries

The current C# source implements the routes above for metadata, parameters, formulas, database logon, page setup, save, and export. It does not yet expose arbitrary report-object geometry/section manipulation or SQL replacement. Those require dedicated RAS controller implementations, report-specific testing, and the exact SDK assemblies installed on the Windows host.
