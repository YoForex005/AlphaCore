using System.Text.Json;

namespace TraderIntelligence.Infrastructure.Copy;

public sealed class DemoCopyFill
{
    public string Broker { get; set; } = "ACHIEVER";
    public string SourceLogin { get; set; } = "";
    public string SourcePositionId { get; set; } = "";
    public bool IsLong { get; set; }
    public decimal Lots { get; set; }
    public string? DestPositionId { get; set; }
    public string? DestClOrdId { get; set; }
    public decimal? DestFillPrice { get; set; }
    public bool DestClosed { get; set; }
}

public static class DemoCopyLedger
{
    public static string Path { get; } = @"D:\Prop\data\demo_copy_ledger.json";

    public static List<DemoCopyFill> Load()
    {
        if (!File.Exists(Path))
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<DemoCopyFill>>(File.ReadAllText(Path)) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void Save(IReadOnlyList<DemoCopyFill> rows)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        var json = JsonSerializer.Serialize(new
        {
            updatedUtc = DateTimeOffset.UtcNow,
            dest = "demo.pepperstone.5328266",
            open = rows.Count(r => !r.DestClosed),
            closed = rows.Count(r => r.DestClosed),
            total = rows.Count,
            fills = rows
        }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
        try
        {
            Directory.CreateDirectory(@"D:\Prop\apps\web\public");
            File.WriteAllText(@"D:\Prop\apps\web\public\copy-live.json", json);
        }
        catch
        {
            // dashboard snapshot is best-effort
        }
    }
}
