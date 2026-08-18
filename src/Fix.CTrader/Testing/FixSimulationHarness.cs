using System;
using System.Collections.Generic;
using System.Linq;
using TraderIntelligence.Fix.CTrader.Parsing;

namespace TraderIntelligence.Fix.CTrader.Testing;

/// <summary>
/// Generates cTrader-like FIX responses for unit tests (no live FIX connection required).
/// All returned messages use '|' separators as accepted by <see cref="FixMessageParser"/>.
/// </summary>
public sealed class FixSimulationHarness
{
    private readonly FixMessageParser _parser = new();

    public string SimulateLogonSuccess(string senderCompId, string senderSubId, string targetCompId = "CSERVER", string targetSubId = "QUOTE")
        => BuildStandardMessage(
            new[] {
                (8, "FIX.4.4"),
                (35, "A"), // Logon
                (49, senderCompId),
                (56, targetCompId),
                (57, targetSubId),
                (50, senderSubId),
                (98, "0"), // EncryptMethod
                (108, "30"), // HeartBtInt
                (141, "Y") // ResetSeqNumFlag
            });

    public string SimulateLogonFail(string senderCompId, string senderSubId, string reason = "InvalidCredentials", string targetCompId = "CSERVER", string targetSubId = "TRADE")
        => BuildStandardMessage(
            new[] {
                (8, "FIX.4.4"),
                (35, "3"), // Reject (simplified)
                (49, senderCompId),
                (56, targetCompId),
                (57, targetSubId),
                (50, senderSubId),
                (45, "2"), // RefSeqNum (simplified)
                (371, reason) // Text
            });

    public string SimulateExecutionReport_New(string clOrdId, string orderId, string symbol = "XAUUSD", string senderCompId = "SENDER", string senderSubId = "TRADE", string execTransType = "0")
        => BuildStandardMessageWithExecReport(
            clOrdId: clOrdId,
            orderId: orderId,
            symbol: symbol,
            execType: execTransType, // 0=New
            ordStatus: "0",
            senderCompId: senderCompId,
            senderSubId: senderSubId);

    public string SimulateExecutionReport_Fill(string clOrdId, string orderId, string symbol = "XAUUSD", string senderCompId = "SENDER", string senderSubId = "TRADE", decimal lastQty = 1m, decimal lastPx = 2400m)
        => BuildStandardMessageWithExecReport(
            clOrdId: clOrdId,
            orderId: orderId,
            symbol: symbol,
            execType: "F", // F=Trade/Filling
            ordStatus: "2",
            senderCompId: senderCompId,
            senderSubId: senderSubId,
            lastQty: lastQty,
            lastPx: lastPx);

    public string SimulateExecutionReport_PartialFill(string clOrdId, string orderId, string symbol = "XAUUSD", string senderCompId = "SENDER", string senderSubId = "TRADE", decimal lastQty = 0.5m, decimal lastPx = 2400m)
        => BuildStandardMessageWithExecReport(
            clOrdId: clOrdId,
            orderId: orderId,
            symbol: symbol,
            execType: "F",
            ordStatus: "1", // Partial fill
            senderCompId: senderCompId,
            senderSubId: senderSubId,
            lastQty: lastQty,
            lastPx: lastPx);

    public string SimulateExecutionReport_Canceled(string clOrdId, string orderId, string symbol = "XAUUSD", string senderCompId = "SENDER", string senderSubId = "TRADE")
        => BuildStandardMessageWithExecReport(
            clOrdId: clOrdId,
            orderId: orderId,
            symbol: symbol,
            execType: "4", // 4=Canceled
            ordStatus: "4",
            senderCompId: senderCompId,
            senderSubId: senderSubId);

    public string SimulateExecutionReport_Rejected(string clOrdId, string orderId, string text = "Rejected", string symbol = "XAUUSD", string senderCompId = "SENDER", string senderSubId = "TRADE")
        => BuildStandardMessageWithExecReport(
            clOrdId: clOrdId,
            orderId: orderId,
            symbol: symbol,
            execType: "8", // 8=Rejected
            ordStatus: "8",
            senderCompId: senderCompId,
            senderSubId: senderSubId,
            text: text);

    public string SimulateExecutionReport_Expired(string clOrdId, string orderId, string symbol = "XAUUSD", string senderCompId = "SENDER", string senderSubId = "TRADE")
        => BuildStandardMessageWithExecReport(
            clOrdId: clOrdId,
            orderId: orderId,
            symbol: symbol,
            execType: "C", // C=Expired
            ordStatus: "C",
            senderCompId: senderCompId,
            senderSubId: senderSubId);

    public string SimulateExecutionReport_UnknownState(string clOrdId, string orderId, string symbol = "XAUUSD", string senderCompId = "SENDER", string senderSubId = "TRADE")
        => BuildStandardMessageWithExecReport(
            clOrdId: clOrdId,
            orderId: orderId,
            symbol: symbol,
            execType: "I", // I=Status (we'll treat this as "unknown state" in service)
            ordStatus: "0",
            senderCompId: senderCompId,
            senderSubId: senderSubId);

    public string SimulateDuplicateExecutionReport(string duplicateExecutionReport)
        => duplicateExecutionReport; // In tests, just send the same raw string twice.

    public string SimulateDisconnect(string text = "Connection dropped")
        => BuildStandardMessage(
            new[] {
                (8, "FIX.4.4"),
                (35, "0"), // Heartbeat (used as placeholder)
                (1128, text) // TestMessageIndicator-like field (simplified)
            });

    public string SimulateSecurityList(string senderCompId = "SENDER", string senderSubId = "QUOTE", string targetCompId = "CSERVER", string targetSubId = "QUOTE")
    {
        // Extremely simplified. Only the tags our services look at:
        // 1007 = SymbolName, 55 = InstrumentID (numeric)
        // cTrader often returns instruments in a repeating group, but for unit tests we model both instrument fields as separate tags.
        return BuildStandardMessage(new[] {
            (8, "FIX.4.4"),
            (35, "y"), // SecurityList (simplified; actual MsgType for SecurityList is gateway-specific)
            (49, senderCompId),
            (56, targetCompId),
            (57, targetSubId),
            (50, senderSubId),
            (55, "123456"), // XAUUSD instrument numeric ID (as string)
            (1007, "XAUUSD")
        });
    }

    public string SimulateMarketDataSnapshot(string symbolIdNumeric, string senderCompId = "SENDER", string senderSubId = "QUOTE", string bid = "2399.50", string ask = "2400.50")
    {
        // Simplified: bid/ask plus time. In real FIX market data, these are in MDIncGrp (269/270/271/etc).
        // For unit tests we use tags that services can map from.
        var now = DateTimeOffset.UtcNow;
        return BuildStandardMessage(new[] {
            (8, "FIX.4.4"),
            (35, "X"), // MarketDataSnapshotFullRefresh (simplified)
            (49, senderCompId),
            (56, "CSERVER"),
            (57, "QUOTE"),
            (50, senderSubId),
            (55, symbolIdNumeric),
            (1320, bid), // Custom-ish: we'll reuse 1320 for tests as "Bid"
            (1321, ask), // 1321 for tests as "Ask"
            (60, now.ToString("yyyyMMdd-HH:mm:ss.fff"))
        });
    }

    private string BuildStandardMessage(IEnumerable<(int tag, string value)> tagValues)
    {
        return _parser.BuildFixMessage(tagValues.Select(t => new KeyValuePair<int, string>(t.tag, t.value)));
    }

    private string BuildStandardMessageWithExecReport(
        string clOrdId,
        string orderId,
        string symbol,
        string execType,
        string ordStatus,
        string senderCompId,
        string senderSubId,
        decimal lastQty = 0m,
        decimal lastPx = 0m,
        string text = "")
    {
        var tags = new List<(int tag, string value)>
        {
            (8, "FIX.4.4"),
            (35, "8"), // ExecutionReport
            (49, senderCompId),
            (56, "CSERVER"),
            (57, "TRADE"),
            (50, senderSubId),
            (11, clOrdId), // ClOrdID
            (37, orderId), // OrderID
            (55, symbol),
            (150, execType),
            (39, ordStatus),
            (60, DateTimeOffset.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff")),
        };

        if (lastQty != 0m) tags.Add((32, lastQty.ToString(System.Globalization.CultureInfo.InvariantCulture))); // LastQty
        if (lastPx != 0m) tags.Add((31, lastPx.ToString(System.Globalization.CultureInfo.InvariantCulture))); // LastPx
        if (!string.IsNullOrEmpty(text)) tags.Add((58, text)); // Text

        return BuildStandardMessage(tags);
    }
}

