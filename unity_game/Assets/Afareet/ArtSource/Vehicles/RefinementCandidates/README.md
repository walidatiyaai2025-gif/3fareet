# Afareet King Hero — Refinement Candidate Intake

This directory is reserved for a **non-production** Blender-generated Hero candidate.

Expected artist handoff:

- File: `AfareetKing_Hero.fbx`
- SHA-256: `97b02c87118c451d068c881fc551787d6e468ec8002cce7802db62258cc4cda2`
- Size: `1475244` bytes
- Classification: `REFINEMENT_CANDIDATE`
- UART-003 production gate: **not eligible**
- UPER-009 owner/Art Director gate: **not accepted**

Materialize the exact FBX with:

```powershell
.\tools\android\import_hero_refinement_candidate_windows.ps1 -SourceFbx "C:\path\to\AfareetKing_Hero.fbx"
```

Optionally pass `-UnityPath` to invoke the Unity staging method after the hash-checked
copy. The resulting Resource prefab is for Editor/experimental APK inspection only.
A normal Android production build fails closed if that refinement prefab is present.

Before UART-003 can close, an artist must refine the silhouette/surfaces/materials,
optimize LOD geometry into the production budgets, move the approved source into the
canonical authored Hero source path, bind provenance through the production binder,
build a new exact candidate APK, and receive explicit owner/Art Director acceptance.
