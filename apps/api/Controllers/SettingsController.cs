using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace TraderIntelligence.Api.Controllers;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _config;

    public SettingsController(IConnectionMultiplexer redis, IConfiguration config)
    {
        _redis = redis;
        _config = config;
    }

    /// <summary>
    /// Returns non-secret settings: risk limits and feature flags.
    /// Passwords and API keys are never exposed.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            RiskEngine = new
            {
                MaxDailyDrawdownPct = _config.GetValue("RiskEngine:MaxDailyDrawdownPct", 5.0m),
                MaxPositionSize = _config.GetValue("RiskEngine:MaxPositionSize", 10.0m),
                MaxOpenPositions = _config.GetValue("RiskEngine:MaxOpenPositions", 20),
                KillSwitchEnabled = _config.GetValue("RiskEngine:KillSwitchEnabled", true)
            },
            FeatureFlags = new
            {
                ShadowTradingEnabled = _config.GetValue("FeatureFlags:ShadowTradingEnabled", true),
                LiveCopyEnabled = _config.GetValue("FeatureFlags:LiveCopyEnabled", false),
                AutoPromotionEnabled = _config.GetValue("FeatureFlags:AutoPromotionEnabled", false)
            }
        });
    }

    /// <summary>
    /// Updates non-secret settings via Redis-backed config store.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] SettingsUpdateRequest request)
    {
        var db = _redis.GetDatabase();

        if (request.RiskEngine is not null)
        {
            if (request.RiskEngine.MaxDailyDrawdownPct.HasValue)
                await db.StringSetAsync("settings:risk:max_daily_drawdown_pct", request.RiskEngine.MaxDailyDrawdownPct.Value.ToString());
            if (request.RiskEngine.MaxPositionSize.HasValue)
                await db.StringSetAsync("settings:risk:max_position_size", request.RiskEngine.MaxPositionSize.Value.ToString());
            if (request.RiskEngine.MaxOpenPositions.HasValue)
                await db.StringSetAsync("settings:risk:max_open_positions", request.RiskEngine.MaxOpenPositions.Value.ToString());
        }

        if (request.FeatureFlags is not null)
        {
            if (request.FeatureFlags.ShadowTradingEnabled.HasValue)
                await db.StringSetAsync("settings:flags:shadow_trading", request.FeatureFlags.ShadowTradingEnabled.Value.ToString());
            if (request.FeatureFlags.LiveCopyEnabled.HasValue)
                await db.StringSetAsync("settings:flags:live_copy", request.FeatureFlags.LiveCopyEnabled.Value.ToString());
            if (request.FeatureFlags.AutoPromotionEnabled.HasValue)
                await db.StringSetAsync("settings:flags:auto_promotion", request.FeatureFlags.AutoPromotionEnabled.Value.ToString());
        }

        return Ok(new { Updated = true });
    }
}

public sealed class SettingsUpdateRequest
{
    public RiskEngineSettings? RiskEngine { get; set; }
    public FeatureFlagSettings? FeatureFlags { get; set; }
}

public sealed class RiskEngineSettings
{
    public decimal? MaxDailyDrawdownPct { get; set; }
    public decimal? MaxPositionSize { get; set; }
    public int? MaxOpenPositions { get; set; }
}

public sealed class FeatureFlagSettings
{
    public bool? ShadowTradingEnabled { get; set; }
    public bool? LiveCopyEnabled { get; set; }
    public bool? AutoPromotionEnabled { get; set; }
}
