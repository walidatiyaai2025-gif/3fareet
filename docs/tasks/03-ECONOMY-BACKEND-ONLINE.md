# Afareet Asphalt — Task Register Segment
> Controlled task source. Update task status/owner here before starting work.
## ECO — Economy / Monetization
Owner default: **Product + Backend**
| ID | Priority | Task | Owner | Status |
|---|---|---|---|---|
| ECO-001 | P2 | تعريف currencies: Coins/Spirit/Pass XP | Product + Backend | TODO |
| ECO-002 | P2 | تعريف economy ledger model | Product + Backend | TODO |
| ECO-003 | P2 | إنشاء reward grants abstraction | Product + Backend | TODO |
| ECO-004 | P2 | إنشاء purchase/spend validation | Product + Backend | TODO |
| ECO-005 | P2 | إنشاء local dev economy | Product + Backend | TODO |
| ECO-006 | P2 | إنشاء shop catalog model | Product + Backend | TODO |
| ECO-007 | P2 | إنشاء rewarded ad entitlement flow | Product + Backend | TODO |
| ECO-008 | P2 | دمج Rewarded Ads sandbox | Product + Backend | TODO |
| ECO-009 | P2 | تعريف IAP product catalog | Product + Backend | TODO |
| ECO-010 | P2 | دمج IAP sandbox | Product + Backend | TODO |
| ECO-011 | P2 | إنشاء purchase restore | Product + Backend | TODO |
| ECO-012 | P2 | إنشاء receipt verification contract | Product + Backend | TODO |
| ECO-013 | P2 | إنشاء anti-duplicate reward guards | Product + Backend | TODO |
| ECO-014 | P2 | إنشاء economy telemetry | Product + Backend | TODO |
| ECO-015 | P2 | توازن economy أولي | Product + Backend | TODO |
## BCK — Backend Foundation
Owner default: **Backend/Network Engineer**
| ID | Priority | Task | Owner | Status |
|---|---|---|---|---|
| BCK-001 | P2 | تثبيت backend baseline: Laravel API + MySQL + منع direct client DB access | Backend/Network Engineer | VERIFIED |
| BCK-002 | P2 | تعريف API versioning | Backend/Network Engineer | TODO |
| BCK-003 | P2 | إنشاء authentication service | Backend/Network Engineer | TODO |
| BCK-004 | P2 | إنشاء guest account flow | Backend/Network Engineer | TODO |
| BCK-005 | P2 | إنشاء account upgrade/link flow | Backend/Network Engineer | TODO |
| BCK-006 | P2 | إنشاء player profile service | Backend/Network Engineer | TODO |
| BCK-007 | P2 | إنشاء inventory service | Backend/Network Engineer | TODO |
| BCK-008 | P2 | إنشاء garage/equipment service | Backend/Network Engineer | TODO |
| BCK-009 | P2 | إنشاء economy ledger service | Backend/Network Engineer | TODO |
| BCK-010 | P2 | إنشاء rewards service | Backend/Network Engineer | TODO |
| BCK-011 | P2 | إنشاء remote config service | Backend/Network Engineer | TODO |
| BCK-012 | P2 | إنشاء feature flags | Backend/Network Engineer | TODO |
| BCK-013 | P2 | إنشاء leaderboard storage contract | Backend/Network Engineer | TODO |
| BCK-014 | P2 | إنشاء telemetry ingestion contract | Backend/Network Engineer | TODO |
| BCK-015 | P2 | إنشاء rate limiting | Backend/Network Engineer | TODO |
| BCK-016 | P2 | إنشاء audit logging | Backend/Network Engineer | TODO |
| BCK-017 | P2 | إنشاء secrets/config management | Backend/Network Engineer | TODO |
| BCK-018 | P2 | إنشاء dev/staging/prod environments | Backend/Network Engineer | TODO |
| BCK-019 | P2 | إنشاء backup/restore policy | Backend/Network Engineer | TODO |
## NET — Real-time Multiplayer
Owner default: **Backend/Network Engineer + Gameplay Lead**
| ID | Priority | Task | Owner | Status |
|---|---|---|---|---|
| NET-001 | P2 | تعريف authoritative race protocol | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-002 | P2 | تعريف client input packet | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-003 | P2 | تعريف server snapshot packet | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-004 | P2 | تعريف tick/update rate | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-005 | P2 | إنشاء lobby service | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-006 | P2 | إنشاء room lifecycle | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-007 | P2 | إنشاء matchmaking queue | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-008 | P2 | إنشاء 4-player session allocation | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-009 | P2 | إنشاء countdown synchronization | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-010 | P2 | تنفيذ client prediction | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-011 | P2 | تنفيذ server reconciliation | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-012 | P2 | تنفيذ opponent interpolation | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-013 | P2 | تنفيذ input sequence/ack | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-014 | P2 | تنفيذ packet loss handling | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-015 | P2 | تنفيذ latency measurement | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-016 | P2 | تنفيذ reconnect window | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-017 | P2 | تنفيذ disconnect replacement/forfeit rules | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-018 | P2 | تنفيذ checkpoint validation server-side | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-019 | P2 | تنفيذ finish time validation | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-020 | P2 | تنفيذ power-up authority | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-021 | P2 | تنفيذ anti-speedhack sanity checks | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-022 | P2 | إنشاء online race results | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-023 | P2 | إنشاء online reward grant | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-024 | P2 | اختبار 4 clients محليًا | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-025 | P2 | اختبار high latency profile | Backend/Network Engineer + Gameplay Lead | TODO |
| NET-026 | P2 | اختبار packet loss profile | Backend/Network Engineer + Gameplay Lead | TODO |

## BCK-001 Evidence
- Architecture: [`../BACKEND_ARCHITECTURE.md`](../BACKEND_ARCHITECTURE.md)
- Locked production data path: `Unity Game Client → HTTPS API → Laravel → MySQL`; Flutter is a legacy reference client and direct database access is prohibited for every client.
- Direct client-to-MySQL access is prohibited.
- Implementation remains deferred until the P1 playable prototype gate unless a small interface-only task is explicitly approved.
