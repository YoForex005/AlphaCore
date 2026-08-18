using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Target = table.Column<string>(type: "text", nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: true),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "brokers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Server = table.Column<string>(type: "text", nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    ManagerLogin = table.Column<long>(type: "bigint", nullable: false),
                    ServerName = table.Column<string>(type: "text", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    PoolSize = table.Column<int>(type: "integer", nullable: false),
                    ProxyEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ProxyHost = table.Column<string>(type: "text", nullable: true),
                    ProxyPort = table.Column<int>(type: "integer", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brokers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "canonical_instruments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_canonical_instruments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "copy_intents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceLogin = table.Column<long>(type: "bigint", nullable: false),
                    SourceTradeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourcePositionId = table.Column<long>(type: "bigint", nullable: false),
                    CanonicalSymbol = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SourceEventTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: false),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExecutionIntentId = table.Column<Guid>(type: "uuid", nullable: true),
                    StopLoss = table.Column<decimal>(type: "numeric", nullable: true),
                    TakeProfit = table.Column<decimal>(type: "numeric", nullable: true),
                    LimitPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    StopTrigger = table.Column<decimal>(type: "numeric", nullable: true),
                    OrdType = table.Column<string>(type: "text", nullable: false),
                    DestClOrdId = table.Column<string>(type: "text", nullable: true),
                    DestPositionId = table.Column<string>(type: "text", nullable: true),
                    DestFillPrice = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_copy_intents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "destination_quotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalSymbol = table.Column<string>(type: "text", nullable: false),
                    VenueInstrumentId = table.Column<string>(type: "text", nullable: true),
                    Bid = table.Column<decimal>(type: "numeric", nullable: false),
                    Ask = table.Column<decimal>(type: "numeric", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VenueTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_destination_quotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "execution_intents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CopyIntentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationSymbol = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    VolumeLots = table.Column<decimal>(type: "numeric", nullable: false),
                    ClOrdId = table.Column<string>(type: "text", nullable: true),
                    FixOrderId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FilledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FillPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    RejectReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_execution_intents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fix_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Qualifier = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Host = table.Column<string>(type: "text", nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    SenderCompId = table.Column<string>(type: "text", nullable: false),
                    TargetCompId = table.Column<string>(type: "text", nullable: false),
                    SenderSubId = table.Column<string>(type: "text", nullable: true),
                    TargetSubId = table.Column<string>(type: "text", nullable: true),
                    InboundSeq = table.Column<int>(type: "integer", nullable: false),
                    OutboundSeq = table.Column<int>(type: "integer", nullable: false),
                    LastInboundAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastOutboundAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReconnectCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    OwnerHeld = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerInstance = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fix_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "kill_switches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    SetBy = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kill_switches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mt5_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<long>(type: "bigint", nullable: false),
                    GroupName = table.Column<string>(type: "text", nullable: true),
                    Leverage = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    Equity = table.Column<decimal>(type: "numeric", nullable: false),
                    Margin = table.Column<decimal>(type: "numeric", nullable: false),
                    MarginFree = table.Column<decimal>(type: "numeric", nullable: false),
                    Profit = table.Column<decimal>(type: "numeric", nullable: false),
                    RegistrationAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAccessAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mt5_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mt5_deals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealTicket = table.Column<long>(type: "bigint", nullable: false),
                    Login = table.Column<long>(type: "bigint", nullable: false),
                    OrderTicket = table.Column<long>(type: "bigint", nullable: false),
                    PositionId = table.Column<long>(type: "bigint", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<long>(type: "bigint", nullable: false),
                    Entry = table.Column<long>(type: "bigint", nullable: false),
                    VolumeNative = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Profit = table.Column<decimal>(type: "numeric", nullable: false),
                    Commission = table.Column<decimal>(type: "numeric", nullable: false),
                    Swap = table.Column<decimal>(type: "numeric", nullable: false),
                    DealTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mt5_deals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mt5_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    CurrencyDigits = table.Column<int>(type: "integer", nullable: false),
                    Company = table.Column<string>(type: "text", nullable: true),
                    MarginCall = table.Column<decimal>(type: "numeric", nullable: true),
                    MarginStopOut = table.Column<decimal>(type: "numeric", nullable: true),
                    ConnectionsAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    EnabledForAnalysis = table.Column<bool>(type: "boolean", nullable: false),
                    PlanMapping = table.Column<string>(type: "text", nullable: true),
                    LastDiscoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mt5_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mt5_positions_current",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionTicket = table.Column<long>(type: "bigint", nullable: false),
                    Login = table.Column<long>(type: "bigint", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    VolumeNative = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    PriceOpen = table.Column<decimal>(type: "numeric", nullable: false),
                    PriceCurrent = table.Column<decimal>(type: "numeric", nullable: false),
                    PriceSl = table.Column<decimal>(type: "numeric", nullable: false),
                    PriceTp = table.Column<decimal>(type: "numeric", nullable: false),
                    Profit = table.Column<decimal>(type: "numeric", nullable: false),
                    Swap = table.Column<decimal>(type: "numeric", nullable: false),
                    TimeCreate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TimeUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mt5_positions_current", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    AggregateId = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reconstructed_trades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<long>(type: "bigint", nullable: false),
                    PositionId = table.Column<long>(type: "bigint", nullable: false),
                    CanonicalSymbol = table.Column<string>(type: "text", nullable: false),
                    SourceSymbol = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EntryVwap = table.Column<decimal>(type: "numeric", nullable: false),
                    ExitVwap = table.Column<decimal>(type: "numeric", nullable: true),
                    InitialVolumeLots = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxVolumeLots = table.Column<decimal>(type: "numeric", nullable: false),
                    ClosedVolumeLots = table.Column<decimal>(type: "numeric", nullable: false),
                    GrossRealizedPnl = table.Column<decimal>(type: "numeric", nullable: false),
                    Commission = table.Column<decimal>(type: "numeric", nullable: false),
                    Swap = table.Column<decimal>(type: "numeric", nullable: false),
                    Fees = table.Column<decimal>(type: "numeric", nullable: false),
                    NetRealizedPnl = table.Column<decimal>(type: "numeric", nullable: false),
                    DealCount = table.Column<int>(type: "integer", nullable: false),
                    OrderCount = table.Column<int>(type: "integer", nullable: false),
                    InitialSl = table.Column<decimal>(type: "numeric", nullable: true),
                    InitialTp = table.Column<decimal>(type: "numeric", nullable: true),
                    FinalSl = table.Column<decimal>(type: "numeric", nullable: true),
                    FinalTp = table.Column<decimal>(type: "numeric", nullable: true),
                    WasScaledIn = table.Column<bool>(type: "boolean", nullable: false),
                    WasPartialClose = table.Column<bool>(type: "boolean", nullable: false),
                    WasAveragedDown = table.Column<bool>(type: "boolean", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reconstructed_trades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "risk_decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CopyIntentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    ApprovedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    AllowFixSend = table.Column<bool>(type: "boolean", nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_decisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shadow_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CopyIntentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceLogin = table.Column<long>(type: "bigint", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Spread = table.Column<decimal>(type: "numeric", nullable: false),
                    SourceVsShadowSlippage = table.Column<decimal>(type: "numeric", nullable: false),
                    FilledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shadow_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "source_symbol_mappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceSymbol = table.Column<string>(type: "text", nullable: false),
                    CanonicalInstrumentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_symbol_mappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sync_checkpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<long>(type: "bigint", nullable: false),
                    Stream = table.Column<string>(type: "text", nullable: false),
                    LastTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastTicket = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_checkpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trader_score_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<long>(type: "bigint", nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric", nullable: false),
                    BehaviorScore = table.Column<decimal>(type: "numeric", nullable: false),
                    EarlyQualityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trader_score_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trader_scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<long>(type: "bigint", nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric", nullable: false),
                    BehaviorScore = table.Column<decimal>(type: "numeric", nullable: false),
                    EarlyQualityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    CompletedXauTrades = table.Column<int>(type: "integer", nullable: false),
                    Martingale = table.Column<bool>(type: "boolean", nullable: false),
                    AveragingDown = table.Column<bool>(type: "boolean", nullable: false),
                    LotEscalation = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentState = table.Column<int>(type: "integer", nullable: false),
                    LastScoredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trader_scores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_brokers_Code",
                table: "brokers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_canonical_instruments_Code",
                table: "canonical_instruments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_copy_intents_IdempotencyKey",
                table: "copy_intents",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_execution_intents_ClOrdId",
                table: "execution_intents",
                column: "ClOrdId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fix_sessions_Qualifier",
                table: "fix_sessions",
                column: "Qualifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mt5_accounts_BrokerId_Login",
                table: "mt5_accounts",
                columns: new[] { "BrokerId", "Login" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mt5_deals_BrokerId_DealTicket",
                table: "mt5_deals",
                columns: new[] { "BrokerId", "DealTicket" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mt5_deals_BrokerId_Login_DealTime",
                table: "mt5_deals",
                columns: new[] { "BrokerId", "Login", "DealTime" });

            migrationBuilder.CreateIndex(
                name: "IX_mt5_groups_BrokerId_Name",
                table: "mt5_groups",
                columns: new[] { "BrokerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mt5_positions_current_BrokerId_PositionTicket",
                table: "mt5_positions_current",
                columns: new[] { "BrokerId", "PositionTicket" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_ProcessedAt",
                table: "outbox_events",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_reconstructed_trades_BrokerId_Login_PositionId_OpenedAt",
                table: "reconstructed_trades",
                columns: new[] { "BrokerId", "Login", "PositionId", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_risk_decisions_CopyIntentId",
                table: "risk_decisions",
                column: "CopyIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_source_symbol_mappings_BrokerId_SourceSymbol",
                table: "source_symbol_mappings",
                columns: new[] { "BrokerId", "SourceSymbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sync_checkpoints_BrokerId_Login_Stream",
                table: "sync_checkpoints",
                columns: new[] { "BrokerId", "Login", "Stream" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trader_score_history_BrokerId_Login_RecordedAt",
                table: "trader_score_history",
                columns: new[] { "BrokerId", "Login", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_trader_scores_BrokerId_Login",
                table: "trader_scores",
                columns: new[] { "BrokerId", "Login" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "brokers");

            migrationBuilder.DropTable(
                name: "canonical_instruments");

            migrationBuilder.DropTable(
                name: "copy_intents");

            migrationBuilder.DropTable(
                name: "destination_quotes");

            migrationBuilder.DropTable(
                name: "execution_intents");

            migrationBuilder.DropTable(
                name: "fix_sessions");

            migrationBuilder.DropTable(
                name: "kill_switches");

            migrationBuilder.DropTable(
                name: "mt5_accounts");

            migrationBuilder.DropTable(
                name: "mt5_deals");

            migrationBuilder.DropTable(
                name: "mt5_groups");

            migrationBuilder.DropTable(
                name: "mt5_positions_current");

            migrationBuilder.DropTable(
                name: "outbox_events");

            migrationBuilder.DropTable(
                name: "reconstructed_trades");

            migrationBuilder.DropTable(
                name: "risk_decisions");

            migrationBuilder.DropTable(
                name: "shadow_orders");

            migrationBuilder.DropTable(
                name: "source_symbol_mappings");

            migrationBuilder.DropTable(
                name: "sync_checkpoints");

            migrationBuilder.DropTable(
                name: "trader_score_history");

            migrationBuilder.DropTable(
                name: "trader_scores");
        }
    }
}
