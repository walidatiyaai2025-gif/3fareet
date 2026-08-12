# Performance Budgets

## PER-001 — Target device tiers

- Low tier: 4 GB RAM class devices, 720p–900p target, reduced VFX/texture tier.
- Mid tier: 6–8 GB RAM class devices, 1080p target, standard VFX/texture tier.
- High tier: 8+ GB RAM class devices, 1080p+ target, premium VFX where frame budget allows.

## PER-002 — FPS / frame-time budgets

- Primary gameplay target: 60 FPS.
- 60 FPS frame budget: 16.67 ms.
- Degraded low-tier fallback: 30 FPS / 33.33 ms only when 60 FPS cannot be sustained.
- CPU simulation budget target: <= 6 ms/frame at 60 FPS.
- Rendering/VFX budget target: <= 8 ms/frame at 60 FPS, leaving headroom for OS/input/audio.

## PER-003 — Memory budget

- Low tier working-set target: <= 700 MB.
- Mid tier working-set target: <= 1.0 GB.
- High tier working-set target: <= 1.3 GB.
- Runtime caches and pools must be bounded and clearable between races.

## PER-004 — Texture budget

- Low tier resident texture budget: <= 220 MB.
- Mid tier: <= 350 MB.
- High tier: <= 500 MB.
- Prefer 1K assets unless a hero asset demonstrably benefits from 2K.

## PER-005 — VFX particle budget

- Low tier: <= 600 active particles across race scene.
- Mid tier: <= 1,200 active particles.
- High tier: <= 2,000 active particles.
- Drop cosmetic particles before gameplay-critical feedback.

## Enforcement

Budgets are engineering targets, not proof of device compliance. PER-017→019 remain unverified until measured on real target hardware.
