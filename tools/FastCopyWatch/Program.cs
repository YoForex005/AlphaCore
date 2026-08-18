using System.Text.Json;
using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Domain.Copy;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Instruments;
using TraderIntelligence.Fix.CTrader.Sessions;
using TraderIntelligence.Infrastructure.Copy;
using TraderIntelligence.Infrastructure.Mt5Live;
using TraderIntelligence.Mt5.Env;

EnvFile.FindAndLoad();

var host = Environment.GetEnvironmentVariable("CTRADER_FIX_HOST") ?? "";
var sender = Environment.GetEnvironmentVariable("CTRADER_FIX_TRADE_SENDER_COMP_ID") ?? "";
var target = Environment.GetEnvironmentVariable("CTRADER_FIX_TRADE_TARGET_COMP_ID") ?? "cServer";
var account = Environment.GetEnvironmentVariable("CTRADER_FIX_ACCOUNT_ID") ?? "";
var password = Environment.GetEnvironmentVariable("CTRADER_FIX_PASSWORD") ?? "";

if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase) || account == "1369850")
{
    Console.WriteLine("REFUSED live dest");
    return 2;
}

var symbols = new SymbolNormalizer();
var connectors = LiveMt5Registration.CreateConnectorsFromEnvironment();
foreach (var c in connectors)
    await c.ConnectAsync(CancellationToken.None);

var allow = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase);
async Task RefreshAllow()
{
    foreach (var c in connectors)
    {
        var accounts = await c.GetAccountsAsync(null, CancellationToken.None);
        allow[c.BrokerCode] = accounts
            .Where(a => CopyGroupFilter.IsDemoOrContest(a.GroupName))
            .Select(a => a.Login)
            .ToHashSet();
        Console.WriteLine("ALLOW " + c.BrokerCode + " demo/contest logins=" + allow[c.BrokerCode].Count);
    }
}

await RefreshAllow();

var baselinePath = @"D:\Prop\data\copy_watch_baseline.json";
var baseline = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
if (File.Exists(baselinePath))
{
    try
    {
        var loaded = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(baselinePath));
        if (loaded is not null)
            foreach (var k in loaded)
                baseline.Add(k);
    }
    catch { /* first run */ }
}

async Task<Dictionary<string, List<Mt5PositionDto>>> LoadBooks()
{
    var books = new Dictionary<string, List<Mt5PositionDto>>(StringComparer.OrdinalIgnoreCase);
    foreach (var conn in connectors)
    {
        if (conn is not IMt5BulkPositionReader bulk)
            continue;
        try
        {
            var rows = (await bulk.GetGroupPositionsAsync("*", CancellationToken.None)).ToList();
            if (!CopyLifecycle.TrustManagerBook(rows.Count))
            {
                Console.WriteLine("BOOK_EMPTY " + conn.BrokerCode + " skip closes");
                continue;
            }
            books[conn.BrokerCode] = rows;
        }
        catch (Exception ex)
        {
            Console.WriteLine("BOOK_FAIL " + conn.BrokerCode + " " + ex.GetType().Name + " " + ex.Message);
        }
    }
    return books;
}

static HashSet<string> Tickets(IEnumerable<Mt5PositionDto> book) =>
    book.Select(p => p.PositionTicket.ToString()).ToHashSet();

var books = await LoadBooks();
foreach (var (broker, book) in books)
{
    foreach (var p in book)
        baseline.Add(broker + ":" + p.Login + ":" + p.PositionTicket);
}
Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
File.WriteAllText(baselinePath, JsonSerializer.Serialize(baseline.ToList()));

async Task<DestBookResult> DestSnapshot()
{
    var dest = await CTraderFixDestBook.RequestAsync(host, sender, target, account, password, CancellationToken.None);
    Console.WriteLine(
        "DEST_BOOK complete=" + dest.Complete + " venueOpen=" + dest.Positions.Count
        + " err=" + (dest.Error ?? ""));
    return dest;
}

void WriteReconcile(
    IReadOnlyList<DemoCopyFill> ledger,
    Dictionary<string, List<Mt5PositionDto>> liveBooks,
    DestBookResult? dest)
{
    var destOpenIds = dest is { Complete: true }
        ? dest.Positions.Select(p => p.PosId).ToHashSet(StringComparer.Ordinal)
        : null;
    var liveByBroker = liveBooks.ToDictionary(
        kv => kv.Key,
        kv => Tickets(kv.Value),
        StringComparer.OrdinalIgnoreCase);

    var open = ledger.Where(f => !f.DestClosed && !string.IsNullOrWhiteSpace(f.DestPositionId)).ToList();
    var masterLive = 0;
    var masterGone = 0;
    var destAlreadyFlat = 0;
    var destStillOpen = 0;
    foreach (var f in open)
    {
        liveByBroker.TryGetValue(f.Broker ?? "ACHIEVER", out var live);
        var masterInBook = live is not null && live.Contains(f.SourcePositionId);
        if (masterInBook) masterLive++;
        else masterGone++;
        if (destOpenIds is not null && destOpenIds.Contains(f.DestPositionId!)) destStillOpen++;
        if (destOpenIds is not null && !destOpenIds.Contains(f.DestPositionId!)) destAlreadyFlat++;
    }

    var snap = new
    {
        updatedUtc = DateTimeOffset.UtcNow,
        dest = "demo.pepperstone.5328266",
        ledgerOpen = open.Count,
        ledgerClosed = ledger.Count(f => f.DestClosed),
        destVenueOpen = dest?.Positions.Count,
        destBookComplete = dest?.Complete,
        destBookError = dest?.Error,
        masterStillOpen = masterLive,
        masterGoneShouldClose = masterGone,
        destAlreadyFlat,
        destVenueStillOpen = destStillOpen
    };
    var json = JsonSerializer.Serialize(snap, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(@"D:\Prop\data\copy_reconcile.json", json);
    try
    {
        Directory.CreateDirectory(@"D:\Prop\apps\web\public");
        File.WriteAllText(@"D:\Prop\apps\web\public\copy-reconcile.json", json);
    }
    catch { /* dashboard snapshot is best-effort */ }

    Console.WriteLine(
        "VALIDATE ledgerOpen=" + snap.ledgerOpen
        + " ledgerClosed=" + snap.ledgerClosed
        + " destVenueOpen=" + snap.destVenueOpen
        + " masterLive=" + snap.masterStillOpen
        + " masterGone=" + snap.masterGoneShouldClose
        + " destAlreadyFlat=" + snap.destAlreadyFlat);
}

var dest0 = await DestSnapshot();
WriteReconcile(DemoCopyLedger.Load(), books, dest0);
Console.WriteLine("FAST_COPY_WATCH auto-close: Manager book vs dest 35=AN; new tickets only; 500ms");

var ticks = 0;
while (true)
{
    try
    {
        ticks++;
        if (ticks % 120 == 0)
            await RefreshAllow();

        var ledger = DemoCopyLedger.Load();
        var known = ledger
            .Where(f => !string.IsNullOrWhiteSpace(f.DestPositionId))
            .Select(f => (f.Broker ?? "ACHIEVER") + ":" + f.SourceLogin + ":" + f.SourcePositionId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        books = await LoadBooks();
        DestBookResult? destBook = null;
        if (ticks == 1 || ticks % 10 == 0)
            destBook = await DestSnapshot();

        var ledgerOpen = ledger.Count(f => !f.DestClosed && !string.IsNullOrWhiteSpace(f.DestPositionId));
        var destOpen = destBook is not null
                       && CopyLifecycle.TrustDestVenueSnapshot(destBook.Complete, destBook.Positions.Count, ledgerOpen)
            ? destBook.Positions.Select(p => p.PosId).ToHashSet(StringComparer.Ordinal)
            : null;

        foreach (var conn in connectors)
        {
            if (!books.TryGetValue(conn.BrokerCode, out var book))
                continue;
            var liveTickets = Tickets(book);

            foreach (var fill in ledger.Where(f =>
                         !f.DestClosed
                         && !string.IsNullOrWhiteSpace(f.DestPositionId)
                         && string.Equals(f.Broker ?? "ACHIEVER", conn.BrokerCode, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                if (destOpen is not null && !destOpen.Contains(fill.DestPositionId!))
                {
                    fill.DestClosed = true;
                    DemoCopyLedger.Save(ledger);
                    Console.WriteLine("MARK_FLAT dest=" + fill.DestPositionId + " " + fill.SourceLogin + "/" + fill.SourcePositionId);
                    continue;
                }

                var masterLive = liveTickets.Contains(fill.SourcePositionId);
                if (!CopyLifecycle.ShouldCloseDestBecauseMasterGone(masterLive, true, fill.DestClosed))
                    continue;

                var r = await CTraderFixCopyOpen.SendAsync(
                    host, sender, target, account, password,
                    fill.SourceLogin, fill.SourcePositionId, fill.IsLong, fill.Lots,
                    CancellationToken.None, fill.DestPositionId);
                Console.WriteLine(
                    $"CLOSE {fill.SourceLogin}/{fill.SourcePositionId} dest={fill.DestPositionId} filled={r.Filled} px={r.LastPx} err={r.Error}");
                if (r.Filled || r.OrderSent
                    || (!string.IsNullOrWhiteSpace(r.Error)
                        && (r.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                            || r.Error.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
                            || r.Error.Contains("UNKNOWN", StringComparison.OrdinalIgnoreCase))))
                {
                    fill.DestClosed = true;
                    DemoCopyLedger.Save(ledger);
                }
            }

            allow.TryGetValue(conn.BrokerCode, out var logins);
            logins ??= [];
            var opened = 0;
            foreach (var p in book)
            {
                if (opened >= 1)
                    break;
                if (!logins.Contains(p.Login))
                    continue;
                if (!symbols.TryMapSource(p.Symbol, out var canon) || canon != "XAUUSD")
                    continue;
                var id = conn.BrokerCode + ":" + p.Login + ":" + p.PositionTicket;
                if (known.Contains(id) || baseline.Contains(id))
                    continue;
                var lots = p.VolumeNative / 10_000m;
                if (lots <= 0)
                    continue;
                var isLong = p.Direction == TradeDirection.Long;
                var r = await CTraderFixCopyOpen.SendAsync(
                    host, sender, target, account, password,
                    p.Login.ToString(), p.PositionTicket.ToString(), isLong, lots, CancellationToken.None);
                Console.WriteLine($"OPEN {conn.BrokerCode}/{p.Login}/{p.PositionTicket} lots={lots} filled={r.Filled} dest={r.PosId} err={r.Error}");
                if (!r.Filled)
                    continue;
                ledger.Add(new DemoCopyFill
                {
                    Broker = conn.BrokerCode,
                    SourceLogin = p.Login.ToString(),
                    SourcePositionId = p.PositionTicket.ToString(),
                    IsLong = isLong,
                    Lots = lots,
                    DestPositionId = r.PosId,
                    DestClOrdId = r.ClOrdId,
                    DestFillPrice = decimal.TryParse(r.LastPx, out var px) ? px : null
                });
                known.Add(id);
                baseline.Add(id);
                DemoCopyLedger.Save(ledger);
                opened++;
            }
        }

        DemoCopyLedger.Save(ledger);
        File.WriteAllText(baselinePath, JsonSerializer.Serialize(baseline.ToList()));
        WriteReconcile(ledger, books, destBook);
    }
    catch (Exception ex)
    {
        Console.WriteLine("LOOP " + ex.GetType().Name + ": " + ex.Message);
    }

    await Task.Delay(500);
}
