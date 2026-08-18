# W500_SLICE_74 — Achiever HTTP proxy / MT_RET_AUTH_MANAGER_IPBLOCK 1012

| Field | Value |
|---|---|
| Slot | 74 |
| File | `D:/Prop/MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Angle | Achiever HTTP proxy / `MT_RET_AUTH_MANAGER_IPBLOCK` 1012 |
| Date | 2026-08-18 |
| Method | `read_file` of the assigned file (L1–1000; §47–49; §55–58; §62–63; §67; §69–75 / L2855–2867) + `grep` on this file for `Achiever`, `proxy`, `1012`, `IPBLOCK`, `MT_RET_AUTH`, `HTTP`, `SOCKS`, `ProxySet`, `81.29.145.69` |
| Product source modified | **No.** This report is the only write. |
| Secrets printed | **None.** Architecture placeholders remain `<SECRET>`. No `.env` password, proxy auth, or FIX password copied. |
| Verdict | **FAIL** |

This is **not** an empty PASS. The assigned file was read. The angle applies to this document (it is the Achiever connect / egress law). The file specifies the allow-list IP and an optional proxy hop; it does **not** specify HTTP as the hop type, does **not** name `MT_RET_AUTH_MANAGER_IPBLOCK` / 1012, and does **not** classify 1012 as a non-retryable Manager IP-identity refusal.

---

## 1. What was read

Assigned file is Architecture v2.0 (*MT5 Trader Intelligence + cTrader FIX 4.4 Execution Platform*). Line 7 pins **Execution default: Disabled**. Achiever is a **source** broker (pipeline L27–29, diagram L194, final target L2818), not the Pepperstone/cServer execution venue.

`grep` on this file:

| Pattern | Hits in assigned file |
|---|---|
| `Achiever` | present (§1, §4, §7, §17 `ACHIEVER_MT5_TICKS`, §56, §67 Phase 1, §75) |
| `proxy` / `PROXY` | present (§7, §8, §55, §56) |
| `81.29.145.69` | present (§7 whitelist; §56 `ACHIEVER_EGRESS_IP` + `ACHIEVER_PROXY_HOST`) |
| `49527` | present (§56 `ACHIEVER_PROXY_PORT`) |
| `HTTP` / `SOCKS` / `ProxySet` / `PROXY_TYPE` | **0** |
| `MT_RET_AUTH_MANAGER_IPBLOCK` | **0** |
| `1012` | **0** |
| `IPBLOCK` | **0** |

The vendor retcode name/number and the HTTP proxy type are **absent**. The operational **cause** of 1012 (Manager source IP not on Achiever allow-list `81.29.145.69`) **is** named. That is enough to review the angle; it is not enough to pass the connect contract.

---

## 2. Angle check

`MT_RET_AUTH_MANAGER_IPBLOCK = 1012` is the official Manager API retcode *IP address unallowed for manager*. It is a **source-side Connect refusal**. TCP to `57.128.141.65:443` can be open while Manager auth still returns 1012 if the presented source IP is not the allow-list. R012 measured this desktop’s public egress ≠ `81.29.145.69` and historical YoPips `proxy mode: DISABLED` → **1012**.

This architecture file’s job on the angle:

1. Name the Achiever allow-list IP.
2. Require an HTTP Manager `ProxySet` hop that **presents** that IP when process egress is not already SNAT as it.
3. Name 1012 / `MT_RET_AUTH_MANAGER_IPBLOCK` as the expected fail when the hop is omitted or the toggle is wrong.
4. Distinguish 1012 (do not retry the same egress) from 7 (network) and 3 (bad manager password).
5. Keep proxy credentials out of logs and React.
6. Keep live execution off until ingestion/shadow/risk are proven.

Items 1, 5, and 6 are in the file. Items 2–4 are **not**.

§7 vs §56 tension (material to this FAIL):

- §7: “If proxying is **required**…” (conditional; no compare of process egress to `ACHIEVER_EGRESS_IP`).
- §7 startup/resync starts at **Connect** — no `ProxySet` / HTTP type before Connect.
- §56 sample: `# Optional proxy` then `ACHIEVER_PROXY_ENABLED=true` on host `81.29.145.69:49527`.
- Nowhere: `HTTP` / `PROXY_HTTP` / `MT5_PROXY_TYPE=HTTP`.

“Optional” plus a silent default of HTTP is not a recipe. Wrong type (SOCKS5 vs HTTP) or a C++ toggle bound to `IS_MT5_PROXY_ENABLED` while the file only documents `ACHIEVER_PROXY_*` / `MT5_PROXY_ENABLED` is a documented 1012 path (A004). StarwaveFX is correctly `MT5_STARWAVEFX_PROXY_ENABLED=false` with no whitelist (§8) — do **not** reuse Achiever’s hop.

§62 treats all “MT5 unavailable” as **Continue retrying**. 1012 is not a flap. Retrying the same non-allow-listed egress never authenticates.

§58 metrics (`mt5_connected`, `mt5_reconnects`) and §48 Brokers page (`Connection status`, `Reconnect count`) have **no** last `MTAPIRES` / IP-block field. Phase 1 deliverable is the string **Achiever connected** with no 1012-free gate.

---

## 3. Evidence quotes

Architecture header (execution off — contains capital, does not fix the 1012 contract):

> **Execution default:** Disabled until shadow/reconciliation/risk controls are proven.

§1 pipeline (Achiever is source, not the execution venue):

```27:29:D:/Prop/MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
MT5 SOURCE BROKERS
(Achiever + StarwaveFX + future brokers)
```

§7 Achiever whitelist + proxy secret rule (no HTTP type, no 1012, no ProxySet):

```379:387:D:/Prop/MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
Required whitelisted outbound IP:

81.29.145.69

If proxying is required, credentials must be in secret storage/environment variables.

Never log proxy credentials.
```

§7 Connect sequence (starts at Connect; hop is not a step):

```397:409:D:/Prop/MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
Connect
  ↓
Enumerate groups
  ↓
Upsert groups
  ↓
Enumerate accounts
  ↓
Associate accounts with broker + group
  ↓
Sync history
```

§8 contrast (StarwaveFX — no whitelist; proxy off):

```426:437:D:/Prop/MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
MT5_STARWAVEFX_PROXY_ENABLED=false
...
No IP whitelist is currently required.

Still design the connector so proxy/whitelist routing can be enabled later.
```

§55 (do not leak proxy auth to the dashboard — correct, orthogonal to 1012):

> Never expose:
>
> `MT5 passwords` / `proxy credentials` / `cTrader account password` / `FIX password` / …

§56 secret-safe example (hop on, type unnamed, labeled Optional):

```2047:2054:D:/Prop/MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
ACHIEVER_EGRESS_IP=81.29.145.69

# Optional proxy
ACHIEVER_PROXY_ENABLED=true
ACHIEVER_PROXY_HOST=81.29.145.69
ACHIEVER_PROXY_PORT=49527
ACHIEVER_PROXY_USERNAME=<SECRET>
ACHIEVER_PROXY_PASSWORD=<SECRET>
```

§62 (retry without classifying 1012):

```2321:2328:D:/Prop/MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
## MT5 unavailable

Do not invent source trades.
Continue retrying.
Expose stale-source status.
Do not open new copied positions from stale source data.
```

§67 Phase 1 (connected with no retcode gate):

```2497:2500:D:/Prop/MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
Achiever connected
StarwaveFX connected
all groups discovered
accounts synchronized
```

This file does **not** contain:

- `MT_RET_AUTH_MANAGER_IPBLOCK`
- retcode `1012`
- `HTTP` / `SOCKS` / `ProxySet` / `MT5_PROXY_TYPE`
- a decision procedure: if process egress ≠ `81.29.145.69` then HTTP `ProxySet` **before** Connect
- a fail-closed rule: 1012 → do not retry the same path; do not paint Phase 1 connected
- a TRADE `NewOrderSingle` enablement (live send remains off per L7 / L2867)

---

## 4. No-loss implication

1012 is fail-closed **at Achiever**: the Manager login is refused before any deal/order/position pump. No source events → no reconstruction → no CopyIntent → no ExecutionIntent.

The proxy in this document is a **source-side egress hop** so the broker sees `81.29.145.69`. It is not on the Pepperstone/cServer FIX TRADE path. Misconfiguring or omitting the hop yields 1012 (no session), not a live fill.

Combined with line 7 (execution default Disabled), §23 SHADOW-only after trade #3, §62 “Do not open new copied positions from stale source data,” and L2867 (real order submission OFF), **this file cannot by itself reduce destination equity.**

Residual (why this is still FAIL, not empty PASS):

- Implementer treats `# Optional proxy` as skippable on a non-allow-listed host → 1012 forever.
- §62 “Continue retrying” burns the pool on a non-transient IP block (historical 1012 then 7).
- Phase 1 “Achiever connected” has no 1012-free definition; a Fake/demo connector can paint healthy while live Manager is IP-blocked (C42 honesty).
- Later flipping live copy on that painted book is an **omission / honesty** loss path (copying empty or dummy source), not an architecture instruction to send size.

Empty-PASS rule does **not** apply: proxy + allow-list evidence is in the assigned file at §7 L379–387 and §56 L2047–2054. The FAIL is the missing HTTP type, missing 1012 name, missing ProxySet-before-Connect, and missing 1012-vs-retry classification in the same law document.

---

## 5. Verdict

**FAIL.** Architecture v2 names Achiever whitelist `81.29.145.69` and an optional `ACHIEVER_PROXY_*` hop (secrets not logged; execution default Disabled). It never names HTTP, `ProxySet`, or `MT_RET_AUTH_MANAGER_IPBLOCK` / 1012, and §62 retries undifferentiated “MT5 unavailable.” That is an incomplete Achiever connect contract. It is **not** a live capital-send path.
