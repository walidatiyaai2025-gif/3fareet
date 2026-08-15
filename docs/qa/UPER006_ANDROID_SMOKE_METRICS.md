# UPER-006 Android Smoke Metrics Gate

`UPER-006` device smoke evidence is not satisfied by checkpoint labels alone. The raw ADB capture already stores `meminfo`, `gfxinfo`, `thermalservice`, battery and crash/ANR/native-fatal logs; this gate converts the Android-observable subset into deterministic measurements and binds it to the same physical-device evidence session.

## Required checkpoints

- `smoke-cold-start`
- `smoke-warm-race`
- `smoke-after-restarts`

## Analyzer

```bash
python tools/android/analyze_device_smoke.py \
  --session evidence/p1-device \
  --tier mid
```

The tier must match the capability tier being reviewed under `docs/performance/UNITY_DEVICE_TIERS.md`.

The analyzer checks what ADB can truthfully measure from the captured evidence:
- checkpoint APK/device fingerprint consistency;
- automated crash/ANR/native-fatal red flags;
- process PSS for warm-race and post-restart checkpoints;
- Android `gfxinfo` P95/P99 total frame-time samples when available;
- restart PSS growth against the 5% UPER-001 budget;
- Android thermal status, rejecting SEVERE or worse.

It deliberately does **not** invent Unity main-thread, render-thread, GPU or sustained-20-minute profiler data. Those remain separate profiler/manual evidence under UPER-001/UPER-006 review.

Possible analyzer verdicts are only:
- `BLOCKED`
- `PASSABLE_FOR_MANUAL_REVIEW`

`verified` is always `false`.

## Authoritative publication preflight

`verify_release_with_production_art.py` now requires `--performance-tier low|mid|high` and runs the UPER-006 smoke analyzer before the existing publication preflight. A blocked smoke analysis aborts before publication eligibility can be returned.

This does not complete `UPER-006`: a new exact candidate, physical device captures, human review, and the remaining profiler/sustained evidence are still required.
