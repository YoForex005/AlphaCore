# D86 — Kafka / Kubernetes / LLM are absent (correct)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D86_notbuild.md` |
| Agent | D86 (senior engineer; §71 absence recensus, read-only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 13:44:03 +05:30 |
| Assigned | Confirm Kafka, K8s, and LLM are **absent**. Write this file. **Do not modify product source.** |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Measure HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |
| Binding law | Architecture v2 **§71** (lines 2681–2697), **§13** (lines 575–595), **§5** (lines 310–318 + “No LLM API is required”), header line 6 (`AI/LLM dependency: None`) |
| Binding siblings | **A80** (full §71 list), A03, A29 L29, A30 §0/§15, A41, A52, A54, A65, B37, B39, C12, C44, D45, D63 |
| Does not supersede | A80 (nine-item non-goal list). This file **re-measures** the three items named in the task. |
| Classification (§73.B) | Absence of Kafka / K8s / LLM = **`EXISTS_AND_GOOD`**. Do not add them to close a perceived gap. |

Honesty vocabulary for this file:

```text
MEASURED     — observed on disk at the timestamp above
ABSENT       — zero product-source hits after the searches in §1
CORRECT      — required by §13 / §5 / §71; not a first-useful-version defect
STALE        — prior swarm row that no longer matches this hash (named below)
FALSE POS    — substring in a lockfile integrity hash, not a package
GREENWASH    — treating this absence as “infra ready” or adding a broker/cluster/chat API to look production-ready
```

**Assigned answer:** Kafka, Kubernetes, and an LLM/AI API are **not** in product source. That is the **correct** state under architecture §71 / §13 / §5. Mentions exist only in the architecture file and swarm reports (this one included). Product source was not modified.

---

## 0. Verdict

**CONFIRMED. Kafka is absent. K8s is absent. LLM is absent. All three are `EXISTS_AND_GOOD` as absence.**

| # | Item | Architecture name | Product tree | Class |
|---|---|---|---|---|
| 1 | Kafka | Kafka (§13, §71) | **0** product hits. No `Confluent.Kafka` / MassTransit / NATS / RabbitMQ. | **`EXISTS_AND_GOOD`** |
| 2 | K8s | Kubernetes (§5, §71) | **0** product hits. No Helm, `Chart.yaml`, `kustomization.yaml`, `KubernetesClient`, `.github` workflows, `deploy/k8s`. | **`EXISTS_AND_GOOD`** |
| 3 | LLM | LLM / AI API (header, §5, §71) | **0** product hits. No OpenAI / Semantic Kernel / LangChain / chat client. `D:\Prop\services` is empty. | **`EXISTS_AND_GOOD`** |

Do **not** claim “no Kafka/K8s/LLM anywhere in the repo.” The architecture file names them as **forbidden**. Swarm reports (A80, this file) quote those names. Two `package-lock.json` integrity strings contain the letters `LLM` as a **hash substring** (false positive; see §5).

Do **not** treat this PASS as “event bus / deploy / ranker are done.” The **legal substitutes** are only partly built:

| Need | Legal substitute | Measured now |
|---|---|---|
| Async after ingest | Postgres transactional outbox + `IEventBus` behind the outbox (A41, §13) | `OutboxEvent` type exists; **`IEventBus` / `IOutboxWriter` = 0 hits**. Write site is demo-only (D45), not §12 same-TX. |
| Deploy | Windows `apps/mt5-worker` + Linux compose for API/Postgres/Redis (A54, §5) | `docker-compose.yml` exists: **`postgres` + `redis` + `api` only**. No K8s. MT5 stays off Linux (D63). |
| Rank traders in v1 | Deterministic C# `BaselineScorer` (A22). Not an LLM. Not Phase 6 XGBoost. | Scorer exists. It is **NOT ML** and **NOT an LLM** (C44). |

Honest one-liner: **§71 is still honored for Kafka, Kubernetes, and LLM. Build the outbox poller and the Windows/Linux split — do not fill these holes with a broker, a cluster, or a chat API.**

`PHASE0_AUDIT.md` already records `Kafka/K8s/LLM | DEPRECATED / not to build`. D86 re-measures the same three facts. They are still true.

---

## 1. Method

Read-only. No `dotnet add package`, no Helm, no Docker start, no product edit.

| Source | Action |
|---|---|
| Architecture v2 | Read header, §5, §13, §71. Hash below. |
| Product C# / csproj / props | `src/` (66 `*.cs`), `apps/` (6 `*.cs`), `tests/` (12 `*.cs`), 10 product `*.csproj` |
| Web | `apps/web/src` (28 `*.ts`/`*.tsx`), `package.json`, `package-lock.json` (integrity false-positives only) |
| Owned C++ | `mt5-sdk/src` + `config` + `tests` (33 `.cpp`/`.h`); `CMakeLists.txt` |
| Deploy surface | `docker-compose.yml` (only `*.yml`/`*.yaml` in the product tree), `Directory.Build.props`, `Mt5TraderIntelligence.sln`, `README.md`, `docs/*.md` |
| Env | Operator `.env` **key names only** — no `KAFKA_*` / `OPENAI_*` / `ANTHROPIC_*` / `LLM_*` / `K8S_*` / `KUBERNETES_*` |
| NuGet restore graph | All 10 product `obj/project.assets.json` scanned for forbidden package ids |
| Path existence | `deploy/k8s`, `charts/`, `helm/`, `Chart.yaml`, `kustomization.yaml`, `services/ml-service`, `.github`, `Dockerfile*` |
| Prior law | A80, A41, A54, A65, C12, C44, D45, D63 — **re-measured**, not copied as the verdict |

Exclude from “product source”: `bin/`, `obj/`, `node_modules/`, `.git/`, `mt5-sdk/vendor/`, `reports/swarm/20260818/_tmp_*`.

SHA-256 via PowerShell `Get-FileHash -Algorithm SHA256`.

---

## 2. Binding law (quoted)

Architecture `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`

| Field | Value |
|---|---|
| Bytes | **50966** |
| Content lines (Measure-Object -Line) | **2116** |
| SHA-256 | `0B3C0EDC09081C25D097FF0E6AADC7A638562EBB8DB345DC325DC54EC904D37E` |

Same hash as D58. Unchanged this pass.

Header line 6:

> **AI/LLM dependency:** None.

§5 stack (line 295):

> No LLM API is required.

§5 deployment (lines 310–318): Docker where compatible; Windows worker if the Manager DLL requires Windows; Linux for API / Postgres / Redis / Python / React. **Not** Kubernetes.

§13 (lines 575–595):

> # 13. Why Use an Outbox Instead of Kafka Initially
>
> At ~5,000 users, introducing Kafka on day one is likely unnecessary.
>
> Use: PostgreSQL transactional outbox
>
> If measured throughput later requires a dedicated broker, migrate behind an event-bus abstraction.
>
> Do not preemptively introduce distributed infrastructure.

§71 in full (lines 2681–2697):

```text
# 71. What Not to Build Yet

Do not add initially:

Kafka
Kubernetes
ClickHouse
LLM/AI API
deep learning
reinforcement learning
complex microservice mesh
cross-region active-active FIX execution
automated model self-promotion

These can be revisited only when measurements justify them.
```

Architecture word counts (SimpleMatch on that file only): **Kafka = 3**, **Kubernetes = 1**, **LLM = 3**, **ClickHouse = 1**. Those are **law**, not implementations.

This task names the first, second, and fourth rows. The other six §71 rows stay **OUT** (A80). D86 does not reopen them.

---

## 3. Trees searched (product)

| Tree | Files this run | Kafka | K8s / Helm | LLM / OpenAI / SK |
|---|---:|---:|---:|---:|
| `D:\Prop\src` `*.cs` | **66** | 0 | 0 | 0 |
| `D:\Prop\apps` `*.cs` | **6** | 0 | 0 | 0 |
| `D:\Prop\tests` `*.cs` | **12** | 0 | 0 | 0 |
| Product `*.csproj` (`src` + `apps` + `tests`) | **10** | 0 | 0 | 0 |
| `apps/web/src` `*.ts`/`*.tsx` | **28** | 0 | 0 | 0 |
| `apps/web/package.json` | 1 | 0 | 0 | 0 |
| `appsettings*.json` (3 hosts) | 6 | 0 | 0 | 0 |
| `docs/*.md` | **7** | 0 | 0 | 0 |
| Owned `mt5-sdk` `.cpp`/`.h` | **33** | 0 | 0 | 0 |
| `mt5-sdk/CMakeLists.txt` | 1 | 0 | 0 | 0 |
| `README.md` | 1 | 0 | 0 | 0 |
| `docker-compose.yml` | 1 | 0 | 0 | 0 |
| `Directory.Build.props` | 1 | 0 | 0 | 0 |
| `Mt5TraderIntelligence.sln` | 10 product projects | 0 extra hosts | 0 | 0 |
| `D:\Prop\services` | **0 children** | — | — | no `ml-service` |
| Product `obj/project.assets.json` | **10** | CLEAN | CLEAN | CLEAN |

Patterns that returned **zero** product hits (C# / csproj / ts / tsx / json / yml, excluding `package-lock.json` integrity — see §5):

```text
Confluent.Kafka
MassTransit
RabbitMQ.Client
NATS.Client
Azure.Messaging.ServiceBus
Kafka / kafka / KAFKA
Redpanda
KubernetesClient
Kubernetes / kubernetes
kustomization
Helm / helm
StatefulSet / DaemonSet
kind: Deployment
apiVersion: apps/
SemanticKernel
Azure.AI.OpenAI
OpenAI
LangChain
ChatGPT
anthropic
ollama
llm-service
\bLLM\b   (except lockfile hashes)
IEventBus
IOutboxWriter
ITransactionalOutbox
```

File-name search: **0** `*Kafka*`, **0** `*EventBus*`, **0** `Dockerfile*`. Outbox filenames are only:

- `D:\Prop\src\Domain\Entities\OutboxEvent.cs`
- `D:\Prop\src\Domain\Enums\OutboxEventType.cs`

---

## 4. Package census (why the NuGet graph is clean)

Every product `PackageReference` on disk:

| Project | Packages |
|---|---|
| `TraderIntelligence.Domain` | *(none)* |
| `TraderIntelligence.Application` | `FluentValidation` 11.9.2 |
| `TraderIntelligence.Infrastructure` | EF Core Design 8.0.4, EF InMemory 8.0.4, `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.4, `StackExchange.Redis` 2.8.0 |
| `TraderIntelligence.Mt5` | *(none)* |
| `TraderIntelligence.Fix.CTrader` | *(none)* — QuickFIX/n still **not** referenced (D52) |
| `TraderIntelligence.Api` | SignalR.Common 8.0.4, Serilog.AspNetCore 8.0.2, Swashbuckle 6.6.2 |
| `TraderIntelligence.Mt5Worker` | `Microsoft.Extensions.Hosting` 8.0.1 |
| `TraderIntelligence.FixWorker` | `Microsoft.Extensions.Hosting` 8.0.1 |
| Unit tests | coverlet, FluentAssertions, Test.Sdk, Moq, xunit |
| Integration tests | coverlet, FluentAssertions, EF InMemory, Test.Sdk, xunit |

`Directory.Build.props` (269 B, SHA-256 `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0`) sets language/nullable only. **No** central package management of Kafka / K8s / LLM SDKs.

`apps/web/package.json` (739 B, SHA-256 `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6`) dependencies:

```text
react, react-dom, react-router-dom, @tanstack/react-query,
axios, recharts, @microsoft/signalr
```

Dev: Vite / TypeScript / Tailwind / PostCSS. **No** `openai`, `langchain`, `@anthropic-ai/*`, Helm UI, Kafka JS client.

Solution projects (10): Domain, Application, Infrastructure, Mt5, Fix.CTrader, Api, Mt5Worker, FixWorker, Unit, Integration. **No** `ml-service`, **no** broker worker, **no** k8s operator project.

---

## 5. False positives (do not misread)

| Hit | Why it is not Kafka / K8s / LLM |
|---|---|
| `apps/web/package-lock.json:2969` integrity `…/LLMVyas0ljjA…` | SHA-512 **hex/base64 substring** `LLM`. No package named LLM. |
| `apps/web/package-lock.json:323` integrity `…Vj1jF3cPfxg7OAfoI7QnVKLoILlm2JF9pnV…` | Same class of hash collision on letters `LLM`. |
| Earlier swarm `k8s` hit in the same lockfile integrity (`…IG0K8sQC…`) | Letters `k8s` inside a hash. No `kubernetes` package. |
| Architecture lines 6, 295, 575–577, 2686–2689 | **Prohibition**, not a client. |
| Swarm reports A80 / A29 / C44 / this file | Audit prose. |
| Operator `.env` key `FEATURE_ML_SCORING_ENABLED` | Phase-6 **ML** flag name. **Zero** C# / TS references this pass. Not an LLM SDK and not a Kafka client. Do not treat it as “LLM is wired.” |

Vendor `mt5-sdk/vendor` was **not** scored as product. It is the MetaQuotes Manager SDK. It is not Confluent, not Helm, not OpenAI.

---

## 6. Item-by-item (the three named in the task)

### 6.1 Kafka — ABSENT (correct)

**Law.** §13 + §71. At ~5,000 accounts a broker is premature.

**Forbidden packages (A80 §6, still absent from every csproj and every `project.assets.json`):** `Confluent.Kafka`, `MassTransit*`, `RabbitMQ.Client`, `NATS.Client`, `Azure.Messaging.ServiceBus`.

**Forbidden files:** `src/**/Kafka*.cs` — **0**.

**Legal substitute.** PostgreSQL `outbox_events` written in the **same commit** as the raw persist, then a poller. Five kinds already exist as an enum:

```1:10:D:\Prop\src\Domain\Enums\OutboxEventType.cs
namespace TraderIntelligence.Domain.Enums;

public enum OutboxEventType
{
    TradeCompleted = 0,
    ScoreUpdate = 1,
    ShadowCopyIntent = 2,
    RiskCheckRequest = 3,
    NotificationEvent = 4
}
```

| Check | Measured | Class |
|---|---|---|
| Kafka client / broker package | **0** | **`EXISTS_AND_GOOD`** |
| `OutboxEvent` + `OutboxEventType` | present (546 B / 211 B) | type exists |
| `IEventBus` / `IOutboxWriter` | **0** hits | **`MISSING`** (build this, **not** Kafka) |
| §12 same-TX outbox on deal persist | **not** this file’s scope; D45: demo `ScoreUpdate` souvenir only | do not “fix” with Kafka |
| Compose / Redis Streams-as-SoT | compose has Redis for cache, not an outbox | correct |

`OutboxEvent.cs` this pass: **546** bytes, SHA-256 `78108643D4C8E25DBEA767C30145366B3337C59D6E39EA3F613B480CDE6649A8`.  
`OutboxEventType.cs`: **211** bytes, SHA-256 `163ED842EE9AF0C94EA912A91845F31C8644F2A1A373A67C77E7FA16154BAADA` (unchanged vs D45).

**Do not.** Add Kafka “to buffer catch-up,” to fan-out `NewOrderSingle`, or as a feature store.

### 6.2 Kubernetes — ABSENT (correct)

**Law.** §5 + §71. Manager API is a Windows PE. A cluster does not load `MT5APIManager64.dll` on a Linux node (A54, A105, D63).

**Forbidden artifacts — all `Test-Path` = False:**

```text
D:\Prop\deploy\k8s
D:\Prop\charts
D:\Prop\helm
D:\Prop\Chart.yaml
D:\Prop\kustomization.yaml
D:\Prop\.github
D:\Prop\Dockerfile   (and every Dockerfile*)
```

Only YAML in the product tree: `D:\Prop\docker-compose.yml`.

| Field | Value |
|---|---|
| Path | `D:\Prop\docker-compose.yml` |
| Bytes | **687** |
| SHA-256 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` |
| Last write (this pass) | unchanged vs D63 |
| Service keys | `postgres`, `redis`, `api` |
| Kafka / Redpanda / NATS / ClickHouse service | **none** |
| `kind:` / Helm / `platform:` | **none** |

Line 30 of compose (quoted, not edited):

> `# Native MT5 Manager DLL workers stay on Windows hosts. Do not put them in Linux containers.`

**Legal substitute (already the file on disk):** Linux compose for Postgres 16 + Redis 7 + API; Windows host process for `apps/mt5-worker`. That is §5. It is **not** K8s.

`docs/deployment.md` describes the same Windows/Linux split and a simplified compose snippet (`api` / `db` / `redis`). **Zero** “Kubernetes” / “Helm” / “kubectl” words in `docs/`.

**A80 §5 stale row:** A80 recorded `docker-compose.yml` as **MISSING**. That row is **stale**. Compose exists (B37 / C12 / D63 / this hash). Absence of K8s is still correct.

**Do not.** Add Helm “to look production-ready,” a DaemonSet of the Manager DLL, or Wine the PE.

### 6.3 LLM / AI API — ABSENT (correct)

**Law.** Header: *AI/LLM dependency: None.* §5: *No LLM API is required.* §71 fourth row. Scoring is deterministic first (§18, §69 item 7). An LLM is not a scorer, not a risk engine, not a FIX codec, not a symbol mapper (A80 §3.4).

**Forbidden packages — still absent:** `Microsoft.SemanticKernel`, `Azure.AI.OpenAI`, `OpenAI`, LangChain, any chat-completions client.

**Forbidden path:** `D:\Prop\services\ml-service` — **does not exist**. `Get-ChildItem -Force D:\Prop\services` = **0 children**.

**Legal substitute.** In-process `TraderIntelligence.Domain.Scoring.BaselineScorer` (rules + statistics). Humans read the React dashboard. C44: that class is **NOT ML**. It is also **not** an LLM.

| Check | Measured | Class |
|---|---|---|
| OpenAI / SK / LangChain package | **0** | **`EXISTS_AND_GOOD`** |
| `services/` children | **0** | **`EXISTS_AND_GOOD`** (Phase 6 still closed) |
| Product C# `LLM` / `OpenAI` / `ChatGPT` | **0** | **`EXISTS_AND_GOOD`** |
| Env keys `OPENAI_*` / `ANTHROPIC_*` / `LLM_*` | **0** | **`EXISTS_AND_GOOD`** |
| Dashboard chat / “ask the model” page | **0** (pages are traders / risk / FIX / recon / …) | **`EXISTS_AND_GOOD`** |

**Do not.** Call an LLM from the MT5 callback, from scoring, from risk, or from ClOrdID / symbol discovery. Do not stub a fake `mlProbability` so the UI looks complete (A26, C44). Phase 6, when it opens, is still XGBoost — **still not** an LLM (A52, A80).

---

## 7. Files hashed (this pass)

| Bytes | Lines | SHA-256 | Path |
|---:|---:|---|---|
| 50966 | 2116 | `0B3C0EDC09081C25D097FF0E6AADC7A638562EBB8DB345DC325DC54EC904D37E` | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| 687 | 27 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | `D:\Prop\docker-compose.yml` |
| 269 | 9 | `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0` | `D:\Prop\Directory.Build.props` |
| 7019 | 93 | `AD5030070166D81EE478B632BCE3F381F2C9D3FFBBC6EB0FDD407047C5B5A7B4` | `D:\Prop\Mt5TraderIntelligence.sln` |
| 1746 | 33 | `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764` | `D:\Prop\README.md` |
| 739 | 30 | `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6` | `D:\Prop\apps\web\package.json` |
| 546 | 14 | `78108643D4C8E25DBEA767C30145366B3337C59D6E39EA3F613B480CDE6649A8` | `D:\Prop\src\Domain\Entities\OutboxEvent.cs` |
| 211 | 9 | `163ED842EE9AF0C94EA912A91845F31C8644F2A1A373A67C77E7FA16154BAADA` | `D:\Prop\src\Domain\Enums\OutboxEventType.cs` |
| 218 | 7 | `E151F959964EB450A5B86B72765E3F9C505645FA9516EAE485743D2B43911C8E` | `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj` |
| 433 | 13 | `44E3448AE56A9D79BF562F6D68B6CC52915E6B334C3F49D7AE9E9C2313AA9DE2` | `D:\Prop\src\Application\TraderIntelligence.Application.csproj` |
| 1035 | 21 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` | `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` |
| 419 | 11 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` |
| 419 | 11 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` |
| 803 | 17 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | `D:\Prop\apps\api\TraderIntelligence.Api.csproj` |
| 840 | 17 | `E0321028B0E12EEFE97A9BE2D0A08E8E8F89B819CCA4403D301B76A90C56B91C` | `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` |
| 856 | 17 | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` | `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` |
| 1113 | 25 | `EB7A4ECA27D4953313F58129C6494BE556AE616FDB9260DCA1112D4C2FEC7F50` | `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` |
| 1328 | 27 | `E749992347A22BB8241B76DA8A9008CFCA2C74F567C070A64D7B7B79B4F6E4F4` | `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` |
| 19608 | 262 | `A17533AC94A8E45EAF56B25CDD247501D8FDA631783753669D24C02927333B1F` | `D:\Prop\reports\swarm\20260818\A80_not_to_build.md` (law sibling, not product) |

Compose hash matches D63. Architecture hash matches D58. Domain/Mt5/Fix csproj hashes show **zero** extra package groups.

---

## 8. What is still in scope (do not over-ban)

```text
[IN]  PostgreSQL 16 as SoT
[IN]  Redis for cache / coordination / later FIX lease — not a Kafka stand-in
[IN]  PostgreSQL transactional outbox (finish the poller; D45 is not §12)
[IN]  IEventBus as a thin seam (outbox now; broker only later, behind it)
[IN]  docker-compose.yml for Linux data plane (already on disk)
[IN]  Windows mt5-worker + native Manager DLL
[IN]  Deterministic BaselineScorer (not an LLM)
[IN]  Phase 6 XGBoost later — still no LLM/DNN/RL
```

```text
[DO NOT] Kafka / MassTransit / NATS / Rabbit / Service Bus
[DO NOT] Kubernetes / Helm / kustomize / operators
[DO NOT] LLM / AI API / Semantic Kernel / chat-completions
[DO NOT] ClickHouse / DNN / RL / mesh / active-active TRADE / auto-promote  (A80; not re-opened here)
```

Copy-paste reject list if a PR adds any of:

```text
Confluent.Kafka
MassTransit
RabbitMQ.Client
NATS.Client
Azure.Messaging.ServiceBus
KubernetesClient
Microsoft.SemanticKernel
Azure.AI.OpenAI
OpenAI
LangChain
deploy/k8s/**
charts/**
kustomization.yaml
services/ml-service/**          (not until Phase 6 is actually open — and even then not an LLM)
src/**/Kafka*.cs
```

---

## 9. Stale siblings (honesty)

| Prior row | Then | Now (this hash) |
|---|---|---|
| A80 §5 `docker-compose.yml` **MISSING** | true at A80 write | **EXISTS** — postgres/redis/api; still no Kafka/K8s (D63 + this file) |
| A80 §5 `IEventBus` + outbox migration **MISSING** | true | **Still missing** `IEventBus`. Entity + enum exist; D45 write is not §12 |
| A03 “no Kafka package” | true | **Still true** |
| C44 “no LLM / no ml-service” | true | **Still true** — `services/` still empty |
| PHASE0_AUDIT `Kafka/K8s/LLM \| DEPRECATED / not to build` | classification | **Still the right classification** |

---

## 10. Sign-off

```text
[x] §71 / §13 / §5 / header quoted
[x] Kafka confirmed ABSENT in product source; substitute = Postgres outbox (not finished)
[x] Kubernetes confirmed ABSENT; substitute = Windows worker + Linux compose (compose exists)
[x] LLM confirmed ABSENT; header + §5; substitute = BaselineScorer (not an LLM)
[x] 10 product csproj + 10 project.assets.json have no forbidden packages
[x] package-lock “LLM” hits classified as integrity false positives
[x] A80 compose-MISSING row marked stale
[x] Product source not modified
[x] Measured 2026-08-18 13:44:03 +05:30; HEAD 398a142
```

**Status:** Kafka, K8s, and LLM are **not built** and **must not be built**. That absence is a §71 pass, not a backlog item.
