# W500_SLICE_23

- **slot:** 23
- **file:** `D:/Prop/src/Mt5/Env/EnvFile.cs`
- **angle:** live cTrader NewOrderSingle or capital-loss path
- **read:** full file (23 lines) via `read_file`; grep on this file for `NewOrderSingle|cTrader|capital.?loss|OrderSend|PlaceOrder|live` returned no matches
- **verdict:** PASS

## Evidence quotes

`EnvFile` is a static dotenv-style loader only. It reads lines, skips blanks/comments, splits on first `=`, optionally strips surrounding quotes, and calls `Environment.SetEnvironmentVariable`. There is no FIX, no cTrader client, no order builder, and no send path.

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

- `NewOrderSingle` / FIX tag 35=D
- cTrader TRADE socket, logon, or session
- `OrderSend` / place-order / cancel-replace
- position size, SL/TP, or PnL
- any broker I/O besides process-local env mutation

Live NewOrderSingle / capital-at-risk controls live elsewhere (not this type), e.g. `Fix.CTrader/Configuration/CTraderFixOptions.cs` (`When true, allow placing new orders (NewOrderSingle). Default OFF.`) and `Application/Runtime/LiveRuntimeStatus.cs` (`NewOrderSingle disabled. SHADOW/CopyIntent only.`). Those are out of this slice’s file.

## No-loss implication

`EnvFile.Load` cannot open a TRADE session, cannot emit FIX `NewOrderSingle`, and cannot reduce account equity. Worst case is setting process environment keys from a text file; that is configuration, not execution. Slot 23 therefore has **no live capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read; the angle (live cTrader NewOrderSingle / capital-loss) is absent by construction, not by skipped review.
