# A35 — QuickFIX/n packages for .NET 8, FIX 4.4 dictionary, SSL, cTrader customization

**Date:** 2026-08-18  
**Agent:** A35  
**Scope:** Research / pin only. No product source was modified.  
**Target TFM:** `net8.0`  
**Venue:** cTrader / cServer FIX 4.4 (initiator; two sessions: QUOTE + TRADE)

---

## Verdict (pin this)

| Item | Pin | Why |
|---|---|---|
| Engine | **QuickFIXn.Core 1.14.1** | Latest official engine. TFMs: `net8.0` + `net10.0`. Last release that still supports .NET 8. |
| FIX 4.4 messages | **QuickFIXn.FIX44 1.14.1** | Official FIX 4.4 generated types. Depends on Core `1.14.1`. Ships stock `DataDictionary/FIX44.xml`. |
| FIXT / FIX5 packages | **Do not add** | cTrader `BeginString` is `FIX.4.4`, not FIXT.1.1. |
| Deprecated name | **Do not add `QuickFIXn.FIX4.4`** | Legacy id (last 1.13.0). NuGet marks it deprecated; successor is `QuickFIXn.FIX44`. |
| Unofficial forks | **Do not add** | Official site: only `QuickFIXn.*` from QuickFIXEngine.org are authorized. |

**PackageReference (exact):**

```xml
<ItemGroup>
  <PackageReference Include="QuickFIXn.Core" Version="1.14.1" />
  <PackageReference Include="QuickFIXn.FIX44" Version="1.14.1" />
</ItemGroup>
```

**CLI:**

```text
dotnet add package QuickFIXn.Core --version 1.14.1
dotnet add package QuickFIXn.FIX44 --version 1.14.1
```

Keep the two versions **identical**. Mixing 1.13 message packages with 1.14 Core (or the reverse) is unsupported.

**nupkg SHA-256** (downloaded 2026-08-18 from `api.nuget.org`):

| Package | SHA-256 |
|---|---|
| `QuickFIXn.Core.1.14.1.nupkg` | `1A30DC9BEF15DEE380279AEAE44290A34A5D4A3581582C77A53704742F9244D2` |
| `QuickFIXn.FIX44.1.14.1.nupkg` | `C6E994609AD65C5068CC703B457113D459672322C092D1F45C29F44B76F05F10` |

Published 2026-06-05. Current as of this research date (2026-08-18). No 1.14.2 / 1.15 on NuGet yet.

---

## Sources checked

| Source | URL | What it established |
|---|---|---|
| Site redirect | https://quickfixn.org/ → https://quickfixengine.org/n | Docs/downloads moved; GitHub README still cites the old host. |
| Downloads | https://quickfixengine.org/n/download/ | Latest = **v1.14.1**. Official package id list. Unauthorized-package warning. |
| GitHub | https://github.com/connamara/quickfixn | .NET 8 from 1.13. Stock DDs under `spec/fix/`. DDTool codegen. |
| Release notes | https://github.com/connamara/quickfixn/blob/master/RELEASE_NOTES.md | 1.13 = first net8. 1.14 = package rename + ILogger. **1.14.1 = final net8**. 1.15 **removes** net8. |
| NuGet Core | https://www.nuget.org/packages/QuickFIXn.Core/ | 1.14.1 targets net8.0 + net10.0. Dep: `Microsoft.Extensions.Logging.Abstractions >= 8.0.3`. |
| NuGet FIX44 | https://www.nuget.org/packages/QuickFIXn.FIX44/ | 1.14.0 and 1.14.1 only. Depends on Core `>= 1.14.1` for the 1.14.1 build. |
| NuGet legacy | https://www.nuget.org/packages/QuickFIXn.FIX4.4/ | Last **1.13.0**, deprecated → `QuickFIXn.FIX44`. |
| Config / SSL | https://quickfixengine.org/n/documentation/configuration.html | `SSLEnable` (not QF/J `SocketUseSSL`). Full SSL + validation keys. |
| Custom DD | https://quickfixengine.org/n/documentation/custom-fields-groups-messages.html | Edit XML; point `DataDictionary=`; typed getters vs `StringField`. |
| ILogger (1.14) | https://quickfixengine.org/n/documentation/dotnet-ilogger-api.html | Optional `ILoggerFactory` ctors. Legacy `ILogFactory` still works. |
| App tutorial | https://quickfixengine.org/n/documentation/creating-an-application.html | Assemblies `QuickFix.dll` + `QuickFix.FIX44.dll`. `SocketInitiator` for client. |
| cTrader RoE | https://help.ctrader.com/fix/specification/ | FIX 4.4 only. Transport security, not FIX encryption (`98=0`). |
| cTrader intro / FAQ | https://help.ctrader.com/fix/ · https://help.ctrader.com/fix/faqs/ | Two connections. QUOTE may omit heartbeats while quotes stream. Duplicate reports if two TRADE sessions. |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§5, 25–34, 74 | Prefer QuickFIX/n; generic FIX44 is **not** enough; pin versions. |
| Sibling audit | `D:\Prop\reports\swarm\20260818\A05_fix_ctrader_audit.md` | Same pin (1.14.1). RoE tags 721 / 1000–1008 / numeric 55. |

Inspected nupkg layouts (not installed into the product):

```text
QuickFIXn.Core.1.14.1.nupkg
  lib/net8.0/QuickFix.dll
  lib/net8.0/QuickFix.xml
  lib/net10.0/QuickFix.dll
  lib/net10.0/QuickFix.xml

QuickFIXn.FIX44.1.14.1.nupkg
  lib/net8.0/QuickFix.FIX44.dll
  lib/net10.0/QuickFix.FIX44.dll
  DataDictionary/FIX44.xml          ← stock dictionary (~341 KB). Not a contentFiles copy-to-output item.
```

On restore the XML lives in the global cache, e.g.

```text
%USERPROFILE%\.nuget\packages\quickfixn.fix44\1.14.1\datadictionary\FIX44.xml
```

Do **not** point `DataDictionary=` at that cache path in production. Copy it into the adapter project and customize (see below).

---

## Official NuGet ids (and only these)

From https://quickfixengine.org/n/download/ (emphasis theirs): *“These are the only packages published by us. Any other packages are unauthorized.”*

Prefix-reserved **`QuickFIXn.*`**. Owners on NuGet: `grantb`, `snorris`. License: QuickFIX Software License (`requireLicenseAcceptance=true`).

| Package id | Role | 1.14.x versions | Needed for cTrader? |
|---|---|---|---|
| **QuickFIXn.Core** | Engine (`QuickFix.dll`) | 1.14.0, **1.14.1** | **Yes** |
| **QuickFIXn.FIX44** | FIX 4.4 messages (`QuickFix.FIX44.dll`) + stock XML | 1.14.0, **1.14.1** | **Yes** |
| QuickFIXn.FIX40 | FIX 4.0 messages | 1.14.0, 1.14.1 | No |
| QuickFIXn.FIX41 | FIX 4.1 messages | 1.14.0, 1.14.1 | No |
| QuickFIXn.FIX42 | FIX 4.2 messages | 1.14.0, 1.14.1 | No |
| QuickFIXn.FIX43 | FIX 4.3 messages | 1.14.0, 1.14.1 | No |
| QuickFIXn.FIX50 | FIX 5.0 app messages | 1.14.0, 1.14.1 | No |
| QuickFIXn.FIX50SP1 | FIX 5.0 SP1 | 1.14.0, 1.14.1 | No |
| QuickFIXn.FIX50SP2 | FIX 5.0 SP2 | 1.14.0, 1.14.1 | No |
| QuickFIXn.FIXT11 | FIXT 1.1 transport (FIX5+) | 1.14.0, 1.14.1 | No |

**Rename (breaking for PackageReference ids, starting 1.14.0, issue #627):**

| Pre-1.14 (legacy) | 1.14+ |
|---|---|
| `QuickFIXn.FIX4.0` | `QuickFIXn.FIX40` |
| `QuickFIXn.FIX4.1` | `QuickFIXn.FIX41` |
| `QuickFIXn.FIX4.2` | `QuickFIXn.FIX42` |
| `QuickFIXn.FIX4.3` | `QuickFIXn.FIX43` |
| **`QuickFIXn.FIX4.4`** | **`QuickFIXn.FIX44`** |
| `QuickFIXn.FIX5.0` | `QuickFIXn.FIX50` |
| `QuickFIXn.FIX5.0SP1` | `QuickFIXn.FIX50SP1` |
| `QuickFIXn.FIX5.0SP2` | `QuickFIXn.FIX50SP2` |
| `QuickFIXn.FIXT1.1` | `QuickFIXn.FIXT11` |

**Reject / do not restore:** `QuickFix.Net.NetCore`, `QuickFix`, `QuickFIXn` (no suffix), any package that is not the table above. Most unofficial packages misspelled the project name.

**Transitive (do not need a direct pin unless CPM requires it):**

```text
Microsoft.Extensions.Logging.Abstractions >= 8.0.3
```

That is the 1.14 logging API. Legacy `QuickFix.Logger.ILogFactory` (`FileLogFactory`, `ScreenLogFactory`) still works.

---

## Version map vs net8

| QF/n | TFM | FIX44 package id | Notes |
|---|---|---|---|
| 1.11.2 | net6.0 | `QuickFIXn.FIX4.4` | Too old. |
| 1.12.2 | net6.0 | `QuickFIXn.FIX4.4` | Last net6 line. 1.12.0/1.12.1 yanked (critical bugs). |
| 1.13.0 | **net8.0** | `QuickFIXn.FIX4.4` **1.13.0** | First net8. Core 1.13.0 **deprecated (critical bugs)** — #951 disconnect. |
| 1.13.1 | **net8.0** | *no new FIX4.4 nupkg* | Core-only backport of #951. Message package stays **FIX4.4 1.13.0**. |
| **1.14.0** | net8.0 | **`QuickFIXn.FIX44`** | Rename + ILogger. Use only if 1.14.1 cannot be restored. |
| **1.14.1** | **net8.0 + net10.0** | **`QuickFIXn.FIX44` 1.14.1** | **Pin.** Adds net10; redaction settings; SSL CA empty-path fix (#895); last net8. |
| 1.15 (upcoming) | net10 only | `QuickFIXn.FIX44` | **Drops net8.** Also breaks `DateOnly`/`TimeOnly` field backing. Stay on 1.14.1 until the product moves TFM. |

**Fallback only if 1.14 ILogger ctor changes are blocked:** `QuickFIXn.Core 1.13.1` + `QuickFIXn.FIX4.4 1.13.0`. Not recommended. 1.13.0 Core is the broken disconnect build.

**Do not take 1.15 on net8.** Release notes (verbatim intent): *SUPPORT FOR .NET 8.0 WILL BE REMOVED IN 1.15*. Microsoft ends .NET 8 support 2026-11-10; plan a later TFM move to net10 + QF/n 1.15 together.

1.13 breaking changes still apply under 1.14 (nullable, `Message.ConstructString()`, logger/store namespaces `QuickFix.Logger` / `QuickFix.Store`). 1.14 additionally internalizes `SessionFactory` and switches default logging plumbing to `ILogger`.

---

## Assemblies, namespaces, initiator shape

| NuGet | Assembly | Typical usings |
|---|---|---|
| Core | `QuickFix.dll` | `QuickFix`, `QuickFix.Logger`, `QuickFix.Store` |
| FIX44 | `QuickFix.FIX44.dll` | `QuickFix.FIX44` (typed `NewOrderSingle`, `ExecutionReport`, …) |

cTrader is a **client**. Use `SocketInitiator` (not `ThreadedSocketAcceptor`).

Two independent sessions (architecture §§27–28, A05):

- Two `[SESSION]` blocks (or two `SessionSettings` files).
- Distinct `SessionQualifier` **and** `TargetSubID` (`QUOTE` / `TRADE`).
- Distinct `FileStorePath` / `FileLogPath`. **Never** share sequence files.
- Same `BeginString=FIX.4.4`, same `SenderCompID` (`<env>.<brokerUid>.<login>`), `TargetCompID` as configured (`CSERVER` in the official RoE table — do not silently case-fold).

`DefaultMessageFactory` reflects official QF/n message assemblies. Custom tags do **not** get generated properties unless you rerun DDTool. Runtime validation uses the **XML** dictionary, not the C# class shape.

---

## FIX44 dictionary — where it is, how QF/n uses it

Stock files:

| Location | Path |
|---|---|
| GitHub (source of generated types) | https://github.com/connamara/quickfixn/blob/master/spec/fix/FIX44.xml |
| Same tree | `spec/fix/FIX40.xml` … `FIX50SP2.xml`, `FIXT11.xml` |
| 1.14.1 nupkg | `DataDictionary/FIX44.xml` |

Session keys (FIX 4.x — **not** `AppDataDictionary` / `TransportDataDictionary`; those are FIXT.1.1 / FIX5):

```ini
UseDataDictionary=Y
DataDictionary=dictionaries/FIX44-cTrader.xml
```

Related validation (defaults in parentheses):

| Setting | Default | cTrader note |
|---|---|---|
| `UseDataDictionary` | Y | Keep Y. Repeating groups (MD 268/267/146) need a DD. |
| `ValidateLengthAndChecksum` | Y | Keep Y. |
| `ValidateFieldsOutOfOrder` | Y | cTrader FAQ: tag order matters. Keep Y after the RoE DD matches wire order. |
| `ValidateFieldsHaveValues` | Y | Keep Y. |
| `ValidateUserDefinedFields` | Y | UDFs are tags **≥ 5000**. cTrader 1000–1008 are **< 5000** — this flag will **not** save you. |
| `AllowUnknownMsgFields` | N | Y would paper over 721-on-D and 1000–1008. Prefer editing the XML instead. |
| `AllowUnknownEnumValues` | N | Keep N unless a broker sends extra enums. |

**Generic `FIX44.xml` is not sufficient** (architecture §5, A05, RoE). Confirmed against the 1.14.1 stock XML:

| Gap | Stock 1.14.1 `FIX44.xml` | cTrader RoE |
|---|---|---|
| Tag 55 `Symbol` | `type="STRING"` | Numeric Spotware instrument id (`Long`). Wire examples: `55=1`. Reject if a ticker string is sent. |
| Tag 721 `PosMaintRptID` | Defined `STRING`. Present on AM/AO/AP only. **Absent** from `NewOrderSingle` (35=D) and `ExecutionReport` (35=8). | Required on attach-to-position 35=D (hedge) and returned on 35=8 / Position Report. |
| Tags **1000–1008** | **Not present at all** | Absolute/Relative TP/SL, trailing/trigger/guaranteed SL, `SymbolName`, `SymbolDigits`. |
| Tag 494 `Designation` | Present on 35=D | Custom order label — OK as-is. |
| Logon 553/554 | Already on admin Logon | Username = numeric login; password = FIX password. QF/n has **no** `Username=` / `LogonTag=` setting — inject in `ToAdmin`. |
| 35=AF `IssueDate` (225) | Field exists as `LOCALMKTDATE` and is **not** on `OrderMassStatusRequest` | RoE optional filter; example value is a **timestamp** (`20170404-07:20:44.582`). Type clash if validated as LOCALMKTDATE. |
| 35=AN / 35=AP required set | Parties/Account/AccountType/ClearingBusinessDate **required=Y** | cTrader position workflow is a subset. Stock required flags will reject valid RoE messages. |
| 35=W `NoMDEntries` | No `MDEntryID` (278) in the group | Depth snapshot repeats `269/270/271/278`. FIX 4.4 **group field order is significant**. |
| 35=X group | 279, (DeleteReason), 269, 278, Instrument, … 270, 271 | Wire: `279, 269, 278, 55, 270, 271`. Close, but Instrument component vs bare 55 can still break parsing if extra required children appear. |
| 35=V body order | `NoMDEntryTypes` then `NoRelatedSym` | Official request examples send `146/55` **before** `267/269`. Outbound: emit DD order (or reorder the XML to match examples). |

Do not “fix” this with `UseDataDictionary=N`. MD incremental/snapshot groups will mis-parse.

---

## How to customize the data dictionary for cTrader

Official method: https://quickfixengine.org/n/documentation/custom-fields-groups-messages.html

### 1. Fork the stock XML (do not edit the NuGet cache)

Suggested repo path (architecture / A05): adapter content, e.g. `FIX44-cTrader.xml`.

```xml
<None Include="Dictionaries\FIX44-cTrader.xml" CopyToOutputDirectory="PreserveNewest" />
```

Seed from either:

- nupkg `DataDictionary/FIX44.xml` (same bytes as the generated `QuickFix.FIX44` types), or
- GitHub `spec/fix/FIX44.xml` at tag **v1.14.1** (keep generator and runtime DD in lockstep).

### 2. Point the session at the fork

```ini
UseDataDictionary=Y
DataDictionary=Dictionaries/FIX44-cTrader.xml
```

`BeginString` stays `FIX.4.4`. Do not invent `FIX.4.4.CTRADER`.

### 3. Edit the XML (minimum RoE set)

**A. Fields that do not exist — add under `<fields>`:**

```xml
<field number="1000" name="AbsoluteTP" type="PRICE"/>
<field number="1001" name="RelativeTP" type="PRICE"/>
<field number="1002" name="AbsoluteSL" type="PRICE"/>
<field number="1003" name="RelativeSL" type="PRICE"/>
<field number="1004" name="TrailingSL" type="PRICE"/>
<field number="1005" name="TriggerMethodSL" type="INT"/>
<field number="1006" name="GuaranteedSL" type="BOOLEAN"/>
<field number="1007" name="SymbolName" type="STRING"/>
<field number="1008" name="SymbolDigits" type="INT"/>
```

Names 1000–1008 follow A05 / RoE commentary. Confirm descriptions against the current Spotware spec before treating enums as frozen.

**B. Attach existing/custom fields to the messages that actually carry them:**

- `NewOrderSingle`: `PosMaintRptID` required="N" (already has `Designation`).
- `ExecutionReport`: `PosMaintRptID` + 1000–1006 as required="N".
- `PositionReport`: 1000–1006 as required="N" (721 is already required there).
- `SecurityList` / `NoRelatedSym`: `SymbolName` (1007), `SymbolDigits` (1008).
- `OrderMassStatusRequest`: optional filter field — prefer a **new** cTrader-only field or `STRING`/`UTCTIMESTAMP` if you must accept 225 with a time component; do not leave it as stock `LOCALMKTDATE` if you send the RoE example.

**C. Numeric instrument id:** keep tag 55 name `Symbol`. Either leave `STRING` (values are decimal digits) or change to `INT`. Application code must still treat it as a Spotware id, never `"XAUUSD"`. Discover via Security List (architecture §30).

**D. Relax required=Y** on 35=AN / 35=AP / 35=y / 35=8 to the RoE tables. Stock “Parties required” will reject cTrader.

**E. Repeating groups (FIX 4.4 order is load-bearing):**

- Snapshot 35=W `NoMDEntries`: include `MDEntryID` and match wire order `269, 270, 271, 278` (plus `299` if present).
- Incremental 35=X `NoMDEntries`: delimiter remains `MDUpdateAction` (279); keep 269/278/55/270/271 in the order cServer sends.
- Request 35=V: either emit `267` then `146` (stock) or reorder the XML to the official example.

Wrong group order → reject or silently mangled books.

### 4. Runtime access to custom tags (no codegen required)

```csharp
const int PosMaintRptID = 721;
const int AbsoluteTP = 1000;

// get
string posId = message.GetString(PosMaintRptID);

// set
message.SetField(new StringField(PosMaintRptID, existingPositionId));
```

Typed `QuickFix.FIX44.NewOrderSingle` still works for standard fields (`ClOrdID`, `OrdType`, …). It will **not** grow `AbsoluteTP` properties until DDTool is rerun.

### 5. Optional: regenerate C# types (only if you want typed 1000–1008)

DDTool (C#, replaced the old Ruby generator in 1.12):

```text
dotnet run --project DDTool --reporoot <quickfixn-clone> --outputdir <dest> spec/fix/FIX44-cTrader.xml
# or
pwsh scripts\Generate-Message-Sources.ps1
```

That is a **fork of the message assembly**, not something NuGet will give you. For this repo: **XML-only customization + `StringField` is enough** through Phase 4/7. Do not vendor a regenerated `QuickFix.FIX44` unless a later phase proves typed custom fields pay for the maintenance.

### 6. What not to do

- Do not set `ValidateUserDefinedFields=N` and call it a cTrader dictionary. Tags 721 and 1000–1008 are **< 5000**.
- Do not share one DD file that is still stock FIX44 “plus AllowUnknownMsgFields=Y”. Groups will still be wrong.
- Do not regenerate / overwrite files inside `~/.nuget/packages`.
- Do not hand-write a `TcpClient` FIX engine (architecture §5; Spotware sample is explicitly not an engine).

---

## SSL / TLS (QF/n native — not stunnel)

cTrader RoE: *“Currently, only transport-level security is supported. EncryptMethod = 0 (NONE_OTHER).”* TLS wraps the socket. There is no FIX-level encryption.

Broker UI / community convention (confirm per account; do not hardcode):

| Session | TLS port | Plaintext port |
|---|---|---|
| QUOTE (price) | **5211** | 5201 |
| TRADE | **5212** | 5202 |

Architecture §25 / A05 / A27 use SSL **5211 / 5212**. Architecture §72 rule 12: **TLS in production; no plaintext production.**

QF/n setting name is **`SSLEnable`**, not QuickFIX/J `SocketUseSSL`.

| Setting | Initiator (cTrader) | Default | Pin / note |
|---|---|---|---|
| **SSLEnable** | **Y** | Y only if a cert path is set, else **N** | Must be explicit Y. Docs mention `SSLCertificatePath`; the real key is `SSLCertificate`. |
| SSLServerName | DNS name on the server cert | `SocketConnectHost` | Set this if you connect by **IP** (SNI / name mismatch). |
| SSLProtocols | `Tls12` or `Tls13` | `Default` (obsolete enum) | Do not leave Default. `Tls12` is the safe net8 pin; add `Tls13` if the venue cert stack supports it. |
| SSLValidateCertificates | **Y** | Y | N is a documented security risk and forces revocation off. |
| SSLCheckCertificateRevocation | Y | Y | Keep Y in production. |
| SSLCertificate | omit (no client cert unless the broker issues one) | — | Required for **acceptors**; initiator only if mutual TLS. |
| SSLCertificatePassword | only with a client .pfx | — | |
| SSLRequireClientCertificate | n/a (acceptor) | Y | Ignore on initiator. |
| SSLCACertificate | omit to use OS trust store | OS roots | 1.14.0 #895: empty `SSLCACertificate` used to fail startup — 1.14.1 includes the fix. |
| SocketSendTimeout / SocketReceiveTimeout | e.g. 10000 | 0 | Added in 1.10 specifically because SSL sockets hung with 0. |

**Sample initiator SSL fragment (QUOTE). TRADE is the same except port, `TargetSubID`, `SessionQualifier`, store paths:**

```ini
[DEFAULT]
ConnectionType=initiator
ReconnectInterval=30
HeartBtInt=30
NonStopSession=Y
ResetOnLogon=Y
ResetOnLogout=Y
ResetOnDisconnect=Y
UseDataDictionary=Y
DataDictionary=Dictionaries/FIX44-cTrader.xml
SSLEnable=Y
SSLProtocols=Tls12
SSLValidateCertificates=Y
SSLCheckCertificateRevocation=Y
RedactFieldsInLogs=553,554
RedactionLogText=<redacted>
SocketNodelay=Y
SocketSendTimeout=10000
SocketReceiveTimeout=10000
FileStorePath=store/quote
FileLogPath=log/quote

[SESSION]
BeginString=FIX.4.4
SenderCompID=live.theBroker.12345
TargetCompID=CSERVER
SenderSubID=QUOTE
TargetSubID=QUOTE
SessionQualifier=QUOTE
SocketConnectHost=live-us-eqx-01.p.c-trader.com
SocketConnectPort=5211
```

`SenderCompID` / host / ports / `TargetCompID` casing are **broker-form values**. Official examples use `CSERVER`; some broker PDFs print `cServer`. Make them configurable. Never rewrite case (architecture §26).

**Logon body** (not config keys): in `ToAdmin`, when `MsgType=A`, set:

- `EncryptMethod` = 0  
- `HeartBtInt` = session heartbeat  
- `ResetSeqNumFlag` = Y (RoE: both sides reset on establish)  
- `Username` (553) = numeric trader login  
- `Password` (554) = FIX password  

QF/n will not add 553/554 by itself. 1.14.1 `RedactFieldsInLogs=553,554` keeps passwords out of FileLog / ILogger message dumps.

**Certificate practice:** initiator validates the **server** cert via OS CAs. No Spotware client .pfx is required for the usual retail FIX ports. If a broker later requires mutual TLS, put the client .pfx in `SSLCertificate` + password in a secret store (never in git).

**Do not use plaintext 5201/5202 in production.** Fine for a local simulator only.

---

## Recommended SessionSettings extras for this product

| Setting | QUOTE | TRADE |
|---|---|---|
| `PersistMessages` | `N` is acceptable (MD stream; GapFill-only) | `Y` (orders / ER / resend) |
| `FileStorePath` | `store/quote` | `store/trade` |
| `CheckLatency` | Y | Y |
| `TimestampPrecision` | Milliseconds (RoE examples have `.sss`) | same |
| `MillisecondsInTimestamp` | deprecated; do not set (removed in 1.15) | same |

Two `IApplication` instances (or one class keyed by `SessionID`) — architecture: `CTraderQuoteSession` + `CTraderTradeSession`.

---

## What this pin does **not** decide

- Whether to take `ILoggerFactory` now or keep `FileLogFactory` (both valid on 1.14.1).
- Exact `TargetCompID` string per broker form.
- Whether to regenerate `QuickFix.FIX44` from the forked XML (not required).
- Live `NewOrderSingle` enablement (still gated; this report only pins packages).

---

## Implementation checklist (when a later agent adds packages)

```text
[ ] PackageReference QuickFIXn.Core 1.14.1
[ ] PackageReference QuickFIXn.FIX44 1.14.1
[ ] No QuickFIXn.FIX4.4, no unofficial FIX packages, no QuickFIXn.FIXT11
[ ] Copy 1.14.1 DataDictionary/FIX44.xml → FIX44-cTrader.xml and apply RoE edits
[ ] Copy XML to output; DataDictionary= points at the fork
[ ] SSLEnable=Y, SSLProtocols=Tls12 (or Tls13), validate+revocation on
[ ] Two sessions, two stores, TargetSubID + SessionQualifier QUOTE/TRADE
[ ] ToAdmin injects 98=0, 141=Y, 553, 554; RedactFieldsInLogs=553,554
[ ] ResetOnLogon=Y to match RoE sequence reset
[ ] Do not connect a TRADE session to the live host until §41 flags + ownership lease exist
```

---

## References (canonical)

- https://quickfixn.org/ (redirects to https://quickfixengine.org/n)
- https://quickfixengine.org/n/download/
- https://github.com/connamara/quickfixn
- https://github.com/connamara/quickfixn/blob/master/RELEASE_NOTES.md
- https://www.nuget.org/packages/QuickFIXn.Core/1.14.1
- https://www.nuget.org/packages/QuickFIXn.FIX44/1.14.1
- https://quickfixengine.org/n/documentation/configuration.html
- https://quickfixengine.org/n/documentation/custom-fields-groups-messages.html
- https://quickfixengine.org/n/documentation/dotnet-ilogger-api.html
- https://help.ctrader.com/fix/specification/
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§5, 25–34, 72, 74
