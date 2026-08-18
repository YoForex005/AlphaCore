# P501 — Demo test-trade gates (cTrader FIX)

| Field | Value |
|---|---|
| Agent | P501 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\P501_TEST_TRADE_GATES.md` |
| Product source edited | **No** |
| Official RoE | https://help.ctrader.com/fix/specification/ (FIX 4.4) |
| Spec extract | `D:\Prop\reports\swarm\20260818\A32_ctrader_fix_specification.md` |
| Demo helper (read-only) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` |
| Live account (forbidden for this helper) | `1369850` |

This file is a **gate contract**, not an execution log. It does not print passwords, FIX `554`, env secrets, or raw Logon bodies. Product source was not modified.

---

## 0. Verdict (binding)

A **demo** `35=D` market fill on `demo-*` / `demo.*` proves only that:

1. the demo-only host/sender/account gate allowed the socket,
2. TRADE Logon (`35=A`) succeeded on that demo session,
3. Security List (`35=y`) returned at least one **numeric** `55`,
4. a market New Order Single (`40=1`) was accepted or rejected by the **demo** engine,
5. optionally a flatten `35=D` (`54=2`) closed the demo position.

It does **not** prove:

- copy profitability,
- lot → `OrderQty` conversion,
- live Pepperstone / `1369850` routing,
- QUOTE session health,
- risk-engine gating,
- ClOrdID idempotency under restart,
- partial-fill / cancel-replace / recon after crash,
- architecture §70 live FIX acceptance.

**PASS of this helper ≠ copy system PASS.** Treat a green `Filled=true` as a **connectivity + tag-shape** check on demo only.

---

## 1. Demo-only gate (must refuse otherwise)

Source: `CTraderFixDemoTestTrade.SendAsync` (lines 42–59). Conjunction is **fail-closed**. Any single miss returns `Allowed=false` and does **not** open TCP/TLS.

| Check | Required | Refuse when |
|---|---|---|
| Host prefix | `host` starts with `demo-` (ordinal ignore-case) | host is `live-…`, bare hostname, IP, or any non-`demo-` prefix |
| Host live substring | host must **not** contain `live-` (ignore-case) | e.g. `demo-live-…` or `live-us-eqx-01.p.c-trader.com` |
| Sender prefix | `senderCompId` starts with `demo.` (ignore-case) | `live.pepperstone.…`, missing env segment, or bare login |
| Sender live substring | sender must **not** contain `live.` (ignore-case) | mixed `demo.…live.…` or official live triple |
| Account | `account != "1369850"` (exact string) | live Pepperstone trader login used anywhere in this helper |

Refused result (no socket):

- `Allowed=false`, `LoggedOn=false`, `OrderSent=false`, `Filled=false`, `Flattened=false`
- `Error = "Refused: test trade is demo-only (host/sender/account gate)."`
- `Host` / `Account` echoed for diagnostics (not secrets)

Official CompID shape (A32 / RoE): `<Environment>.<BrokerUID>.<Trader Login>`. Demo environment is the first dotted segment (`demo.…`). Logon `Username` (553) is the **numeric login only**; do not put the dotted triple in 553.

**Hard rule:** this helper must never target the live account `1369850`, never send `49` starting with `live.`, and never connect to a host containing `live-`.

---

## 2. Official New Order Single tags (`35=D`)

Direction: client → cTrader on the **TRADE** session (`57=TRADE`). Catalog and official examples: A32 § New Order Single.

### 2.1 Required application tags (RoE table)

| Tag | Field | Required | Official type / values | Gate |
|---|---|---|---|---|
| 11 | `ClOrdID` | **Yes** | unique client id | unique per send; helper uses `T` + UTC `yyyyMMddHHmmssfff` |
| 55 | `Symbol` | **Yes** | **Long** — Spotware instrument id | **numeric id**, never ticker, never MT5 name |
| 54 | `Side` | **Yes** | `1` Buy / `2` Sell | helper open = `1`; flatten = `2` |
| 60 | `TransactTime` | **Yes** | UTC timestamp | client-generated |
| 38 | `OrderQty` | **Yes** | Qty, max precision **0.01** | **units**, not MT5 lots (see §4) |
| 40 | `OrdType` | **Yes** | `1` Market / `2` Limit / `3` Stop | this helper: **`1` only** |

Standard header (always): `8=FIX.4.4`, `9`, `35=D`, `49` (demo CompID), `56=CSERVER`, `57=TRADE`, `34`, `52`. Trailer `10`.

### 2.2 Conditional / optional (do not invent)

| Tag | When | Note |
|---|---|---|
| 44 `Price` | required if `40=2` | not sent on market test |
| 99 `StopPx` | required if `40=3` | not sent on market test |
| 59 `TimeInForce` | **deprecated, ignored** | Market **implies IOC**; do not treat 59 as the TIF source |
| 126 `ExpireTime` | Limit/Stop → GTD | not used for market |
| 721 `PosMaintRptID` | hedged close / attach | helper sends on flatten only if ER returned 721 |
| 494 `Designation` | optional label | not required |

### 2.3 Official market-order example (shape only)

Quoted from A32 / RoE (live CompID in the **vendor example**, not a credential):

```
35=D | 57=TRADE | 11=… | 55=1 | 54=1 | 60=… | 40=1 | 38=10000
```

`55=1` is a numeric Spotware id (EURUSD in the RoE book), **not** the string `EURUSD`. `38=10000` is **10 000 units**, not `0.10` lots.

Helper body actually sent after the gate (demo only):

```
35=D  11=<ClOrdID>  55=<numeric symbolId>  54=1  60=<UTC>  40=1  38=1000
```

Then, if filled, a flatten `35=D` with `54=2`, same `55`, `40=1`, `38` = last fill qty (`32` else `14` else `1000`), optional `721`.

### 2.4 Execution Report acceptance (read path)

Fill is counted only from official ER (`35=8`) fields:

- `150` ExecType `F` (Trade) or (helper also accepts `"2"`)
- `39` OrdStatus `1` (partial) or `2` (filled)
- reject: `150=8` / `39=8` / `35=3` (Reject) / `35=j` (Business Message Reject)

Official market New→Fill pair echoes `40=1` and **`59=3` (IOC)** even though the request omitted 59.

---

## 3. Security List: tag `55` is numeric

Catalog: `35=x` request / `35=y` list on **TRADE**. Official:

- `55` Symbol = **Integer / Long** — “Instrument identifiers are provided by Spotware.”
- Ticker is custom tag **`1007` `SymbolName`**, not 55.
- Digits = `1008`.
- Repeating group: `146` then per instrument `55` / `1007` / `1008`.
- Official single-symbol: `55=39|1007=NZDCHF|1008=4`.
- MD reject text (A32): `Expected numeric symbolId, but got CS8260` — confirms **55 must be numeric** on both Security List and Market Data.

**Gate:** never put `XAUUSD`, `GOLD`, or an MT5 symbol name in `35=D` tag 55. Resolve id from `35=y` first.

Helper order: send `35=x` with `559=0` (full book; 55 omitted), parse `35=y`, prefer name `XAUUSD` via tags `1007` (also 107/965/58 as fallbacks), else first `55=`. Abort with `No symbol in SecurityList` if no numeric 55.

---

## 4. `OrderQty` is units — not MT5 lots

Official RoE comment on tag 38: “The number of **shares** ordered … maximum precision is 0.01.” Official market example uses `38=10000`. That is a **base-unit / contract-unit** quantity, not MetaTrader lot volume.

Do **not** map:

| Wrong source | Wrong 38 | Why |
|---|---|---|
| MT5 `Volume()` classic (`lots * 10_000`) | `38=10000` for 1.00 lot | happens to look like FX units for 1.00 lot of 100k, **coincidence**, not a rule |
| MT5 `VolumeExt()` (`lots * 100_000_000`) | huge 38 | 10 000× oversize vs classic; not FIX Qty |
| MT4 hundredths (`lots * 100`) | `38=100` for 1.00 lot | not MT5, not cTrader units |
| Blind `Normalize(lots, 1, dest)` | `38=0.10` for 0.10 lots | architecture: never convert lots directly to OrderQty |

Helper hard-codes **`38=1000`** (one thousand units) on the open. That is a **tiny demo size**, not a copy of any MT5 deal volume.

Worked FX illustration (contract 100 000): `38=1000` ≈ 0.01 lot; `38=10000` ≈ 0.10 lot; `38=100000` ≈ 1.00 lot. Metals / CFD contract sizes differ — **do not** reuse that table for XAUUSD without Security List + symbol spec. A43 / A38 remain the conversion law for copy; this helper does not implement it.

---

## 5. Market = IOC (TIF is implied, not chosen)

Official `OrdType` (40):

| 40 | Type | Implied TIF (59 ignored on request) | ER echo |
|---|---|---|---|
| **1** | Market | **IOC** (`3`) | official fill pair: `40=1\|59=3` |
| 2 | Limit | GTC (`1`) unless `126` → GTD (`6`) | resting |
| 3 | Stop | GTC / GTD same as limit | stop |

RoE: tag 59 is **“Deprecated, this value will be ignored.”** TIF is detected from `40` (+ `126`). Sending `59=1` on a market order does **not** make it GTC.

**Gate for this helper:** only `40=1`. Expect IOC behavior: full fill, partial + cancel remainder, or reject. No GTC rest, no expire, no replace. Flatten is a second market IOC.

---

## 6. What a demo fill is allowed to claim

| Claim | Allowed? |
|---|---|
| Demo TRADE socket + TLS + Logon `35=A` works for this demo CompID | Yes, if `LoggedOn=true` |
| Security List returns numeric `55` (+ optional `1007`) | Yes, if `SymbolId` set |
| Demo engine accepted or rejected a 1000-unit market buy | Yes, if `OrderSent=true` |
| Demo position was flattened | Yes, if `Flattened=true` |
| Tag 55 was a ticker / MT5 name | **No** |
| `38=1000` equals 1000 MT5 lots or 0.10 lots | **No** |
| Market order rested as GTC | **No** (IOC) |
| Live `1369850` / `live.*` / `live-*` was exercised | **No** (gate refuse) |
| Copy pipeline, risk, recon, §70, or **profitability** is proven | **No** |

### Profitability (explicit)

A single demo IOC fill (and optional flatten) has **no statistical, cost, slippage, or edge content**. It does not measure spread, commission, swap, latency vs MT5 source, sizing, or expectancy. **A demo test fill does not prove copy profitability.**

---

## 7. Secrets / redaction

- Do not log or paste tag **554** (password). Helper `Sanitize` drops `554=` before `Raw`.
- Do not write passwords, tokens, or `.env` values into this report or swarm logs.
- Account id `1369850` is a **public live-login gate id**, not a secret; still do not pair it with a password.
- `Raw` on results is truncated (1500 chars) and must stay sanitized.

---

## 8. Header mapping reminder (do not treat as this helper’s job)

Official TRADE: client `57=TRADE`, `50` = originator string (not the session qualifier). Server inbound session is **tag 50**. This report does not change product. Implementers comparing the helper to A32 should not “fix” live copy from a demo fill.

---

## 9. Sources

- https://help.ctrader.com/fix/specification/
- `D:\Prop\reports\swarm\20260818\A32_ctrader_fix_specification.md`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (read-only)
- Volume units context: `D:\Prop\reports\swarm\20260818\A38_mt5_volume_units.md`

**Product tree under `D:\Prop\src` was not modified.**
