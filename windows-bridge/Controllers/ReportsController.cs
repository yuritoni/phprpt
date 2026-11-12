using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using CrystalReportsBridge.Infrastructure;
using CrystalReportsBridge.Models;

namespace CrystalReportsBridge.Controllers
{
    [ApiKey]
    [RoutePrefix("api/v1/reports")]
    public sealed class ReportsController : ApiController
    {
        [HttpPost, Route("open")]
        public IHttpActionResult Open(OpenReportRequest request)
        {
            try
            {
                if (request == null) return BadRequest("Request body is required.");
                var path = PathPolicy.ResolveReport(request.Path);
                var session = ReportSessionStore.Create(path);
                return Ok(new { reportId = session.Id, fileName = Path.GetFileName(path), opened = true });
            }
            catch (Exception ex) { return Error(ex); }
        }

        [HttpGet, Route("{id}/metadata")]
        public IHttpActionResult Metadata(string id) => Execute(id, s => Ok(new {
            reportId = s.Id,
            fileName = Path.GetFileName(s.Document.FileName),
            pageOrientation = s.Document.PrintOptions.PaperOrientation.ToString(),
            paperSize = s.Document.PrintOptions.PaperSize.ToString(),
            tables = s.Document.Database.Tables.Count,
            formulas = s.Document.DataDefinition.FormulaFields.Count,
            subreports = s.Document.Subreports.Count
        }));

        [HttpGet, Route("{id}/parameters")]
        public IHttpActionResult Parameters(string id) => Execute(id, s => Ok(s.Document.DataDefinition.ParameterFields.Cast<ParameterFieldDefinition>().Select(p => new {
            name = p.ParameterFieldName,
            valueType = p.ValueType.ToString(),
            allowMultipleValues = p.EnableAllowMultipleValue,
            allowNullValue = p.EnableNullValue
        }).ToArray()));

        [HttpGet, Route("{id}/tables")]
        public IHttpActionResult Tables(string id) => Execute(id, s => Ok(s.Document.Database.Tables.Cast<Table>().Select(t => new {
            name = t.Name,
            location = t.Location
        }).ToArray()));

        [HttpGet, Route("{id}/fields")]
        public IHttpActionResult Fields(string id) => Execute(id, s => Ok(s.Document.Database.Tables.Cast<Table>().SelectMany(t => t.Fields.Cast<FieldDefinition>().Select(f => new {
            table = t.Name,
            name = f.Name,
            valueType = f.ValueType.ToString()
        })).ToArray()));

        [HttpGet, Route("{id}/formulas")]
        public IHttpActionResult Formulas(string id) => Execute(id, s => Ok(s.Document.DataDefinition.FormulaFields.Cast<FormulaFieldDefinition>().Select(f => new {
            name = f.Name,
            formula = f.Text,
            valueType = f.ValueType.ToString()
        }).ToArray()));

        [HttpGet, Route("{id}/subreports")]
        public IHttpActionResult Subreports(string id) => Execute(id, s => Ok(s.Document.Subreports.Cast<ReportDocument>().Select(r => new {
            name = r.Name,
            tables = r.Database.Tables.Count
        }).ToArray()));

        [HttpPut, Route("{id}/parameters")]
        public IHttpActionResult SetParameter(string id, ParameterRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Parameter name is required.");
            return Execute(id, s => {
                s.Document.SetParameterValue(request.Name, request.Value);
                return Ok(new { updated = true, name = request.Name });
            });
        }

        [HttpPut, Route("{id}/formulas")]
        public IHttpActionResult SetFormula(string id, FormulaRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name) || request.Formula == null) return BadRequest("Formula name and formula text are required.");
            return Execute(id, s => {
                var formula = s.Document.DataDefinition.FormulaFields[request.Name];
                if (formula == null) return (IHttpActionResult)NotFound();
                formula.Text = request.Formula;
                return Ok(new { updated = true, name = request.Name, formula = formula.Text });
            });
        }

        [HttpPut, Route("{id}/database-connection")]
        public IHttpActionResult SetDatabaseConnection(string id, DatabaseConnectionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Server) || string.IsNullOrWhiteSpace(request.Database))
                return BadRequest("Server and database are required.");
            return Execute(id, s => {
                ApplyConnection(s.Document, request);
                foreach (ReportDocument sub in s.Document.Subreports) ApplyConnection(sub, request);
                return Ok(new { updated = true, tableCount = s.Document.Database.Tables.Count });
            });
        }

        [HttpPut, Route("{id}/page")]
        public IHttpActionResult SetPage(string id, PageRequest request)
        {
            if (request == null) return BadRequest("Request body is required.");
            return Execute(id, s => {
                if (!string.IsNullOrWhiteSpace(request.Orientation))
                    s.Document.PrintOptions.PaperOrientation = (PaperOrientation)Enum.Parse(typeof(PaperOrientation), request.Orientation, true);
                if (!string.IsNullOrWhiteSpace(request.PaperSize))
                    s.Document.PrintOptions.PaperSize = (PaperSize)Enum.Parse(typeof(PaperSize), request.PaperSize, true);
                return Ok(new { updated = true, orientation = s.Document.PrintOptions.PaperOrientation.ToString(), paperSize = s.Document.PrintOptions.PaperSize.ToString() });
            });
        }

        [HttpPost, Route("{id}/save")]
        public IHttpActionResult Save(string id, SaveRequest request)
        {
            return Execute(id, s => {
                var name = request == null ? null : request.Destination;
                if (string.IsNullOrWhiteSpace(name)) name = Guid.NewGuid().ToString("N") + ".rpt";
                if (!name.EndsWith(".rpt", StringComparison.OrdinalIgnoreCase)) name += ".rpt";
                var destination = PathPolicy.ResolveOutput(name);
                s.Document.SaveAs(destination);
                return Ok(new { saved = true, path = destination, fileName = Path.GetFileName(destination) });
            });
        }

        [HttpPost, Route("{id}/export")]
        public HttpResponseMessage Export(string id, ExportRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Format)) return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Export format is required.");
                var session = ReportSessionStore.Get(id);
                session.Gate.Wait();
                try
                {
                    var format = ParseExportFormat(request.Format);
                    using (var stream = session.Document.ExportToStream(format))
                    {
                        var output = new MemoryStream();
                        stream.CopyTo(output);
                        output.Position = 0;
                        var response = Request.CreateResponse(HttpStatusCode.OK);
                        response.Content = new StreamContent(output);
                        response.Content.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeFor(request.Format));
                        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "report." + ExtensionFor(request.Format) };
                        return response;
                    }
                }
                finally { session.Gate.Release(); }
            }
            catch (Exception ex) { return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message); }
        }

        [HttpDelete, Route("{id}")]
        public IHttpActionResult Close(string id) => Ok(new { closed = ReportSessionStore.Remove(id) });

        private IHttpActionResult Execute(string id, Func<ReportSession, IHttpActionResult> action)
        {
            try
            {
                var session = ReportSessionStore.Get(id);
                session.Gate.Wait();
                try { session.Touch(); return action(session); }
                finally { session.Gate.Release(); }
            }
            catch (Exception ex) { return Error(ex); }
        }

        private static void ApplyConnection(ReportDocument document, DatabaseConnectionRequest request)
        {
            foreach (Table table in document.Database.Tables)
            {
                var info = table.LogOnInfo;
                info.ConnectionInfo.ServerName = request.Server;
                info.ConnectionInfo.DatabaseName = request.Database;
                info.ConnectionInfo.UserID = request.Username ?? string.Empty;
                info.ConnectionInfo.Password = request.Password ?? string.Empty;
                table.ApplyLogOnInfo(info);
            }
        }

        private static ExportFormatType ParseExportFormat(string format)
        {
            switch (format.Trim().ToLowerInvariant())
            {
                case "pdf": return ExportFormatType.PortableDocFormat;
                case "xlsx": return ExportFormatType.ExcelWorkbook;
                case "xls": return ExportFormatType.Excel;
                case "doc": return ExportFormatType.WordForWindows;
                case "rtf": return ExportFormatType.RichText;
                case "csv": return ExportFormatType.CharacterSeparatedValues;
                default: throw new ArgumentException("Unsupported export format. Use pdf, xlsx, xls, doc, rtf, or csv.");
            }
        }
        private static string ContentTypeFor(string format)
        {
            switch (format.ToLowerInvariant())
            {
                case "pdf": return "application/pdf";
                case "xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case "xls": return "application/vnd.ms-excel";
                case "doc": return "application/msword";
                case "rtf": return "application/rtf";
                default: return "text/csv";
            }
        }
        private static string ExtensionFor(string format) => format.ToLowerInvariant();
        private IHttpActionResult Error(Exception ex)
        {
            var status = ex is FileNotFoundException ? HttpStatusCode.NotFound :
                ex is UnauthorizedAccessException ? HttpStatusCode.Forbidden :
                ex is ArgumentException ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError;
            return Content(status, new { error = ex.GetType().Name, message = ex.Message });
        }
    }
}
