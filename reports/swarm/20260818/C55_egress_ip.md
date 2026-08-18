# C55 — Achiever egress IP `81.29.145.69` is non-secret

| Field | Value |
|---|---|
| Agent | C55 (senior engineer, classification document only) |
| Date | 2026-08-18 |
| Assigned | Achiever egress IP `81.29.145.69` is non-secret. Document. Write this report. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`) |
| Binding law | Architecture §§7, 8, 55, 56; A19 (live identifier FLAG vs secret); A58 (registry map); A76 (log keep); A54 (Windows egress / 1012) |
| Literal under classification | `81.29.145.69` |

Classification vocabulary is architecture §73.B. The **value class** of this IP is **`NON-SECRET`**. That is the standing order for this report.

---

## 0. Verdict

**`81.29.145.69` is not a secret.**

It is the documented Achiever **whitelisted outbound / expected egress** address. Architecture §7 prints it in the open as “Required whitelisted outbound IP.” Architecture §56 repeats it as `ACHIEVER_EGRESS_IP=81.29.145.69` inside the **Secret-Safe** example (the block that uses `<SECRET>` only for passwords). A58 marks the key **not secret** and **not a Connect argument**. A76 says **Keep** it in logs.

| Claim | Ruling |
|---|---|
| Is `81.29.145.69` a password / token / API key / connection-string secret? | **No.** |
| May reports, architecture, and operator docs print the literal? | **Yes.** |
| May logs / health / `LastError` mention it? | **Yes.** Do not redact it as if it were `MT5_PASSWORD`. |
| Must Connect send it as a Manager API argument? | **No.** The broker sees the TCP source IP (or the proxy’s source IP). |
| Is it required for `Connect` to *succeed*? | **Operationally yes** (Achiever will return **1012** if the presented source IP is not this whitelist). **Config-wise no** — A58: `ACHIEVER_EGRESS_IP` is documentation / `ExpectedEgressIp`, not a Connect field. |
| Does printing it leak Achiever Manager password, proxy user, or proxy password? | **No.** Those remain `<SECRET>`. |
| Does “non-secret” mean “advertise on a public marketing site”? | **No.** Need-to-know still applies to live venue identity (A19 FLAG). It does **not** reclassify the IP as a credential. |

Honest one-liner: **`81.29.145.69` is an operational identifier. Treat it like `MT5_SERVER=57.128.141.65`, not like `MT5_PASSWORD`.**

---

## 1. Binding quotes

Architecture §7 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`):

> Required whitelisted outbound IP:
>
> `81.29.145.69`
>
> If proxying is required, credentials must be in secret storage/environment variables.
>
> Never log proxy credentials.

The IP is listed **outside** the `MT5_PASSWORD=<SECRET>` block. The secret adjacent to egress is the **proxy credential pair**, not the address.

Architecture §56 (Secret-Safe Example Configuration):

```env
ACHIEVER_EGRESS_IP=81.29.145.69

# Optional proxy
ACHIEVER_PROXY_ENABLED=true
ACHIEVER_PROXY_HOST=81.29.145.69
ACHIEVER_PROXY_PORT=49527
ACHIEVER_PROXY_USERNAME=<SECRET>
ACHIEVER_PROXY_PASSWORD=<SECRET>
```

Secret-safe here means: **the example may contain this IP**. The same stanza still placeholders the only real secrets (`USERNAME` / `PASSWORD`).

A58 §4.1 (`D:\Prop\reports\swarm\20260818\A58_broker_registry.md`):

| §56 key | Secret? | Required to connect? | Maps to |
|---|---|---|---|
| `ACHIEVER_EGRESS_IP` | **no** | **no** | `ExpectedEgressIp` (`81.29.145.69`) — **not** a Connect argument |
| `ACHIEVER_PROXY_USERNAME` | **yes** | no | `Proxy.Username` |
| `ACHIEVER_PROXY_PASSWORD` | **yes** | no | `Proxy.Password` |

A76 §4.2 (`D:\Prop\reports\swarm\20260818\A76_log_redaction.md`):

| Key | Class | Log rule |
|---|---|---|
| `ACHIEVER_PROXY_HOST` / `PORT` / `ACHIEVER_EGRESS_IP` | **Non-secret** | **Keep** |
| `Mt5BrokerOptions.EgressIp` | non-secret | **Keep** |
| `Password` / `ProxyPassword` / `ProxyLogin` / `ApiKey` | **SECRET** | **Never log** |

A19 §4.1 lists `81.29.145.69` under **“Live identifiers present (non-secret)”**, same table as `57.128.141.65` and manager login `2027`. That FLAG is “this is a live venue identity in the architecture,” **not** “this is a committed password.”

---

## 2. What the IP is (and is not)

### 2.1 What it is

| Fact | Detail |
|---|---|
| Role | Achiever Manager **IP allow-list** for the Windows `mt5-worker` (or a SOCKS/HTTP proxy that **presents** this address). |
| Env key | `ACHIEVER_EGRESS_IP` |
| C# field (product) | `Mt5BrokerOptions.EgressIp` — comment: “Egress IP used for broker allowlisting documentation.” (`D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` lines 46–49) |
| Spec field (A58, not yet a separate type on disk) | `ExpectedEgressIp` — Achiever only; StarwaveFX has **no** egress key |
| Failure mode | Manager retcode **1012** `MT_RET_AUTH_MANAGER_IPBLOCK` (“IP address unallowed for manager”) |
| SDK mapping | `mt5_manager.cpp:64` — human string, **does not embed** `81.29.145.69` |
| Topology (A54 §7.2) | Prefer NIC/NAT whose default egress **is** this IP; else `ACHIEVER_PROXY_HOST=81.29.145.69` port `49527`. Do **not** SNAT Achiever through the Linux compose NAT. |

### 2.2 What it is not

- Not `MT5_PASSWORD`.
- Not `ACHIEVER_PROXY_USERNAME` / `ACHIEVER_PROXY_PASSWORD`.
- Not a Manager login (`2027`).
- Not a Connect / `IMTManagerAPI` parameter. The server observes the TCP source address.
- Not a reason to move Postgres / API / React onto Windows (A54, A105).
- Not a StarwaveFX constraint. StarwaveFX `ExpectedEgressIp` stays null (A58).
- Not malware, not a license, not a crack. It is a broker-side ACL entry we must match.

### 2.3 Same digits, two keys

`81.29.145.69` appears as **both**:

1. `ACHIEVER_EGRESS_IP` — expected public source the broker already allow-listed.
2. `ACHIEVER_PROXY_HOST` — optional hop **when** the worker cannot egress as that IP natively (architecture §56 sample has proxy **on**; committed `.env.example` has proxy **off** and empty host/port — B40).

The **host/IP string is still non-secret** in both keys. Enabling the proxy does **not** make the address a secret. Only `ACHIEVER_PROXY_USERNAME` and `ACHIEVER_PROXY_PASSWORD` become secrets in that path. Never log those (architecture §7).

---

## 3. Measured locations of the literal (this tree)

Read-only grep of `81.29.145.69` on 2026-08-18. **No product file was edited.**

| Surface | Path | How it appears | Secret? |
|---|---|---|---|
| Architecture §7 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md:382` | Required whitelist text | **No** |
| Architecture §56 | same file `:2047` | `ACHIEVER_EGRESS_IP=81.29.145.69` | **No** |
| Architecture §56 | same file `:2051` | `ACHIEVER_PROXY_HOST=81.29.145.69` | **No** (host) |
| Committed example env | `D:\Prop\.env.example:14` | `ACHIEVER_EGRESS_IP=81.29.145.69` | **No** |
| Product C# options | `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` | property `EgressIp` only — **no hardcoded IPv4** | n/a |
| `apps/mt5-worker` | `Program.cs` / `appsettings*.json` | **no** `EgressIp` / `ACHIEVER_EGRESS` bind | n/a |
| `apps/api` | no match | — | n/a |
| C++ Manager | `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp:64` | 1012 string, **no** IPv4 literal | n/a |
| Vendor header | `MT5APIConstants.h` | `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` | n/a |
| Swarm reports | A04, A07, A19, A26, A54, A56–A58, A63, A66, A75, A76, A105, B40 | operational docs | **No** |

**§55 / git:** presence of this IPv4 in architecture and `.env.example` is **not** a production-secret commit. B40 already scored `.env.example` as passing “no production secrets in Git” while **failing** A75’s stricter “placeholder every live identifier” content law. Those are different bars. C55 binds the **secret** bar: this IP does not belong behind `<SECRET>`.

---

## 4. How operators should treat it

| Action | Required |
|---|---|
| Put `ACHIEVER_EGRESS_IP=81.29.145.69` in Windows-only env / operator sheet | Yes (documentation + health / 1012 hint) |
| Place the worker so TCP to `57.128.141.65:443` **source-NATs as** `81.29.145.69`, **or** enable the §56 proxy that presents that address | Yes, or Connect stays 1012 |
| Pass the IPv4 into `MT5Manager::Connect(...)` | **No** |
| Redact it from logs as `***` | **No** (A76 Keep) |
| Redact proxy user/password if proxy is used | **Yes** |
| Commit `MT5_PASSWORD` or proxy password next to it | **Never** |
| Assume Linux compose egress equals this IP | **No** — that is the A54 high-risk 1012 path |

Suggested operator check (conceptual; this agent did **not** run a live Connect):

1. From the Windows worker, observe the source address of outbound TCP 443 to `57.128.141.65`.
2. It must be `81.29.145.69` (or the configured proxy must make the broker see that address).
3. If `GetLastError` / disconnect reason contains `MT_RET_AUTH_MANAGER_IPBLOCK` / 1012, fix **routing/NAT/proxy**, not the password slot.

---

## 5. Tension with A75 (resolved, not ignored)

A75 §2 tells `.env.example` authors to write `ACHIEVER_EGRESS_IP=<EGRESS_IP>` and says:

> Do **not** treat “non-secret” as “safe to publish our live account.”

C55 does **not** overturn A75’s *publish-placeholder* preference for a public example file. C55 **does** overturn any reading that:

- the IPv4 is a credential,
- the IPv4 must be redacted in logs,
- writing `81.29.145.69` in this swarm tree is a secret leak,
- later agents should refuse to quote the address.

A19-02 / A75 = **need-to-know for a public dump**.  
C55 / §7 / §56 / A58 / A76 = **class = non-secret**.

If a later agent rewrites `.env.example`, it may keep the literal (matches §56 and today’s file) or use `<EGRESS_IP>` (matches A75). Either choice is **not** a secrets decision. **Do not** replace the value with `<SECRET>`.

---

## 6. Product-source status (unchanged)

| Check | Result |
|---|---|
| This agent edited `D:\Prop\src` | **No** |
| This agent edited `D:\Prop\apps` / `mt5-sdk` / `.env.example` | **No** |
| `Mt5BrokerOptions.EgressIp` | Exists as optional documentation field; unset in worker appsettings |
| Hardcoded `81.29.145.69` under `src/` or `apps/` | **Absent** |
| Classification change required in C# | None for this task. Wiring `ExpectedEgressIp` is A58 work, out of scope. |

Worktree has unrelated dirty product files (API/FIX/MT5 workers, Infrastructure). C55 did not touch them.

---

## 7. Classification table (pin)

| Item | Class | Redact? | May appear in committed docs / `.env.example` / reports? |
|---|---|---|---|
| `81.29.145.69` | **NON-SECRET** operational identifier | No | **Yes** |
| `ACHIEVER_EGRESS_IP` | **NON-SECRET** env key | No | **Yes** |
| `ACHIEVER_PROXY_HOST` (same digits when used) | **NON-SECRET** host | No | **Yes** |
| `ACHIEVER_PROXY_PORT=49527` | **NON-SECRET** | No | **Yes** |
| `ACHIEVER_PROXY_USERNAME` | **SECRET** | Always | Placeholder only |
| `ACHIEVER_PROXY_PASSWORD` | **SECRET** | Always | Placeholder only |
| `MT5_PASSWORD` | **SECRET** | Always | Placeholder only |
| `MT5_SERVER=57.128.141.65` | **NON-SECRET** identifier (A19 FLAG) | No | Yes (same class family) |

---

## 8. Done criteria

- [x] Standing classification recorded: Achiever egress **`81.29.145.69` is non-secret**.
- [x] Distinguished from proxy/manager **secrets**.
- [x] Distinguished from A75 “don’t publish live account identity.”
- [x] Measured where the literal exists; product C# does not hardcode it.
- [x] Product source not modified.
