# W500_SLICE_48

- **slot:** 48
- **file:** `D:/Prop/src/Mt5/Env/EnvFile.cs`
- **angle:** DealRequestByGroup 90-day timeout without chunking
- **read:** full file (23 lines) via `read_file`; grep on this file / `src/Mt5/Env` for `DealRequestByGroup|90.?day|timeout|chunk` returned no matches; repo grep `EnvFile.Load` in product `*.cs` returned **zero callers**
- **verdict:** PASS

## Evidence quotes

`EnvFile` is a static dotenv-style loader only. It reads lines, skips blanks and `#` comments, splits on the first `=`, optionally strips surrounding quotes, and calls `Environment.SetEnvironmentVariable`. There is no Manager API, no group mask, no `from`/`to` unix window, no deal array, and no network I/O.

```1:23:D:/Prop/src/Mt5/Env/EnvFile.cs
namespace TraderIntelligence.Mt5.Env;

public static class EnvFile
{
    public static void Load(string path)
    {
        if (!File.Exists(path))
            return;

        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || !line.Contains('='))
                continue;
            var i = line.IndexOf('=');
            var key = line[..i].Trim();
            var value = line[(i + 1)..].Trim();
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                value = value[1..^1];
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
```

This file does not contain:

- `DealRequestByGroup` / `GetGroupDealsAsync` / `IMt5BulkDealReader`
- `AddDays(-90)` or any deal-history `from`/`to` window
- paging, 1–14 day chunking, `DealRequestPage`, or a deal-request timeout
- `CIMTManagerAPI`, `lock (_gate)`, or any broker RPC
- any key-name special-case for ingest windows (keys are copied blindly)

`EnvFile.Load` has **zero callers** in product C# (class is referenced only by its own declaration). Even if a host later invoked it, this type still would not choose a 90-day span or issue `DealRequestByGroup`.

The 90-day ingest window and the group-deal RPC live **elsewhere** (out of this slice’s file; not re-scored here):

```32:33:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
            var from = DateTimeOffset.UtcNow.AddDays(-90);
            var to = DateTimeOffset.UtcNow.AddMinutes(1);
```

```64:69:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        if (connector is IMt5BulkDealReader bulk)
        {
            foreach (var group in groups)
            {
                var deals = await bulk.GetGroupDealsAsync(group.Name, from, to, ct);
```

```296:315:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5DealDto> GetGroupDealsCore(string group, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_gate)
        {
            Ensure();
            var all = new List<Mt5DealDto>();
            foreach (var (start, end) in Windows(from, to))
            {
                var arr = _manager!.DealCreateArray();
                try
                {
                    var res = _manager.DealRequestByGroup(group, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), arr);
                    if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                        throw new InvalidOperationException(Describe(res, $"{BrokerCode} DealRequestByGroup {group}"));
                    all.AddRange(ReadDeals(arr));
                }
                finally { arr.Release(); }
            }
```

Those sites own the 90-day host window and the Manager `DealRequestByGroup` loop. Slot 48 does not re-score the ingest path.

## No-loss implication

`EnvFile.Load` cannot issue `DealRequestByGroup`, cannot hold a 90-day unchunked Manager call until timeout, and cannot drop or duplicate deals from a truncated group dump. It cannot send orders or mutate ledger/PnL. Worst case in this file is writing process environment keys from a text file (and today nothing even calls `Load`). Slot 48 therefore has **no 90-day DealRequestByGroup timeout / no-chunk capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read; the angle (DealRequestByGroup 90-day timeout without chunking) is absent by construction, not by skipped review.
