namespace CrystalReportsBridge.Models
{
    public sealed class OpenReportRequest { public string Path { get; set; } }
    public sealed class ParameterRequest { public string Name { get; set; } public object Value { get; set; } }
    public sealed class FormulaRequest { public string Name { get; set; } public string Formula { get; set; } }
    public sealed class DatabaseConnectionRequest { public string Server { get; set; } public string Database { get; set; } public string Username { get; set; } public string Password { get; set; } }
    public sealed class PageRequest { public string Orientation { get; set; } public string PaperSize { get; set; } }
    public sealed class SaveRequest { public string Destination { get; set; } }
    public sealed class ExportRequest { public string Format { get; set; } }
}
