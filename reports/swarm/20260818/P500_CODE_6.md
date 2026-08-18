# P500_CODE_6 — QuantityNormalizer vs FIX OrderQty (logon host)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_CODE_6.md` |
| Slot | **6** |
| Date | 2026-08-18 |
| Agent | P500 CODE subagent, slot 6 |
| File | `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` |
| Angle | Is `QuantityNormalizer` wired to any FIX `OrderQty` or is it unit-test only? |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **No** (FIX password read from config is not quoted) |
| Method | `read_file` + `grep` only. Full 113-line host re-read. `QuantityNormalizer.cs`, `CTraderFixSession.cs`, `CTraderFixOptions.cs`, `fix-worker/Worker.cs`, conversion tests, `src/` greps. No `35=D`. No NewOrderSingle build/send. |

Measured live context (assigned, this wave): **8463** accounts; **Achiever scoring**; **Starwave deals-done scored 0**; **SHADOW all demo**; **destinationRealPnl 0**; FIX **LoggedOn**; **REAL_COPY false**.

---

## 0. Verdict

**UNIT_TEST_ONLY. Not wired to any FIX `OrderQty`. Live tag 38 is `SAFE_BY_ABSENCE`, not a proven converter.**

`CTraderFixLogonHostedService` was read in full. It does **not** import `TraderIntelligence.Domain.Execution`, does **not** construct `QuantityNormalizer`, does **not** call `Normalize`, and does **not** write tag 38 / `OrderQty`. After QUOTE+TRADE logon it forces `_runtime.RealCopyEnabled = false` and logs that NewOrderSingle is still disabled. The only FIX message this stack can emit is `35=A` Logon (`CTraderFixSession.BuildLogon`).

Product `src/**/*.cs`: `QuantityNormalizer` appears **only** as its own definition (`Domain\Execution\QuantityNormalizer.cs`). `new QuantityNormalizer` exists **only** under `tests/`. `IQuantityConverter` does **not** exist. `grep OrderQty` over `D:\Prop\src` = **0 hits**.

This is **not** an empty PASS. The host file was read. The answer to the angle is measured: **unit-test only**.

| Question | Measured answer |
|---|---|
| Does the assigned host wire `QuantityNormalizer` → FIX `OrderQty`? | **No.** Zero mentions of either identifier in the 113-line file. |
| Does any product path write `Normalize(...)` as tag 38? | **No.** Zero product callers. Zero `35=D` / `OrderQty` / `38=` builders in `src/`. |
| Who instantiates `QuantityNormalizer`? | Unit tests only: `ExecutionAndSizingTests`, `SourceDestinationQuantityConversionTests`, `QuantityNormalizerStepMinMaxTests`. |
| Would the class be a legal §38 converter if later wired? | **No.** `Normalize(0.10, 1, dest) = 0.10`, not `10.00`. G7 / A43 E01 FAIL. Binding Fact `Never_passthrough_MT5_lots` is **Skipped**. |
| Can copy take a live loss today? | **No.** No NOS encoder. Host hard-sets `RealCopyEnabled = false`. SHADOW demo only. `destinationRealPnl 0`. |

**Risk to capital from this file / this type: NONE.**

---

## 1. Assigned file (read in full)

Path: `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` (113 lines).

Responsibilities actually present:

1. Read FIX password from config; skip logon if missing / placeholder (password value **not** quoted here).
2. `CTraderFixSession.TryLogonAsync` QUOTE `:5211` then TRADE `:5212`.
3. Copy session flags onto `LiveRuntimeStatus`.
4. **Force** `RealCopyEnabled = false`.
5. Persist `FixSessionState` host/port/status only.

### Evidence quotes — no quantity, no OrderQty, no send

```60:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        _runtime.Quote.Status = quote.Status;
        _runtime.Quote.LastError = quote.LastError;
        _runtime.Quote.UpdatedAt = DateTimeOffset.UtcNow;
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        _runtime.Trade.Status = trade.Status;
        _runtime.Trade.LastError = trade.LastError;
        _runtime.Trade.UpdatedAt = DateTimeOffset.UtcNow;
        _runtime.RealCopyEnabled = false;

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

Persist path writes session metadata only (`Host`, `Port`, `Status`, timestamps). No qty column, no ClOrdID, no `CopyIntent`:

```96:109:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var set = ctx.Set<TraderIntelligence.Domain.Entities.FixSessionState>();
        foreach (var result in new[] { quote, trade })
        {
            var row = await set.FirstOrDefaultAsync(s => s.Qualifier == result.Qualifier, ct);
            if (row is null)
                continue;
            row.Host = host;
            row.Port = result.Qualifier == FixSessionQualifier.Quote ? 5211 : 5212;
            row.Status = result.LoggedOn ? FixSessionStatus.LoggedOn : FixSessionStatus.Error;
            row.LastError = result.LastError;
            row.LastInboundAt = DateTimeOffset.UtcNow;
            row.LastOutboundAt = DateTimeOffset.UtcNow;
            row.UpdatedAt = DateTimeOffset.UtcNow;
        }
```

Usings on the host: `EF Core`, `Configuration`, `DI`, `Hosting`, `Logging`, `Application.Runtime`, `Domain.Enums`, `Fix.CTrader.Sessions`. **No** `Domain.Execution`.

---

## 2. Adjacent FIX stack — still no OrderQty

`CTraderFixSession` outbound body is Logon only (`35=A`). Tags present: 35, 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554. **No tag 38.**

```89:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(
        string sender, string target, string senderSub, string targetSub,
        string username, string password, int seq)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender),
            (56, target),
            (50, senderSub),
            (57, targetSub),
            (52, sendingTime),
            (98, "0"),
            (108, "30"),
            (141, "Y"),
            (553, username),
            (554, password)
        };
        return Assemble(fields);
    }
```

`CTraderFixOptions.RealCopyExecutionEnabled` default **false**; comment says NewOrderSingle is allowed only when true:

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

`Fix.CTrader` grep for `QuantityNormalizer` / `OrderQty` / `38=` / `35=D`: **host log string + options comment only**. Session layer has **no** `OrderQty` match.

`apps/fix-worker/Worker.cs` never calls the normalizer. Even if config `CTrader:RealCopyExecutionEnabled` is true, it still refuses send:

```21:46:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
        // ...
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
        // ...
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

`LiveRuntimeStatus.Snapshot` copyNote when `RealCopyEnabled` is false: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

---

## 3. QuantityNormalizer — definition only in product; callers are tests

```9:29:D:\Prop\src\Domain\Execution\QuantityNormalizer.cs
public sealed class QuantityNormalizer
{
    public decimal Normalize(decimal sourceLots, decimal allocationFactor, InstrumentQuantitySpec dest)
    {
        // ...
        var raw = sourceLots * allocationFactor;
        var steps = decimal.Truncate(raw / dest.StepSize);
        var qty = steps * dest.StepSize;
        qty = decimal.Round(qty, dest.Precision, MidpointRounding.ToZero);
        if (qty < dest.MinQuantity)
            return 0m;
        if (qty > dest.MaxQuantity)
            return dest.MaxQuantity;
        return qty;
    }
}
```

`grep QuantityNormalizer` over `D:\Prop\**\*.cs`:

| Path | Role |
|---|---|
| `src\Domain\Execution\QuantityNormalizer.cs` | **definition only** |
| `tests\Unit\ExecutionAndSizingTests.cs` | `new QuantityNormalizer()`; asserts `Normalize(0.10, 1) == 0.10` |
| `tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` | `private readonly QuantityNormalizer _n = new()` |
| `tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs` | last-stage min/step/max matrix |

No hit in `Fix.CTrader`, `Application`, `Infrastructure`, `apps/api`, `apps/fix-worker`, `apps/mt5-worker`, `RiskEngine`, `ShadowCopyEngine`.

`RiskEngine` copies `request.RequestedQuantity` through as `ApprovedQuantity` — it never calls `Normalize`. `IQuantityConverter` is **MISSING** (`grep` over `src/` = 0).

Tests **document** the gap (skipped Facts that fail closed on purpose):

```173:182:D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs
    [Fact(Skip = "A43 §6: shadow and live must call the same converter.")]
    public void Shadow_and_live_share_converter()
    {
        true.Should().BeFalse("QuantityNormalizer is unused by ShadowCopyEngine and RiskEngine");
    }

    [Fact(Skip = "A43 §6: FIX worker must not rescale requested_quantity.")]
    public void Fix_worker_does_not_rescale()
    {
        true.Should().BeFalse("No FIX NOS builder consumes QuantityNormalizer output");
    }
```

Passthrough is **locked green** (the opposite of A43 E01 / G7):

```19:24:D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs
    [Fact]
    public void QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one()
    {
        _n.Normalize(0.10m, 1m, DestBaseUnits1Oz).Should().Be(0.10m);
        _n.Normalize(0.10m, 1m, DestBaseUnits1Oz).Should().NotBe(10.00m);
    }
```

```49:49:D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs
    [Fact(Skip = "A43 G7 / E01: IQuantityConverter missing. 0.10 MT5 lots × 100 oz → BaseUnits OrderQty 10.00, not 0.10.")]
```

`QuantityNormalizerStepMinMaxTests` header: *"Does not cover lots→ounces→OrderQty (that is G7 / A43 converter)."*

---

## 4. Profit implication

**No destination profit can be produced from this wiring, because there is no wiring.**

- FIX is **LoggedOn** (`35=A` only). Logon is session proof, not a size or a fill.
- `REAL_COPY` is **false** (host hard-set, options default false).
- SHADOW is **all demo**. `destinationRealPnl` is **0**.
- Achiever scoring can rank source traders; it does **not** emit tag 38.
- Starwave **deals-done scored 0** — no scored Starwave book to size, even if a converter existed.
- 8463 manager accounts are a **read census**, not 8463 destination tickets.
- If someone later naively wired `Normalize(sourceLots, 1, dest)` into a future NOS, `0.10` MT5 lots would become `OrderQty 0.10` instead of `10.00` on a BaseUnits XAU book — **100× too small**. That would **cap live profit** (and fail G7), not create it.

Profit path that is missing, not “passing”:

```text
source deal → reconstruct → IQuantityConverter (MISSING) → QuantityNormalizer last-stage (UNUSED)
    → RiskEngine (passthrough RequestedQuantity) → 35=D tag 38 (DOES NOT EXIST)
```

---

## 5. Lower-loss implication

**Current capital risk: NONE (`SAFE_BY_ABSENCE`).**

- Host cannot place an order. It cannot mis-size an order.
- No `OrderQty` exists on the wire, so a 100× gold error cannot hit Pepperstone today.
- Forcing `RealCopyEnabled = false` after a successful logon prevents the runtime snapshot from advertising an armed send.
- Worker still refuses NewOrderSingle even if the config flag is flipped.
- Demo SHADOW + `destinationRealPnl 0` means no live destination inventory to flatten badly.

**Future loss mode (not live today):** wiring the unit-test helper straight into tag 38 without `IQuantityConverter` would emit lots-as-units. On a lots-convention book that happens to expect `0.10`, it would look “right” by accident; on Spotware BaseUnits XAU it would be 100× small (or rejected as below min). The inverse (treating ounces as lots) is the 100× **oversize** path — also not reachable, because no NOS builder exists.

Do **not** treat this slot as a license to add `35=D`. Do **not** set `REAL_COPY_EXECUTION_ENABLED=true`. Do **not** grow `QuantityNormalizer` into a converter inside `Fix.CTrader`.

---

## 6. Grep ledger (this slot)

| Pattern / path | Result |
|---|---|
| `QuantityNormalizer` in assigned host | **0** |
| `OrderQty` in assigned host | **0** |
| `OrderQty` in `D:\Prop\src` | **0** |
| `QuantityNormalizer` in `D:\Prop\src` | **1 file** — definition |
| `new QuantityNormalizer` in product | **0** |
| `new QuantityNormalizer` in tests | **3 files** |
| `IQuantityConverter` in `src/` | **0** |
| `35=D` in `Fix.CTrader` | **0** |
| `BuildLogon` tag 35 | `"A"` only |
| Host `RealCopyEnabled` | **hard `false`** |
| Live measured | 8463 accounts; Achiever scoring; Starwave deals-done scored 0; SHADOW demo; destinationRealPnl 0; FIX LoggedOn; REAL_COPY false |

---

## 7. One-liner

`QuantityNormalizer` is unit-test only: `CTraderFixLogonHostedService` logs FIX LoggedOn with NewOrderSingle disabled and never touches OrderQty; live copy cannot profit or lose because tag 38 does not exist.
