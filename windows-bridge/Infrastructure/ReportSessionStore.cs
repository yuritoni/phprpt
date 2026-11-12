using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace CrystalReportsBridge.Infrastructure
{
    public static class ReportSessionStore
    {
        private static readonly ConcurrentDictionary<string, ReportSession> Sessions = new ConcurrentDictionary<string, ReportSession>();
        private static readonly int IdleMinutes = int.TryParse(ConfigurationManager.AppSettings["SessionIdleMinutes"], out var n) ? n : 30;

        public static ReportSession Create(string path)
        {
            CleanupExpired();
            var session = new ReportSession(path);
            Sessions[session.Id] = session;
            return session;
        }

        public static ReportSession Get(string id)
        {
            CleanupExpired();
            if (!Sessions.TryGetValue(id, out var session)) throw new KeyNotFoundException("Report session not found or expired. Open the report again.");
            session.Touch();
            return session;
        }

        public static bool Remove(string id)
        {
            if (!Sessions.TryRemove(id, out var session)) return false;
            session.Dispose();
            return true;
        }

        private static void CleanupExpired()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-IdleMinutes);
            foreach (var pair in Sessions.Where(x => x.Value.LastAccessUtc < cutoff).ToArray())
                if (Sessions.TryRemove(pair.Key, out var expired)) expired.Dispose();
        }
    }
}
