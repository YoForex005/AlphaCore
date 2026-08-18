# D27 — `FixMessageParser` (pipe codec, not a FIX engine)

| Field | Value |
|---|---|
| Agent | D27 (senior engineer, parser only) |
| Date | 2026-08-18 |
| Assigned | Read `FixMessageParser.cs`. Write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D27_parser.md` |
| SUT | `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs` |
| Product source modified | **No.** This report is the only product-adjacent write. Eval harness lives under `reports/` only. |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` |
| Method | Full read of the 145-line SUT; SHA-256 + byte/line counts; `git diff` vs HEAD; repo-wide `grep` of `FixMessageParser` / `BuildFixMessage`; independent ASCII checksum arithmetic; `dotnet run` of `_tmp_d27_parser` against the current project (not a stale DLL). |
| Classification | architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` |
| Siblings | B28 (parser+lease, static), D05 (Fix.CTrader census), A68 §2.2, A25 §3.5, A35, A36, A86, A89 #60/#61/#74, C19 |
| Eval log | `D:\Prop\reports\swarm\20260818\_tmp_d27_parser\stdout.txt` |

**Honesty rule:** a compiling checksum helper is not QuickFIX/n. A last-wins `Dictionary<int,string>` is not a Security List or a book. `Parse(Build(x))` matching for unique tags is not RoE. A89 `EXISTS` is a plan row, not a green test.

---

## 0. Verdict

**`FixMessageParser` is a small, internally consistent pipe/`|` fixture codec. It is not a production FIX parser and must not emit live outbound.**

| Slice | Class | One line |
|---|---|---|
| Type as a **unit-test pipe helper** for unique-tag messages | **EXISTS_NEEDS_REFACTOR** | Checksum = ASCII sum mod 256, `D3`. Build 9/10 arithmetic is self-consistent **after** `Parse` drops the extra empty field. |
| Type as a **wire / SOH decoder** | **UNSAFE** if so used | Split is `|` only. Raw SOH frames throw `FormatException` (“missing checksum”). |
| Type as an **MD / SecurityList / PositionReport decoder** | **UNSAFE** | `tags[tag] = val` last-wins. Official `268=2\|269=0\|270=1.10\|269=1\|270=1.20` keeps **only** `269=1` / `270=1.20`. |
| Type as a **live outbound builder** | **UNSAFE** | Remaining tags `OrderBy(Key)`. Every `BuildFixMessage` emits `\|\|10=`. No `34`/`52` unless the caller passed them. Not RoE order. |
| Official QuickFIX/n + `FIX44-CSERVER.xml` | **MISSING** | Not this type’s job. A35 still absent (D05/C19). |
| A89 #60 / #61 / #74 on disk | **MISSING** | `tests/Unit/Fix` does not exist. Zero `FixMessageParser` hits under `tests/`. |

`dotnet run` of `_tmp_d27_parser` (Release, project-reference to current `Fix.CTrader`): eval compiled and executed. Product source was not edited.

Worktree SHA matches B28/C19/C43/D05: **`C58681E761D43052B53D2A8D00883C461A9E3CEB5B7DF8995D50F8155F710E3D`**. The only unstaged delta vs HEAD is dropping `StringComparison.Ordinal` from `EndsWith(char)` (the B01 `CS1503` fix). Behaviour of last-wins + sorted remaining is unchanged.

---

## 1. Files measured (this pass)

| Path | Bytes | Lines (all) | Non-blank | SHA-256 | Role |
|---|---:|---:|---:|---|---|
| `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs` | 6016 | 145 | 120 | `C58681E761D43052B53D2A8D00883C461A9E3CEB5B7DF8995D50F8155F710E3D` | SUT |
| `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` | 8970 | 205 | — | `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616` | only product caller of `BuildFixMessage` |
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | 419 | 14 | — | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | net8.0; Domain+Application; **no** package refs |

`LastWriteTimeUtc` of the SUT: `2026-08-18T07:49:07.1579602Z`.

`git status --porcelain` (pre-existing, **not** this agent):

```text
 M src/Fix.CTrader/Parsing/FixMessageParser.cs
 M src/Fix.CTrader/Testing/FixSimulationHarness.cs
 M src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj
```

`git diff` of the parser vs HEAD is **one** line:

```diff
-        normalized = normalized.EndsWith(SeparatorChar, StringComparison.Ordinal)
+        normalized = normalized.EndsWith(SeparatorChar)
```

HEAD still has unofficial `<PackageReference Include="QuickFix.Net" Version="1.8.0" />`. Worktree deleted that line. Neither is QuickFIX/n 1.14.1.

`grep` of `FixMessageParser` / `BuildFixMessage` over product `*.cs`:

| Location | Hits |
|---|---|
| `Parsing\FixMessageParser.cs` | definition |
| `Testing\FixSimulationHarness.cs` | `new FixMessageParser()` + `BuildFixMessage` |
| `apps/**` | **none** (assembly reference on `fix-worker` only) |
| `tests/**` | **none** |

---

## 2. What the type actually is

```8:15:D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs
/// <summary>
/// Lightweight FIX 4.x parser/builder intended for unit tests.
/// Input examples use '|' instead of SOH for field separators.
/// </summary>
public sealed class FixMessageParser
{
    private const char SeparatorChar = '|';
    private const char SohChar = '\u0001';
```

The class comment is **honest**. Keep that scope. Public surface:

| Member | Signature | Mutates instance? |
|---|---|---|
| `Parse` | `IReadOnlyDictionary<int,string> Parse(string fixPipeDelimited)` | no |
| `BuildFixMessage` | `string BuildFixMessage(IEnumerable<KeyValuePair<int,string>> fields)` | no |
| `JoinSohFields` | private static | — |
| `ComputeChecksum` | private static, rebuilds SOH join + trailing SOH | — |
| `ComputeChecksumFromRaw` | private static, ASCII bytes, `sum % 256`, `"D3"` | — |

No fields, no interface, sealed. Thread-safe as a helper. Not a session. Not a framer. Not a dictionary-aware codec.

---

## 3. Independent checksum proof (not “the class said so”)

FIX tag 10 = sum of every byte **up to but not including** `10=` , mod 256, three ASCII digits.

Heartbeat that `BuildFixMessage({8=FIX.4.4, 35=0})` intends:

```text
SOH raw (what Build checksums):  8=FIX.4.4<SOH>9=5<SOH>35=0<SOH>
pipe that a faithful transliteration would emit: 8=FIX.4.4|9=5|35=0|10=163
```

Byte sum (ASCII):

| Segment | Bytes | Running sum |
|---|---|---:|
| `8=FIX.4.4` | 56+61+70+73+88+46+52+46+52 | 544 |
| SOH | 1 | 545 |
| `9=5` | 57+61+53 | 716 |
| SOH | 1 | 717 |
| `35=0` | 51+53+61+48 | 930 |
| SOH | 1 | **931** |

`931 % 256 = 163`. BodyLength of `35=0<SOH>` = **5**. Both match the class.

**What Build actually returned (eval):**

```text
8=FIX.4.4|9=5|35=0||10=163
```

Note the **double pipe** before tag 10. Cause, lines 101–111:

```csharp
var body = JoinSohFields(bodyFields) + SohChar;          // already trailing SOH
var withoutChecksum = header + body;                     // ...35=0<SOH>
var checksum = ComputeChecksumFromRaw(withoutChecksum);  // correct FIX trailer input
return withoutChecksum.Replace(SohChar, SeparatorChar) + $"|10={checksum}";
//                          ^^^^^^^^ already ends with '|'      ^^^ extra '|'
```

Checksum arithmetic is right. The pipe dialect is **not** a 1:1 SOH↔`|` map. Naively replacing `|` back to SOH produces `<SOH><SOH>10=` — an extra empty field on the wire. `Parse` hides this with `StringSplitOptions.RemoveEmptyEntries` (see §4). Round-trip of unique tags still works. Exact-string fixture asserts of `…35=0|10=163` will **fail** against current `Build`.

Eval row `FAIL Build_HB` is that exact mismatch. It is a **codec-dialect defect**, not an eval bug.

---

## 4. `Parse` — measured contract

Order of operations (lines 23–64):

1. `IsNullOrWhiteSpace` → `ArgumentException`.
2. `Trim()`; drop **one** trailing `|` if present.
3. `Split('|', RemoveEmptyEntries)`.
4. Last part must `StartsWith("10=")` else `FormatException`.
5. `int.TryParse(last.AsSpan(3))` else `FormatException` (“not numeric”).
6. Rebuild `string.Join(SOH, parts[0..^1]) + SOH`, compare tag-10 **string** to `sum%256` `"D3"`. Mismatch → `InvalidOperationException`.
7. **Then** split each part on first `=`, `int.TryParse` the tag, `tags[tag] = val` (last-wins).

### 4.1 Eval results (`_tmp_d27_parser`)

| Case | Result | Exception / value |
|---|---|---|
| `Parse("")` / whitespace | reject | `ArgumentException` (“cannot be null/empty”) |
| missing `10=` | reject | `FormatException` (“missing checksum”) |
| `10=abc` | reject | `FormatException` (“not numeric”) |
| `10=000` on a 163 body | reject | `InvalidOperationException` (“Expected 163, got 000”) |
| `10=0` when expected `000` | reject | string compare; three-digit trailer is enforced by **equals**, not by `TryParse` |
| raw SOH frame (no `\|`) | reject | `FormatException` missing checksum — **A68 “SOH-normalized text accepted” is false** |
| trailing `\|` on a valid message | accept | dropped before split |
| `9=999` with checksum of the **rebuilt** join | **accept** | `9=999`, `10=025`. **BodyLength is not validated.** |
| `8=FIX.4.4\|\|35=0\|10=<sum of collapsed fields>` | **accept** | empty segment discarded; checksum of a *different* message |
| `1=hi\|10=<ok>` | **accept** | tag 8 / 35 **not** required |
| `0=x\|10=<ok>` | **accept** | tag 0 is a legal `int` |
| `+10=x\|10=066` | **accept** | `int.TryParse("+10")` → tag 10; trailer overwrites; `count=1` |
| `58=a=b=c` | accept | first `=` wins; value `a=b=c` |
| `8=FIX.4.4\|bad\|10=000` | reject **checksum first** | `InvalidOperationException` Expected 073, not `FormatException` “invalid field” |
| `8=FIX.4.4\|abc=1\|10=000` | reject **checksum first** | Expected 182. Field-shape runs **after** tag 10. |
| mid-message `10=999` then real trailer | accept | trailer overwrites; mid `10=` still participates in the sum |
| return type | `Dictionary<int,string>` | boxed as `IReadOnlyDictionary`; eval **cast-mutated** tag 999 |

### 4.2 P0 — last-wins is not a decoder

Eval, official-shaped snapshot group:

```text
8=FIX.4.4|9=XX|35=W|268=2|269=0|270=1.10|269=1|270=1.20|10=<ok>
→ count=7  268=2  269=1  270=1.20
```

The bid (`269=0` / `270=1.10`) is gone. Same collapse for:

| MsgType | Repeating tags | What survives |
|---|---|---|
| `35=W` / `35=X` | `268` then many `269`/`270`/`271` | last MD entry |
| `35=y` SecurityList | `146` then many `55`/`1007`/`1008` | last instrument |
| `35=AP` PositionReport | NoPositions / NoRelatedSym | last member |

A86 / A68 already named this. It is a **codec model** defect. §61 MD replay and §30 discovery **cannot** use this dictionary.

Required shape when someone next touches the file: ordered `IReadOnlyList<(int tag, string value)>` (or QuickFIX `Message`). Keep `ToLastWinsMap()` as an explicit lossy view for header/ER unit tests.

### 4.3 P1 — comments vs measured

| Claim | Measured |
|---|---|
| “Validates checksum (tag 10)” | **True** (rebuilt SOH join, not original bytes) |
| Accepts `|` **or** SOH-normalized text (A68) | **False.** `|` only. |
| Validates BodyLength (9) | **False.** |
| Requires `8` first, `9` second, `35` third | **False.** Only “`10=` last”. |
| Checksum over original bytes | **False.** `Trim` + drop trailing `\|` + drop empty parts, then rejoin. |
| Accepts live TCP frames | **False.** |

`int.TryParse` on tags and on tag 10 uses current-culture default styles (`NumberStyles.Integer`). Tags are ASCII digits; pin `NumberStyles.None` + `InvariantCulture` on the next edit. Today `+10` and leading spaces are tags.

---

## 5. `BuildFixMessage` — measured contract

```75:111:D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs
        var ordered = fields
            .Where(kv => kv.Key != 9 && kv.Key != 10) // 9 and 10 are computed
            .ToList();
        // ... require tag 8 ...
        // place BeginString first, then MsgType (if present), then remaining tags ascending.
        var remaining = ordered.Where(kv => kv.Key != 8 && kv.Key != 35).OrderBy(kv => kv.Key).ToList();
        // body = JoinSohFields(bodyFields) + SohChar
        // header = 8=…<SOH>9=<ASCII byte count of body><SOH>
        return withoutChecksum.Replace(SohChar, SeparatorChar) + $"|10={checksum}";
```

### 5.1 What is actually correct

| Behaviour | Eval |
|---|---|
| Null `fields` | `ArgumentNullException` |
| Missing tag 8 | `ArgumentException` (“must include tag 8”) |
| Caller 9 and 10 stripped and recomputed | PASS |
| BodyLength = ASCII bytes of `35=…` … + trailing SOH when 35 present | PASS (`9=5` on `{8,35=0}`; `9=104` on the ER sample; `9=55` on the unique-tag Logon) |
| Checksum of `header+body` matches independent sum | PASS (`10=163`, `10=041`, `10=223`) |
| Unique-tag `Parse(Build(x))` | PASS (Logon 8/35/49/56/98/108/141) |

### 5.2 P0 — sorted remaining is not RoE

Harness-shaped ER tags `8,35,49,56,57,50,11,37,55,150,39,60` **emitted**:

```text
8=FIX.4.4|9=104|35=8|11=CL1|37=OID|39=0|49=SENDER|50=TRADE|55=XAUUSD|56=cServer|57=TRADE|60=20260818-00:00:00.000|150=0||10=041
tag order: 8,9,35,11,37,39,49,50,55,56,57,60,150,10
```

Official required order (A25 §3.5 / RoE / FAQ): **`8, 9, 35`, then header `49,56,57,50,34,52`, then body.** FAQ: missing tags, wrong order, bad checksum, or non-UTC time → **no response**.

Missing on this emit: `34`, `52`. `49`/`56`/`57`/`50` sit in numeric order **after** `11`/`37`/`39`. Self-consistent 9/10 prove arithmetic, not venue acceptance.

**Law:** QuickFIX/n (A35) owns field order and tags 9/10. This builder is a **fixture writer**. Sending its output to `*.c-trader.com` is **UNSAFE**.

### 5.3 P0 — Build shuffles repeating groups *before* Parse last-wins

Eval input (SecurityList-shaped, two instruments):

```text
8, 35=y, 55=1, 1007=EURUSD, 55=2, 1007=XAUUSD
```

Build emitted:

```text
8=FIX.4.4|9=39|35=y|55=1|55=2|1007=EURUSD|1007=XAUUSD||10=148
```

`OrderBy(Key)` gathered both `55` then both `1007`. Group pairing is already dead on the wire-shaped string. `Parse` then last-wins to `55=2` / `1007=XAUUSD`. **Two** defects, not one.

### 5.4 Smaller Build holes

- Missing 35 is allowed: `8=FIX.4.4|9=5|49=S||10=203`. Invalid FIX, self-consistent 9/10.
- Duplicate 8/35: first wins for placement; extras dropped from `remaining`. Other duplicate tags are **emitted twice** (list, not map).
- Null `kv.Value`: `StringBuilder.Append(null)` is a no-op → `35=` empty.
- `Encoding.ASCII` maps non-ASCII to `?` (`0x3F`). BodyLength/checksum would then disagree with a Latin-1/UTF-8 peer. cTrader FIX is ASCII; still pin that in comments on the next edit.

---

## 6. What this class must never become

Do **not**:

- add `TcpClient` / `SslStream` framing here (architecture: no hand-rolled engine);
- parse `35=y` / `35=W` / `35=X` / `35=AP` into this dictionary and claim instrument, book, or position coverage;
- “fix” `||10=` by sending the pipe string to a live socket; the live path is QuickFIX/n;
- replace QuickFIX/n with more methods on this type;
- treat a clean compile or this eval as “parser done.”

Do:

- keep it as the **pipe fixture** helper for unique-tag messages (Logon, Logout, Heartbeat, simple ER without groups);
- add `FixFieldList` (or switch tests to QuickFIX `Message` + `FIX44-CSERVER.xml`) **before** any SecurityList / MD test is written;
- write A89 `#60` `FixMessageParseBuildTests`, `#61` `FixChecksumValidationTests`, `#74` `FixParserRejectsGarbageTests` against the **current** contract, including:
  - exact `Build` string contains `||10=`;
  - a two-entry MD group **does not** survive `Parse`;
  - garbage fields are only `FormatException` **after** a matching checksum (or expect `InvalidOperationException` if `10=000`).

---

## 7. Tests claimed vs tests on disk

| A89 id | Claimed class | Pri | A89 status | On disk 2026-08-18 |
|---|---|---|---|---|
| 60 | `FixMessageParseBuildTests` | P0 | EXISTS | **MISSING** (`tests/Unit/Fix` absent) |
| 61 | `FixChecksumValidationTests` | P0 | EXISTS | **MISSING** |
| 74 | `FixParserRejectsGarbageTests` | P2 | EXISTS | **MISSING** |

`TraderIntelligence.Tests.Unit.csproj` **does** reference `Fix.CTrader`. Product tests on disk: `BaselineScorerTests`, `ExecutionAndSizingTests`, `RiskEngineTests`, `SymbolNormalizerTests`, `TradeReconstructionTests`, `VolumeConverterTests`, leftover `UnitTest1`, plus two sizing/normalization files. **No** FIX codec class.

A89 **EXISTS** on rows 60/61/74 means “SUT exists — write the test.” It is **not** evidence the test exists. B28 already said this; remeasured: still true.

`apps/fix-worker` references the assembly and constructs **zero** types from it. Live `NewOrderSingle` remains off by absence + `RealCopyExecutionEnabled` default false. Vacuous safety, not a gate implemented by this class.

---

## 8. Sibling notes (so the next agent does not re-open them)

| Note | Status after this pass |
|---|---|
| B01: unit tests fail `FixMessageParser` CS1503 | **Stale for worktree.** `EndsWith(char)` compiles on net8. HEAD still has `EndsWith(char, StringComparison)` — that is the CS1503. Unstaged one-line fix. |
| B05: parser 6042 B / 120 lines / SHA `3E2C30C8…` | **Stale.** Now 6016 B / 145 lines / `C58681E7…`. |
| B05: csproj has `QuickFix.Net` 1.8.0 | **True of HEAD. False of worktree.** A35 pin still missing. |
| B28 static review of last-wins + sorted remaining | **Still accurate.** This pass **adds** empirical `\|\|10=`, checksum-before-shape, group-shuffle on Build, SOH reject, wrong-9 accept. |
| A68 “SOH-normalized text accepted” | **False** unless the caller replaces SOH → `\|` first. Eval: raw SOH → `FormatException`. |
| C19 / C43 / D05 SHA `C58681E7…` | **Still the file.** |

---

## 9. Residual risk if someone wires this type tomorrow

| Risk | Severity | Why |
|---|---|---|
| Live outbound with sorted tags, `\|\|10=`, no 34/52 | P0 venue | Silent no-response or reject; not a fill |
| MD / SecurityList / PositionReport via `Parse` | P0 book / map | Last bid/ask or last symbol only; Build also un-groups |
| Exact-string golden fixtures that omit the extra `\|` | P1 tests | False reds, or someone “fixes” fixtures by sending the double-SOH form |
| Dashboard / worker `LoggedOn` without this type | P1 ops | Already true — orthogonal lie |

**Current production send path: still absent.** Residual money risk from this file is **latent** until a host calls `BuildFixMessage` or feeds `Parse` a repeating group and trusts the map. The correct control is to never put this type on a `NetworkStream`.

---

## 10. Do / do not

**Do**

- Keep `FixMessageParser` as a documented **pipe fixture** helper for unique-tag messages.
- Put QuickFIX/n 1.14.1 + `FIX44-CSERVER.xml` on the session path (A35 / A36); engine owns 9/10/order.
- Write the three missing unit classes against the **measured** contract in §4–§5 before claiming codec coverage.
- If the next coding task touches this file: emit a field list, stop `OrderBy` of remaining tags, and make `\|\|10=` a single separator in the pipe dialect (or stop exposing pipe as if it were SOH).

**Do not**

- Send `BuildFixMessage` output to `*.c-trader.com`.
- Decode `35=y` / `35=W` / `35=X` / `35=AP` through `Dictionary<int,string>` and call it a book or a symbol list.
- Treat A89 EXISTS, a clean compile, or this eval as “parser done.”
- Hand-write a second FIX engine around this parser.
- Modify product source in a census/review pass.

---

## 11. Bottom line

`FixMessageParser` is a **145-line, compiling, checksum-competent test codec** with:

1. a fatal **decode** model (last-wins map);
2. a fatal **send** model (sorted remaining tags, no session-header discipline);
3. a measured **pipe-dialect** bug (`Build` always emits `||10=`; `Parse` paper-clips it with `RemoveEmptyEntries`);
4. **zero** unit tests despite A89 marking three classes EXISTS.

Architecture still requires QuickFIX/n sessions and the cTrader DD. This file is a seed fixture writer, not those things.

**Reviewer verdict: FAIL as a production FIX parser. PASS only as a named test helper with the defects above recorded.**

Product source was not modified.
