# W500_SLICE_73

- **slot:** 73
- **file:** `D:/Prop/src/Mt5/Env/EnvFile.cs`
- **angle:** live cTrader NewOrderSingle or capital-loss path
- **read:** full file (42/42 lines) via `read_file`; grep on this file for `NewOrderSingle|capital.?loss|cTrader|OrderSend|PlaceOrder|loss` returned no matches
- **verdict:** PASS

## Binding law (this angle)

Slot 73 asks whether this type can emit a live cTrader FIX `NewOrderSingle` (tag 35=D) or otherwise take a path that can lose capital (send, amend, cancel-into-fill, flatten, size-up, or bypass risk before destination submit).

Empty PASS is allowed only after the assigned file is fully read. It was.

## Evidence quotes

`EnvFile` is a static `.env` locator/loader under `TraderIntelligence.Mt5.Env`. It has two public methods: `FindAndLoad()` (search + load) and `Load(string path)` (parse lines into `Environment.SetEnvironmentVariable`). There is no session, no FIX codec, no order DTO, and no broker I/O.

The entire type:

```1:41:D:/Prop/src/Mt5/Env/EnvFile.cs
namespace TraderIntelligence.Mt5.Env;

public static class EnvFile
{
    public static string? FindAndLoad()
    {
        var cwd = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(cwd, ".env")),
            Path.GetFullPath(Path.Combine(cwd, "..", ".env")),
            Path.GetFullPath(Path.Combine(cwd, "..", "..", ".env")),
            Path.GetFullPath(Path.Combine(cwd, "..", "..", "..", ".env")),
            @"D:\Prop\.env"
        };
        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null)
            return null;
        Load(path);
        return path;
    }

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

Behavior on disk I/O only:

1. Walks five candidate paths (cwd and three parents, plus a fixed lab root `D:\Prop\.env`).
2. First existing file wins; missing file → `null` / no-op.
3. Splits on the first `=`; skips blanks, `#` comments, and lines without `=`.
4. Strips matching surrounding quotes; writes process environment.

Grep on this exact file for the live-send / capital-loss vocabulary (`NewOrderSingle`, `capital.?loss`, `cTrader`, `OrderSend`, `PlaceOrder`, `loss`) returned **no matches**.

This file does not contain:

- `NewOrderSingle`, `35=D`, `MsgType`, QuickFix/N `Session.SendToTarget`, or any FIX encode/send
- cTrader trade-session types (`CTraderFixSession` trade qualifier, `OrdType`, `Side`, `OrderQty`, `ClOrdID`)
- MT5 `OrderSend` / `DealerSend` / Manager trade request APIs
- risk, sizing, kill-switch, copy-intent, outbox dispatch, or flatten logic
- network sockets, HTTP clients, or broker endpoints
- hardcoded credentials, proxy auth, or FIX passwords (values come from an external `.env` if present; this type never logs or prints them)

Any later consumer that *reads* env vars (FIX host, sender, passwords) is outside this slice’s type. Loading configuration is not submitting an order.

## No-loss implication

`EnvFile` cannot open, close, or amend a live cTrader (or MT5) position. Worst case inside this type is: no `.env` found (`null`), missing path no-op, or process env vars overwritten from disk. Those outcomes do not emit `NewOrderSingle` and do not size or route risk.

No-loss: capital cannot be lost *by this file*. A mis-loaded `.env` could later change *other* hosts’ endpoints or credentials, but Slot 73’s live send / capital-loss path is absent here by construction.

Empty-PASS justification: the assigned file was fully read (42 lines); live cTrader `NewOrderSingle` and any capital-loss path are absent, not skipped.
