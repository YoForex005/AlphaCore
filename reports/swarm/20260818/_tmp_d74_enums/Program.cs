using System.Text.Json;
using System.Text.Json.Serialization;
using TraderIntelligence.Application.Dashboard;
using TraderIntelligence.Domain.Entities;
using TraderIntelligence.Domain.Enums;

var with = new JsonSerializerOptions(JsonSerializerDefaults.Web);
with.Converters.Add(new JsonStringEnumConverter());
var without = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var camelEnum = new JsonSerializerOptions(JsonSerializerDefaults.Web);
camelEnum.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

var row = new TraderRowDto(
    "ACHIEVER", 10001, "demo", 3, 12.3m, 70m, null, 10m, false, false, false,
    TraderState.WATCH, 0m, DateTimeOffset.Parse("2026-08-18T00:00:00Z"));
var hl = new TradeHighlightDto(1, "XAUUSD.s", "XAUUSD", TradeDirection.Long,
    DateTimeOffset.Parse("2026-08-18T00:00:00Z"), null, 1.25m, 0.10m, true, true);
var trade = new ReconstructedTrade { Direction = TradeDirection.Short, Login = 10001 };

Console.WriteLine("WITH_ROW=" + JsonSerializer.Serialize(row, with));
Console.WriteLine("WITHOUT_ROW_STATE=" + JsonSerializer.Serialize(new { state = row.State }, without));
Console.WriteLine("WITH_HL=" + JsonSerializer.Serialize(hl, with));
Console.WriteLine("WITHOUT_HL_DIR=" + JsonSerializer.Serialize(new { direction = hl.Direction }, without));
Console.WriteLine("WITH_TRADE=" + JsonSerializer.Serialize(new { trade.Direction }, with));
Console.WriteLine("CAMEL_DIR=" + JsonSerializer.Serialize(new { direction = TradeDirection.Long }, camelEnum));
Console.WriteLine("KS_TOSTRING=" + KillSwitchMode.StopNewExecution.ToString());
Console.WriteLine("FIXQ_UPPER=" + FixSessionQualifier.Quote.ToString().ToUpperInvariant());
Console.WriteLine("FIXS_TOSTRING=" + FixSessionStatus.LoggedOn.ToString());

static void TryRead(string label, string json, JsonSerializerOptions o)
{
    try
    {
        var v = JsonSerializer.Deserialize<TraderState>(json, o);
        Console.WriteLine($"READ_{label}={v}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"READ_{label}_FAIL={ex.GetType().Name}:{ex.Message.Split('\n')[0]}");
    }
}

TryRead("WATCH_exact", "\"WATCH\"", with);
TryRead("watch_lower", "\"watch\"", with);
TryRead("Watch_mixed", "\"Watch\"", with);
TryRead("int_2", "2", with);
TryRead("int_quoted", "\"2\"", with);
TryRead("without_WATCH", "\"WATCH\"", without);
TryRead("without_int", "2", without);
Console.WriteLine("TRYPARSE_watch=" + Enum.TryParse<TraderState>("watch", true, out var st1) + "," + st1);
Console.WriteLine("TRYPARSE_2=" + Enum.TryParse<TraderState>("2", true, out var st2) + "," + st2);
Console.WriteLine("CTOR=" + typeof(JsonStringEnumConverter).GetConstructors().Length);
