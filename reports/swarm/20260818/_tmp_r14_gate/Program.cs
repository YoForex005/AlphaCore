using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TraderIntelligence.Infrastructure;
using TraderIntelligence.Infrastructure.Mt5Live;

static IConfiguration Cfg(params (string k, string? v)[] pairs)
{
    var d = new Dictionary<string, string?>();
    foreach (var (k, v) in pairs)
        d[k] = v;
    return new ConfigurationBuilder().AddInMemoryCollection(d).Build();
}

var rows = new List<object>();

void Case(string name, bool expected, IConfiguration cfg)
{
    var actual = LiveMt5Registration.HasRealPasswords(cfg);
    rows.Add(new { name, expected, actual, pass = actual == expected });
}

Case("both_missing", false, Cfg());
Case("both_empty", false, Cfg(("MT5_PASSWORD", ""), ("MT5_STARWAVEFX_PASSWORD", "")));
Case("both_whitespace", false, Cfg(("MT5_PASSWORD", "  "), ("MT5_STARWAVEFX_PASSWORD", "\t")));
Case("achiever_only", false, Cfg(("MT5_PASSWORD", "not-a-placeholder-token"), ("MT5_STARWAVEFX_PASSWORD", "")));
Case("starwave_only", false, Cfg(("MT5_PASSWORD", ""), ("MT5_STARWAVEFX_PASSWORD", "not-a-placeholder-token")));
Case("both_SECRET_token", false, Cfg(("MT5_PASSWORD", "<SECRET>"), ("MT5_STARWAVEFX_PASSWORD", "<SECRET>")));
Case("achiever_SECRET_starwave_ok", false, Cfg(("MT5_PASSWORD", "<SECRET>"), ("MT5_STARWAVEFX_PASSWORD", "not-a-placeholder-token")));
Case("achiever_ok_starwave_SECRET", false, Cfg(("MT5_PASSWORD", "not-a-placeholder-token"), ("MT5_STARWAVEFX_PASSWORD", "<SECRET>")));
Case("both_account_comment", false, Cfg(("MT5_PASSWORD", "pw (a/c 1)"), ("MT5_STARWAVEFX_PASSWORD", "pw (a/c 2)")));
Case("both_ok_synthetic", true, Cfg(("MT5_PASSWORD", "not-a-placeholder-token"), ("MT5_STARWAVEFX_PASSWORD", "not-a-placeholder-token")));
Case("lowercase_secret_token", true, Cfg(("MT5_PASSWORD", "<secret>"), ("MT5_STARWAVEFX_PASSWORD", "<secret>")));
Case("mixed_case_secret_token", true, Cfg(("MT5_PASSWORD", "<Secret>"), ("MT5_STARWAVEFX_PASSWORD", "<Secret>")));
Case("dummy_word", true, Cfg(("MT5_PASSWORD", "dummy"), ("MT5_STARWAVEFX_PASSWORD", "changeme")));
Case("single_char", true, Cfg(("MT5_PASSWORD", "x"), ("MT5_STARWAVEFX_PASSWORD", "y")));
Case("uppercase_account_comment", true, Cfg(("MT5_PASSWORD", "pw (A/C 1)"), ("MT5_STARWAVEFX_PASSWORD", "pw (A/C 2)")));

var throwOk = false;
string? throwMsg = null;
try
{
    var services = new ServiceCollection();
    services.AddTraderIntelligence(Cfg(("MT5_PASSWORD", "<SECRET>"), ("MT5_STARWAVEFX_PASSWORD", "<SECRET>")));
}
catch (InvalidOperationException ex)
{
    throwOk = ex.Message == "Real MT5 passwords are required. Dummy/fake broker data is disabled.";
    throwMsg = ex.Message;
}

var payload = new
{
    probe = "R14_HasRealPasswords",
    utc = DateTimeOffset.UtcNow,
    cases = rows,
    casesPassed = rows.Count(r => (bool)r.GetType().GetProperty("pass")!.GetValue(r)!),
    casesTotal = rows.Count,
    diThrowOnSecret = throwOk,
    diThrowMessage = throwMsg,
    note = "Synthetic tokens only. No operator secrets."
};

var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
var outPath = @"D:\Prop\reports\swarm\20260818\_tmp_r14_gate\RESULT.json";
File.WriteAllText(outPath, json);
Console.WriteLine(json);
