# UPER-006 Android Smoke Metrics Gate

`UPER-006` device smoke evidence is not satisfied by checkpoint directory names alone. The raw ADB capture stores `meminfo`, `gfxinfo`, `thermalservice`, battery and crash/ANR/native-fatal evidence; this gate converts the Android-observable subset into deterministic measurements and binds every required checkpoint to the same exact APK and physical-device session.

## Required checkpoints

- `smoke-cold-start`
- `smoke-warm-race`
- `smoke-after-restarts`

Each required checkpoint must also carry trustworthy metadata. The analyzer fails closed when any of the following is missing or malformed:

- session APK SHA-256;
- session device-serial SHA-256;
- checkpoint APK SHA-256;
- checkpoint device-serial SHA-256;
- checkpoint metadata label;
- checkpoint automated crash/ANR/native-fatal red-flag counter.

Both fingerprints must be valid 64-character hexadecimal SHA-256 values. A checkpoint metadata label must exactly match its required checkpoint directory, and every checkpoint fingerprint must match the session fingerprint. Missing metadata is never treated as a default clean result.

## Analyzer

```bash
python tools/android/analyze_device_smoke.py \
  --session evidence/p1-device \
  --tier mid
```

The tier must match the capability tier being reviewed under `docs/performance/UNITY_DEVICE_TIERS.md`.

The analyzer checks what ADB can truthfully measure from the captured evidence:

- mandatory session/checkpoint APK and device fingerprint integrity;
- checkpoint metadata-label identity;
- mandatory automated crash/ANR/native-fatal counter and any reported red flags;
- process PSS for warm-race and post-restart checkpoints;
- Android `gfxinfo` P95/P99 total frame-time samples when available;
- restart PSS growth against the 5% UPER-001 budget, with a positive warm-race PSS baseline required;
- Android thermal status, rejecting SEVERE or worse.

It deliberately does **not** invent Unity main-thread, render-thread, GPU or sustained-20-minute profiler data. Those remain separate profiler/manual evidence under UPER-001/UPER-006 review.

Possible analyzer verdicts are only:

- `BLOCKED`
- `PASSABLE_FOR_MANUAL_REVIEW`

`verified` is always `false`.

## Authoritative publication preflight

`verify_release_with_production_art.py` requires `--performance-tier low|mid|high` and runs the UPER-006 smoke analyzer before the existing publication preflight. A blocked smoke analysis—including missing fingerprint or checkpoint-integrity metadata—aborts before publication eligibility can be returned.

This does not complete `UPER-006`: a new exact candidate, physical-device captures, human review, and the remaining profiler/sustained evidence are still required.
