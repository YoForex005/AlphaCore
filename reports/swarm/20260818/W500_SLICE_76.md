# W500_SLICE_76

- **slot:** 76
- **file:** `D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs`
- **angle:** dashboard hiding accounts that have no score yet
- **read:** full file (108 lines) via `read_file`; grep on this file for score/dashboard/hide/account; followed `ListLoginsAsync`, `SyncCatalogAsync`, `RebuildTraderAsync`, and current `EfDashboardQueries.GetTradersAsync` / `GetOverviewAsync` (A005 claim that traders are score-driven is **stale**)
- **verdict:** PASS

## Binding question (this angle)

Does live ingest cause, or implement, a dashboard that **omits accounts until a `TraderScore` exists**? Hiding the un-scored book would make a partial census look complete (missed martingale / averaging-down / live candidates) while `Mt5Accounts` already hold those logins.

## Evidence quotes

`LiveIngestHostedService` is the live ingest host (`AddHostedService<LiveIngestHostedService>`). It does **not** query traders for the UI. It does **not** filter `Mt5Accounts` by score. Catalog is written **before** scoring, then **every** stored login is rebuilt.

Phase order: connect → catalog → deals → scoring. Catalog counts are published on `LiveRuntimeStatus` independently of `Scored`:

```56:70:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
                    var catalog = await ingest.SyncCatalogAsync(connector.BrokerCode, stoppingToken);
                    st.Groups = catalog.Groups;
                    st.Accounts = catalog.Accounts;
                    st.Phase = "deals";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    _log.LogInformation("{Broker} catalog groups={Groups} accounts={Accounts}",
                        connector.BrokerCode, catalog.Groups, catalog.Accounts);

                    var deals = await ingest.SyncBrokerAsync(connector.BrokerCode, from, to, stoppingToken);
                    st.DealsInserted = deals;
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsAsync(brokerId, stoppingToken);
                    st.Accounts = Math.Max(st.Accounts, logins.Count);
                    st.Phase = "scoring";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
```

`SyncCatalogAsync` (callee, invoked from this host **before** the score loop) upserts **all** Manager accounts, not scored traders:

```37:50:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
    public async Task<BrokerSyncResult> SyncCatalogAsync(string brokerCode, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;

        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);

        return new BrokerSyncResult(groups.Count, accounts.Count, 0, 0);
    }
```

The score loop is over `ListLoginsAsync` (all `Mt5Accounts` for the broker), not over existing `TraderScores`. There is no `if (hasScore)`, no skip of zero-trade logins, no dashboard delete:

```72:91:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
                    var scored = 0;
                    foreach (var login in logins)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
                        scored++;
                        if (scored % 50 == 0)
                        {
                            st.Scored = scored;
                            st.UpdatedAt = DateTimeOffset.UtcNow;
                        }
                    }

                    st.Scored = scored;
                    st.Phase = "done";
                    st.Connected = true;
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    _log.LogInformation(
                        "{Broker} ingest done. dealsInserted={Deals} accounts={Accounts} scored={Scored}",
                        connector.BrokerCode, deals, logins.Count, scored);
```

`ListLoginsAsync` is the full account census:

```339:341:D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

`RebuildTraderAsync` **always** `UpsertScoreAsync`s, including `INSUFFICIENT_DATA` / zero-XAU books. An account that has been through this loop is not “score-less”; it has a score row even with no completed XAU trades.

Runtime snapshot exposes **both** `Accounts` and `Scored` (progress, not a hide list):

```45:58:D:/Prop/src/Application/Runtime/LiveRuntimeStatus.cs
        brokers = Brokers.Values
            .OrderBy(b => b.BrokerCode)
            .Select(b => new
            {
                b.BrokerCode,
                b.Connected,
                b.LastError,
                b.Groups,
                b.Accounts,
                b.DealsInserted,
                b.Positions,
                b.Scored,
                b.Phase,
                b.UpdatedAt
            }),
```

Current dashboard (not this file; needed to close the hide claim) iterates **`Mt5Accounts`**, left-joins scores, and defaults missing scores to `INSUFFICIENT_DATA` / `0`. Unscored logins stay on `/traders`:

```96:117:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
        var mapped = new List<TraderRowDto>();
        foreach (var account in accounts)
        {
            if (!brokers.TryGetValue(account.BrokerId, out var b))
                continue;
            scoreMap.TryGetValue((account.BrokerId, account.Login), out var s);
            pnlMap.TryGetValue((account.BrokerId, account.Login), out var pnl);
            mapped.Add(new TraderRowDto(
                b.Code,
                account.Login,
                account.GroupName,
                s?.CompletedXauTrades ?? 0,
                pnl,
                s?.EarlyQualityScore ?? 0,
                null,
                s?.RiskScore ?? 0,
                s?.Martingale ?? false,
                s?.AveragingDown ?? false,
                s?.LotEscalation ?? false,
                s?.CurrentState ?? TraderState.INSUFFICIENT_DATA,
                0,
                s?.LastScoredAt ?? account.LastSyncedAt));
        }
```

Overview `totalAccounts` is `Mt5Accounts.CountAsync`, not `TraderScores.Count`. Broker/group pages count `Mt5Accounts` the same way.

This file does **not** contain:

- `GetTradersAsync` / `TraderScores` as a driver set / `Where(s => s.EarlyQualityScore > 0)`
- any hide / skip / delete of accounts lacking a score
- a dashboard DTO or UI filter
- dummy substitution on failure (`No dummy data will be substituted.`)

Mid-loop window: after catalog, before `RebuildTraderAsync` finishes a login, that login is already in `Mt5Accounts` and appears on the traders query with default `INSUFFICIENT_DATA`. That is **visibility of the un-scored book**, not hiding.

Residual (out of this file’s type, not a hide): one-shot host, swallowed per-broker catch, no resync — those can **omit** Manager logins that never reached `Mt5Accounts`. That is census failure, not “hide until scored.” Score-bucket counts on overview (`Watch`/`Shadow`/`Live`) still come from `TraderScores` only; those are state tallies, not the traders table.

## No-loss implication

This host never sends orders. Catalog-first + score-all + dashboard-by-account means operators still see every **ingested** login while `Scored` is catching up. Hiding the un-scored set would be the capital-relevant lie (invisible martingale / averaging-down / live-candidate books). This file does not implement that hide.

Worst case inside this type is a thrown/cancelled score mid-loop or a failed broker (`Phase = "failed"`): remaining logins stay on the account list as `INSUFFICIENT_DATA` until scored, and no dummy books are injected. Visibility of a zero score is not a live fill and does not arm copy.

Empty-PASS justification: the assigned file was fully read (108 lines). Absence of dashboard-hide logic is measured from the catalog-before-score sequence, the unfiltered `ListLoginsAsync` loop, split `Accounts`/`Scored` status, and the current account-driven `GetTradersAsync` join — not from skipped review.
