# R030 — Official cTrader FIX headers: `SenderSubID=QUOTE/TRADE`, `TargetCompID=cServer`, SSL `5211`/`5212`

| Field | Value |
|---|---|
| Agent | R030 (official FIX header pin only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:56:36+05:30 (hashes / env names) / 2026-08-18T08:26:34Z (`.env` LastWriteUtc) |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` / India Standard Time |
| Artifact | `D:\Prop\reports\swarm\20260818\R030_fix_headers.md` |
| Assigned | cTrader official: **SenderSubID=`QUOTE`/`TRADE`**, **TargetCompID=`cServer`**, **SSL 5211/5212**. Write this file. Password was **not** provided as a real secret. Do **not** invent one. Do **not** modify product source. |
| Product source modified | **No.** This report is the only product-adjacent write. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` / user-secrets edited | **No.** |
| Secret values printed | **None.** Password slots classified by token / length only. |
| Live `35=A` / `35=D` sent this pass | **No.** |
| HEAD at measure | `18964024409c3d8764d38feca6d64fa6e831e175` (`Add remaining audit artifacts`) |
| Binding law | Architecture v2 **§25** (ports / CompIDs), **§26** (do not infer tags from form labels; never silent `cServer`→`CSERVER`), **§41** / **§55** / **§56** (`CTRADER_FIX_PASSWORD=<SECRET>`). A25 §3; A31 (official overview); A32 (RoE extract). |
| Official pages (re-fetched 2026-08-18) | https://help.ctrader.com/fix/getting-credentials/ · https://help.ctrader.com/fix/img/getting-fix-api-0.png · https://help.ctrader.com/fix/specification/ · https://help.ctrader.com/fix/sending-and-receiving-messages/ |
| Siblings (do not treat as this snapshot) | A25, A31, A32, B27, C09, C21, D26 (`cServer` recensus; **HEAD now `cServer`** — D26 “HEAD=`CSERVER`” is **stale**), E037 (host / empty options password), R001 (env hunt) |
| Method | Re-fetch official Help + credentials screenshot. Full read of `CTraderFixOptions.cs`, `CTraderFixSession.cs`, `CTraderFixLogonHostedService.cs`, `DemoSeeder` FIX rows, `FixSimulationHarness`, `docs/ctrader-fix.md`, architecture §25–§26 / §56. Classify gitignored `.env` `CTRADER_FIX_*` keys without copying secret values. Process / User / Machine env **names** only. SHA-256 of measured files. Did **not** open TLS. Did **not** send Logon. Did **not** invent a password. |

This is a **read-only official-header pin**. It does not rewrite options, bind `IOptions<>`, commit the untracked session files, or authorize live copy.

**Honesty rule:** the cTrader **credentials form** labels the session qualifier `SenderSubID`. The current **Rules of Engagement** put that qualifier on **`TargetSubID` (57)**. Those are **not the same FIX tag**. A hostname + port + CompID string is **not** a proven Logon. A 10-character `.env` slot is **not** treated as a real secret on this assignment.

---

## 0. Verdict (binding)

**Official cTrader credentials form (Help screenshot, 2026-08-18):**

| Form field | Price Connection | Trade Connection |
|---|---|---|
| `SenderSubID` | **`QUOTE`** | **`TRADE`** |
| `TargetCompID` | **`cServer`** | **`cServer`** |
| Port | **`5211` (SSL)**, `5201` (plain) | **`5212` (SSL)**, `5202` (plain) |

**Official RoE standard header (client → cTrader) does not collapse those labels onto one tag:**

| Tag | Name | Official RoE value / rule |
|---|---|---|
| 56 | `TargetCompID` | Table text: **`CSERVER`**. Form + sample prose: **`cServer`**. Do **not** silently fold case. |
| 57 | `TargetSubID` | **Required** session qualifier: **`QUOTE`** or **`TRADE`**. |
| 50 | `SenderSubID` | Optional originator. **Must be `QUOTE` if `TargetSubID=QUOTE`.** TRADE examples use `any_string`. |

**Production transport default is TLS:** QUOTE **5211**, TRADE **5212**. Plain 5201/5202 must not be the production default.

**Password:** this assignment states it was **not provided as a real secret**. This agent **did not invent one**. Process / User / Machine `CTRADER_FIX_PASSWORD` = **ABSENT**. `CTraderFixOptions.Password` = `""`. User-secrets root = **absent**. Live QUOTE/TRADE Logon remains **NOT PROVEN**. Live `NewOrderSingle` stays **off**.

One-liner:

```text
FORM:  SenderSubID=QUOTE|TRADE   TargetCompID=cServer   SSL 5211|5212
RoE:   tag 57 = QUOTE|TRADE (qualifier)   tag 50 = QUOTE when 57=QUOTE
WIRE:  49=issued CompID  56=cServer (issued case)  57=qualifier  50=per RoE
PASSWORD: NOT A REAL SECRET ON THIS ASSIGNMENT — do not invent — do not Logon
```

---

## 1. Official credentials form (source of the assigned sentence)

Page: https://help.ctrader.com/fix/getting-credentials/

Quoted:

> “There are 2 types of connection, price connection and trade connection, and each type has its own separate set of credentials. Trading operations requests cannot be sent through the price connection's credentials and vice versa.”

Official screenshot on that page (`https://help.ctrader.com/fix/img/getting-fix-api-0.png`, re-fetched 2026-08-18, 49986 bytes) prints **exactly** these labels (example account `4791386`, **not** Pepperstone `1369850`):

### Price Connection

```text
Host name: eqx-01.p.c-trader.com
Port: 5211 (SSL), 5201 (Plain text)
Password: **** (a/c 4791386 password)
SenderCompID: ctrader.4791386
TargetCompID: cServer
SenderSubID: QUOTE
```

### Trade Connection

```text
Host name: 01.p.c-trader.com          ← screenshot wrap of the same host family
Port: 5212 (SSL), 5202 (Plain text)
Password: **** (a/c 4791386 password)
SenderCompID: ctrader.4791386
TargetCompID: cServer
SenderSubID: TRADE
```

That is the assigned official sentence:

```text
SenderSubID = QUOTE / TRADE
TargetCompID = cServer
SSL = 5211 / 5212
```

The screenshot does **not** print `TargetSubID`. Hosts on the screenshot are **examples**. RoE Connectivity does **not** publish a global hostname. This lab’s issued host remains `live-us-eqx-01.p.c-trader.com` (architecture §25; `CTraderFixOptions.Host`; E037). Do not replace it with `fix.ctrader.com` or the screenshot demo host.

---

## 2. Official RoE header (do not confuse with the form)

Source: https://help.ctrader.com/fix/specification/ — Standard header (re-fetched 2026-08-18).

Quoted comments:

| Tag | Field | Required | Official comment (quoted) |
|---|---|---|---|
| 8 | `BeginString` | Yes | `FIX.4.4` — first field |
| 9 | `BodyLength` | Yes | second field |
| 35 | `MsgType` | Yes | third field |
| 49 | `SenderCompID` | Yes | `<Environment>.<BrokerUID>.<Trader Login>` |
| 56 | `TargetCompID` | Yes | “A message target. The valid value is `CSERVER`.” |
| 57 | `TargetSubID` | Yes | “An additional session qualifier. Possible values are `QUOTE` and `TRADE`.” |
| 50 | `SenderSubID` | No | “The assigned value used to identify a specific message originator. **Must be set to `QUOTE` if `TargetSubID=QUOTE`.**” |
| 34 | `MsgSeqNum` | Yes | per session |
| 52 | `SendingTime` | Yes | UTC |

Official TRADE Logon **request** example (same page):

```text
8=FIX.4.4|9=126|35=A|49=live.theBroker.12345|56=CSERVER|34=1|52=20170117-08:03:04|57=TRADE|50=any_string|98=0|108=30|141=Y|553=12345|554=passw0rd!|10=131|
```

Official TRADE Logon **success** (CompIDs **and** SubIDs swap):

```text
8=FIX.4.4|9=106|35=A|34=1|49=CSERVER|50=TRADE|52=20170117-08:03:04.509|56=live.theBroker.12345|57=any_string|98=0|108=30|141=Y|10=066|
```

Official QUOTE market-data request originator: `50=QUOTE` (tag 57 omitted on that older sample; later reject samples send both).

Official send/receive article (dated **2017-02-03**, RoE v2.9.1) `ConstructHeader`:

```text
56 = _targetCompID          // comment: Valid value is "CSERVER"
57 = qualifier.ToString()   // comment: Possible values are: "QUOTE", "TRADE"
50 = _senderSubID           // comment: Assigned value used to identify specific message originator
```

Same article’s constructor list says `TargetCompID` is “usually it is **cServer**” and (incorrectly, dated) “`SenderSubID` – it is the second part of SenderCompID”. Prefer **current RoE** over that 2017 comment.

Official current Spotware C# sample (linked from that page) uses **TLS on 5211/5212**:

```csharp
private int _pricePort = 5211;
private int _tradePort = 5212;
_priceStreamSSL.AuthenticateAsClient(_host);
_tradeStreamSSL.AuthenticateAsClient(_host);
```

---

## 3. The §26 trap (why this report exists)

Architecture §26 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 1075–1104):

> Do not blindly infer FIX tag placement from the human-readable credential form.
>
> The provided connection details label the session qualifier as:
>
> `SenderSubID = QUOTE / TRADE`

Required implementation behaviour (quoted / restated):

1. Preserve the **exact** broker-issued credentials (including case).
2. Make **both** `SenderSubID` and `TargetSubID` configurable.
3. Follow the **current** official RoE.
4. **Never silently** change `cServer` → `CSERVER` unless the issued configuration/spec requires it.
5. Prove Logon for **both** sessions in diagnostics before enabling execution.
6. Do not hardcode assumptions from an old sample.

### 3.1 Correct client → cServer mapping for **this** lab

Pepperstone issued CompID / account (non-secret identifiers, architecture §25):

| Role | Value |
|---|---|
| Host | `live-us-eqx-01.p.c-trader.com` |
| `SenderCompID` (49) | `live.pepperstone.1369850` |
| `Username` (553) | numeric **`1369850`** — **not** the dotted CompID |
| `TargetCompID` (56) | issued form **`cServer`** (default). `CSERVER` only as an **explicit, logged** operator override |
| QUOTE `TargetSubID` (57) | `QUOTE` |
| QUOTE `SenderSubID` (50) | `QUOTE` (RoE: mandatory when 57=`QUOTE`; also the form label) |
| TRADE `TargetSubID` (57) | `TRADE` |
| TRADE `SenderSubID` (50) | form label `TRADE` **or** a stable configured originator string (RoE examples: `any_string`). Configurable; do not invent a second semantic |
| QUOTE SSL port | **5211** |
| TRADE SSL port | **5212** |
| `EncryptMethod` (98) | `0` (transport TLS only) |
| `HeartBtInt` (108) | `30` default |
| `ResetSeqNumFlag` (141) | `Y` on establish |
| Password (554) | secret store only — **not provided as a real secret this assignment** |

### 3.2 Illegal mappings

| Mapping | Why it fails |
|---|---|
| Put form `SenderSubID=QUOTE/TRADE` on tag **50 only** and omit tag **57** | RoE: 57 is **required**. Likely no Logon / silent drop (FAQ: missing tags → no response). |
| Send `50=TRADE` as if it were the TRADE **qualifier** and leave `57` empty | Qualifier is 57, not 50. |
| Send a free-form `50` on QUOTE while `57=QUOTE` | RoE: 50 **must** be `QUOTE`. |
| Fold `cServer` → `CSERVER` in code | §26 item 4. Form + architecture env sample are camel `cServer`. |
| Put `live.pepperstone.1369850` in tag **553** | RoE: 553 is the **numeric** login. |
| Use plaintext **5201/5202** in production | Official form lists them; production default is SSL 5211/5212. |
| Treat inbound 50/57 with client meaning | Server **swaps** Comp/Sub IDs. Inbound session qualifier is **tag 50**. |

---

## 4. Password — not a real secret on this assignment

Instruction (binding for this agent): **password was not provided as a real secret. Do not invent one.**

| Slot | Class | Notes |
|---|---|---|
| Process `CTRADER_FIX_PASSWORD` | **ABSENT** | Not set |
| User `CTRADER_FIX_PASSWORD` | **ABSENT** | |
| Machine `CTRADER_FIX_PASSWORD` | **ABSENT** | |
| `%APPDATA%\Microsoft\UserSecrets` | **ABSENT** | directory does not exist |
| `CTraderFixOptions.Password` | **EMPTY** (`""`) | compiled default; comment “Must never be logged.” |
| Architecture / docs sample | **PLACEHOLDER** | `CTRADER_FIX_PASSWORD=<SECRET>` |
| `apps/*/appsettings*.json` | **NO_KEY** | logging stubs only; API JSON has no FIX password |
| `FixSessionState` / dashboard DTO | **no column** | password never on the wire to the UI |
| `D:\Prop\.env` `CTRADER_FIX_PASSWORD` | **slot exists, length 10** | gitignored. **This assignment does not treat that string as a real secret.** Value **not copied**. Not used. Not invented. |

`.env` (ignored) SHA-256 `556ACAA9EFF6106D601E4BCC556811C149A5140477B974AF77A3F9B5D77396FF`, 3422 bytes. `EnvFile.Load` exists under `src/Mt5/Env/EnvFile.cs` and is **never called** from `apps/api/Program.cs` or `apps/fix-worker/Program.cs`. Flat `CTRADER_FIX_*` keys therefore do **not** enter `IConfiguration` unless the process environment is populated elsewhere.

`CTraderFixLogonHostedService` (untracked; see §6) skips Logon when password is missing or contains `<SECRET>`:

```29:34:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var password = _config["CTRADER_FIX_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password) || password.Contains("<SECRET>", StringComparison.Ordinal))
        {
            _log.LogWarning("cTrader FIX password missing. QUOTE/TRADE logon skipped.");
            return;
        }
```

This report does **not** populate that key. Live Logon is **NOT PROVEN** (C43 still holds).

Official Logon examples print a sample `554=passw0rd!`. That is a Help-page dummy, **not** account `1369850`. Do not copy it.

---

## 5. Product measured vs official form / RoE

`CTraderFixOptions.cs` SHA-256 `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` (2344 B). HEAD blob now matches worktree (`cServer` committed — D26 “HEAD=`CSERVER`” is stale).

| Surface | QUOTE | TRADE | vs official form | vs RoE |
|---|---|---|---|---|
| `UseSsl` | `true` | `true` | **PASS** | transport-level only (`98=0`) |
| `SslPort` | **5211** | **5212** | **PASS** | RoE has no port table; screenshot + official sample **PASS** |
| `PlainPort` | 5201 | 5202 | listed on form; not production default | — |
| `TargetCompId` | **`cServer`** | **`cServer`** | **PASS** (form) | RoE table says `CSERVER` — **do not fold** |
| `TargetSubId` | **`QUOTE`** | **`TRADE`** | form does not name tag 57 | **PASS** (RoE qualifier) |
| `SenderSubId` options default | **`""`** | **`""`** | **FAIL** vs form `QUOTE`/`TRADE` | **FAIL** on QUOTE (RoE: must be `QUOTE` when 57=`QUOTE`) |
| `SenderCompId` | `live.pepperstone.1369850` | same | issued CompID | RoE format **PASS** |
| `AccountId` / `Password` | `""` / `""` | | empty slots | 553/554 unbound |
| `Host` | `live-us-eqx-01.p.c-trader.com` | | issued (not screenshot demo) | RoE publishes **no** host |
| `RealCopyExecutionEnabled` | `false` | | correct floor | — |

Gitignored `.env` **non-secret** header keys (values are identifiers, not passwords):

| Key | Value |
|---|---|
| `CTRADER_FIX_QUOTE_TARGET_COMP_ID` | `cServer` |
| `CTRADER_FIX_TRADE_TARGET_COMP_ID` | `cServer` |
| `CTRADER_FIX_QUOTE_SENDER_SUB_ID` | `QUOTE` |
| `CTRADER_FIX_TRADE_SENDER_SUB_ID` | `TRADE` |
| `CTRADER_FIX_QUOTE_TARGET_SUB_ID` | `QUOTE` |
| `CTRADER_FIX_TRADE_TARGET_SUB_ID` | `TRADE` |
| `CTRADER_FIX_QUOTE_SSL_PORT` | `5211` |
| `CTRADER_FIX_TRADE_SSL_PORT` | `5212` |
| `CTRADER_FIX_USE_SSL` | `true` |
| `REAL_COPY_EXECUTION_ENABLED` | `false` |

Those `.env` SubID lines **match the official form and satisfy RoE** (QUOTE 50=`QUOTE`; TRADE 50=`TRADE` is a legal originator string). They are **unbound**: no `Configure<CTraderFixOptions>`, no `IOptions<>`, no dotenv load from the hosts.

`DemoSeeder` (SHA-256 `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20`):

| Row | Port | `TargetCompId` | `TargetSubId` | `SenderSubId` | Status |
|---|---:|---|---|---|---|
| QUOTE | 5211 | `cServer` | `QUOTE` | **`null`** | `Disconnected` |
| TRADE | 5212 | `cServer` | `TRADE` | **unset → `null`** | `Disconnected` |

Seeder is **not** a Logon. Integration test asserts `TargetCompId` distinct values equal `"cServer"` (`tests/Integration/SeedingAndStoreTests.cs`).

`FixSimulationHarness` defaults `targetCompId = "cServer"` and emits tag 56 as `cServer` (worktree + HEAD). Simulator only; worker does not call it.

`docs/architecture.md`: “TargetCompID = `cServer` (case preserved)”.

`docs/ctrader-fix.md` still writes tag 50 as `<BROKER_ISSUED_VALUE>` and tag 57 as `QUOTE`/`TRADE` — correct **RoE** split; the **form** label for the qualifier is `SenderSubID`. Both documents are consistent with §26 if SubIDs stay independently configurable.

---

## 6. Untracked session builder (measured; not authorized by this task)

Present on disk at measure, **not in HEAD**:

| Path | Git | SHA-256 | Bytes |
|---|---|---|---:|
| `src/Fix.CTrader/Sessions/CTraderFixSession.cs` | `??` | `A2AD3BA5EB0258644FDDD8C66409F62FC64552266B0C0B10FB872B502874699E` | 4789 |
| `src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs` | `??` | `18B30632721A1D3AA647B6BE05FA376B0E53FEA81E189F29E05BD1F69B20DC5E` | 3929 |
| `src/Infrastructure/DependencyInjection.cs` | ` M` | `2C736852E23353C51698618615629984265910D415B74F18FDBDF6E96637CD2B` | 2264 |

This agent **did not** create or edit those files.

Header bytes `BuildLogon` would emit (if it ran):

```text
8=FIX.4.4 | 9=… | 35=A | 34=1 | 49={SenderCompID} | 56={TargetCompID} | 50={SenderSubID} | 57={TargetSubID} | 52=UTC | 98=0 | 108=30 | 141=Y | 553={username} | 554={password} | 10=…
```

Hosted-service defaults if env keys missing:

| Arg | QUOTE | TRADE |
|---|---|---|
| SSL port (hardcoded) | **5211** | **5212** |
| `target` | `CTRADER_FIX_QUOTE_TARGET_COMP_ID` ?? **`cServer`** | same QUOTE key reused |
| `senderSub` | ?? **`QUOTE`** | ?? **`TRADE`** |
| `targetSub` | ?? **`QUOTE`** | ?? **`TRADE`** |

That default pair **matches the official form** (`SenderSubID=QUOTE/TRADE`, `TargetCompID=cServer`, SSL 5211/5212) **and** fills RoE tag 57.

Defects (header / safety — do **not** “fix” from this report):

1. **Raw `TcpClient` + `SslStream`.** Architecture §5 / A25: prefer QuickFIX/n. Official Help sample is explicitly **not** a production engine. `Fix.CTrader.csproj` still has **zero** `QuickFIXn.*` references (C19).
2. **Tag 553 username is the dotted CompID.** Call site passes `sender` (`live.pepperstone.1369850`) as `username`. RoE: `553` = numeric `1369850`. That Logon would be **invalid** even with a real password.
3. **`Infrastructure` does not reference `Fix.CTrader`.** `AddHostedService<CTraderFixLogonHostedService>()` has **no** `using` and no project reference. Current DI graph **cannot resolve** that type.
4. **Accept-any server certificate** (`(_, _, _, _) => true`) and **no lease / fencing**.
5. Persist path writes `LoggedOn` if `35=A` — would re-open the D22 forged-health class if it ever compiled and connected.
6. Password gate reads `IConfiguration["CTRADER_FIX_PASSWORD"]` only. Process env is absent; dotenv is not loaded. Gate currently **skips**. Do not feed it a guessed secret.

This report does **not** compile, run, or delete those files.

---

## 7. Dead API JSON (do not bind as-is)

`D:\Prop\apps\api\appsettings.json` section `CTraderFix` (unbound; no `GetSection("CTraderFix")` consumer):

```json
"QuoteHost": "fix.ctrader.com",
"QuotePort": 5201,
"TradeHost": "fix.ctrader.com",
"TradePort": 5202,
"SenderCompId": "",
"TargetCompId": "CSERVER"
```

| Field | JSON | Official form / this lab |
|---|---|---|
| Host | `fix.ctrader.com` | **unofficial.** Issued host is `live-us-eqx-01.p.c-trader.com`. E037. |
| Ports | **5201 / 5202** | form **plain** ports. Production SSL is **5211 / 5212**. |
| `TargetCompId` | **`CSERVER`** | form + §26 default **`cServer`**. |
| `SenderCompId` | empty | issued `live.pepperstone.1369850`. |
| SubIDs | **missing** | form `QUOTE`/`TRADE`; RoE tags 50 and 57. |

Do **not** bind this section onto `CTraderFixOptions` without renaming and correcting case / ports / host.

`apps/fix-worker/appsettings.json` is logging only.

---

## 8. What this pin is **not**

| Claim | Status |
|---|---|
| Official form `SenderSubID=QUOTE/TRADE` | **True** (screenshot) |
| Official form `TargetCompID=cServer` | **True** (screenshot) |
| Official SSL ports 5211 / 5212 | **True** (screenshot + official C# sample) |
| Official RoE qualifier is tag **50** | **False** — qualifier is tag **57** |
| Official RoE `TargetCompID` spelling is only `cServer` | **False** — table says `CSERVER`; form says `cServer` |
| Live QUOTE/TRADE Logon proven | **False** |
| Password supplied as a real secret | **False** (assignment + process/user-secrets empty) |
| Product source edited by R030 | **False** |
| QuickFIX/n wired | **False** (C19) |
| `NewOrderSingle` allowed | **False** (`REAL_COPY_EXECUTION_ENABLED=false`; no send path that is ready) |

---

## 9. File census (this check)

| Path | Bytes | SHA-256 | Role |
|---|---:|---|---|
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | 2344 | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | compiled defaults |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | 8970 | `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616` | pipe simulator |
| `src/Infrastructure/Seeding/DemoSeeder.cs` | 5082 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | seed headers |
| `docs/ctrader-fix.md` | 3195 | `52E80263C4D1672121842F17A382FFC691CB9350A1B26BF53EE8252C5ABD0C77` | lab FIX note |
| `docs/architecture.md` | 1379 | `A5FB4FEFD9EFECDDCECDD884D1F1FA2042658AB06989F2155BF35B67BBFE5B3D` | `cServer` case pin |
| `apps/api/appsettings.json` | — | (read; unbound `CTraderFix`) | dead JSON |
| `D:\Prop\.env` | 3422 | `556ACAA9EFF6106D601E4BCC556811C149A5140477B974AF77A3F9B5D77396FF` | ignored; SubIDs only classified |

`git grep CSERVER -- src` on this worktree: **zero** hits. `cServer` hits: options (2), harness (5), seeder (2).

---

## 10. Authorized later work (do **not** apply in R030)

1. Keep compiled / env default **`TargetCompID=cServer`**. `CSERVER` is an explicit override only.
2. Default QUOTE `SenderSubID` to **`QUOTE`**. Keep TRADE `SenderSubID` configurable (form `TRADE` is acceptable; do not invent).
3. Always send **tag 57** = session qualifier (`QUOTE`/`TRADE`).
4. Bind flat `CTRADER_FIX_*` **verbatim** (no case fold).
5. Wire official **QuickFIXn.Core + QuickFIXn.FIX44 1.14.1** (A35). Do not ship the untracked raw `TcpClient` as the engine.
6. `553` = numeric account; `49` = dotted CompID.
7. Diagnostic Logon only after a **real** operator secret is supplied out of band. **Do not invent** `CTRADER_FIX_PASSWORD`.
8. Persist a §3.6 header-mapping evidence record (exact 49/56/50/57 sent) before any application message.
9. Leave `REAL_COPY_EXECUTION_ENABLED=false`.

---

## 11. Sources

- https://help.ctrader.com/fix/getting-credentials/
- https://help.ctrader.com/fix/img/getting-fix-api-0.png (official form: `SenderSubID` `QUOTE`/`TRADE`, `TargetCompID` `cServer`, SSL `5211`/`5212`)
- https://help.ctrader.com/fix/specification/ (RoE standard header, Logon examples)
- https://help.ctrader.com/fix/sending-and-receiving-messages/ (2017 sample; two ports; `56`/`57`/`50` construction)
- https://github.com/spotware/FIX-API-Sample (official TLS 5211/5212)
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§25–26, §56
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- Prior: `A25_fix_session_spec.md`, `A31_ctrader_fix_overview.md`, `A32_ctrader_fix_specification.md`, `D26_cserver.md`, `E037_fixhost.md`

---

*End of R030. Product source was not modified. No password was invented. No live FIX message was sent.*
