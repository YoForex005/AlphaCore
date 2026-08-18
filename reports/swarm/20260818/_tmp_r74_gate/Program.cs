using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Application.Runtime;
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
Case("secret_embedded", false, Cfg(("MT5_PASSWORD", "pre<SECRET>post"), ("MT5_STARWAVEFX_PASSWORD", "not-a-placeholder-token")));
Case("account_comment_embedded", false, Cfg(("MT5_PASSWORD", "not-a-placeholder-token"), ("MT5_STARWAVEFX_PASSWORD", "note (a/c 99) leftover")));

var throwOk = false;
string? throwMsg = null;
string? throwType = null;
try
{
    var services = new ServiceCollection();
    services.AddTraderIntelligence(Cfg(("MT5_PASSWORD", "<SECRET>"), ("MT5_STARWAVEFX_PASSWORD", "<SECRET>")));
}
catch (Exception ex)
{
    throwType = ex.GetType().FullName;
    throwMsg = ex.Message;
    throwOk = ex is InvalidOperationException
              && ex.Message == "Real MT5 passwords are required. Dummy/fake broker data is disabled.";
}

var oneSidedThrowOk = false;
try
{
    var services = new ServiceCollection();
    services.AddTraderIntelligence(Cfg(("MT5_PASSWORD", "not-a-placeholder-token"), ("MT5_STARWAVEFX_PASSWORD", "")));
}
catch (InvalidOperationException)
{
    oneSidedThrowOk = true;
}

var open = new ServiceCollection();
open.AddTraderIntelligence(Cfg(
    ("MT5_PASSWORD", "not-a-placeholder-token"),
    ("MT5_STARWAVEFX_PASSWORD", "not-a-placeholder-token")));
using var sp = open.BuildServiceProvider();
var runtime = sp.GetRequiredService<LiveRuntimeStatus>();
var connectors = sp.GetServices<IMt5BrokerConnector>().ToList();
var connectorTypes = connectors.Select(c => new { c.BrokerCode, type = c.GetType().FullName }).ToList();
var fakeCount = connectors.Count(c => c.GetType().Name.Contains("Fake", StringComparison.Ordinal));
var nativeCount = connectors.Count(c => c.GetType().Name == "NativeMt5BrokerConnector");

var unguarded = LiveMt5Registration.CreateConnectors(Cfg());
var unguardedTypes = unguarded.Select(c => new { c.BrokerCode, type = c.GetType().Name }).ToList();

var payload = new
{
    probe = "W500_R74_HasRealPasswords",
    utc = DateTimeOffset.UtcNow,
    cases = rows,
    casesPassed = rows.Count(r => (bool)r.GetType().GetProperty("pass")!.GetValue(r)!),
    casesTotal = rows.Count,
    diThrowOnSecret = throwOk,
    diThrowType = throwType,
    diThrowMessage = throwMsg,
    diThrowOnAchieverOnly = oneSidedThrowOk,
    openGate = new
    {
        realCopyEnabled = runtime.RealCopyEnabled,
        connectorCount = connectors.Count,
        nativeCount,
        fakeCount,
        connectorTypes
    },
    createConnectorsUnguardedEmptyCfg = new
    {
        count = unguarded.Count,
        types = unguardedTypes,
        note = "CreateConnectors does not call HasRealPasswords; empty cfg still builds two Native connectors."
    },
    note = "Synthetic tokens only. No operator secrets. No Manager Connect. No FIX send."
};

var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
var outPath = @"D:\Prop\reports\swarm\20260818\_tmp_r74_gate\RESULT.json";
File.WriteAllText(outPath, json);
Console.WriteLine(json);
