using Microsoft.EntityFrameworkCore;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Infrastructure.Seeding;

var options = new DbContextOptionsBuilder<TraderDbContext>()
    .UseInMemoryDatabase("d37-" + Guid.NewGuid())
    .Options;
await using var db = new TraderDbContext(options);
var store = new EfTradingStore(db);
var scoring = new ReconstructionScoringService(store, new TradeReconstructor(), new BaselineScorer());
await DemoSeeder.SeedAsync(db, store, scoring, CancellationToken.None);

void Dump(string name, int n) => Console.WriteLine($"{name}\t{n}");

Dump("Brokers", db.Brokers.Count());
Dump("Mt5Groups", db.Mt5Groups.Count());
Dump("Mt5Accounts", db.Mt5Accounts.Count());
Dump("Mt5Deals", db.Mt5Deals.Count());
Dump("Mt5Positions", db.Mt5Positions.Count());
Dump("ReconstructedTrades", db.ReconstructedTrades.Count());
Dump("ReconstructedCompletedXau", db.ReconstructedTrades.Count(t => t.Completed && t.CanonicalSymbol == "XAUUSD"));
Dump("TraderScores", db.TraderScores.Count());
Dump("TraderScoreHistory", db.TraderScoreHistory.Count());
Dump("FixSessionStates", db.FixSessionStates.Count());
Dump("DestinationQuotes", db.DestinationQuotes.Count());
Dump("KillSwitches", db.KillSwitches.Count());
Dump("CanonicalInstruments", db.CanonicalInstruments.Count());
Dump("OutboxEvents", db.OutboxEvents.Count());
Dump("CopyIntents", db.CopyIntents.Count());
Dump("ShadowOrders", db.ShadowOrders.Count());
Dump("SyncCheckpoints", db.SyncCheckpoints.Count());
Dump("SourceSymbolMappings", db.SourceSymbolMappings.Count());
Dump("AuditLogs", db.AuditLogs.Count());
Dump("RiskDecisions", db.RiskDecisions.Count());
Dump("ExecutionIntents", db.ExecutionIntents.Count());

Console.WriteLine("---BROKERS---");
foreach (var b in db.Brokers.OrderBy(x => x.Code))
    Console.WriteLine($"{b.Code}\t{b.Id}\t{b.Server}\t{b.ManagerLogin}");

Console.WriteLine("---GROUPS---");
foreach (var g in db.Mt5Groups.OrderBy(x => x.Name))
    Console.WriteLine($"{g.BrokerId}\t{g.Name}");

Console.WriteLine("---ACCOUNTS---");
foreach (var a in db.Mt5Accounts.OrderBy(x => x.Login))
    Console.WriteLine($"{a.BrokerId}\t{a.Login}\t{a.GroupName}");

Console.WriteLine("---SCORES---");
foreach (var s in db.TraderScores.OrderBy(x => x.Login))
    Console.WriteLine($"{s.Login}\t{s.CurrentState}\tXau={s.CompletedXauTrades}\tMart={s.Martingale}\tAvg={s.AveragingDown}\tLot={s.LotEscalation}\tRisk={s.RiskScore}\tQual={s.EarlyQualityScore}");

Console.WriteLine("---FIX---");
foreach (var f in db.FixSessionStates.OrderBy(x => x.Qualifier))
    Console.WriteLine($"{f.Qualifier}\t{f.Status}\t{f.Host}\t{f.Port}\t{f.SenderCompId}\t{f.TargetCompId}\t{f.LastError}");

Console.WriteLine("---OUTBOX---");
foreach (var o in db.OutboxEvents)
    Console.WriteLine($"{o.Type}\t{o.AggregateId}\t{o.PayloadJson}");

Console.WriteLine("---COPY---");
foreach (var c in db.CopyIntents.OrderBy(x => x.IdempotencyKey))
    Console.WriteLine($"{c.SourceLogin}\t{c.Status}\t{c.IdempotencyKey}\t{c.RequestedQuantity}");

var before = db.Mt5Deals.Count();
await DemoSeeder.SeedAsync(db, store, scoring, CancellationToken.None);
Console.WriteLine($"RESEED_DEALS\t{db.Mt5Deals.Count()}\twas={before}");
Console.WriteLine($"RESEED_SCORES\t{db.TraderScores.Count()}");
Console.WriteLine($"RESEED_OUTBOX\t{db.OutboxEvents.Count()}");
