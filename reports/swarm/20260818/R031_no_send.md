# R031 — `REAL_COPY` stays **false** even with FIX sessions configured

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\R031_no_send.md` |
| Agent | R031 (REAL_COPY vs configured FIX sessions — no-send pin) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:57:03+05:30 / 2026-08-18T08:27:03Z |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` |
| HEAD | `18964024409c3d8764d38feca6d64fa6e831e175` (`Add remaining audit artifacts`) |
| Assigned | Confirm `REAL_COPY` stays **false** even with FIX sessions configured. Write this file. **Do not modify product source.** |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** |
| Secret values printed | **None.** `.env` password slots are classified only (placeholder `<SECRET>` already in file). |
| Live host this pass | `http://127.0.0.1:5000` (`GET /health` **200**) |
| Binding law | Architecture **§41** / **§56** (sessions may be on; `REAL_COPY_EXECUTION_ENABLED=false`); A25 §6.3; A49 (flags are **independent**); A101 item 12 / “TRADE up ≠ license to send” |
| Siblings (do not collapse) | D69 (POCO default), E002 (no sender), E034 (`35=D` = 0), E038 (settings GET literal false), E008 (TRADE `Disconnected`), D32 (worker stamp), C43 (Logon **NOT PROVEN**), A49 (design independence) |
| Method | Re-read `CTraderFixOptions`, seeder QUOTE+TRADE rows, `CTraderFix` appsettings, gitignored `.env` session block, fix-worker `GetValue`, API `MapGet("/api/settings")`, dashboard literals, `FixSessionState` entity, DI (no `IOptions<CTraderFixOptions>`), `EnvFile` callers, RiskEngine caller bit. SHA-256 via `Get-FileHash`. Live HTTP: `/health`, `/api/settings`, `/api/overview`, `/api/fix/sessions`, `/api/risk`. Product-tree hunt for `RealCopyExecutionEnabled=true` / `REAL_COPY_EXECUTION_ENABLED=true`. **No product edit. No `35=D` attempted. No flag flipped.** |

**Honesty rule:** “FIX sessions configured” means **identity + ports + session-enable flags + EF rows exist**. It does **not** mean TLS Logon (C43: **NOT PROVEN**). A hardcoded JSON `false` is a **display floor**, not a coded choke. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-while-LoggedOn gate. Session-on **must not** imply send-on. Do not tick §70.12 from this file.

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

---

## 0. Verdict (binding)

**CONFIRMED: `REAL_COPY` stays `false` when FIX sessions are configured.**

The four §41 bits are **independent**. This tree’s session surface is **on / present**; the send license is **off** on every measured floor. No product assignment flips `RealCopyExecutionEnabled` (or `REAL_COPY_EXECUTION_ENABLED`) to `true` because QUOTE/TRADE exist.

| Assigned claim | Measured | Class |
|---|---|---|
| FIX sessions are **configured** (host/ports/CompIDs + QUOTE+TRADE rows + session flags default **true**) | **Yes** | session identity `EXISTS`; live socket **ABSENT** |
| `REAL_COPY` / `RealCopyExecutionEnabled` stays **false** on that same configuration | **Yes** | default `EXISTS_AND_GOOD` vs §41 |
| Any product path sets send=true **because** sessions are configured | **No** — **0** `= true` writers | independence `EXISTS_AND_GOOD` (by non-coupling) |
| Live dashboard/API infer send from session rows | **No** — `executionEnabled` / `realCopyEnabled` are **literals** `false` | display floor; **not** a binder |
| Configured sessions + `REAL_COPY=false` can emit `35=D` | **No** | **`SAFE_BY_ABSENCE`** (E002 / E034: 0 builders) |
| Implemented conjunction “session up AND flag false → refuse send” | **No** | `GATE_INCOMPLETE` (no send function to refuse; TRADE never LoggedOn) |

One-line:

```text
CTraderFixOptions: QuoteEnabled=true, TradeSessionEnabled=true, RealCopyExecutionEnabled=false
AND .env: CTRADER_FIX_*_ENABLED=true + REAL_COPY_EXECUTION_ENABLED=false
AND live GET /api/fix/sessions → 2 rows (live host:5211/5212) executionEnabled=false
AND live GET /api/settings → featureFlags.REAL_COPY_EXECUTION_ENABLED=false
AND 0 product writers set REAL_COPY true.
```

Do **not** treat this file as A101 “Submit=0 even if LoggedOn” PASS. TRADE is `Disconnected`. Do **not** enable the flag to “match” configured sessions.

---

## 1. What “FIX sessions configured” means on this tree

Architecture §41 **explicitly** allows this matrix (connect / quote / reconcile **without** new real orders):

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

A49: *“Turning TRADE on is not a license to send 35=D.”*

### 1.1 Same POCO — three independent bools

```26:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    public bool UseSsl { get; set; } = true;

    public bool QuoteEnabled { get; set; } = true;

    public bool TradeSessionEnabled { get; set; } = true;

    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

| Property | Compile default | Role |
|---|---|---|
| `QuoteEnabled` | **`true`** | may start QUOTE |
| `TradeSessionEnabled` | **`true`** | may start TRADE (read/reconcile) |
| **`RealCopyExecutionEnabled`** | **`false`** | **only** new-exposure license |

`new CTraderFixOptions()` yields **sessions on, send off**. No constructor, factory, or binder sets the third bool from the first two.

Identity defaults on the **same** type (configured venue, not a send license):

| Field | Default |
|---|---|
| `Host` | `live-us-eqx-01.p.c-trader.com` |
| `Quote.SslPort` / `PlainPort` | `5211` / `5201` |
| `Trade.SslPort` / `PlainPort` | `5212` / `5202` |
| `Quote.SenderCompId` / `Trade.SenderCompId` | `live.pepperstone.1369850` |
| `TargetCompId` | `cServer` |
| `Quote.TargetSubId` / `Trade.TargetSubId` | `QUOTE` / `TRADE` |
| `Password` | `""` (empty) |

`AddTraderIntelligence` does **not** `Configure<CTraderFixOptions>`. The POCO is unused by hosts. Defaults still prove the **intended** independence.

### 1.2 Gitignored `.env` — sessions configured, send off

`D:\Prop\.env` (gitignored; **3422** bytes; SHA-256 `556ACAA9EFF6106D601E4BCC556811C149A5140477B974AF77A3F9B5D77396FF`):

```text
CTRADER_FIX_HOST=live-us-eqx-01.p.c-trader.com
CTRADER_FIX_QUOTE_SSL_PORT=5211
CTRADER_FIX_TRADE_SSL_PORT=5212
CTRADER_FIX_QUOTE_SENDER_COMP_ID=live.pepperstone.1369850
CTRADER_FIX_TRADE_SENDER_COMP_ID=live.pepperstone.1369850
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

| Adjacent key | Class (this pass) |
|---|---|
| `CTRADER_FIX_PASSWORD` | **PLACEHOLDER** (literal `<SECRET>`) |
| `CTrader__RealCopyExecutionEnabled` | **NO_KEY** |
| Process / User env `REAL_COPY_EXECUTION_ENABLED` | **UNSET** |

`EnvFile.Load` has **zero callers**. Fix-worker / API do **not** ingest this file. The slot still documents operator intent: **sessions true, send false**.

### 1.3 API `CTraderFix` JSON — host/ports present, no send key

`apps/api/appsettings.json` SHA-256 `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20`:

```23:34:D:\Prop\apps\api\appsettings.json
  "CTraderFix": {
    "QuoteHost": "fix.ctrader.com",
    "QuotePort": 5201,
    "TradeHost": "fix.ctrader.com",
    "TradePort": 5202,
    "SenderCompId": "",
    "TargetCompId": "CSERVER",
    "HeartBeatInterval": 30,
    "ResetOnLogon": true,
    "FileStorePath": "./fixstore",
    "FileLogPath": "./fixlogs"
  },
```

- Session **identity** is present (dead unofficial host `fix.ctrader.com` — E037).
- **No** `RealCopyExecutionEnabled` / `QuoteEnabled` / `TradeSessionEnabled` keys.
- `FeatureFlags:LiveCopyEnabled` = **`false`** (different name; unmapped controller).
- `appsettings.Development.json` has **no** `CTrader*` / `FeatureFlags` keys.
- Live `MapGet("/api/settings")` **ignores** this block (literal dict — E038).

### 1.4 Seeded EF rows = configured sessions in the dashboard

`DemoSeeder` inserts **two** `FixSessionState` rows (live Pepperstone host, SSL ports, Comp/Sub IDs). Status is `Disconnected`. TRADE `LastError` says NewOrderSingle is off. **No** execution-enabled column exists on the entity.

```68:102:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.FixSessionStates.AddRange(
            new FixSessionState
            {
                Qualifier = FixSessionQualifier.Quote,
                Status = FixSessionStatus.Disconnected,
                Host = "live-us-eqx-01.p.c-trader.com",
                Port = 5211,
                SenderCompId = "live.pepperstone.1369850",
                TargetCompId = "cServer",
                TargetSubId = "QUOTE",
                LastError = "No live QUOTE socket. Demo seed only.",
            },
            new FixSessionState
            {
                Qualifier = FixSessionQualifier.Trade,
                Status = FixSessionStatus.Disconnected,
                Host = "live-us-eqx-01.p.c-trader.com",
                Port = 5212,
                SenderCompId = "live.pepperstone.1369850",
                TargetCompId = "cServer",
                TargetSubId = "TRADE",
                LastError = "No live TRADE socket. NewOrderSingle off.",
            });
```

`FixSessionState` fields: qualifier, status, host/port, Comp/Sub IDs, seq, timestamps, reconnects, lastError, owner bits. **No** `ExecutionEnabled` / `RealCopy*` property.

### 1.5 Worker + launch + compose

| Surface | Session config | REAL_COPY |
|---|---|---|
| `apps/fix-worker/appsettings*.json` | logging only | key **absent** → `GetValue(..., false)` |
| `apps/fix-worker` `launchSettings.json` | `DOTNET_ENVIRONMENT=Development` only | **absent** |
| `docker-compose.yml` | api + postgres + redis; **0** `CTRADER_*` / `REAL_COPY*` | **absent** |
| `apps/api` `launchSettings.json` | `ASPNETCORE_ENVIRONMENT=Development` only | **absent** |

Worker loop **requires** the two session rows (updates them) and **does not** flip send:

```21:46:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
        // ... stamp QUOTE + TRADE Status = Disconnected ...
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

- Session rows present → still `Disconnected`.
- Flag is a **different** key (`CTrader:RealCopyExecutionEnabled`), default **false**.
- Architecture flat name `REAL_COPY_EXECUTION_ENABLED` is **not** this key.
- If `real` were true, the worker **only logs**. No socket. No `35=D`.

---

## 2. Live HTTP — sessions exist, send flags stay false

`GET http://127.0.0.1:5000/health` → **200** `{"status":"ok","utc":"2026-08-18T08:27:03.7934597+00:00"}`.

### 2.1 Settings (architecture name)

`GET /api/settings` → **200**:

```json
{
  "riskLimits": { "maxQuoteAgeSeconds": 3, "maxSignalAgeSeconds": 15 },
  "featureFlags": { "REAL_COPY_EXECUTION_ENABLED": false },
  "brokerConfigs": [
    { "id": "ACHIEVER", "name": "Achiever", "enabled": true },
    { "id": "STARWAVEFX", "name": "StarwaveFX", "enabled": true }
  ]
}
```

Source is a **C# literal** (`Program.cs` L45). It does **not** read session rows, `.env`, or the POCO.

### 2.2 FIX session cards (configured rows)

`GET /api/fix/sessions` → **200**, **two** objects:

| Field | QUOTE | TRADE |
|---|---|---|
| `host` | `live-us-eqx-01.p.c-trader.com` | same |
| `port` | **5211** | **5212** |
| `status` | `Disconnected` | `Disconnected` |
| `connected` / `loggedOn` | `false` / `false` | `false` / `false` |
| `lastError` | `No live QUOTE socket. Demo seed only.` | `No live TRADE socket. NewOrderSingle off.` |
| bid/ask (seeded snapshot) | `2399.45` / `2399.85` | same invented quote |
| **`executionEnabled`** | **`false`** | **`false`** |

Sessions are **configured** (identity + ports + rows). Send bit is **false** on both cards.

`GetFixSessionsAsync` last ctor arg is the literal `false` — **not** `s.Status`, **not** `RealCopyExecutionEnabled`:

```166:183:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return sessions.Select(s => new FixSessionDto(
            s.Qualifier.ToString().ToUpperInvariant(),
            s.Host,
            s.Port,
            s.Status != FixSessionStatus.Disconnected && s.Status != FixSessionStatus.Error,
            s.Status is FixSessionStatus.LoggedOn or FixSessionStatus.ReadyForMarketData or FixSessionStatus.ReadyForExecution or FixSessionStatus.Reconciling,
            s.Status.ToString(),
            /* seq / quote fields */
            false)).ToList();
```

Health bits **do** follow session status. `ExecutionEnabled` **does not**. Even if a later change stamped `LoggedOn`, this DTO would still report `executionEnabled=false`.

### 2.3 Overview + risk (same process)

| Endpoint | HTTP | Flag | Value | Session health |
|---|---:|---|---|---|
| `GET /api/overview` | 200 | `realCopyEnabled` | **`false`** | `quoteHealthy=false`, `tradeHealthy=false` |
| `GET /api/risk` | 200 | `realCopyEnabled` | **`false`** | `killSwitch=None` |

Overview this pass: `shadow=2`, `live=0`, `liveCandidates=0`, `destinationRealPnl=0`. Last overview ctor arg is literal `false` (L42). Risk 7th arg is literal `false` (L196).

React `FixSessionsPage` prints `executionEnabled` from that GET. `LiveCopyPage` hardcodes “REAL_COPY_EXECUTION_ENABLED is false” and does not read session status.

---

## 3. Independence census (no coupling writer)

Product trees `src/`, `apps/`, `tests/` (`*.cs` / `*.json` / `*.ts` / `*.tsx`; exclude `bin` / `obj` / `node_modules`):

| Pattern | Hits | Meaning |
|---|---:|---|
| `RealCopyExecutionEnabled = true` | **0** | nobody assigns send-on |
| `REAL_COPY_EXECUTION_ENABLED=true` | **0** in product config | `.env` line is **false**; architecture markdown may show the *enable* example — not product |
| `LiveCopyEnabled: true` / `= true` | **0** | appsettings + dead controller stay **false** |
| `35=D` in product `*.cs` | **0** | E034 still holds |
| `QuoteEnabled = true` / `TradeSessionEnabled = true` | **2** (same POCO) | session defaults **on** |

`RealCopyExecutionEnabled` product C# sites — **two**, neither a session-derived assignment:

| File:line | Kind |
|---|---|
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs:35` | initializer **`= false`** |
| `apps/fix-worker/Worker.cs:21` | `GetValue("CTrader:RealCopyExecutionEnabled", false)` — log only |

`CTraderQuoteService` takes `CTraderFixOptions` and reads **`Quote` + `MaxQuoteAgeMs` only**. It never reads `QuoteEnabled`, `TradeSessionEnabled`, or `RealCopyExecutionEnabled`. Tag lists are `35=y` / `35=V`. No TRADE client.

`RiskEngine` uses a **caller** bit `RealExecutionEnabled` (unit fixture `false`). Empty `if` when false; `AllowFixSend` ANDs the bit later. **Zero** workers read `AllowFixSend`. Session status is not an input to `Evaluate`.

`SettingsController` PUT `LiveCopyEnabled` would write Redis `settings:flags:live_copy`. Controller is **unmapped** (`AddControllers` / `MapControllers` absent). Redis multiplexer **not** registered. That name is **not** `REAL_COPY_EXECUTION_ENABLED`.

DI (`DependencyInjection.cs` SHA `2C736852E23353C51698618615629984265910D415B74F18FDBDF6E96637CD2B`): EF + fake MT5 + ingestion + scoring + dashboard. **No** FIX session start. **No** options bind. **No** execution worker.

---

## 4. File identity (this pass)

| Bytes | SHA-256 | Path |
|---:|---|---|
| 2344 | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` |
| 2093 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | `apps/fix-worker/Worker.cs` |
| 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | `apps/fix-worker/Program.cs` |
| 137 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | `apps/fix-worker/appsettings.json` (and `.Development`) |
| 4731 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | `apps/api/Program.cs` |
| 1254 | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | `apps/api/appsettings.json` |
| 3732 | `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` | `apps/api/Controllers/SettingsController.cs` |
| 5082 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | `src/Infrastructure/Seeding/DemoSeeder.cs` |
| 8708 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | `src/Infrastructure/Dashboard/EfDashboardQueries.cs` |
| 2264 | `2C736852E23353C51698618615629984265910D415B74F18FDBDF6E96637CD2B` | `src/Infrastructure/DependencyInjection.cs` |
| 3088 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | `src/Application/Dashboard/DashboardModels.cs` |
| 979 | `46C20D6A1BF5F84769DB483FD17A0EBEB8BDA8C1C56BBA2B8B30A59FCE44697E` | `src/Domain/Entities/FixSessionState.cs` |
| 8567 | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | `src/Domain/Risk/RiskEngine.cs` |
| 5453 | `7D2FDE1D33B47D619EA8BB0EC5F943BC21D8D97B46BEA269D70D46A20859B44A` | `src/Fix.CTrader/Services/CTraderQuoteService.cs` |
| 321 | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` | `apps/web/src/pages/LiveCopyPage.tsx` |
| 1312 | `EC93326688719E10D3ED5CB275D9BF1E7113C7F61EEA99803F42E1EA268BB886` | `apps/web/src/pages/FixSessionsPage.tsx` |
| 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | `docker-compose.yml` |
| 3422 | `556ACAA9EFF6106D601E4BCC556811C149A5140477B974AF77A3F9B5D77396FF` | `.env` (local, gitignored) |

Options / worker / API `Program` / dashboard hashes **match** D69 / E002 / E038. `.env` SHA **moved** vs D69 (`56C81786…`); `REAL_COPY` line is still `false`. DI SHA **moved** vs E038 (`EF0E0E46…`); still no `CTraderFixOptions` bind.

---

## 5. Classification roll-up

| Slice | Class |
|---|---|
| §41 independence (sessions on ≠ send on) | **`EXISTS_AND_GOOD`** as a **default matrix** |
| `RealCopyExecutionEnabled` C# default | **`false` (`EXISTS_AND_GOOD`)** |
| `.env` `CTRADER_FIX_*_ENABLED=true` + `REAL_COPY=false` | **matches §41**; **not loaded** by hosts |
| Seeded QUOTE+TRADE rows | **configured identity**; status **Disconnected** |
| Live `executionEnabled` / `realCopyEnabled` / settings flag | **`false`** (literals) |
| Product writer session→send | **MISSING** (correct) |
| `IOptions<CTraderFixOptions>` / env-name binder | **MISSING** |
| `GuardedNewOrderSingle` / `35=D` | **MISSING** → live send **`SAFE_BY_ABSENCE`** |
| Implemented refuse-on-LoggedOn-TRADE + flag false | **`GATE_INCOMPLETE`** |
| Live FIX Logon | **NOT PROVEN** (C43) |
| Product source edited by R031 | **No** |

---

## 6. What this file does **not** prove

- Live QUOTE/TRADE TLS Logon (`35=A`). Configured ≠ connected.
- A coded choke that would refuse a future `35=D` builder when TRADE is LoggedOn and the flag is false.
- That `CTraderFix` JSON (`fix.ctrader.com`) is the live session (dashboard uses seeder host).
- That `.env` `CTRADER_FIX_*` is consumed (`EnvFile` unused; worker key is nested `CTrader:*`).
- Phase 8 / §68 / §70 readiness. Those remain **0**.
- Safe to set `REAL_COPY_EXECUTION_ENABLED=true` because “sessions are already there.” **It is not.**

---

## 7. Assigned answers (do not paraphrase away)

1. **Does `REAL_COPY` stay false when FIX sessions are configured?**  
   **Yes.** POCO: `QuoteEnabled=true`, `TradeSessionEnabled=true`, `RealCopyExecutionEnabled=false`. `.env`: three `CTRADER_FIX_*_ENABLED=true` and `REAL_COPY_EXECUTION_ENABLED=false`. API `CTraderFix` hosts/ports exist without a send key. Seeder + live `GET /api/fix/sessions` return two configured rows with **`executionEnabled=false`**. Settings / overview / risk all report **`false`**. **Zero** product assignments set the send flag true from session presence.

2. **Does configuring (or seeding) FIX sessions enable live send?**  
   **No.** Worker still stamps `Disconnected`. Dashboard does not derive send from session status. There is still **no** `35=D` builder / QuickFIX initiator. **`SAFE_BY_ABSENCE`.**

**Do not enable `REAL_COPY_EXECUTION_ENABLED`. Do not add a sender in this task.** Product source was not modified.

*End of R031.*
