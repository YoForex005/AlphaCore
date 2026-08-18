let meta = #{
    name: "live-mt5-all-groups",
    description: "500-agent review and hard research of live Achiever/Starwave group+trader ingest and no-loss cTrader copy",
    phases: [
        #{ title: "Slice-review", detail: "read-only code slices for dummy-data leaks and missing all-groups fetch" },
        #{ title: "Hard-research", detail: "Manager API, proxy IP-block, FIX 4.4 no NewOrderSingle" },
        #{ title: "Verify", detail: "adversarial check that live send stays off and reports are evidence-based" },
    ],
};

let note_schema = #{
    "type": "object",
    "required": ["slot", "verdict", "evidence", "risk_to_capital"],
    "properties": #{
        "slot": #{ "type": "number" },
        "verdict": #{ "type": "string" },
        "evidence": #{ "type": "string" },
        "risk_to_capital": #{ "type": "string" },
    },
};

let files = [
    "D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs",
    "D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs",
    "D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs",
    "D:/Prop/src/Infrastructure/DependencyInjection.cs",
    "D:/Prop/apps/api/Program.cs",
    "D:/Prop/src/Application/Ingestion/DealIngestionService.cs",
    "D:/Prop/src/Infrastructure/Seeding/DemoSeeder.cs",
    "D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs",
    "D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs",
    "D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs",
    "D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs",
    "D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs",
    "D:/Prop/apps/api/TraderIntelligence.Api.csproj",
    "D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj",
    "D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h",
    "D:/Prop/src/Domain/Risk/RiskEngine.cs",
    "D:/Prop/src/Domain/Shadow/ShadowCopyEngine.cs",
    "D:/Prop/src/Application/Contracts/Mt5Contracts.cs",
    "D:/Prop/apps/web/src/pages/LiveCopyPage.tsx",
    "D:/Prop/apps/web/src/pages/GroupsPage.tsx",
    "D:/Prop/apps/web/src/pages/TradersPage.tsx",
    "D:/Prop/apps/web/src/pages/OverviewPage.tsx",
    "D:/Prop/src/Fix.CTrader/Configuration/CTraderFixOptions.cs",
    "D:/Prop/src/Mt5/Env/EnvFile.cs",
    "D:/Prop/MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md",
];

let angles = [
    "dummy or seeded data still reachable on the live path",
    "failure to fetch ALL manager groups",
    "failure to fetch ALL manager traders/logins",
    "live cTrader NewOrderSingle or capital-loss path",
    "Achiever HTTP proxy / MT_RET_AUTH_MANAGER_IPBLOCK 1012",
    "API not net8.0-windows x64 so MetaQuotes DLL cannot load",
    "dashboard hiding accounts that have no score yet",
    "positions limited to first 200 accounts",
    "DealRequestByGroup 90-day timeout without chunking",
    "env file not loaded before WebApplication.CreateBuilder",
];

let research = [
    "Read MT5APIManager.h GroupRequestArray UserRequestArray UserLogins PositionRequestByGroup DealRequestByGroup. Confirm request APIs work without pump.",
    "Read YoPips mt5_manager.cpp Connect fallback to pump-none and proxy address IP:port auth login:password.",
    "Read YoPips mt5_group_probe.cpp. How does a proven probe enumerate all groups without echoing passwords?",
    "Confirm MT_RET_AUTH_MANAGER_IPBLOCK=1012 and that Achiever requires HTTP proxy 81.29.145.69:49527.",
    "Confirm Starwave must connect direct with no proxy.",
    "Confirm UserGetByGroup is pump-cache and UserRequestArray is the request path for ALL traders.",
    "Confirm IMTDeal.Volume scale is 10000 not hundredths and not VolumeExt 1e8.",
    "Confirm cTrader is destination venue not LP. TargetCompID cServer case preserved. Ports 5211 QUOTE and 5212 TRADE SSL.",
    "Confirm REAL_COPY_EXECUTION_ENABLED must stay false. No 35=D NewOrderSingle until risk/recon gates.",
    "Confirm trade 3 is EARLY_SCORE/SHADOW never auto LIVE.",
    "Search CTraderFixSession.cs for 35=D or NewOrderSingle. Verdict FAIL if live send exists.",
    "Search Program.cs for DemoSeeder FakeMt5 10001 10002 dummy.",
    "Search NativeMt5BrokerConnector for GroupRequestArray and UserRequestArray.",
    "Check Api.csproj TargetFramework. net8.0 without windows/x64 cannot load MT5APIManager64.",
    "Check LiveMt5Registration HasRealPasswords fail-closed.",
    "Check DealIngestionService Take(200) positions cap.",
    "Check EfDashboardQueries GetTradersAsync only scores vs all Mt5Accounts.",
    "Check FEATURE_COPY_TRADING_ENABLED and REAL_COPY_EXECUTION_ENABLED defaults.",
    "Check QuantityNormalizer never blindly converts MT5 lots to FIX OrderQty.",
    "Check RiskEngine sits between CopyIntent and ExecutionIntent.",
];

phase("Slice-review");
let jobs = [];
let i = 0;
while i < 200 {
    let f = files[i % files.len()];
    let a = angles[i % angles.len()];
    let p = "You are a senior engineer on D:\\Prop. Use read_file and grep only.";
    p += " Never print passwords, proxy auth, or FIX passwords.";
    p += " Write D:\\Prop\\reports\\swarm\\20260818\\W500_SLICE_";
    p += i.to_string();
    p += ".md with: file, angle, verdict PASS/FAIL, evidence quotes, no-loss implication.";
    p += " File: ";
    p += f;
    p += " Angle: ";
    p += a;
    p += " Slot ";
    p += i.to_string();
    p += ". Empty PASS is allowed only after you actually read the file.";
    jobs.push(#{
        prompt: p,
        label: "slice-" + i.to_string(),
        capability_mode: "read-write",
        output_schema: note_schema,
    });
    i += 1;
}
let slice_results = parallel(jobs);
log("slice-review panel finished");

phase("Hard-research");
let rjobs = [];
let j = 0;
while j < 200 {
    let topic = research[j % research.len()];
    let rp = "You are a senior engineer. Research this with read_file/grep on D:\\Prop and D:\\Projects\\YoPips\\Backend\\C++ Backend PropFirm when relevant.";
    rp += " Never print secrets. Write D:\\Prop\\reports\\swarm\\20260818\\W500_RESEARCH_";
    rp += j.to_string();
    rp += ".md with measured evidence.";
    rp += " Topic: ";
    rp += topic;
    rp += " Slot ";
    rp += j.to_string();
    rp += ". Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders yet (no loss).";
    rjobs.push(#{
        prompt: rp,
        label: "research-" + j.to_string(),
        capability_mode: "read-write",
        output_schema: note_schema,
    });
    j += 1;
}
let research_results = parallel(rjobs);
log("hard-research panel finished");

phase("Verify");
let vjobs = [];
let k = 0;
while k < 100 {
    let vp = "Adversarial verifier. Read the live path files yourself. Do not trust other agents.";
    vp += " Confirm: (1) DemoSeeder is not the API startup path, (2) Native connector can list all groups via GroupRequestArray or GroupTotal, (3) all traders via UserRequestArray/UserLogins, (4) CTraderFixSession has no 35=D, (5) REAL_COPY_EXECUTION stays false.";
    vp += " Write D:\\Prop\\reports\\swarm\\20260818\\W500_VERIFY_";
    vp += k.to_string();
    vp += ".md. Slot ";
    vp += k.to_string();
    vp += ". Set verdict FAIL if you cannot prove a claim from the file.";
    vp += " Never print secrets.";
    vjobs.push(#{
        prompt: vp,
        label: "verify-" + k.to_string(),
        capability_mode: "read-write",
        output_schema: note_schema,
    });
    k += 1;
}
let verify_results = parallel(vjobs);

let slice_ok = 0;
for r in slice_results {
    if r != () && r.success { slice_ok += 1; }
}
let research_ok = 0;
for r in research_results {
    if r != () && r.success { research_ok += 1; }
}
let verify_ok = 0;
for r in verify_results {
    if r != () && r.success { verify_ok += 1; }
}

complete(#{
    slice_ok: slice_ok,
    research_ok: research_ok,
    verify_ok: verify_ok,
    planned_agents: 500,
    note: "Reports under D:/Prop/reports/swarm/20260818/W500_*.md",
});
