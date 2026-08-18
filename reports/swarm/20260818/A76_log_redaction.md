# A76 — FIX / MT5 tags and config keys that must be redacted in logs

| Field | Value |
|---|---|
| Agent | A76 (log-redaction catalog) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A76_log_redaction.md` |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§7–8, 25, 48, 52, **55–57** |
| Sibling specs | A19 secrets scan, A25 FIX session, A26 dashboard denylist, **A50** Serilog / `FixPasswordTagCatalog` / `FixWireRedactor` |
| Official FIX | cTrader RoE Logon (`35=A`) tags **553 / 554**; generic FIX 4.4 Logon + UserRequest (`95/96/925`); FIX 5.x encrypted-password family (`1400–1404`) |
| Product source edited | **No** |

This file is the **binding denylist** for log redaction. It does not implement code. When a later wave wires `FixPasswordTagCatalog` / `SecretNameCatalog` / `FixWireRedactor` (A50 §5), those types must consume this catalog. Do not invent a second list.

Replacement token is always the three-character literal `***`. Do **not** pad to the original length. Do **not** hash the value into the log.

---

## 0. Verdict (honest)

| Capability | Classification | Evidence |
|---|---|---|
| Central log redactor | **MISSING** | No `FixWireRedactor`, no Serilog enricher. A50 is spec only. |
| C++ `logger.h` filter | **MISSING** | `D:\Prop\mt5-sdk\src\utils\logger.h` formats message text only. Relies on call sites. |
| Product comments as control | **NOT A CONTROL** | `CTraderFixOptions.Password` / `AccountId` say “Must never be logged.” That is a comment. |
| Live secret values in this report | **NONE** | Placeholders only. Official RoE sample `passw0rd!` is cited as a **test fixture**, not a live password. |
| C++ manager password in logs today | **NOT LOGGED** (call-site luck) | `mt5_manager.cpp` logs `type + address:port` for proxy; connect logs **login number + server**, not password. |
| HTTP client request bodies | **NOT LOGGED** (call-site luck) | `MT5HttpClient` does not print JSON. `CreateUser` / `ChangePassword` bodies still carry plaintext `password` / `investor_password` on the wire. |

Architecture §57: **“Never log authentication tags containing passwords. Redact sensitive values centrally.”** Call-site discipline is not that control.

---

## 1. Binding law (do not weaken)

| Source | Rule |
|---|---|
| §7 | `MT5_PASSWORD` is secret. **Never log proxy credentials.** |
| §8 | `MT5_STARWAVEFX_PASSWORD` is secret. |
| §25 / §52 | `CTRADER_FIX_PASSWORD` is secret. **Never show FIX password** (UI). Logs have the same bar. |
| §55 | Never expose: MT5 passwords, proxy credentials, cTrader account password, FIX password, database passwords, Redis passwords. |
| §57 | Never log authentication tags containing passwords. Redact centrally. |
| A19 rec 5 | Reject logging of tags **553/554** and any `Password=` field before wiring FIX. |
| A25 §3.3 / test 12 | Never log 554. Password never appears in structured log output. |
| A26 §3 | Same denylist on JSON, SignalR, CSV, `audit_logs` blobs. |
| A50 §5.1 | Tag catalog + `***` replacement + raw-wire 553 redaction. This file **owns** the expanded list. |

Fail closed: if a FIX string cannot be parsed, emit `fix_raw=[UNPARSEABLE_REDACTED]` and drop the original (A50 §5.2). Never send a redacted copy to the socket.

---

## 2. FIX tags — MUST redact (values)

Keep the tag number so operators can see that an auth field was present. Replace the value with `***`.

cTrader production Logon uses **plaintext 554**. Official RoE example (A32; **sample only**):

```text
8=FIX.4.4|9=126|35=A|49=live.theBroker.12345|56=CSERVER|34=1|52=20170117-08:03:04|57=TRADE|50=any_string|98=0|108=30|141=Y|553=12345|554=passw0rd!|10=131|
```

Logged form of that line **must** be:

```text
…|553=***|554=***|10=131|
```

`passw0rd!` must be absent from every sink, including exceptions and QuickFIX `ILog`.

### 2.1 Hard denylist (always)

| Tag | Name | Why | Authority |
|---|---|---|---|
| **554** | `Password` | cTrader Logon password. Production path. | RoE Logon; §52 / §57; A25; A50 |
| **925** | `NewPassword` | Password change on UserRequest (`35=BE`). | FIX 4.4 `FIX44.xml`; A50 |
| **96** | `RawData` | Generic FIX 4.4 encrypted / raw password blob. Also A26 dashboard denylist. | FIX 4.4 Logon + UserRequest; A26 §3.1; A50 |
| **91** | `SecureData` | Encrypted payload. Not used by cTrader (`98=0`) but must not leak if a dictionary / engine emits it. | FIX 4.4 standard header/security block |
| **89** | `Signature` | Digital signature bytes. | FIX 4.4 trailer |
| **1401** | `EncryptedPassword` | Encrypted Logon password. | FIX 5.x; A50 future-proof |
| **1403** | `EncryptedNewPassword` | Encrypted new password. | FIX 5.x; A50 |

### 2.2 Raw-wire only (credential-pair replay)

| Tag | Name | Raw `fix_raw` / QuickFIX OnIncoming / OnOutgoing / FileLog | Structured property | Metric attribute | Dashboard |
|---|---|---|---|---|---|
| **553** | `Username` | **Redact** (`553=***`) | `source_login` / destination account **allowed** (numeric identifier; §57) | **Forbidden** | Allowed as trader login; never as a password |
| **49** | `SenderCompID` | **Keep** (session identity) | Allowed (`sender_comp_id`) | Forbidden (cardinality + identity) | Allowed |

553 is the numeric trader login (`1369850` on this venue), **not** a password. A19/A25/A50 still require it redacted **on the raw wire dump** so a pasted Logon line is not a replayable `553+554` pair. Do **not** put 553’s value into a property named `username` / `password` / `AccountId`.

`CTraderFixOptions.AccountId` is documented “Must never be logged.” That means: never dump the options object; never print `AccountId=<value>` next to `Password=`. The numeric login **may** appear as structured `source_login` / destination account after it is an operational identifier, not as a FIX username field.

### 2.3 Length / method tags — KEEP (not secrets)

| Tag | Name | Action |
|---|---|---|
| 95 | `RawDataLength` | Keep integer |
| 90 | `SecureDataLen` | Keep integer |
| 93 | `SignatureLength` | Keep integer |
| 98 | `EncryptMethod` | Keep (`0` = none; RoE transport TLS only) |
| 1400 | `EncryptedPasswordMethod` | Keep enum |
| 1402 | `EncryptedPasswordLen` | Keep integer |
| 1404 | `EncryptedNewPasswordLen` | Keep integer |

Lengths are not the secret. Do **not** infer or log password length from 554’s original value.

### 2.4 Tags that must NOT be treated as secrets

These stay visible. They are the §57 / ops identifiers.

| Tag | Name | Notes |
|---|---|---|
| 8 / 9 / 10 | BeginString / BodyLength / CheckSum | Header/trailer. Do **not** recompute checksum after redaction — log-only string. |
| 35 | MsgType | Extract **before** redaction. |
| 34 | MsgSeqNum | Extract **before** redaction. |
| 11 | ClOrdID | Required log property `cl_ord_id`. Not a secret. |
| 37 | OrderID | `cserver_order_id`. |
| 721 | PosMaintRptID | `destination_position_id` (cTrader position id). |
| 50 / 56 / 57 | SenderSubID / TargetCompID / TargetSubID | Session mapping. Broker-issued SubIDs are **not** passwords unless they match a secret-name pattern. |
| 55 | Symbol | Numeric Spotware instrument id. Not a credential. |
| 58 | Text | Keep **unless** the value matches `Password=` / contains a denylisted key. Logout `58=InternalError: RET_INVALID_DATA` is safe. |

### 2.5 Name-based FIX tokens (non-numeric dumps)

QuickFIX `Message.ToString()` is tag-numeric. Humans, exception messages, and session-settings dumps are not. After the tag split, also redact (case-insensitive name, value → `***`):

```text
Password
NewPassword
EncryptedPassword
EncryptedNewPassword
RawData
SecureData
Signature
Username          // only when the dump is a FIX field / Logon line, not source_login
```

Regex (A50 §5.2, **extended** by this file — A50’s published regex omitted 553 / 91 / 89):

```regex
(?<![0-9])(?<tag>554|925|96|91|89|1401|1403|553)=(?<val>[^\x01|]*)
(?i)(?<name>Password|NewPassword|EncryptedPassword|EncryptedNewPassword|RawData|SecureData|Signature)=(?<val>[^\x01;,&\s]*)
```

Acceptance fixture: official RoE Logon (A32). Assert `passw0rd!` absent and `554=***` plus `553=***` present. Test SOH (`\u0001`), `|`, and `^A` delimiters.

---

## 3. QuickFIX session-settings keys — MUST redact / MUST forbid

These are **not** FIX tags. They appear in `SessionSettings` / `.cfg` / engine `ToString()`.

| Key | Action |
|---|---|
| `Password` | **Redact.** This is how QuickFIX stores 554. |
| `Username` | Redact in settings dumps (same as tag 553 on wire). |
| `FileLogPath` | **Forbidden** in product settings. Factory must throw if present (A50 §5.6). Default FileLog persists raw 554. |
| `ScreenLog` / `ScreenLogShowIncoming` / `ScreenLogShowOutgoing` / `ScreenLogShowEvents` | **Forbidden** in product. Prints raw wire to console. |
| `FileStorePath` | Not a secret. Allowed. Sequence files must never be concatenated with the password. |

Product `ILog` must be `RedactingQuickFixLog` (A50 §5.8). Never `FileLogFactory` / `ScreenLogFactory`.

Allowed Logon **Information** shape (booleans only; A50):

```text
FIX Logon sent
  fix_session=TRADE
  sender_comp_id=<issued SenderCompID>
  target_comp_id=<issued TargetCompID>
  reset_seq=Y
  encrypt_method=0
  heart_bt_int=30
  username_present=true
  password_present=true
```

No 553/554 values.

---

## 4. Config keys — MUST redact (FIX + MT5)

Match after normalizing the key: lowercase, strip `_`, `-`, `:`, `.`. Redact if the normalized name **equals** or **ends with** a name in §6, **or** equals one of the exact keys below.

Values become `***`. Keys stay so operators can see `secretConfigured`.

### 4.1 cTrader / FIX env (architecture §§25, 56 + `D:\Prop\.env.example`)

| Key | Class | Log rule |
|---|---|---|
| **`CTRADER_FIX_PASSWORD`** | **SECRET — always redact** | Never value, never substring. |
| `CTRADER_FIX_ACCOUNT_ID` | Identifier | Do not dump in `{@options}` / `IConfiguration.AsEnumerable()`. Allowed as structured destination account / `source_login` analogue. Never next to the password. |
| `CTRADER_FIX_HOST` | Non-secret | Keep |
| `CTRADER_FIX_USE_SSL` | Non-secret | Keep |
| `CTRADER_FIX_QUOTE_SSL_PORT` / `PLAIN_PORT` | Non-secret | Keep |
| `CTRADER_FIX_TRADE_SSL_PORT` / `PLAIN_PORT` | Non-secret | Keep |
| `CTRADER_FIX_*_SENDER_COMP_ID` | Identifier | Keep in structured session logs; do not dump entire options object |
| `CTRADER_FIX_*_TARGET_COMP_ID` | Non-secret | Keep |
| `CTRADER_FIX_*_SESSION_QUALIFIER` | Non-secret | Keep (`QUOTE` / `TRADE`) |
| `CTRADER_FIX_*_SENDER_SUB_ID` / `TARGET_SUB_ID` | Broker-issued | Keep unless value matches a secret-name / `Password=` pattern |
| `CTRADER_FIX_ENABLED` / `QUOTE_ENABLED` / `TRADE_SESSION_ENABLED` | Flag | Keep |
| `REAL_COPY_EXECUTION_ENABLED` | Flag | Keep |
| `CTRADER_FIX_HEARTBT_INT` / `CTRADER_FIX_RESET_SEQ_NUM` | Non-secret (A25) | Keep |

C# bind (`D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`):

| Property | Maps to | Log rule |
|---|---|---|
| **`CTraderFixOptions.Password`** | `CTRADER_FIX_PASSWORD` | **Never log.** Comment on the type is already this rule. |
| `CTraderFixOptions.AccountId` | `CTRADER_FIX_ACCOUNT_ID` | Never log via `{@options}`. See §2.2. |
| `Quote.SenderCompId` / `Trade.SenderCompId` | `*_SENDER_COMP_ID` | Structured only |
| Host / ports / SSL / flags | matching env | Keep |

### 4.2 Achiever / default MT5 env (architecture §7, §56 + `.env.example` + `AppConfig`)

| Key | Class | Log rule |
|---|---|---|
| **`MT5_PASSWORD`** | **SECRET** | Always redact |
| **`ACHIEVER_PROXY_USERNAME`** | **SECRET** (proxy credential, §7) | Always redact |
| **`ACHIEVER_PROXY_PASSWORD`** | **SECRET** | Always redact |
| **`MT5_PROXY_LOGIN`** | **SECRET** (SDK name; same as proxy user) | Always redact |
| **`MT5_PROXY_PASSWORD`** | **SECRET** | Always redact |
| **`MT5_API_KEY`** | **SECRET** (HTTP `X-API-Key`) | Always redact |
| **`MT5_PASSWORD_ENCRYPTION_KEY`** | **SECRET** (AES-256-GCM at-rest key) | Always redact |
| `MT5_LOGIN` | Identifier | Allowed as structured `source_login` / manager-login. **Mask** in dashboard list views (`**27` for `2027`, A26). Do not put on metric labels. |
| `MT5_SERVER` / `MT5_PORT` / `MT5_SERVER_NAME` | Non-secret | Keep |
| `MT5_DEFAULT_GROUP` / `MT5_GROUP_*` | Non-secret | Keep (group path, not a password) |
| `MT5_MODE` / `MT5_POOL_SIZE` | Non-secret | Keep |
| `MT5_REMOTE_URL` | Non-secret host | Keep. If someone stuffs `user:pass@` into the URL, run the URI redactor. |
| `IS_MT5_PROXY_ENABLED` / `ACHIEVER_PROXY_ENABLED` | Flag | Keep |
| `MT5_PROXY_TYPE` / `ADDRESS` / `PORT` | Non-secret | Keep (`mt5_manager.cpp` already logs these) |
| `ACHIEVER_PROXY_HOST` / `PORT` / `ACHIEVER_EGRESS_IP` | Non-secret | Keep |
| `MT5_HTTP_TIMEOUT_MS` / `MT5_HTTP_POOL_*` | Non-secret | Keep |
| `MT5_VOLUME_SCALE` | Non-secret | Keep |

C# bind (`D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs`):

| Property | Log rule |
|---|---|
| **`Password`** | **Never log** (comment: “secret placeholder in config”) |
| **`ProxyPassword`** | **Never log** |
| **`ProxyLogin`** | **Never log** (proxy credential pair, §7) |
| **`ApiKey`** | **Never log** |
| `Login` | Identifier; structured only; no metric label |
| `Server` / `Port` / `ServerName` / `Mode` / `PoolSize` / `ProxyEnabled` / `ProxyType` / `ProxyHost` / `ProxyPort` / `RemoteUrl` / `EgressIp` / `BrokerId` / `DisplayName` | Keep |

### 4.3 StarwaveFX MT5 env (architecture §8, §56)

| Key | Class | Log rule |
|---|---|---|
| **`MT5_STARWAVEFX_PASSWORD`** | **SECRET** | Always redact |
| `MT5_STARWAVEFX_LOGIN` | Identifier | Same as `MT5_LOGIN` |
| `MT5_STARWAVEFX_SERVER` / `PORT` / `SERVER_NAME` / `MODE` / `POOL_SIZE` / `DISPLAY_NAME` / `PROVISIONING_ENABLED` / `PROXY_ENABLED` | Non-secret | Keep |

If StarwaveFX later grows `MT5_STARWAVEFX_PROXY_USERNAME` / `PASSWORD`, the §6 suffix rule (`password`, `proxyusername`) catches them without a code change.

### 4.4 C++ `AppConfig` field names (`D:\Prop\mt5-sdk\config\app_config.h`)

These are the in-process names if someone logs `cfg` / `AppConfig` as text:

| Field | Env | Redact value? |
|---|---|---|
| `mt5_password` | `MT5_PASSWORD` | **Yes** |
| `mt5_proxy_login` | `MT5_PROXY_LOGIN` | **Yes** |
| `mt5_proxy_password` | `MT5_PROXY_PASSWORD` | **Yes** |
| `mt5_api_key` | `MT5_API_KEY` | **Yes** |
| `mt5_password_encryption_key` | `MT5_PASSWORD_ENCRYPTION_KEY` | **Yes** |
| `mt5_login` | `MT5_LOGIN` | Identifier only |
| all other `AppConfig` fields in the header | matching env | No |

---

## 5. MT5 Manager / HTTP JSON fields — MUST redact

These appear in request bodies, `UserParams::to_json`, curl debug, and exception messages. Never log the body of these calls.

| Surface | Keys / header | Path / API |
|---|---|---|
| Create / update user JSON | **`password`**, **`investor_password`** | `UserParams` in `mt5_types.h`; `POST /mt5/users` |
| Change password JSON | **`password`** | `PUT /mt5/users/{login}/password` |
| Check password JSON | **`password`** | `POST /mt5/users/{login}/check-password` |
| HTTP auth header | **`X-API-Key`** | every `MT5HttpClient` REST/SSE request (`mt5_http_client.cpp`) |
| Proxy auth blob | `login:password` in `MTProxyInfo.auth` | `MT5Manager::SetProxy` / pool. Today the C++ logs **type + address:port only** — keep it that way. |
| Manager Connect | password argument | `IMTManagerAPI::Connect(server, login, password, …)` — log result code + login number, never the password wide-string. |

`type` on ChangePassword / CheckPassword is `0` master / `1` investor. Keep the type. Redact the password.

Curl `VERBOSE`, `DEBUGFUNCTION`, or “log the JSON we sent” is a **§57 violation** unless the JSON has passed the same denylist.

---

## 6. Name catalog (any key, any language)

Normalize: lowercase, strip `_ - : .`. Redact if the normalized name **equals** or **ends with**:

```text
password
passwd
pwd
secret
newpassword
rawdata
securedata
encryptedpassword
encryptednewpassword
connectionstring
connstr
authorization
proxyusername
proxylogin
proxypassword
apikey
accesstoken
refreshtoken
privatekey
clientsecret
investorpassword
passwordencryptionkey
```

Exact normalized env / options contains (A50 §5.3 + this file):

```text
ctraderfixpassword
mt5password
mt5starwavefxpassword
achieverproxypassword
achieverproxyusername
mt5proxypassword
mt5proxylogin
mt5apikey
mt5passwordencryptionkey
```

This catches `Brokers__0__Password`, `CTraderFix__Password`, `Mt5BrokerOptions:Password`, and Azure Key Vault aliases without listing every binder prefix.

---

## 7. Adjacent platform keys (same log pipeline)

Not FIX/MT5 tags, but they sit in the same `.env` and will leak through `{@options}` / Npgsql / Redis exceptions if ignored. Same `***` rule.

| Key / pattern | Rule |
|---|---|
| `DATABASE_URL` | Redact URI userinfo and `Password=` fragment. Example: `Host=localhost;Port=5432;Database=trader_intelligence;Username=ti;Password=***` |
| `DB_PASSWORD` | Always redact |
| `DB_USER` | Identifier; keep unless paired in a URI with a password |
| `ConnectionStrings:*` / `ConnectionString` | Run `ConnectionStringRedactor` (A50). Keep Host/Port/Database/Username. Redact Password/Pwd/SSL Password. |
| `REDIS_URL` / `REDIS_PASSWORD` / Redis `AUTH` | Redact password / userinfo. Keep host:port. |
| `Authorization` / `Cookie` request headers | Never log (A50 §5.7). Dashboard login password is accepted only on `POST /api/v1/auth/login` and is never logged (A26). |
| JWT `accessToken` / `refreshToken` | Never log raw values. |
| Vault / PEM / `privateKey` / `clientSecret` | Always redact (A26). |

`PQerrorMessage` / `NpgsqlException` / `RedisConnectionException` / `OptionsValidationException` must go through `ExceptionRedactor` (A19, A50 §5.5). Never `ex.ToString()` raw.

---

## 8. Banned dump surfaces (even if individual keys are filtered)

These emit **all** keys, including secrets, unless the redactor is already installed:

| Surface | Rule |
|---|---|
| `IConfiguration.AsEnumerable()` | **Banned** (A50 §5.7) |
| `LogInformation("{@options}", options)` on `CTraderFixOptions` / `Mt5BrokerOptions` / `AppConfig` | **Banned**. Log `host`, `port`, `ssl`, `secret_configured=true\|false` only. |
| `Environment.GetEnvironmentVariables()` | **Banned** |
| QuickFIX `FileLogPath` / `ScreenLog` | **Banned** (see §3) |
| EF `EnableSensitiveDataLogging` | Off when `environment != Development` |
| ASP.NET request body / query dump | Do not log. |
| `audit_logs.before` / `.after` | Same denylist. Write of a secret key → `422 SECRET_FIELD_REJECTED` (A26). |
| OpenTelemetry span attribute `fix.raw` | Only after `FixWireRedactor`. Prefer `msg_type` + `seq`. |
| Metric attributes | **No** passwords, **no** logins, **no** ClOrdID (A50 §7). |

---

## 9. What logs SHOULD contain (so redaction is not “log nothing”)

Architecture §57 identifiers — **keep**:

```text
correlation_id
broker_id
source_login
source_trade_id
copy_intent_id
risk_decision_id
execution_intent_id
cl_ord_id
cserver_order_id
destination_position_id
fix_session
```

A25 also: `fencing_token` (lease id, not a secret).

Safe operational FIX/MT5 fields: host, SSL port, connected, logged-on, sequences, last in/out, reconnects, heartbeat, `msg_type`, `exec_type`, `ord_status`, group names, `secret_configured`, `username_present`, `password_present`.

---

## 10. Implementation pin (do not implement in this wave)

A50 target types. This catalog is their data:

```text
src/Infrastructure/Observability/Redaction/FixPasswordTagCatalog.cs   // §2 tags
src/Infrastructure/Observability/Redaction/SecretNameCatalog.cs       // §4–§6 keys
src/Infrastructure/Observability/Redaction/FixWireRedactor.cs
src/Infrastructure/Observability/Redaction/ConnectionStringRedactor.cs
src/Fix.CTrader/Logging/RedactingQuickFixLog.cs                       // forbid FileLog/ScreenLog
```

C++: do **not** log `UserParams` JSON, curl verbose, `MTProxyInfo.auth`, or `AppConfig` as a blob. `logger.h` has no filter; when a filter is added it must apply the same tag/key sets.

---

## 11. Acceptance (minimum tests — A50 + this file)

1. Official RoE Logon line: `554=passw0rd!` → `554=***`, `553=***`, sample password absent.
2. Same line with SOH delimiters.
3. UserRequest with `925=` and `96=`.
4. `Password=secret` / `NewPassword=` / `EncryptedPassword=` name form.
5. Unparseable binary → `fix_raw=[UNPARSEABLE_REDACTED]`.
6. `{@options}` of `CTraderFixOptions` with a non-empty `Password` never renders the password.
7. `{@options}` of `Mt5BrokerOptions` never renders `Password`, `ProxyPassword`, `ProxyLogin`, `ApiKey`.
8. `DATABASE_URL` / Npgsql exception: `Password=` stripped, `Host=` kept.
9. `X-API-Key: <value>` and `investor_password` in a JSON string → `***`.
10. QuickFIX settings containing `FileLogPath` or `ScreenLog` → factory throws (product).
11. Structured `source_login=12345` is **allowed**; raw `553=12345` on `fix_raw` is **not**.

---

## 12. Honesty close

Measured tree (2026-08-18): **no redactor is installed.** The denylist above is architecture + official FIX + current options/env names. Product comments on `CTraderFixOptions` and the C++ manager’s “don’t log proxy auth” are **call-site luck**, not §57.

Do not copy live `MT5_PASSWORD` / `CTRADER_FIX_PASSWORD` / proxy / API-key / encryption-key **values** into reports, `appsettings`, or this file. This report contains **zero** live secrets.

**Product source was not modified.**
