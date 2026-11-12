using System;
using System.Threading;
using CrystalDecisions.CrystalReports.Engine;

namespace CrystalReportsBridge.Infrastructure
{
    public sealed class ReportSession : IDisposable
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public ReportDocument Document { get; }
        public DateTime LastAccessUtc { get; private set; } = DateTime.UtcNow;
        public SemaphoreSlim Gate { get; } = new SemaphoreSlim(1, 1);

        public ReportSession(string path)
        {
            Document = new ReportDocument();
            Document.Load(path);
        }

        public void Touch() { LastAccessUtc = DateTime.UtcNow; }
        public void Dispose()
        {
            try { Document.Close(); } catch { }
            try { Document.Dispose(); } catch { }
            Gate.Dispose();
        }
    }
}
