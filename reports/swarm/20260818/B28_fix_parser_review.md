# B28 — `FixMessageParser` + `FixSessionOwnership` review

| Field | Value |
|---|---|
| Agent | B28 (senior engineer, parser + TRADE-lease review only) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Product source modified | **No.** This report is the only write. |
| Scope | `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs` and `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs` |
| Adjacent (read, not scored as SUTs) | `Testing\FixSimulationHarness.cs`, `Configuration\CTraderFixOptions.cs`, `TraderIntelligence.Fix.CTrader.csproj`, `apps\fix-worker\Worker.cs`, `tests\Unit\**` |
| Authority | FIX 4.4 tag-10 / BodyLength rules; architecture §§25–34, 41–42, 61; A25 §3; A35; A46; A47; A68 §2.2; A86; A89 #60–74; A99 §6.4 |
| Method | Full read of both SUTs; `dotnet build` of `TraderIntelligence.Fix.CTrader.csproj`; SHA-256; repo-wide `grep` of type names; compare A89 claimed tests to `tests\` on disk |
| Classification vocabulary | architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` |

---

## 0. Verdict

**Both types compile. Neither is a production FIX adapter. Neither is an A46 lease.**

| SUT | Class | One-line |
|---|---|---|
| `FixMessageParser` | **EXISTS_NEEDS_REFACTOR** | Honest unit-test pipe codec. Checksum + BodyLength **build** are internally consistent. `Parse` is last-wins `Dictionary<int,string>` and **cannot** decode Security List / MD groups. Must not emit live outbound. |
| `FixSessionOwnership` + `InMemoryDistributedLockWithFencing` | **EXISTS_NEEDS_REFACTOR** and **UNSAFE if wired as the TRADE lock** | Correct *shape* (`owned && reconciled`). Process-local, racy, no renew, no expiry watch, reconcile survives a new fencing epoch. Must not ship as production ownership. |

`dotnet build D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj --no-incremental -p:WarningLevel=999 -p:Nullable=enable` this pass: **0 errors, 0 warnings.** B01’s downstream `CS1503` (`char` → `string` on `EndsWith` / `string.Join`) is **stale**. On `net8.0` both `string.EndsWith(char)` and `string.Join(char, IEnumerable<string>)` exist; the current file builds.

A89 rows 60, 61, 70, 71, 74 are marked **EXISTS**. On disk under `D:\Prop\tests` there is **no** `Fix\` folder and **no** `FixMessage*` / `FixSessionOwnership*` test class. Those names are a plan, not evidence. Do not treat checksum or lease behaviour as tested.

`apps\fix-worker\Worker.cs` references the `Fix.CTrader` assembly and **calls zero types from it**. Live `NewOrderSingle` remains off by absence + `RealCopyExecutionEnabled` default false. That is **vacuous safety**, not a gate implemented by these two classes.

B05’s tree snapshot is **partly stale**: `FixSessionOwnership.cs` hash still matches; `FixMessageParser.cs` does not (6016 B / 145 lines now vs B05’s 6042 B / 120 lines); current `.csproj` has **no** `PackageReference` (B05’s `QuickFix.Net` 1.8.0 pin is gone). A35 pin (`QuickFIXn.Core` + `QuickFIXn.FIX44` 1.14.1) is still **MISSING**.

---

## 1. Files measured (this pass)

| Path | Bytes | Lines | SHA-256 | Role |
|---:|---:|---:|---|---|
| `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs` | 6016 | 145 | `C58681E761D43052B53D2A8D00883C461A9E3CEB5B7DF8995D50F8155F710E3D` | SUT — pipe parse/build + tag 10 |
| `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs` | 4719 | 134 | `30029E29EE66C2114643AAF8FD0E0D8566C075A0FF693CCA7043CEADED5E6D20` | SUT — in-memory fence + local flags |
| `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` | — | 205 | `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616` | only product caller of `BuildFixMessage` |
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | — | 14 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | `net8.0`; Domain + Application only; **no QuickFIX** |

`grep` of `FixMessageParser` / `FixSessionOwnership` / `IDistributedLockWithFencing` / `ExecutionIntentsAllowed` / `MarkReconciled` over `D:\Prop\**\*.cs`:

| Location | Hits |
|---|---|
| `Parsing\FixMessageParser.cs` | definition |
| `Services\FixSessionOwnership.cs` | definition |
| `Testing\FixSimulationHarness.cs` | `new FixMessageParser()` + `BuildFixMessage` |
| `tests\**` | **none** |
| `apps\**` | **none** (assembly reference only) |

---

## 2. `FixMessageParser` — codec review

Class comment is honest: *“Lightweight FIX 4.x parser/builder intended for unit tests. Input examples use `|` instead of SOH.”* Keep that scope. Do not grow this into an engine. Architecture §25–§27 / A35: production codec is **QuickFIX/n + cTrader DD**, not this type.

### 2.1 What is actually correct

| Behaviour | Evidence | Score |
|---|---|---|
| Tag 10 required last on `Parse` | `parts[^1]` must `StartsWith("10=")` | PASS (for the pipe dialect) |
| Checksum = ASCII byte sum mod 256, three digits | `ComputeChecksumFromRaw` → `sum % 256` → `"D3"` | PASS (FIX trailer) |
| Build checksum includes the SOH after the last body field | `body = JoinSohFields(...) + SohChar`; checksum of `header + body` | PASS |
| BodyLength = bytes after `9=N<SOH>` up to but not including `10=` | `bodyLen = Encoding.ASCII.GetByteCount(body)` where `body` is `35=…` … + trailing SOH | PASS **when tag 35 is present** |
| Build strips caller 9 and 10 and recomputes them | `.Where(kv => kv.Key != 9 && kv.Key != 10)` | PASS |
| Build requires tag 8 | `FirstOrDefault` + `Key != 8` throws | PASS |
| Empty / whitespace input rejected | `IsNullOrWhiteSpace` → `ArgumentException` | PASS |
| Segment without `=` / non-numeric tag rejected | `eqIdx <= 0` / `int.TryParse` → `FormatException` | PASS |
| Checksum mismatch rejected | string-equal to computed `D3` | PASS |
| Stateless instance | no mutable fields | PASS (thread-safe as a helper) |

Round-trip `Parse(Build(fields))` is internally consistent **for unique tags** because Build emits `|`, Parse splits on `|`, and checksum is recomputed from the same SOH-rejoined fields.

`10=7` vs expected `007`: `int.TryParse` succeeds, then `string.Equals("7", "007")` fails. Three-digit trailer is enforced by the compare, not by the parse. That is the right outcome.

### 2.2 P0 — last-wins map is not a FIX decoder

```csharp
var tags = new Dictionary<int, string>(capacity: parts.Length);
// ...
tags[tag] = val;
return tags;
```

Repeating groups **collapse**. Official cTrader / FIX 4.4 messages that this repo must eventually ingest:

| MsgType | Repeating tags | What `Parse` keeps |
|---|---|---|
| `35=W` Snapshot / `35=X` Incremental | `268` then many `269`/`270`/`271` | last `269`, last `270`, last `271` |
| `35=y` SecurityList | `146` then many `55`/`1007`/`1008` | last instrument only |
| `35=AP` PositionReport (and similar) | group NoPositions / NoRelatedSym | last member |

A68 and A86 already named this. It is a **codec defect**, not a mapping defect. §61 MD replay and §30 instrument discovery **cannot** use this dictionary as the decode target.

The returned object is a mutable `Dictionary` boxed as `IReadOnlyDictionary`. A caller can cast and mutate. For a test helper this is a smell, not a venue bug.

**Required shape (when someone next touches this file):** ordered `IReadOnlyList<(int tag, string value)>` (or QuickFIX `Message`). Keep `ToLastWinsMap()` as an explicit, documented lossy view for single-value header/ER tests.

### 2.3 P0 — must not send `BuildFixMessage` on the wire

After `8=` / computed `9=` / `35=`, remaining tags are **`OrderBy(kv => kv.Key)`**.

Harness ER tags `8,35,49,56,57,50,11,37,55,150,39,60` therefore emit approximately:

```text
8=FIX.4.4|9=…|35=8|11=…|37=…|39=…|49=…|50=…|55=…|56=…|57=…|60=…|150=…|10=…
```

Official required order (A25 §3.5 / RoE / FAQ): **`8, 9, 35`, then header `49,56,57,50,34,52`, then body.** Wrong order, missing tags, bad checksum, or non-UTC time → **no response**. `34` and `52` are not inserted unless the caller passed them; the harness does not.

BodyLength / checksum of that sorted body are self-consistent. That only proves the arithmetic, not RoE.

**Law:** QuickFIX/n computes 9 and 10 and owns field order. This builder is a **fixture writer**. Using it as a live outbound codec is **UNSAFE**.

### 2.4 P1 — `Parse` is weaker than its comments

| Claim / expectation | Measured |
|---|---|
| A68: “Accepts `|` or can be fed SOH-normalized text” | **False.** `Split(SeparatorChar)` only. A real `8=FIX.4.4\x01…\x0110=000\x01` is one part; last field is not `10=…`; `FormatException`. Caller must replace SOH → `\|` first. |
| Validates BodyLength (tag 9) | **No.** Wrong or missing `9=` still parses if tag 10 matches the **rebuilt** SOH join. |
| Requires BeginString / MsgType | **No.** Only tag 10 last. `1=hi\|10=…` with a matching checksum parses. |
| Validates `8` first, `9` second, `35` third | **No.** |
| Checksum over original bytes | **No.** `Trim()`, drop trailing `\|`, `RemoveEmptyEntries`, then rejoin with SOH. `8=FIX.4.4\|\|35=A\|10=…` silently drops the empty segment and checksums a *different* message. |
| Accepts live TCP frames | **No.** Pipe dialect only. Correct: do not put this on a `NetworkStream`. |

`RemoveEmptyEntries` plus whole-message `Trim()` are convenience for handwritten fixtures. They are **wrong** for a wire decoder (empty fields and leading/trailing SOH are significant).

### 2.5 P2 — smaller codec holes

- `int.TryParse` on tags and on tag 10 uses current-culture default styles. Tags are ASCII digits; use `NumberStyles.None` + `CultureInfo.InvariantCulture`. `+10` / leading spaces can become a tag today.
- Tag `0=x` is accepted (`eqIdx > 0` and `int.TryParse("0")`).
- Values may contain `=`; first `=` wins. Values may not contain `|` (the test delimiter). Real FIX forbids SOH in values; there is no check for embedded SOH after a future SOH-normalize.
- `Encoding.ASCII` maps non-ASCII to `0x3F` (`?`). BodyLength and checksum would then disagree with a Latin-1/UTF-8 wire. cTrader FIX is ASCII; still pin the encoding in comments.
- Duplicate non-group tags silently last-win. No `duplicate tag` error for header/trailer (8/9/34/35/10).
- Mid-message `10=` is stored then overwritten by the real trailer; it is still included in the checksum as a normal field. Bizarre fixtures can “pass” checksum and confuse readers.
- `BuildFixMessage`: missing 35 is allowed; body is remaining tags + SOH. Invalid FIX, self-consistent length/checksum.
- Duplicate 8/35 in the input list: first wins, extras dropped from `remaining`. Other duplicate tags are **emitted twice** (list, not map). `Parse` of that output last-wins. Round-trip is not byte-identical.
- Null `kv.Value` on build: `StringBuilder.Append(null)` is a no-op → `35=` empty. Fine for a helper; document it.
- Returned map is not frozen.

### 2.6 What this class must never become

Do **not**:

- add `TcpClient` / `SslStream` framing here (architecture: no hand-rolled engine);
- parse `35=y` / `35=W` / `35=X` into this dictionary and claim instrument or book coverage;
- replace QuickFIX/n with more methods on this type.

Do:

- keep it as the **pipe fixture** helper for single-value messages (Logon, Logout, Heartbeat, simple ER without groups);
- add `FixFieldList` (or switch tests to QuickFIX `Message` + `FIX44-CSERVER.xml`) before any SecurityList / MD test is written;
- write the three A89 classes that do not exist (`FixMessageParseBuildTests`, `FixChecksumValidationTests`, `FixParserRejectsGarbageTests`) against the **current** contract, including an explicit test that a two-entry MD group **does not** survive `Parse` until the API changes.

---

## 3. `FixSessionOwnership` — lease review

Comments on the type are honest: production lock “should be backed by Redis and return a monotonically increasing fencing token”; in-memory is “fallback for development/unit tests.” A99: *“must not ship as production ownership.”* That is still the binding rule.

### 3.1 Shape that is worth keeping

```text
ExecutionIntentsAllowed = _hasOwnership && _reconciled
```

That conjunction is the right *local* gate (A46 `may_send` is stricter: DB token + Redis key + PTTL + persist-before-send + risk flags). `MarkReconciled` as a separate step after acquire is the right *sequence* if reconcile actually ran.

`ReleaseAsync` no-ops when `!_hasOwnership` (does not `DEL` with a guessed token). `Release` on the in-memory lock checks `ownerId` **and** `fencingToken` before delete. Cancellation is observed at the start of try-acquire / release. Constructor null-checks provider / owner / key.

None of that makes it A46.

### 3.2 A46 scorecard (fail closed)

| A46 / §28 rule | Measured | Score |
|---|---|---|
| One TRADE owner per `(venue, account, qualifier)` across **processes** | `ConcurrentDictionary` in one process | FAIL |
| Postgres **mints** the fence; Redis only echoes | `Interlocked.Increment(ref _globalToken)` per lock **instance** | FAIL |
| Redis key `ti:fix:lease:{env}:{broker}:{account}:{qual}` | caller-supplied `lockKey`; no grammar | FAIL |
| TTL 10 s, renew ≤ ⅓, min remaining 2 s | one-shot `expiresAt`; **no Renew** | FAIL |
| Release / yield increments the token | `TryRemove` only; token unchanged | FAIL |
| Steal only if DB expired **and** Redis absent | local clock `expiresAt <= UtcNow` | FAIL |
| Fail closed if Redis/DB down | in-memory always “up” | FAIL |
| `0` means never acquired on a send path | failed acquire **copies the winner’s token** into `_fencingToken` | FAIL |
| Reconcile after **this** epoch’s token | `_reconciled` not cleared on re-acquire | FAIL |
| Wired to TRADE send / persist-before-send | **zero callers** | VACUOUS |
| Tests A89 #70 / #71 / #84 | **files absent** | FAIL |

Two `fix-worker` processes each construct `new InMemoryDistributedLockWithFencing()` and both acquire. cTrader then copies every `35=8` to both sockets (official FAQ / architecture §1.9). That is the split-brain this type exists to prevent, and it does not.

### 3.3 P0 defects in the in-memory lock (even as a test double)

**R1 — check-then-set is not a lock.**

```csharp
_locks.TryGetValue(lockKey, out var current);
var expired = current.expiresAt != default && current.expiresAt <= now;
if (!expired && current.ownerId != null)
    return Task.FromResult((false, current.fencingToken));
var fencing = Interlocked.Increment(ref _globalToken);
_locks[lockKey] = (ownerId, fencing, now.Add(ttl));
return Task.FromResult((true, fencing));
```

Two threads can both observe empty/expired, both increment, both write. Both return `acquired: true` with **different** tokens. Last indexer write wins. This is split-brain **inside one process**. A correct test double uses `AddOrUpdate` / `TryUpdate` and only reports acquire if the stored `(owner, token)` is the one just written.

**R2 — same owner cannot renew; heartbeat would drop the local flag.**

If the key is live, **every** caller — including the current `ownerId` — gets `(false, current.fencingToken)`. `AcquireAsync` then does:

```csharp
_hasOwnership = acquired;   // false
_fencingToken = fencing;    // the token they already held, or the winner's
```

A renew/heartbeat that re-calls `AcquireAsync` **clears `HasOwnership`** while the dictionary still holds the lease. `ReleaseAsync` then returns immediately (`if (!_hasOwnership) return`) → **lease leak until TTL**. There is no `Renew` API (A46 Lua `PEXPIRE` only).

**R3 — lease expiry is invisible to the wrapper.**

`FixSessionOwnership` never re-reads the dictionary. After TTL another caller can acquire; this instance still has `_hasOwnership == true` and, if they called `MarkReconciled`, **`ExecutionIntentsAllowed` stays true**. That is the GC-pause / expired-key split-brain A46 exists to kill.

**R4 — reconcile flag survives a new epoch.**

`AcquireAsync` does not set `_reconciled = false`. `ReleaseAsync` does, but expiry does not go through `ReleaseAsync`. Sequence:

1. acquire token 1, `MarkReconciled`, intents allowed;
2. TTL expires (no local update);
3. same instance calls `AcquireAsync` again, dictionary empty/expired, token 2 granted;
4. `_reconciled` still true → **intents allowed on a new fence without re-reconcile.**

A46 / A47: every new token is a new epoch. Positions / working orders must be rebuilt before `ready_for_execution`.

**R5 — `MarkReconciled` is honor-system.**

No check that `_hasOwnership`. Call it first, then acquire → intents allowed with **zero** PositionReport / `35=AF` / persist proof. The boolean is not reconciliation.

**R6 — release delete is not compare-and-remove.**

```csharp
if (_locks.TryGetValue(...) && owner && token match)
    _locks.TryRemove(lockKey, out _);   // removes whatever is there now
```

After the check, a third party can expire-steal and store a new tuple; `TryRemove(key)` deletes the **new** owner. Use `TryRemove(KeyValuePair)` (value-sensitive). A46 release Lua: `DEL` only if `instance_id` **and** token match.

**R7 — failed acquire pollutes `FencingToken`.**

Loser stores the winner’s token. A46 value object: `0` means never acquired; never send with a defaulted/foreign token. Logging / metrics that read `FencingToken` after a failed acquire will attribute the wrong epoch.

### 3.4 P1 / API placement

- Nested `IDistributedLockWithFencing` inside the concrete class is an awkward DI surface. Port belongs in Application (or a small `Fix.Abstractions` contract). Redis + Postgres implementation belongs in **Infrastructure**, not `Fix.CTrader` (B05 §1.4 — still correct).
- `InMemoryDistributedLockWithFencing` in the **product** assembly is a footgun. After a real lease exists, move the double to `tests/` or `Fix.CTrader.Testing`.
- `AcquireAsync` is try-once. Name says acquire; it does not wait, retry, or throw on loss. Standby backoff (A46 1 s, cap 5 s) is absent.
- `ttl` / empty `ownerId` / empty `lockKey` are not validated. `TimeSpan.Zero` or negative ⇒ `expiresAt` already in the past ⇒ next caller steals immediately.
- Wrapper fields `_hasOwnership`, `_reconciled`, `_fencingToken` are unsynchronized. Concurrent `Acquire` / `Release` / `MarkReconciled` can tear. Fine only if a single worker loop owns the instance.
- No session qualifier on the type. TRADE vs QUOTE is entirely the caller’s `lockKey`. Owning QUOTE must never authorize TRADE sends (A46 §0). Nothing here prevents that.
- Domain `FixSessionState.OwnerHeld` / `OwnerInstance` are a **separate** pair of flags with no fencing token. Worker stamps session **LoggedOn** every 15 s without this type. Two uncoordinated “owner” stories.

### 3.5 What a correct next implementation is (do not “fix” the dictionary)

Do **not** harden `InMemoryDistributedLockWithFencing` into a production lock (no Redlock, no “good enough” memory lease).

Keep:

- the `owned && reconciled` conjunction;
- try-acquire returning `(acquired, fencingToken)`;
- release requiring owner + token.

Replace the provider with A46:

1. Postgres `fix_session_leases` mint (`UPDATE … RETURNING` increment);
2. Redis bind Lua on `ti:fix:lease:{session_key}` with the minted token;
3. renew loop `PEXPIRE` only; lose lease ⇒ drop socket, `_hasOwnership=false`, `_reconciled=false`;
4. release increments fence (DB) and `DEL`s Redis only on match;
5. every persist-before-send carries the token and is rejected if stale;
6. `TRADE_OWNERSHIP_ALLOW_DB_ONLY` stays **false**.

The in-memory type may remain as a **unit-test fake** once it has CAS acquire, value-sensitive release, same-owner renew, and epoch reset of `_reconciled`. Write A89 #70 / #71 / #84 against that fake **and** against a contract test the Redis/Postgres impl must pass.

---

## 4. Tests claimed vs tests on disk

| A89 id | Claimed class | Pri | A89 status | On disk 2026-08-18 |
|---|---|---|---|---|
| 60 | `FixMessageParseBuildTests` | P0 | EXISTS | **MISSING** |
| 61 | `FixChecksumValidationTests` | P0 | EXISTS | **MISSING** |
| 70 | `FixSessionOwnershipLeaseTests` | P0 | EXISTS | **MISSING** |
| 71 | `FixSessionOwnershipFencingTokenTests` | P1 | EXISTS | **MISSING** |
| 74 | `FixParserRejectsGarbageTests` | P2 | EXISTS | **MISSING** |
| 84 | `FixSessionReadyForExecutionGateTests` | P0 | EXISTS | **MISSING** |

`D:\Prop\tests\Unit` product tests: `BaselineScorerTests`, `ExecutionAndSizingTests`, `RiskEngineTests`, `SymbolNormalizerTests`, `TradeReconstructionTests`, `VolumeConverterTests`, leftover `UnitTest1`. No FIX codec or lease coverage.

A89 **EXISTS** on these rows is **false**. Treat as a backlog, not a green suite.

---

## 5. Stale sibling notes (so the next agent does not re-open them)

| Note | Status after this pass |
|---|---|
| B01: unit tests fail `FixMessageParser` CS1503 | **Stale.** Current `net8.0` build of Fix.CTrader is 0/0. |
| B05: parser SHA `3E2C30C8…`, 6042 B, 120 lines | **Stale.** Now `C58681E7…`, 6016 B, 145 lines (comments + BodyLength SOH note). Behaviour of `Parse`/`Build` still last-wins + sorted remaining. |
| B05: `.csproj` has `QuickFix.Net` 1.8.0 | **Stale.** Current csproj has **no** package refs. A35 pin still missing. |
| B05 / A99 / A68 on ownership | **Still accurate.** Same SHA `30029E29…`. In-memory, not A46. |
| A05 (`Class1` only) | **Stale** (already superseded by B05). |
| A68 “SOH-normalized text accepted” | **Wrong** unless the caller replaces SOH first. |

---

## 6. Do / do not

**Do**

- Keep `FixMessageParser` as a documented **pipe fixture** helper for unique-tag messages.
- Keep the ownership **conjunction** and the `(acquired, fencingToken)` port idea.
- Put QuickFIX/n 1.14.1 + `FIX44-CSERVER.xml` on the session path (A35 / A36); engine owns 9/10/order.
- Implement A46 in Infrastructure (Postgres mint + Redis echo). Reset reconcile on every new token.
- Add the six missing unit classes before claiming codec or lease coverage.

**Do not**

- Send `BuildFixMessage` output to `*.c-trader.com`.
- Decode `35=y` / `35=W` / `35=X` through `Dictionary<int,string>` and call it a book or a symbol list.
- Register `InMemoryDistributedLockWithFencing` in `fix-worker` DI.
- Treat `MarkReconciled()` as proof of A47.
- Treat A89 EXISTS or a clean compile as “parser/lease done.”
- Hand-write a second FIX engine around this parser.

---

## 7. Residual risk if someone wires these types tomorrow

| Risk | Severity | Why |
|---|---|---|
| Live outbound with sorted tags, no 34/52 | P0 venue | Silent no-response or reject; not a fill |
| MD / SecurityList via `Parse` | P0 book | Last bid/ask or last symbol only |
| In-memory lock as TRADE owner | P0 money | Two processes both send; cTrader duplicates ERs |
| `ExecutionIntentsAllowed` after TTL / new epoch | P0 money | Local flags lie |
| Dashboard / worker `LoggedOn` without these types | P1 ops | Already true (`Worker.cs` 15 s stamp) — orthogonal lie |

**Current production send path: still absent.** Residual money risk from these two files is **latent** until a caller uses them. The correct control is to never call `BuildFixMessage` or the in-memory lock from a host; not to polish them in place.

---

## 8. Bottom line

`FixMessageParser` is a **small, compiling, checksum-competent test codec** with a fatal decode model (last-wins map) and a fatal send model (sorted tags, no session header discipline). `FixSessionOwnership` is a **local boolean pair** in front of a racy process dictionary. Architecture still requires QuickFIX/n sessions and a Redis+Postgres fenced lease. These files are seeds, not those things.

**Reviewer verdict: FAIL as production FIX parser / FAIL as production session ownership. PASS only as named test helpers with the defects above recorded.**
