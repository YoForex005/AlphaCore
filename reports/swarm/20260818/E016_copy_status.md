# E016 — Copy to cTrader live is **OFF**. Demo shadow only.

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E016_copy_status.md` |
| Agent | E016 (copy-status pin) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T08:19:31Z (2026-08-18T13:49:31+05:30) |
| Host | local API `http://127.0.0.1:5000` (HTTP 200) |
| Assigned | Copy to cTrader live is OFF. Demo shadow only. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** |
| Binding law | Architecture §1 / §7 / §41 / §56 / §68–§70; A24; A49; A101 item 12 |
| Siblings (do not treat as this file) | E002 (no sender), D69 (flag default), D32 (worker `Disconnected`), D48 (6 demo `shadow_orders`), D83 (shadow page chrome), D81 / C37 (live page stub), D97 (`CanPromoteToLive` false), C43 (Logon not proven), `reports/CREDENTIALS_AND_COPY_STATUS.md` |
| Method | Re-read options POCO, fix-worker, API `Program` + `SettingsController` + `appsettings`, dashboard queries, `PersistDemoShadowAsync`, `ShadowCopyEngine`, seeder FIX rows, Live/Shadow pages, local `.env` **flag line only**. Product-tree grep for `NewOrderSingle` / `35=D` / `RealCopy`. SHA-256 via `Get-FileHash`. Live `GET /api/overview`, `/api/settings`, `/api/risk`, `/api/fix/sessions`, `/api/health`. **No product edit. No live FIX Logon. No `35=D` attempted.** |

**Honesty rule:** a hardcoded dashboard `false` is a **display floor**, not a send gate. Demo `shadow_orders` are **not** Architecture §24 destination-QUOTE fills. Absence of a `35=D` builder is **SAFE_BY_ABSENCE**. `CTRADER_FIX_ENABLED=true` in the gitignored `.env` is **not** Logon. Do not print secrets.

---

## 0. Verdict (binding)

**CONFIRMED.**

| Assigned claim | Measured result | Class |
|---|---|---|
| Copy to cTrader **live** is OFF | **Yes.** Flag default `false`; live API `realCopyEnabled=false`; TRADE `Disconnected`; **no** `NewOrderSingle` sender | live send **OFF** / **SAFE_BY_ABSENCE** |
| Demo **shadow** only | **Yes.** Live `shadow=2`, `live=0`, `liveCandidates=0`. Persist writes `SHADOW_ONLY` intents + in-process `SimulateEntry` for `TraderState.SHADOW` only | demo shadow **EXISTS**; §24 book **MISSING** |
| A live Pepperstone/cTrader order can be placed from this process | **No** | no socket, no initiator, no `35=D` |
| Safe to set `REAL_COPY_EXECUTION_ENABLED=true` | **No** | §68 still 0/19; §70 still 0/14 |

One-line:

```text
LIVE COPY = OFF
DEMO SHADOW ONLY (2 SHADOW traders, 0 LIVE, destinationRealPnl=0)
NO NewOrderSingle / 35=D SENDER
```

Do **not** treat `shadowPnl=248.20` as destination P&L. Do **not** treat the `/live` page as a live book. Do **not** treat this file as a §68 / §70 PASS.

---

## 1. Live API snapshot (this pass)

`GET http://127.0.0.1:5000/health` → `{"status":"ok","utc":"2026-08-18T08:19:24.5013785+00:00"}`.

| Endpoint | HTTP | Load-bearing fields |
|---|---:|---|
| `/api/overview` | 200 | `shadow=2`, `live=0`, `liveCandidates=0`, `riskBlocked=1`, `shadowPnl=248.20`, `destinationRealPnl=0`, `xauGross=0`, `xauNet=0`, `quoteHealthy=false`, `tradeHealthy=false`, **`realCopyEnabled=false`** |
| `/api/settings` | 200 | `featureFlags.REAL_COPY_EXECUTION_ENABLED=false` (hardcoded in `Program.cs`, not read from `.env`) |
| `/api/risk` | 200 | **`realCopyEnabled=false`**, `killSwitch=None`, `dailyPnl=0`, empty reject list |
| `/api/fix/sessions` | 200 | QUOTE+TRADE **`Disconnected`**, `connected=false`, `loggedOn=false`, **`executionEnabled=false`** |
| `/api/health` | 200 | MT5 = `demo FakeMt5BrokerConnector — not live Manager`; QUOTE = `no live TLS socket` |

FIX session errors (seeded, not a real disconnect handshake):

| Qualifier | Port | LastError |
|---|---:|---|
| QUOTE | 5211 | `No live QUOTE socket. Demo seed only.` |
| TRADE | 5212 | `No live TRADE socket. NewOrderSingle off.` |

Quote book on the FIX page is the **seeded** 2399.45 / 2399.85 row with `instrumentId=null` (not a discovered cTrader tag 55).

Overview counts: 4 MT5 accounts, 2 brokers, 3 XAU traders, 3 with ≥3 trades. That is the **demo seeder** population (`10001` SHADOW, `10002` RISK_BLOCKED, `10003` insufficient / non-shadow, `99001` SHADOW). Zero `LIVE`.

---

## 2. Why live copy is OFF

### 2.1 Config / display floors (all false)

| Surface | Value | Gate? |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | C# default **`false`** | owning POCO; **not bound** by worker |
| Local `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=false` | **not** the worker key (`CTrader:RealCopyExecutionEnabled`) |
| `apps/api/appsettings.json` `FeatureFlags:LiveCopyEnabled` | **`false`** | different name |
| `GET /api/settings` | hardcoded **`false`** | display |
| `EfDashboardQueries` overview / risk / FIX `ExecutionEnabled` | literal **`false`** | display |
| `LiveCopyPage.tsx` | static “is false” | chrome only |
| `docker-compose.yml` / fix-worker `appsettings.json` | key **absent** | worker `GetValue` fallback **`false`** |
| `tests/` `RealCopyExecutionEnabled` | **0** hits | no fixture lock on this property |

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

```42:46:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", () => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool> { ["REAL_COPY_EXECUTION_ENABLED"] = false },
```

### 2.2 Worker does not send even if the flag is flipped

```21:46:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
        // ...
                trade.Status = FixSessionStatus.Disconnected;
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
        // ...
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

If `real` is true the worker **logs a warning**. Status stays `Disconnected`. No socket. No `35=D`.

### 2.3 No live sender exists (SAFE_BY_ABSENCE)

Product `src/` + `apps/` (excluding comments/logs/UI copy) contain **no** outbound NewOrderSingle:

| Pattern | Product hits that send | Meaning |
|---|---:|---|
| `35=D` | **0** | no wire text |
| `QuickFIX` / `SocketInitiator` / `TcpClient` / `SslStream` | **0** in send path | no initiator / TLS session |
| `SubmitNewOrder` / `GuardedNewOrderSingle` / `MaySendNewOrder` | **0** | A101 choke **MISSING** |
| `NewOrderSingle` as a method that builds FIX | **0** | name appears in comments, logs, `MayRetryNewOrderSingle` only |

`SettingsController` PUT `LiveCopyEnabled` writes Redis `settings:flags:live_copy` only and is **not mapped** (`AddControllers` / `MapControllers` absent). A dashboard click cannot enable send.

`TraderStateMachine.CanPromoteToLive` is unconditionally `false` (D97). `FromBaseline` cannot emit `LIVE`. Persist copies `SuggestedState` blindly; today that cannot become LIVE.

`RiskEngine.AllowFixSend` is a DTO bit. **No worker reads it.**

---

## 3. Why the running mode is demo shadow only

### 3.1 The only persist path

```104:104:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
```

`EfTradingStore.PersistDemoShadowAsync`:

1. Always writes an outbox `ScoreUpdate`.
2. **Returns without shadow rows** unless `state == TraderState.SHADOW`.
3. Needs a `destination_quotes` row (seeder invents 2399.45 / 2399.85, `VenueInstrumentId=null`).
4. Inserts `CopyIntent` with `Status = "SHADOW_ONLY"` and idempotency `shadow:{brokerId}:{login}:{positionId}`.
5. Calls `ShadowCopyEngine.SimulateEntry` (in-process taker-touch math, 80 ms modeled delay).
6. Inserts a `ShadowOrder`. **Never** calls `SimulateExit`. **Never** talks to cTrader.

D48 measured first empty-store `SeedAsync`: **6** `shadow_orders` + **6** `SHADOW_ONLY` intents (`10001`×3, `99001`×3). `10002` / `10003` get none. This pass’s live overview `shadowPnl=248.20` is `Sum(ShadowOrders.SourceVsShadowSlippage)`, **not** §24 shadow P&L.

### 3.2 What shadow is **not**

| Claim | Measured |
|---|---|
| Destination QUOTE session prices the fill | **False.** Seeder snapshot; `instrumentId=null`; QUOTE socket down |
| CopyIntent approved + unexpired | **False.** Status is `SHADOW_ONLY`; `ExpiresAt = trade.OpenedAt + 15s` (already stale vs now) |
| `/shadow` paints the 6 rows | **False.** `ShadowPortfolioPage.tsx` is a 14-line stub (D83) |
| `/live` is an empty-safe live book | **False.** 8-line stub (D81); no `GET /live/portfolio` |
| Risk engine gated the shadow write | **False.** `RiskEngine` is unused on this path |
| This is Architecture §24 | **False.** Entry-only calculator rows |

UI copy that **is** true: `OverviewPage` “Live FIX send is off”; `ShadowPortfolioPage` “Live NewOrderSingle remains disabled”; `LiveCopyPage` “REAL_COPY_EXECUTION_ENABLED is false.”

---

## 4. File identity (this pass)

| Path | SHA-256 | Bytes |
|---|---|---:|
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | 2344 |
| `apps/fix-worker/Worker.cs` | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | 2093 |
| `apps/api/Program.cs` | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 4731 |
| `apps/api/appsettings.json` | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | 1254 |
| `apps/api/Controllers/SettingsController.cs` | `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` | 3732 |
| `src/Infrastructure/Dashboard/EfDashboardQueries.cs` | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 8708 |
| `src/Infrastructure/Persistence/EfTradingStore.cs` | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | 12097 |
| `src/Infrastructure/Seeding/DemoSeeder.cs` | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | 5082 |
| `src/Application/Ingestion/DealIngestionService.cs` | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` | 4535 |
| `src/Domain/Shadow/ShadowCopyEngine.cs` | `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` | 3249 |
| `src/Domain/Scoring/BaselineScorer.cs` | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | 8143 |
| `src/Domain/Risk/RiskEngine.cs` | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | 8567 |
| `apps/web/src/pages/LiveCopyPage.tsx` | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` | 321 |
| `apps/web/src/pages/ShadowPortfolioPage.tsx` | `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276` | 628 |
| `apps/web/src/pages/OverviewPage.tsx` | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | 2078 |
| `apps/web/src/pages/RiskPage.tsx` | `FC4C5F05E1FF998FC1172E7F6C181821944066A40B577678B6DD9D0A24C1D8CF` | 1148 |
| `D:\Prop\.env` (gitignored; flag only quoted) | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` | 3408 |

Hashes match D32 / D69 / E002 / D48 / D97 on the same files. This is a **status pin**, not a new implementation.

---

## 5. Related bars (not substitutes)

| Bar | Score | Relation |
|---|---|---|
| E002 no-live-send | **CONFIRMED** | this file adds the **running** demo-shadow vs live-off status |
| D69 flag default | **`false`** | still unbound to the architecture env name |
| D32 worker | **`Disconnected`** | still no socket |
| D48 shadow rows | **6 via rebuild** | demo only; not §24 |
| D97 `CanPromoteToLive` | **`false`** | no LIVE state path |
| §69 first useful version | **0/12 accepted** (D41) | shadow demo does not flip item 11 |
| §68 go-live | **0/19** | keep flag false |
| §70 live FIX | **0/14** | Logon not proven (C43) |

---

## 6. Anti-greenwash

| Claim someone might write | Measured |
|---|---|
| “Copy to cTrader live is OFF” | **True.** |
| “Therefore live copy is correctly flag-gated” | **False.** Gate is incomplete; safety is absence of a sender. |
| “Demo shadow only” | **True** as the **running mode**. |
| “Shadow is destination-QUOTE priced” | **False** on this host (seeded bid/ask, null instrument id). |
| “`shadowPnl=248.20` proves G14 sample” | **False.** Σ slippage. |
| “`CTRADER_FIX_ENABLED=true` means we are logged on” | **False.** Sessions are `Disconnected`. |
| “`mt5Healthy=true` means live Manager” | **False.** Fake connector (health endpoint says so). |
| “PUT LiveCopyEnabled turns on send” | **False.** Unmapped controller; Redis string only. |

---

## 7. Sign-off

```text
[x] Live API realCopyEnabled = false (overview + risk + settings + FIX executionEnabled)
[x] Live API live = 0, liveCandidates = 0, shadow = 2, destinationRealPnl = 0
[x] REAL_COPY_EXECUTION_ENABLED / RealCopyExecutionEnabled default false
[x] No 35=D / NewOrderSingle sender
[x] PersistDemoShadowAsync is SHADOW-only, in-process SimulateEntry
[x] Product source unmodified
[ ] Wired flag gate (env name == worker key == POCO) — NOT IMPLEMENTED
[ ] §24 shadow book / live QUOTE fills — NOT IMPLEMENTED
[ ] §68 19/19 / §70 14/14 — still FAIL
```

**Current operating mode:** copy to cTrader live is **OFF**. The only copy activity is **demo shadow**.

---

## 8. Sources

- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`
- `D:\Prop\apps\web\src\pages\ShadowPortfolioPage.tsx`
- Live `GET http://127.0.0.1:5000/api/overview` (2026-08-18T08:19:31Z)
- `D:\Prop\reports\swarm\20260818\E002_no_live_send.md`
- `D:\Prop\reports\swarm\20260818\D48_shadow_rows.md`
- `D:\Prop\reports\swarm\20260818\D69_flag.md`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`

---

*End of E016. Product source was not modified. Live copy remains OFF. Demo shadow only.*
