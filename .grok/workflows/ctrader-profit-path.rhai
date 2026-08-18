let meta = #{
    name: "ctrader-profit-path",
    description: "500-agent research: how the Pepperstone cTrader destination can make profit with lower loss; no live NewOrderSingle",
    phases: [
        #{ title: "Code-edge", detail: "read scoring, risk, sizing, FIX send absence" },
        #{ title: "Live-book", detail: "what the 8460-account book actually implies for copy PnL" },
        #{ title: "Verify", detail: "adversarial: sending now cannot be profitable" },
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
    "D:/Prop/src/Domain/Scoring/BaselineScorer.cs",
    "D:/Prop/src/Domain/Risk/RiskEngine.cs",
    "D:/Prop/src/Domain/Shadow/ShadowCopyEngine.cs",
    "D:/Prop/src/Domain/Reconstruction/TradeReconstructor.cs",
    "D:/Prop/src/Domain/Execution/QuantityNormalizer.cs",
    "D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs",
    "D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs",
    "D:/Prop/src/Application/Ingestion/DealIngestionService.cs",
    "D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs",
    "D:/Prop/apps/api/Program.cs",
    "D:/Prop/apps/web/src/pages/LiveCopyPage.tsx",
    "D:/Prop/docs/risk.md",
    "D:/Prop/docs/scoring.md",
    "D:/Prop/docs/ctrader-fix.md",
    "D:/Prop/MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md",
    "D:/Prop/reports/CREDENTIALS_AND_COPY_STATUS.md",
    "D:/Prop/src/Domain/Execution/ExecutionOrderStateMachine.cs",
    "D:/Prop/src/Domain/Volume/VolumeConverter.cs",
    "D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs",
    "D:/Prop/src/Fix.CTrader/Configuration/CTraderFixOptions.cs",
];

let angles = [
    "Does this file allow live 35=D send that would put Pepperstone capital at risk?",
    "Would copying every scored XAU trader lose money given RISK_BLOCKED dominate the book?",
    "Does first-3 / earlyScore 95.5 on demo yo-2step measure future destination PnL?",
    "Are martingale and averaging filters strong enough to cut the left tail?",
    "Is shadow PnL using real cTrader quotes or is destinationRealPnl still zero?",
    "Would gold scalp holds under 3 minutes survive FIX latency plus spread?",
    "Is QuantityNormalizer wired to any FIX OrderQty or is it unit-test only?",
    "CanPromoteToLive is hard-false - is that protecting profit or blocking it?",
    "Demo challenge PnL of plus 100 dollars is not an edge after venue costs.",
    "Negative netSourcePnl SHADOW rows prove the scorer is not a profit gate.",
];

let research = [
    "Read BaselineScorer.cs. Recalculate how quality 95.50 can coexist with negative netSourcePnl. Quote the formula.",
    "Read RiskEngine.cs. List every reject reason that reduces loss if live send existed.",
    "Read CTraderFixSession.cs. Prove outbound MsgType is only A. No 35=D.",
    "Read TradeReconstructor and 303274-style overlapping 0.05 lot same-second entries. Is grid flagged?",
    "Read architecture section 3 business goal. Future destination-net PnL is the target, not first-3 dollars.",
    "Official cTrader FIX: QUOTE 5211 TRADE 5212 TargetCompID cServer. Logon is not a fill.",
    "Copying prop-challenge demo accounts is adverse selection. Most accounts exist to pass a profit target then blow.",
    "XAUUSD copy cost: spread plus slippage plus 15s MaxSourceSignalAge reject. Scalps die.",
    "Kill switch MaxDailyExecutionLoss 2000 and MaxLossPerTrader 500 are loss caps, not an edge.",
    "Starwave scored 0 while dealsInserted 91966. Book is incomplete. Do not size from Achiever-only scores.",
    "SHADOW group is 100 percent demo. No real Starwave or contest live book in the copy set.",
    "RISK_BLOCKED source PnL is hundreds of thousands negative. Copying them is how the venue blows up.",
    "Persist ClOrdID before send. Unknown state must not retry. That is lower-loss, not higher-profit.",
    "MFE/MAE FeatureQuality is Unavailable. Exact excursion not used. Do not claim MAE-based stops.",
    "ML is not built. Do not invent a model. Deterministic baseline only.",
    "In-memory DB: scores vanish on restart. Cannot run a live book on that.",
    "FIX quote bid/ask are null. Cannot size or guard spread without a quote tape.",
    "Allocation factor must be tiny (0.01-0.05 of source) until shadow expectancy after costs is positive.",
    "Never flatten the MT5 source. Destination-only flatten.",
    "Trade-copier on cTrader FIX is officially listed, but Spotware says other APIs may fit copy better. Still no license to send today.",
];

phase("Code-edge");
let jobs = [];
let i = 0;
while i < 200 {
    let f = files[i % files.len()];
    let a = angles[i % angles.len()];
    let p = "You are a senior trading-systems engineer on D:\\Prop. Use read_file and grep only.";
    p += " Never print passwords or FIX secrets. Never send or build NewOrderSingle.";
    p += " Write D:\\Prop\\reports\\swarm\\20260818\\P500_CODE_";
    p += i.to_string();
    p += ".md with file, angle, verdict, evidence quotes, profit implication, lower-loss implication.";
    p += " File: ";
    p += f;
    p += " Angle: ";
    p += a;
    p += " Slot ";
    p += i.to_string();
    p += ". Measured live: 8463 accounts, Achiever scoring, Starwave deals-done scored 0, SHADOW all demo, destinationRealPnl 0, FIX LoggedOn, REAL_COPY false.";
    p += " Empty PASS only after you actually read the file.";
    jobs.push(#{
        prompt: p,
        label: "code-" + i.to_string(),
        capability_mode: "read-write",
        output_schema: note_schema,
    });
    i += 1;
}
let code_results = parallel(jobs);
log("code-edge panel finished");

phase("Live-book");
let rjobs = [];
let j = 0;
while j < 200 {
    let topic = research[j % research.len()];
    let rp = "You are a senior quant/engineer. Use read_file/grep on D:\\Prop. Optional: GET http://127.0.0.1:5000/api/overview and /api/traders if curl works.";
    rp += " Never print secrets. Never enable REAL_COPY. Never send 35=D.";
    rp += " Write D:\\Prop\\reports\\swarm\\20260818\\P500_BOOK_";
    rp += j.to_string();
    rp += ".md with measured evidence for higher profit and lower loss.";
    rp += " Topic: ";
    rp += topic;
    rp += " Slot ";
    rp += j.to_string();
    rp += ". Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses.";
    rjobs.push(#{
        prompt: rp,
        label: "book-" + j.to_string(),
        capability_mode: "read-write",
        output_schema: note_schema,
    });
    j += 1;
}
let book_results = parallel(rjobs);
log("live-book panel finished");

phase("Verify");
let vjobs = [];
let k = 0;
while k < 100 {
    let vp = "Adversarial verifier. Read CTraderFixSession.cs, BaselineScorer.cs, RiskEngine.cs, LiveCopyPage.tsx yourself.";
    vp += " Confirm: (1) no 35=D builder, (2) CanPromoteToLive is false, (3) RealCopyEnabled forced false after logon, (4) sending now cannot be the profit path, (5) SHADOW on demo is not destination profit.";
    vp += " Write D:\\Prop\\reports\\swarm\\20260818\\P500_VERIFY_";
    vp += k.to_string();
    vp += ".md. Slot ";
    vp += k.to_string();
    vp += ". FAIL any claim you cannot prove from a file or live GET. Never print secrets.";
    vjobs.push(#{
        prompt: vp,
        label: "verify-" + k.to_string(),
        capability_mode: "read-write",
        output_schema: note_schema,
    });
    k += 1;
}
let verify_results = parallel(vjobs);

let code_ok = 0;
for r in code_results {
    if r != () && r.success { code_ok += 1; }
}
let book_ok = 0;
for r in book_results {
    if r != () && r.success { book_ok += 1; }
}
let verify_ok = 0;
for r in verify_results {
    if r != () && r.success { verify_ok += 1; }
}

complete(#{
    code_ok: code_ok,
    book_ok: book_ok,
    verify_ok: verify_ok,
    planned_agents: 500,
    note: "Reports under D:/Prop/reports/swarm/20260818/P500_*.md",
});
