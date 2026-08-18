# Phase 0 — Repository audit (2026-08-18)

## Current architecture

.NET 8 solution `Mt5TraderIntelligence.sln` plus preserved C++ `mt5-sdk`. React dashboard under `apps/web`.

## Existing MT5

Real Manager API lives in `mt5-sdk` (`IMT5Client`, local + HTTP). C# first useful version uses `FakeMt5BrokerConnector` so ingestion/reconstruction/scoring can be proven without live broker credentials.

## Existing DB

No production migrations yet. `TraderDbContext` maps first-useful tables with compound unique indexes (`broker_id` + ticket/login). Development falls back to EF InMemory.

## Trading / copy

Shadow engine exists. Live copy is feature-flagged **off**. No NewOrderSingle send path is armed.

## Broker config

Achiever + StarwaveFX in `.env.example` (placeholders for secrets).

## Security

Passwords are not in appsettings. Dashboard contracts omit secrets. `cServer` case preserved.

## Dead / duplicate

`Class1` / weatherforecast removed from API. Infrastructure briefly had plural EF types (`Brokers`) that did not match Domain; rewritten.

## Classification

| Component | Status |
|---|---|
| mt5-sdk C++ | EXISTS_AND_GOOD |
| Domain algorithms | EXISTS_AND_GOOD (new) |
| EF persistence | EXISTS_NEEDS_REFACTOR (InMemory first; add migrations) |
| Live MT5 connect | MISSING (Windows worker + credentials required) |
| Live FIX logon | MISSING (simulator + session state only) |
| React dashboard | EXISTS_NEEDS_REFACTOR (pages exist; polish/RBAC later) |
| ML | MISSING (correct — Phase 6) |
| Kafka/K8s/LLM | DEPRECATED / not to build |
