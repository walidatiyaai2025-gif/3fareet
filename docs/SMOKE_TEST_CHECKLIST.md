# P1 Android Smoke Test Checklist

Use this checklist only on a real Android device for a candidate release APK.

- Install succeeds from a clean uninstall state.
- App opens without crash or blank fatal screen.
- Resume after backgrounding does not advance simulation time unexpectedly.
- Pause/resume preserves race state.
- Restart resets race timer, lap, checkpoint progress, Spirit and vehicle state.
- Throttle, brake/reverse, steering and drift input respond consistently.
- Drift exits cleanly when input is released or speed drops below threshold.
- Spirit does not charge below the minimum drift speed.
- Nitro consumes Spirit, ends correctly and honors cooldown.
- Checkpoints cannot be skipped or completed out of order.
- Finish cannot complete until required checkpoints are passed.
- Wrong-way state clears after direction is corrected.
- Safe respawn returns to the last valid checkpoint/start point.
- HUD speed, race timer, lap and Spirit values remain coherent.
- No sustained visual stutter, ANR or thermal runaway during a 10-minute session.

A candidate is not a **Verified APK** until device/API, tester, commit SHA, result,
SHA-256 and evidence are recorded in
[`releases/LAST_VERIFIED_APK.md`](releases/LAST_VERIFIED_APK.md), and the exact
tested binary is uploaded as a GitHub Release asset.
