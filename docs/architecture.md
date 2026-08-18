# MT5 Trader Intelligence — Architecture Overview

## System Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        React Dashboard                          │
│                    (apps/web — Vite + TS)                        │
└────────────────────────────┬────────────────────────────────────┘
                             │ REST / WebSocket
┌────────────────────────────▼────────────────────────────────────┐
│                     .NET 8 API Gateway                          │
│          (ASP.NET Core — Linux or Windows)                       │
│  ┌──────────┐  ┌──────────┐  ┌───────────┐  ┌───────────────┐  │
│  │ Account  │  │  Trade   │  │   Risk    │  │  Copy-Trade   │  │
│  │  Svc     │  │  Recon   │  │  Engine   │  │  Orchestrator │  │
│  └──────────┘  └──────────┘  └───────────┘  └───────────────┘  │
└──────┬──────────────┬──────────────┬──────────────┬─────────────┘
       │              │              │              │
┌──────▼──────┐ ┌─────▼─────┐ ┌─────▼─────┐ ┌─────▼──────────┐
│  PostgreSQL │ │   Redis   │ │ MT5 Worker │ │ cTrader FIX    │
│  (ledger,   │ │ (cache,   │ │ (Windows,  │ │ (QuickFIX/N,  │
│   accounts) │ │  pub/sub) │ │  native    │ │  Linux-safe)   │
└─────────────┘ └───────────┘ │  SDK DLL)  │ └────────────────┘
                              └──────┬─────┘
                    ┌────────────────┼────────────────┐
                    ▼                ▼                ▼
              Achiever MT5    StarwaveFX MT5    (future brokers)
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 18, TypeScript, Vite, TailwindCSS |
| API | ASP.NET Core 8, C# |
| MT5 Worker | C++17, MetaTrader 5 Manager API (native DLL, Windows-only) |
| FIX Engine | QuickFIX/N, FIX 4.4 |
| Database | PostgreSQL 16 |
| Cache | Redis 7 |
| ML (future) | Python, scikit-learn |

## Core Domains

- **Account Management** — MT5 user provisioning, group assignment, password management
- **Trade Reconstruction** — deals → reconstructed trades with P&L, XAUUSD normalization
- **Risk Engine** — hard limits, kill switch, emergency flatten, slippage guard
- **Copy Trading** — prop challenge → live account mirroring via cTrader FIX
- **Evidence Ledger** — immutable raw event + deal revision store (append-only, idempotent)

## Data Flow

1. MT5 pump callbacks (OnDealAdd, OnPositionUpdate, etc.) push events to a lock-free queue
2. Worker thread drains queue, writes raw events to PostgreSQL evidence ledger
3. Trade reconstruction aggregates deals by position ticket into trades
4. Risk engine evaluates every trade against configurable hard limits
5. Copy-trade orchestrator mirrors qualifying trades to cTrader via FIX 4.4

## Phase Plan

| Phase | Scope | Status |
|-------|-------|--------|
| 1 | MT5 SDK wrapper, account CRUD, deal ingestion, evidence ledger | Done |
| 2 | Trade reconstruction, risk engine hard limits, dashboard MVP | In Progress |
| 3 | cTrader FIX integration, copy-trade orchestrator | Planned |
| 4 | ML scoring, news filter, advanced analytics | Future |
