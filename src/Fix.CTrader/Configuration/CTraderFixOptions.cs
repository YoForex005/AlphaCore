using System;

namespace TraderIntelligence.Fix.CTrader.Configuration;

public sealed class CTraderFixOptions
{
    /// <summary>
    /// FIX gateway host (cTrader).
    /// </summary>
    public string Host { get; set; } = "demo-us-eqx-01.p.c-trader.com";

    /// <summary>
    /// FIX username (AccountId). Must never be logged.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// FIX password. Must never be logged.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    public QuoteFixOptions Quote { get; set; } = new();

    public TradeFixOptions Trade { get; set; } = new();

    public bool UseSsl { get; set; } = true;

    public bool QuoteEnabled { get; set; } = true;

    public bool TradeSessionEnabled { get; set; } = true;

    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;

    public int HeartbeatIntervalSec { get; set; } = 30;

    public int MaxQuoteAgeMs { get; set; } = 5000;

    public sealed class QuoteFixOptions
    {
        public int SslPort { get; set; } = 5211;

        public int PlainPort { get; set; } = 5201;

        public string SenderCompId { get; set; } = "demo.pepperstone.5328266";

        public string TargetCompId { get; set; } = "cServer";

        public string TargetSubId { get; set; } = "QUOTE";

        /// <summary>
        /// SenderSubID for QUOTE session (configurable).
        /// </summary>
        public string SenderSubId { get; set; } = string.Empty;
    }

    public sealed class TradeFixOptions
    {
        public int SslPort { get; set; } = 5212;

        public int PlainPort { get; set; } = 5202;

        /// <summary>
        /// cTrader FIX gateway SenderCompID (configurable).
        /// </summary>
        public string SenderCompId { get; set; } = "demo.pepperstone.5328266";

        public string TargetCompId { get; set; } = "cServer";

        public string TargetSubId { get; set; } = "TRADE";

        /// <summary>
        /// SenderSubID for TRADE session (configurable).
        /// </summary>
        public string SenderSubId { get; set; } = string.Empty;
    }
}

