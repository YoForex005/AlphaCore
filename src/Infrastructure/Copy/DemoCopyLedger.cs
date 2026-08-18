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
        File.WriteAllText(Path, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
    }
}
