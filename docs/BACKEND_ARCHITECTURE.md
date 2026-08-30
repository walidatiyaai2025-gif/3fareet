# عفاريت الأسفلت — Backend Architecture Baseline

**Document:** AFA-ARCH-BE-001  
**Decision:** APPROVED  
**Updated:** 2026-08-30  
**Scope:** Backend foundation/security contract only; implementation remains gated behind the playable 3D prototype.

## Locked stack decision

- Canonical current 3D game client/runtime: **Unity**.
- Legacy Flutter/Flame code may remain for historical/launcher/compatibility purposes, but it is not the authoritative new 3D gameplay runtime unless an explicit migration/legacy task says otherwise.
- Backend application/API: **Laravel**.
- Primary relational database: **MySQL**.
- Client/server transport: HTTPS JSON REST API for normal game/account/economy operations.
- Real-time multiplayer transport is a separate later concern under NET tasks and must not be implemented as direct database access.

## Mandatory security boundary

No game client — Unity, Flutter or any future client — may connect directly to MySQL or contain database credentials.

Required data path:

`Game Client → HTTPS API → Laravel → MySQL`

Laravel owns authentication, authorization, validation, rate limiting, business rules, persistence, audit logging and server-side anti-cheat/economy validation.

## Environment model

Use isolated environments:
- local development;
- staging;
- production.

Each environment must have separate application secrets and separate MySQL credentials/databases. Secrets must be injected from environment/configuration and never committed to Git.

## API baseline

- Versioned API prefix, starting with `/api/v1`.
- JSON request/response contracts.
- Standardized success/error envelope.
- Request correlation ID for diagnostics.
- Server timestamps in UTC; presentation/localization handled by clients where appropriate.
- Authentication mechanism selected and documented during BCK-003; Laravel-native token/session facilities should be preferred unless multiplayer requirements justify another mechanism.

## Initial Laravel domain modules

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

## Playable-slice gate interaction

This backend decision is locked so current gameplay code does not evolve toward database coupling. Substantial Laravel/MySQL implementation remains deferred until the Unity playable 3D slice proves the driving loop, visual direction, Android build path and target-device performance.

A small API client abstraction may be introduced in the canonical game client before P6 only when required to preserve clean boundaries; it must not block the playable slice.

## Definition of Done for BCK-001

BCK-001 is satisfied when:
- Laravel is recorded as backend runtime/framework;
- MySQL is recorded as primary database;
- direct client-to-MySQL access is explicitly prohibited;
- environment and API boundaries are documented;
- Master Development Plan and Project Status reference this architecture decision.
