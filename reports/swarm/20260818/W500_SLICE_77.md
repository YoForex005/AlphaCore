# W500_SLICE_77

- **slot:** 77
- **file:** `D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs`
- **angle:** positions limited to first 200 accounts
- **read:** full file (53/53 lines) via `read_file`; grep on this file for `200|account|position|Position` returned **no matches**; grep under `D:/Prop/src/Infrastructure/Mt5Live` for `200` returned **no matches**; workspace grep for `Take(200)` / `accounts.Take` / `first 200` found **no matches under `D:/Prop/src`**
- **verdict:** PASS

## Evidence quotes

`LiveMt5Registration` is a static factory + secret-presence gate. It does not enumerate broker logins, does not call `GetPositionsAsync` / `GetGroupPositionsAsync` / `ReplacePositionsAsync`, and does not apply `Take(200)` or any other account-window constant.

```8:15:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
public static class LiveMt5Registration
{
    public static bool HasRealPasswords(IConfiguration config)
    {
        var a = config["MT5_PASSWORD"];
        var s = config["MT5_STARWAVEFX_PASSWORD"];
        return IsSecret(a) && IsSecret(s);
    }
```

`CreateConnectors` builds **exactly two** `NativeMt5BrokerConnector` instances (Achiever + StarwaveFx), each with a single manager login from config. The return is a two-element array — not an account census and not a position snapshot.

```17:46:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            Server = config["MT5_SERVER"] ?? "",
            Port = int.TryParse(config["MT5_PORT"], out var ap) ? ap : 443,
            Login = ulong.TryParse(config["MT5_LOGIN"], out var al) ? al : 0,
            ...
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            ...
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
    }
```

The only other member is a presence check (no placeholder / no `(a/c` comment). It does not count accounts or slice lists.

```49:52:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
    private static bool IsSecret(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("<SECRET>", StringComparison.Ordinal)
        && !value.Contains("(a/c", StringComparison.Ordinal);
```

This file does not contain:

- `200` / `Take(200)` / `Take(` / `.Skip(` / page size / `limit`
- `GetAccountsAsync` / `ListLoginsAsync` / account entities
- `GetPositionsAsync` / `GetGroupPositionsAsync` / `ReplacePositionsAsync` / `Mt5PositionDto`
- writes to `mt5_positions_current`

Callers only register the two connectors as singletons (`DependencyInjection.AddTraderIntelligence` → `foreach (var c in LiveMt5Registration.CreateConnectors(configuration))`). Registration does not start a position loop.

**Stale prior swarm (do not reuse):** older notes (A005, W500_SLICE_7, W500_SLICE_27) quoted ingest as `foreach (var account in accounts.Take(200))`. That literal is **gone** from current `D:/Prop/src`. Grep of `src` for `Take(200)` / `accounts.Take` is empty. Current `DealIngestionService.SyncBrokerAsync` either bulk-replaces the broker book or walks the **full** account list:

```81:93:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        if (connector is IMt5BulkPositionReader posBulk)
        {
            var positions = await posBulk.GetGroupPositionsAsync("*", ct);
            await _store.ReplaceBrokerPositionsAsync(brokerId, positions, ct);
        }
        else
        {
            foreach (var account in accounts)
            {
                var positions = await connector.GetPositionsAsync(account.Login, ct);
                await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
            }
        }
```

The connectors this file constructs implement `IMt5BulkPositionReader` (`NativeMt5BrokerConnector`), so live ingest uses `GetGroupPositionsAsync("*")` — group mask `*`, not first-200 logins. Per-login `GetPositionsCore` is `PositionRequest((ulong)login, arr)` with no 200-account window.

The only remaining in-repo `Take(200)` is a reconstructed-trade explorer page, **not** a live-position account window:

```107:107:D:/Prop/apps/api/Program.cs
        query.OrderByDescending(t => t.OpenedAt).Take(200), ct);
```

Those paths are **out of this slice’s file**. Slot 77 asked whether **this** registration type limits position sync to the first 200 accounts. It does not.

## No-loss implication

`CreateConnectors` cannot drop, truncate, or skip a position book. It only constructs two manager-side connectors. It cannot send orders, cannot size positions, and cannot leave accounts 201+ with a stale local book — because it never reads or writes positions.

Residual no-loss notes (not owned by this file): (1) current `DealIngestionService` no longer applies `accounts.Take(200)` — completeness of the open book now depends on the native manager returning the full group book for `"*"`; (2) `GET /api/trades` `Take(200)` can hide older reconstructed rows from the explorer, which is a UI/API window, not a live-position capital path; (3) prior reports that still cite ingest `Take(200)` are stale vs the source read for this slot.

Slot 77 therefore has **no first-200-accounts position-blind-spot and no position-cap capital-loss path in the assigned file**.

Empty-PASS justification: the assigned file was fully read (53/53 lines); the angle (positions limited to first 200 accounts) is absent by construction, not by skipped review.
