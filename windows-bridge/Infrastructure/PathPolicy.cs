using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace CrystalReportsBridge.Infrastructure
{
    public static class PathPolicy
    {
        public static string ResolveReport(string relativeOrAbsolute)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolute)) throw new ArgumentException("Report path is required.");
            var root = Path.GetFullPath(ConfigurationManager.AppSettings["ReportRoot"] ?? throw new InvalidOperationException("ReportRoot is not configured."));
            var candidate = Path.GetFullPath(Path.IsPathRooted(relativeOrAbsolute) ? relativeOrAbsolute : Path.Combine(root, relativeOrAbsolute));
            EnsureInside(root, candidate);
            if (!string.Equals(Path.GetExtension(candidate), ".rpt", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only .rpt files are allowed.");
            if (!File.Exists(candidate)) throw new FileNotFoundException("Report file was not found.");
            var maxBytes = long.Parse(ConfigurationManager.AppSettings["MaxReportFileBytes"] ?? "52428800");
            if (new FileInfo(candidate).Length > maxBytes) throw new InvalidOperationException("Report file exceeds configured size limit.");
            return candidate;
        }

        public static string ResolveOutput(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) fileName = Guid.NewGuid().ToString("N") + ".rpt";
            if (Path.IsPathRooted(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName.Contains(".."))
                throw new ArgumentException("Output must be a simple file name, not a path.");
            var root = Path.GetFullPath(ConfigurationManager.AppSettings["OutputRoot"] ?? throw new InvalidOperationException("OutputRoot is not configured."));
            Directory.CreateDirectory(root);
            var candidate = Path.GetFullPath(Path.Combine(root, fileName));
            EnsureInside(root, candidate);
            return candidate;
        }

        private static void EnsureInside(string root, string candidate)
        {
            var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(candidate, root, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Path is outside the configured directory.");
        }
    }
}
