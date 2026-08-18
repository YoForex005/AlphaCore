let meta = #{
    name: "copy-roster-harden",
    description: "Research auto-admit, auto-remove losers, flatten dest copies",
    phases: [
        #{ title: "Research", detail: "policy holes for admit/remove/flatten" },
        #{ title: "Verify", detail: "adversarial check of roster rules" },
    ],
};

let note_schema = #{
    "type": "object",
    "required": ["slot", "verdict", "evidence"],
    "properties": #{
        "slot": #{ "type": "string" },
        "verdict": #{ "type": "string" },
        "evidence": #{ "type": "string" },
    },
};

let topics = [
    "Read XauUsdOneToOneCopyPolicy.cs. Is there auto-remove when XAU PnL goes negative after admit?",
    "Read CopyTradingService. Does it flatten dest when trader is later RISK_BLOCKED?",
    "Read RiskEngine MaxLossPerTrader. Is it applied per copied trader on each tick?",
    "Should a single losing open dest trade be flattened if unrealized loss exceeds a cap?",
    "Auto-add: when a WATCH trader reaches SHADOW + 20 XAU + PnL>0, must roster admit without a human click.",
    "Demo groups must stay excluded even if they look profitable.",
    "Consecutive loss streak should pause the trader and flatten opens.",
    "Peak-to-trough XAU drawdown should remove the trader.",
    "Do not flatten MT5 source. Destination only.",
    "CloseExposure must still be allowed after remove so dest can flatten.",
];

phase("Research");
let jobs = [];
let i = 0;
while i < 40 {
    let t = topics[i % topics.len()];
    let p = "Senior engineer on D:\\Prop. Use read_file/grep. Never print secrets. Never send 35=D.";
    p += " Write D:\\Prop\\reports\\swarm\\20260818\\P503_R_";
    p += i.to_string();
    p += ".md. Topic: ";
    p += t;
    p += " Slot ";
    p += i.to_string();
    p += ". Verdict must say ADMIT/REMOVE/FLATTEN implication.";
    jobs.push(#{
        prompt: p,
        label: "r-" + i.to_string(),
        capability_mode: "read-write",
        output_schema: note_schema,
    });
    i += 1;
}
let research = parallel(jobs);
log("research panel done");

phase("Verify");
let vjobs = [];
let k = 0;
while k < 20 {
    let vp = "Adversarial. Read src/Domain/Copy and CopyTradingService. Confirm losers are not left on the roster.";
    vp += " Write D:\\Prop\\reports\\swarm\\20260818\\P503_V_";
    vp += k.to_string();
    vp += ".md. FAIL if remove/flatten is missing. Slot ";
    vp += k.to_string();
    vp += ".";
    vjobs.push(#{
        prompt: vp,
        label: "v-" + k.to_string(),
        capability_mode: "read-write",
        output_schema: note_schema,
    });
    k += 1;
}
let verify = parallel(vjobs);

let ok = 0;
for r in research { if r != () && r.success { ok += 1; } }
let vok = 0;
for r in verify { if r != () && r.success { vok += 1; } }
complete(#{ research_ok: ok, verify_ok: vok, planned: 60 });
