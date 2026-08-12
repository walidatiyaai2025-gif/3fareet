# عفاريت الأسفلت — Backend Architecture Baseline

**Document:** AFA-ARCH-BE-001  
**Decision:** APPROVED  
**Date:** 2026-08-12  
**Scope:** Backend foundation contract only; implementation remains gated behind P1 Playable Prototype.

## Locked stack decision

- Mobile/Game client: Flutter + Flame.
- Backend application/API: Laravel.
- Primary relational database: MySQL.
- Client/server transport: HTTPS JSON REST API for normal game/account/economy operations.
- Real-time multiplayer transport is a separate later concern under NET tasks and must not be implemented as direct database access.

## Mandatory security boundary

The Flutter application **must never connect directly to MySQL** and must never contain database credentials.

Required data path:

`Flutter / Flame Client → HTTPS API → Laravel → MySQL`

Laravel owns authentication, authorization, validation, rate limiting, business rules, persistence, audit logging and server-side anti-cheat/economy validation.

## Environment model

Use isolated environments:

- local development
- staging
- production

Each environment must have separate application secrets and separate MySQL credentials/databases. Secrets must be injected from environment/configuration and never committed to Git.

## API baseline

- Versioned API prefix, starting with `/api/v1`.
- JSON request/response contracts.
- Standardized success/error envelope.
- Request correlation ID for diagnostics.
- Server timestamps in UTC; presentation/localization handled by clients where appropriate.
- Authentication mechanism selected and documented during BCK-003; Laravel-native token/session facilities should be preferred unless multiplayer requirements justify another mechanism.

## Initial Laravel domain modules

Planned modules map to the existing BCK task register:

1. Auth / guest account / account linking.
2. Player profile.
3. Inventory and garage/equipment.
4. Economy ledger and rewards.
5. Remote config and feature flags.
6. Leaderboard storage contract.
7. Telemetry ingestion contract.
8. Audit logging and rate limiting.
9. Backup/restore and environment operations.

## MySQL principles

- Migrations are the source of truth for schema changes.
- Foreign keys and unique constraints enforce server invariants where practical.
- Monetary/economy balances are server-authoritative.
- Never trust client-supplied reward values, race results or inventory mutations without server validation.
- Add indexes from measured access patterns, not speculation.
- Backups and restore drills are mandatory before production launch.

## P1 gate interaction

This architecture decision is locked now so current gameplay code does not evolve toward direct database coupling. However, substantial Laravel/MySQL implementation remains deferred until the P1 playable prototype proves the driving loop, visual direction and Android release path.

A small API client abstraction may be introduced in Flutter before P6 only when required to preserve clean boundaries; it must not block the playable prototype.

## Definition of Done for BCK-001

BCK-001 is satisfied when:

- Laravel is recorded as backend runtime/framework.
- MySQL is recorded as primary database.
- direct client-to-MySQL access is explicitly prohibited.
- environment and API boundaries are documented.
- Master Development Plan and Project Status reference this architecture decision.
