# D79 — `FixSessionsPage.tsx`: is the password shown?

| Field | Value |
|---|---|
| Agent | D79 (senior engineer, FIX page password-visibility only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:42:52+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Assigned | Read `FixSessionsPage.tsx`. Password shown? Write this file. **Do not modify product source.** |
| Target | `D:\Prop\apps\web\src\pages\FixSessionsPage.tsx` |
| Route | `/fix` via `App.tsx` `path="fix"` |
| Product source modified | **No.** This report (plus catalog notes in `INDEX.md` / `SWARM_LOG.md`) are the only writes. |
| Method | Full `read_file` of the page (26 physical lines). Grep `password` / `Password` / `secret` / `JSON.stringify` on the page. Cross-read `useFixSessions`, `FixSession` TS type, `FixSessionDto`, `GetFixSessionsAsync`, `FixSessionState`, `CTraderFixOptions.Password`, `Program.cs` `GET /api/fix/sessions`. PowerShell SHA-256 + bytes + physical-line + last-write. |
| Binding law | Architecture v2 §52 last line: `Never show FIX password.` Same family as §48 / §55 / §57 / §72.5. A94 (page DTO lock). D08 §7.10. D40 §6. |
| Prior | A94 (page missing at that write), B29 (DTO mismatch), D08 (page census), D38 (route), D39 (hook), D40 (secrets) |
| Measure HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **password-visibility census of one React page**. It is **not** a claim that `/fix` satisfies §52, that FIX Logon works, or that secrets never exist in env / options.

---

## 0. Verdict

**Password is not shown.**

`FixSessionsPage` never binds, prints, stringifies, or inputs a password. The only occurrence of the word is static copy: `Password is never shown.` That sentence is a disclaimer, not a secret value.

The page does **not** dump the API payload (`JSON.stringify` is **0** on this file). Contrast `SettingsPage` / `SystemHealthPage` / `ReconciliationPage`, which dump JSON (still no password field on those DTOs).

The server path that feeds this page (`GET /api/fix/sessions` → `EfDashboardQueries.GetFixSessionsAsync` → `FixSessionDto`) has **no password property**. Domain `FixSessionState` has **no password column**. `CTraderFixOptions.Password` exists as an options slot and is **not** mapped into the dashboard DTO.

§73.B for **password visibility on this page**: **EXISTS_AND_GOOD** (hard-law “never show” is honored).  
§73.B for the **page as a §52 cTrader FIX surface**: **EXISTS_NEEDS_REFACTOR** (one flattened card list; field-name drift vs TS type; unused DTO fields; `s: any`; empty list is a blank grid). That is **out of scope** of the assigned question.

| Question | Answer |
|---|---|
| Does the page render a password value? | **No** |
| Does the page have a password `<input>` / reveal toggle? | **No** (zero `<input>` / `<form>`) |
| Does the page `JSON.stringify` the session payload? | **No** |
| Does the word “Password” appear? | **Yes — disclaimer only** (line 8) |
| Does the TS `FixSession` type include `password`? | **No** |
| Does C# `FixSessionDto` include `Password`? | **No** |
| Does `FixSessionState` persist a password? | **No** |
| Could a future API field leak via this page without an edit? | **No** — only named properties are painted; extra JSON keys are ignored. `s: any` does **not** auto-print unknown keys. |
| Product source edited this pass | **0** |

---

## 1. Measured files

| Path | Bytes | Phys. lines | SHA-256 | Last write (local) | Git |
|---|---:|---:|---|---|---|
| `D:\Prop\apps\web\src\pages\FixSessionsPage.tsx` | 1312 | 26 | `EC93326688719E10D3ED5CB275D9BF1E7113C7F61EEA99803F42E1EA268BB886` | 2026-08-18T13:16:43+05:30 | **untracked** (`??`); git blob `4431d90448777b3795c6d3f79e9750866710db81` |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 2026-08-18T13:16:00+05:30 | unstaged (`M`) |
| `D:\Prop\apps\web\src\types\index.ts` | 2905 | 136 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2026-08-18T13:08:18+05:30 | (type only; no password field) |
| `D:\Prop\apps\web\src\App.tsx` | 2062 | 42 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` | 2026-08-18T13:20:38+05:30 | unstaged (`M`); mounts `/fix` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 3088 | 114 | `A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | 2026-08-18T13:34:59+05:30 | `FixSessionDto` 13 fields, no password |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 8708 | 205 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B6` | 2026-08-18T13:35:15+05:30 | `GetFixSessionsAsync` L162–184 |
| `D:\Prop\src\Domain\Entities\FixSessionState.cs` | 979 | 25 | `6C20D6A1BF5F84769DB483FD17A0EBEB8BDA8C1C56BBA2B8B30A59FCE44697E` | 2026-08-18T13:40:10+05:30 | no password column |
| `D:\Prop\apps\api\Program.cs` | 4731 | 95 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 2026-08-18T13:35:15+05:30 | L61 `MapGet("/api/fix/sessions", …)` |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | 2344 | 80 | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | 2026-08-18T13:12:48+05:30 | `Password` slot exists; **not** on this page path |

Page SHA matches D08 / B22 / C08 (`EC933266…`). Unchanged since last-write `13:16:43`. File is LF-only (26 `\n`, 0 CRLF).

This agent did **not** create, edit, or stage the page.

---

## 2. Full page (verbatim)

26 physical lines. Entire component:

```tsx
import { useFixSessions } from '../api/hooks';

export default function FixSessionsPage() {
  const { data = [] } = useFixSessions();
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-2">cTrader FIX</h1>
      <p className="text-sm text-gray-400 mb-4">QUOTE and TRADE are independent sessions. Password is never shown. TargetCompID stays <code>cServer</code>.</p>
      <div className="grid md:grid-cols-2 gap-4">
        {data.map((s: any) => (
          <div key={s.qualifier} className="bg-gray-800 border border-gray-700 rounded p-4 space-y-1 text-sm text-gray-200">
            <div className="text-lg font-semibold text-blue-300">{s.qualifier}</div>
            <div>{s.host}:{s.port}</div>
            <div>Connected: {String(s.connected)} · Logged on: {String(s.loggedOn)}</div>
            <div>Status: {s.status}</div>
            <div>Seq in/out: {s.inboundSeq} / {s.outboundSeq}</div>
            <div>Reconnects: {s.reconnectCount}</div>
            {s.bid != null && <div>Bid/Ask: {s.bid} / {s.ask} · age {Number(s.quoteAgeSeconds ?? 0).toFixed(1)}s</div>}
            <div>Instrument ID: {s.instrumentId ?? 'not discovered yet'}</div>
            <div>Execution enabled: {String(s.executionEnabled)}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## 3. Password-keyword hits on the page

Grep `password|passwd|pwd|secret|apiKey|JSON.stringify` (case-insensitive) under `D:\Prop\apps\web\src`:

| File | Hits | What |
|---|---:|---|
| `pages/FixSessionsPage.tsx` | **1** | Line 8 static copy: `Password is never shown.` |
| `pages/SettingsPage.tsx` | **1** | Line 8 static copy: `Secrets are never returned to the browser.` (plus a JSON dump of settings, which is **not** this page) |

No `s.password`, `s.Password`, `s.passwd`, `s.secret`, `s.apiKey`. No `type="password"`. No `JSON.stringify` on `FixSessionsPage`.

The `s: any` annotation is a type hole for the DTO/TS mismatch (see §6). It is **not** a dump of the object.

---

## 4. Allow-list of what **is** painted

Every JSX interpolation on the card:

| Painted | Source property | Secret? |
|---|---|---|
| Card title | `s.qualifier` | No (QUOTE / TRADE enum string) |
| Endpoint | `s.host` + `s.port` | **Identifier**, not a password |
| Connected | `s.connected` | Boolean derived from status |
| Logged on | `s.loggedOn` | Boolean derived from status — **not** credentials |
| Status | `s.status` | Enum name (`Disconnected`, `LoggedOn`, …) |
| Seq in/out | `s.inboundSeq` / `s.outboundSeq` | Sequence integers |
| Reconnects | `s.reconnectCount` | Integer |
| Bid / Ask / age | `s.bid` / `s.ask` / `s.quoteAgeSeconds` | Quote snapshot; gated on `bid != null` |
| Instrument ID | `s.instrumentId` | Venue id or `not discovered yet` |
| Execution enabled | `s.executionEnabled` | Boolean (query hard-codes `false`) |

Static chrome (not from API):

- H1: `cTrader FIX`
- Disclaimer: `QUOTE and TRADE are independent sessions. Password is never shown. TargetCompID stays cServer.`

`host:port` is a live-identifier surface (A19/B25/D40 FLAG set). Showing the gateway host is **not** showing the FIX password.

---

## 5. What the wire can even carry

### 5.1 Hook

`useFixSessions` (`hooks.ts` L35–37):

```ts
export function useFixSessions() {
  return useQuery({ queryKey: ['fix-sessions'], queryFn: () => client.get('/api/fix/sessions').then(r => r.data), refetchInterval: 5000 });
}
```

Unversioned `GET /api/fix/sessions`. Poll 5 s. No auth header (D40). Response is used as `data = []` then `.map`.

### 5.2 API

`Program.cs` L61:

```csharp
app.MapGet("/api/fix/sessions", (IDashboardQueries q, CancellationToken ct) => q.GetFixSessionsAsync(ct));
```

Anonymous. No `/api/v1`. Returns whatever `FixSessionDto` serializes to (System.Text.Json camelCase).

### 5.3 C# DTO (`DashboardModels.cs` L75–92)

13 members: `Qualifier`, `Host`, `Port`, `Connected`, `LoggedOn`, `Status`, `LastInbound`, `LastOutbound`, `InboundSeq`, `OutboundSeq`, `ReconnectCount`, `LastError`, `InstrumentId`, `Bid`, `Ask`, `QuoteAgeSeconds`, `ExecutionEnabled`.

**No `Password`. No `AccountId`. No `SenderCompId`. No `SecretConfigured`.**

The page also **does not paint** `lastInbound`, `lastOutbound`, or `lastError` (present on the DTO, unused in JSX).

### 5.4 Query (`EfDashboardQueries.cs` L162–184)

Maps `FixSessionState` + latest `DestinationQuote`. Last constructor arg is literal `false` (`ExecutionEnabled`). No options bind. `CTraderFixOptions.Password` is not referenced.

### 5.5 Entity (`FixSessionState.cs`)

Persists CompIDs, seq, status, host/port, owner, last error. **No password column.** CompIDs are **not** copied onto `FixSessionDto`, so they never reach this page.

### 5.6 Options slot (not on this path)

`CTraderFixOptions.Password` (L17–20) is documented `Must never be logged.` Default `string.Empty`. That slot is for a future initiator, not the dashboard. Absence of a UI leak today is **not** an allow-list control on the API (A94). For **this page**, there is nothing to render.

### 5.7 TS type (`types/index.ts` L73–92)

`FixSession` has `type`, `host`, `port`, `connected`, `loggedOn`, `inSequence`, `outSequence`, `lastHeartbeat`, `errors`, quote/trade extras. **No `password`.** The page ignores this type (`s: any`) and binds the C# camelCase names (`qualifier`, `inboundSeq`, …).

---

## 6. Related honesty (not the assigned question)

These are **not** password leaks. Recorded so this file is not read as “§52 page is done.”

| Gap | Evidence |
|---|---|
| §52 requires two named cards (QUOTE SESSION / TRADE SESSION) | Page is a `.map` over whatever the API returns. Empty `data` → blank grid, no placeholder cards. |
| §52 fields missing on the card | Last inbound/outbound, heartbeat/test, errors[], SSL port vs plain, spread, open orders/positions, last ER, last recon, `secretConfigured`. |
| Quote fields painted on every row | `instrumentId` and `executionEnabled` print on **every** card. Bid/ask only if `bid != null`, but the latest quote is attached to **all** sessions by the query. |
| TS type vs page vs DTO | `FixSession.type` vs `s.qualifier`; `inSequence` vs `inboundSeq`; `quoteAge` vs `quoteAgeSeconds`. Works only because of `any`. |
| Connected / Logged on can be a seeder lie | Query derives booleans from `FixSessionStatus`. D22: seeder can persist `LoggedOn` / `ReadyForMarketData` with zero socket. C14/C54 already flag this. Not a password. |
| Nav label | Sidebar is `FIX` (D38), H1 is `cTrader FIX`. |
| File not in HEAD | `?? apps/web/src/pages/FixSessionsPage.tsx`. Clean checkout of `398a142` does **not** have this page (A94’s “missing” is still true for HEAD). Worktree has it. |

---

## 7. Binding law (quoted)

Architecture v2 §52 last line (A94 quote):

> Never show FIX password.

Architecture §55 / §72.5: never expose FIX / cTrader account password to React.

This page complies: no password in JSX, no password in the DTO, no password in the entity, no JSON dump that could echo a future field without an explicit new binding.

---

## 8. What this agent did not do

- Did not edit `FixSessionsPage.tsx` or any other product file.
- Did not add a `secretConfigured` badge (would be a product change).
- Did not type the `s: any`.
- Did not claim FIX Logon, QuickFIX/n, or §52 completeness.
- Did not print any live password (none is reachable from this page).

---

## 9. One-line answer

**No. `FixSessionsPage` does not show a password.** The only “Password” text is the line-8 disclaimer; host/port and logged-on status are identifiers / booleans, not credentials.
