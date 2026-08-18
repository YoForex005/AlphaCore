using System.Globalization;
using System.Text;
using TraderIntelligence.Fix.CTrader.Parsing;

const char Soh = '\u0001';
var p = new FixMessageParser();

static string IndependentChecksum(string sohRawWithoutTag10)
{
    var sum = Encoding.ASCII.GetBytes(sohRawWithoutTag10).Sum(b => (int)b);
    return (sum % 256).ToString("D3", CultureInfo.InvariantCulture);
}

static string PipeToSohJoin(params string[] fields)
    => string.Join(Soh, fields) + Soh;

void Ok(string name, string detail) => Console.WriteLine($"PASS\t{name}\t{detail}");
void Fail(string name, string detail) => Console.WriteLine($"FAIL\t{name}\t{detail}");
void Note(string name, string detail) => Console.WriteLine($"NOTE\t{name}\t{detail}");

string ExName(Exception ex) => ex.GetType().Name + ": " + ex.Message.Replace('\n', ' ');

// 1) Build heartbeat {8,35=0}
string hb;
try
{
    hb = p.BuildFixMessage(new[]
    {
        new KeyValuePair<int, string>(8, "FIX.4.4"),
        new KeyValuePair<int, string>(35, "0"),
    });
    var expectedHb = "8=FIX.4.4|9=5|35=0|10=" + IndependentChecksum(PipeToSohJoin("8=FIX.4.4", "9=5", "35=0"));
    if (hb == expectedHb) Ok("Build_HB", hb);
    else Fail("Build_HB", $"got '{hb}' expected '{expectedHb}'");
}
catch (Exception ex)
{
    hb = "";
    Fail("Build_HB", ExName(ex));
}

// 2) Parse that heartbeat
try
{
    var map = p.Parse(hb);
    var got = string.Join(";", map.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"));
    if (map[8] == "FIX.4.4" && map[9] == "5" && map[35] == "0" && map[10] == "163")
        Ok("Parse_HB", $"{map.GetType().FullName} {got}");
    else
        Fail("Parse_HB", got);
}
catch (Exception ex) { Fail("Parse_HB", ExName(ex)); }

// 3) empty / whitespace
foreach (var (name, input) in new[] { ("empty", ""), ("ws", "  \n\t") })
{
    try { p.Parse(input); Fail(name, "no throw"); }
    catch (ArgumentException ex) { Ok(name, ExName(ex)); }
    catch (Exception ex) { Fail(name, "wrong " + ExName(ex)); }
}

// 4) missing tag 10
try { p.Parse("8=FIX.4.4|35=0"); Fail("no10", "no throw"); }
catch (FormatException ex) { Ok("no10", ExName(ex)); }
catch (Exception ex) { Fail("no10", "wrong " + ExName(ex)); }

// 5) non-numeric 10
try { p.Parse("8=FIX.4.4|35=0|10=abc"); Fail("nonnum10", "no throw"); }
catch (FormatException ex) { Ok("nonnum10", ExName(ex)); }
catch (Exception ex) { Fail("nonnum10", "wrong " + ExName(ex)); }

// 6) bad checksum
try { p.Parse("8=FIX.4.4|9=5|35=0|10=000"); Fail("bad10", "no throw"); }
catch (InvalidOperationException ex) { Ok("bad10", ExName(ex)); }
catch (Exception ex) { Fail("bad10", "wrong " + ExName(ex)); }

// 7) raw SOH (no pipes)
try
{
    var sohMsg = $"8=FIX.4.4{Soh}9=5{Soh}35=0{Soh}10=163";
    var map = p.Parse(sohMsg);
    Fail("soh_input", "accepted as " + string.Join(";", map.Select(kv => $"{kv.Key}={kv.Value}")));
}
catch (FormatException ex) { Ok("soh_input", "rejected " + ExName(ex)); }
catch (Exception ex) { Note("soh_input", "other " + ExName(ex)); }

// 8) wrong BodyLength but matching rebuilt checksum
try
{
    var c = IndependentChecksum(PipeToSohJoin("8=FIX.4.4", "9=999", "35=0"));
    var map = p.Parse($"8=FIX.4.4|9=999|35=0|10={c}");
    Ok("wrong_bodylen_accepted", $"9={map[9]} 10={map[10]} (BodyLength NOT validated)");
}
catch (Exception ex) { Fail("wrong_bodylen_accepted", "threw " + ExName(ex)); }

// 9) double pipe dropped
try
{
    var c = IndependentChecksum(PipeToSohJoin("8=FIX.4.4", "35=0"));
    var map = p.Parse($"8=FIX.4.4||35=0|10={c}");
    Ok("double_pipe_dropped", $"accepted count={map.Count} (empty segment discarded)");
}
catch (Exception ex) { Fail("double_pipe_dropped", ExName(ex)); }

// 10) last-wins MD group
try
{
    var fields = new[] { "8=FIX.4.4", "9=XX", "35=W", "268=2", "269=0", "270=1.10", "269=1", "270=1.20" };
    var c = IndependentChecksum(string.Join(Soh, fields) + Soh);
    var map = p.Parse(string.Join("|", fields) + $"|10={c}");
    Note("last_wins_md", $"count={map.Count} 268={map[268]} 269={map[269]} 270={map[270]} (bid 1.10 lost)");
}
catch (Exception ex) { Fail("last_wins_md", ExName(ex)); }

// 11) no BeginString / no MsgType
try
{
    var c = IndependentChecksum(PipeToSohJoin("1=hi"));
    var map = p.Parse($"1=hi|10={c}");
    Ok("no_beginstring_accepted", $"1={map[1]} (Parse does not require tag 8 or 35)");
}
catch (Exception ex) { Fail("no_beginstring_accepted", ExName(ex)); }

// 12) tag 0
try
{
    var c = IndependentChecksum(PipeToSohJoin("0=x"));
    var map = p.Parse($"0=x|10={c}");
    Ok("tag_zero_accepted", $"0={map[0]}");
}
catch (Exception ex) { Fail("tag_zero_accepted", ExName(ex)); }

// 13) +10 as tag via current-culture TryParse
try
{
    var c = IndependentChecksum(PipeToSohJoin("+10=x"));
    var map = p.Parse($"+10=x|10={c}");
    Note("plus_tag_accepted", $"count={map.Count} keys={string.Join(",", map.Keys)} last10={map[10]}");
}
catch (Exception ex) { Note("plus_tag", ExName(ex)); }

// 14) missing =
try { p.Parse("8=FIX.4.4|bad|10=000"); Fail("missing_eq", "no throw"); }
catch (FormatException ex) { Ok("missing_eq", ExName(ex)); }
catch (Exception ex) { Fail("missing_eq", "wrong " + ExName(ex)); }

// 15) non-numeric tag
try { p.Parse("8=FIX.4.4|abc=1|10=000"); Fail("bad_tag", "no throw"); }
catch (FormatException ex) { Ok("bad_tag", ExName(ex)); }
catch (Exception ex) { Fail("bad_tag", "wrong " + ExName(ex)); }

// 16) Build null
try { p.BuildFixMessage(null!); Fail("build_null", "no throw"); }
catch (ArgumentNullException ex) { Ok("build_null", ExName(ex)); }
catch (Exception ex) { Fail("build_null", "wrong " + ExName(ex)); }

// 17) Build missing 8
try
{
    p.BuildFixMessage(new[] { new KeyValuePair<int, string>(35, "A") });
    Fail("build_no8", "no throw");
}
catch (ArgumentException ex) { Ok("build_no8", ExName(ex)); }
catch (Exception ex) { Fail("build_no8", "wrong " + ExName(ex)); }

// 18) Build remaining tags sorted — harness ER shape
try
{
    var built = p.BuildFixMessage(new[]
    {
        new KeyValuePair<int, string>(8, "FIX.4.4"),
        new KeyValuePair<int, string>(35, "8"),
        new KeyValuePair<int, string>(49, "SENDER"),
        new KeyValuePair<int, string>(56, "cServer"),
        new KeyValuePair<int, string>(57, "TRADE"),
        new KeyValuePair<int, string>(50, "TRADE"),
        new KeyValuePair<int, string>(11, "CL1"),
        new KeyValuePair<int, string>(37, "OID"),
        new KeyValuePair<int, string>(55, "XAUUSD"),
        new KeyValuePair<int, string>(150, "0"),
        new KeyValuePair<int, string>(39, "0"),
        new KeyValuePair<int, string>(60, "20260818-00:00:00.000"),
    });
    Note("build_sorted_er", built);
    var tags = built.Split('|', StringSplitOptions.RemoveEmptyEntries)
        .Select(s => s.Split('=')[0]).ToArray();
    Note("build_sorted_er_tag_order", string.Join(",", tags));
}
catch (Exception ex) { Fail("build_sorted_er", ExName(ex)); }

// 19) Build without 35
try
{
    var built = p.BuildFixMessage(new[]
    {
        new KeyValuePair<int, string>(8, "FIX.4.4"),
        new KeyValuePair<int, string>(49, "S"),
    });
    Note("build_no35", built);
}
catch (Exception ex) { Fail("build_no35", ExName(ex)); }

// 20) trailing pipe accepted
try
{
    var map = p.Parse(hb + "|");
    Ok("trailing_pipe", $"10={map[10]}");
}
catch (Exception ex) { Fail("trailing_pipe", ExName(ex)); }

// 21) equals in value
try
{
    var c = IndependentChecksum(PipeToSohJoin("8=FIX.4.4", "58=a=b=c"));
    var map = p.Parse($"8=FIX.4.4|58=a=b=c|10={c}");
    Ok("equals_in_value", "58=" + map[58]);
}
catch (Exception ex) { Fail("equals_in_value", ExName(ex)); }

// 22) returned map is mutable Dictionary
try
{
    var map = p.Parse(hb);
    if (map is Dictionary<int, string> d)
    {
        d[999] = "mutated";
        Ok("map_mutable", "IReadOnlyDictionary is live Dictionary; cast-mutated 999");
    }
    else Note("map_mutable", map.GetType().FullName ?? "?");
}
catch (Exception ex) { Fail("map_mutable", ExName(ex)); }

// 23) Build then Parse round-trip unique tags
try
{
    var built = p.BuildFixMessage(new[]
    {
        new KeyValuePair<int, string>(8, "FIX.4.4"),
        new KeyValuePair<int, string>(35, "A"),
        new KeyValuePair<int, string>(49, "live.testbroker.1"),
        new KeyValuePair<int, string>(56, "cServer"),
        new KeyValuePair<int, string>(98, "0"),
        new KeyValuePair<int, string>(108, "30"),
        new KeyValuePair<int, string>(141, "Y"),
    });
    var map = p.Parse(built);
    var ok = map[8] == "FIX.4.4" && map[35] == "A" && map[49] == "live.testbroker.1"
             && map[56] == "cServer" && map[98] == "0" && map[108] == "30" && map[141] == "Y"
             && map.ContainsKey(9) && map.ContainsKey(10);
    if (ok) Ok("roundtrip_logon_unique", built);
    else Fail("roundtrip_logon_unique", built + " map=" + string.Join(";", map.Select(kv => $"{kv.Key}={kv.Value}")));
}
catch (Exception ex) { Fail("roundtrip_logon_unique", ExName(ex)); }

// 24) duplicate non-8/35 tags emitted twice; Parse last-wins
try
{
    var built = p.BuildFixMessage(new[]
    {
        new KeyValuePair<int, string>(8, "FIX.4.4"),
        new KeyValuePair<int, string>(35, "y"),
        new KeyValuePair<int, string>(55, "1"),
        new KeyValuePair<int, string>(1007, "EURUSD"),
        new KeyValuePair<int, string>(55, "2"),
        new KeyValuePair<int, string>(1007, "XAUUSD"),
    });
    Note("build_dup_55", built);
    var map = p.Parse(built);
    Note("parse_dup_55", $"55={map[55]} 1007={map[1007]} (first instrument lost)");
}
catch (Exception ex) { Fail("dup_55", ExName(ex)); }

// 25) 10=7 vs expected 007 — only if we can construct a small checksum. Use compare of unpadded when D3 != raw.
try
{
    // Force a known D3 of 163 as "163" already 3 digits. Construct a tiny raw whose sum%256 < 10.
    // Search a short tag set... easier: parse with 10=163 vs we already have hb checksum 163.
    // Unpadded only matters for values like 7. Build a raw we checksum independently to 007 if possible.
    string? found = null;
    for (var i = 0; i < 256 && found is null; i++)
    {
        var body = "35=0|58=" + i;
        var sohRaw = body.Replace('|', Soh);
        // we need full message. Use 8 + 9 + 35=0 + 58=i
        var fields = new[] { "8=FIX.4.4", "9=x", "35=0", "58=" + i };
        // iterate 9 to make it consistent is unnecessary; we just need sum%256 < 10
        var raw = string.Join(Soh, fields) + Soh;
        var mod = Encoding.ASCII.GetBytes(raw).Sum(b => (int)b) % 256;
        if (mod < 10)
        {
            var padded = mod.ToString("D3", CultureInfo.InvariantCulture);
            try
            {
                p.Parse(string.Join("|", fields) + $"|10={mod}");
                found = $"unpadded 10={mod} ACCEPTED (expected {padded})";
            }
            catch (InvalidOperationException)
            {
                found = $"unpadded 10={mod} REJECTED vs expected {padded}";
            }
        }
    }
    Note("unpadded_10", found ?? "no sample with sum%256<10 in scan");
}
catch (Exception ex) { Fail("unpadded_10", ExName(ex)); }

// 26) mid-message 10= included in checksum then overwritten
try
{
    var fields = new[] { "8=FIX.4.4", "10=999", "35=0" };
    var c = IndependentChecksum(string.Join(Soh, fields) + Soh);
    var map = p.Parse(string.Join("|", fields) + $"|10={c}");
    Note("mid_tag10", $"accepted trailer10={map[10]} (mid 10=999 overwritten; still in checksum input)");
}
catch (Exception ex) { Fail("mid_tag10", ExName(ex)); }

Console.WriteLine("DONE");
