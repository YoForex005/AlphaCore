# W500_SLICE_24 — Achiever HTTP proxy / MT_RET_AUTH_MANAGER_IPBLOCK 1012

| Field | Value |
|---|---|
| Slot | 24 |
| File | `D:/Prop/MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Angle | Achiever HTTP proxy / `MT_RET_AUTH_MANAGER_IPBLOCK` 1012 |
| Date | 2026-08-18 |
| Method | `read_file` of the assigned file (header through §24, then §55–56, §62–63, §67, §75) + `grep` on this file for `Achiever`, `HTTP proxy`, `proxy`, `1012`, `IPBLOCK`, `MT_RET_AUTH_MANAGER` |
| Product source modified | **No** |
| Secrets printed | **None.** Architecture placeholders remain `<SECRET>`. No `.env` password, proxy auth, or FIX password copied. |
| Verdict | **PASS** |

---

## 1. What was read

Assigned file is Architecture v2.0 (implementation prompt, not executable). Title line 1: *MT5 Trader Intelligence + cTrader FIX 4.4 Execution Platform*. Line 7 pins **Execution default: Disabled**. Source brokers in the executive pipeline (lines 27–29) are Achiever + StarwaveFX + future brokers.

`grep` on this file:

| Pattern | Hits in assigned file |
|---|---|
| `Achiever` | present (§1, §4, §7, §56, §67 Phase 1, §75) |
| `proxy` / `PROXY` | present (§7, §8, §55, §56) |
| `81.29.145.69` | present (§7 whitelist; §56 `ACHIEVER_EGRESS_IP` + `ACHIEVER_PROXY_HOST`) |
| `49527` | present (§56 `ACHIEVER_PROXY_PORT`) |
| `MT_RET_AUTH_MANAGER_IPBLOCK` | **0** |
| `1012` | **0** |
| `IPBLOCK` | **0** |

The vendor retcode name/number is **absent** from this document. The **operational cause** of 1012 (Manager source IP not on Achiever allow-list) **is** specified. That is not an unread-file empty PASS.

---

## 2. Angle check

`MT_RET_AUTH_MANAGER_IPBLOCK = 1012` is the official Manager API retcode *IP address unallowed for manager* (SDK header / `mt5_manager.cpp` mapping). It is a **source-side Connect refusal**. It is not a cTrader TRADE send, not `NewOrderSingle`, and not a sizing path.

This architecture file’s job on the angle:

1. Name the Achiever allow-list IP.
2. Allow an HTTP/SOCKS Manager `ProxySet` hop that **presents** that IP when the worker host does not already SNAT as it.
3. Keep proxy credentials out of logs and out of React.
4. Keep live execution off until ingestion/shadow/risk are proven.

All four are in the file. The missing literal `1012` is an implementer-runbook gap, not a capital-open path.

§7 vs §56 tension (not a FAIL for this slot):

- §7: “If proxying is **required**…” (conditional).
- §56 sample: `ACHIEVER_PROXY_ENABLED=true` on host `81.29.145.69:49527`.

That is consistent if “required” means “when process egress ≠ allow-list.” It is **not** a license to Connect from an arbitrary public IP. StarwaveFX is explicit `MT5_STARWAVEFX_PROXY_ENABLED=false` and “No IP whitelist is currently required” (§8).

---

## 3. Evidence quotes

Architecture header (execution off):

> **Execution default:** Disabled until shadow/reconciliation/risk controls are proven.

§1 pipeline (Achiever is source, not the execution venue):

```27:29:D:/Prop/MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
MT5 SOURCE BROKERS
(Achiever + StarwaveFX + future brokers)
```

§7 Achiever whitelist + proxy secret rule (lines 379–387):

> Required whitelisted outbound IP:
>
> `81.29.145.69`
>
> If proxying is required, credentials must be in secret storage/environment variables.
>
> Never log proxy credentials.

§56 secret-safe example (proxy hop on, credentials placeholder-only):

```2047:2054:D:/Prop/MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
ACHIEVER_EGRESS_IP=81.29.145.69

# Optional proxy
ACHIEVER_PROXY_ENABLED=true
ACHIEVER_PROXY_HOST=81.29.145.69
ACHIEVER_PROXY_PORT=49527
ACHIEVER_PROXY_USERNAME=<SECRET>
ACHIEVER_PROXY_PASSWORD=<SECRET>
```

§55 (do not leak proxy auth to the dashboard):

> Never expose:
>
> `MT5 passwords` / `proxy credentials` / `cTrader account password` / `FIX password` / …

§23 / §62 / §63 (even after source data exists, live copy is gated):

> Trade #3 + high score → **SHADOW only**. Do not automatically send real capital after three trades.
>
> Execution service should fail closed for new orders.
>
> Do NOT reconnect and blindly execute all 20 old entries.

§67 Phase 1 deliverable is **Achiever connected** (ingestion), not live FIX send.

This file does **not** contain:

- `MT_RET_AUTH_MANAGER_IPBLOCK`
- retcode `1012`
- a `ProxySet` / `MT5_PROXY_TYPE=HTTP` implementation
- a TRADE `NewOrderSingle` enablement

---

## 4. No-loss implication

1012 is fail-closed **at Achiever**: the Manager login is refused before any deal/order/position pump. No source events → no reconstruction → no CopyIntent → no ExecutionIntent.

The HTTP proxy in this document is a **source-side egress hop** so the broker sees `81.29.145.69`. It is not on the Pepperstone/cServer FIX TRADE path. Misconfiguring or omitting the proxy yields 1012 (no session), not a live fill.

Combined with line 7 (execution default Disabled) and §62 fail-closed for new orders, this file cannot by itself reduce destination equity. Residual (out of this slice) is an implementer treating a 1012-blocked or fake Achiever connector as “connected” and later flipping live copy — that is a Phase-1 honesty / flag problem, not an architecture instruction to send size.

Empty-PASS justification for the **missing 1012 token only**: the assigned file was actually read; the retcode string is absent by document scope (architecture, not SDK header); the allow-list + optional HTTP proxy + never-log-credentials + execution-off rules that *cause and contain* 1012 are present.

---

## 5. Verdict

**PASS.** Architecture v2 specifies Achiever whitelist `81.29.145.69` and an optional HTTP proxy (`ACHIEVER_PROXY_*`, secrets not logged). It does not name `MT_RET_AUTH_MANAGER_IPBLOCK` / 1012, and that omission does not open a live capital-loss path.
