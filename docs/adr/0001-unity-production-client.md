# ADR-0001 — Unity is the production game client

**Status:** APPROVED  
**Date:** 2026-08-13  
**Decision owners:** Product Owner + Unity Tech Lead

## Context

اللعبة المستهدفة سباق 3D للموبايل مع vehicle physics، 3D environments، camera/VFX وasset pipeline. Flutter/Flame أثبت قواعد الميكانيك لكنه ليس المسار المناسب للإنتاج 3D الكامل.

## Decision

Unity `6000.5.8f1` داخل `unity_game/` هو Production Client. Flutter/Flame يصبح Legacy Reference قابلًا للبناء والاختبار ولا يستقبل Features إنتاجية مزدوجة.

## Consequences

- كل Gameplay/Art/UI إنتاجي جديد يذهب إلى Unity.
- تنقل القواعد بالاختبارات والـConfig، لا بربط Runtime بين Dart وC#.
- CI وRelease Gate ينتقلان إلى Unity Android.
- Backend contract يبقى Laravel/MySQL خلف HTTPS API ولا يتأثر بمحرك العميل.
- Flutter evidence تاريخي ولا يغلق Unity milestones.

## Reversal

عكس القرار يحتاج ADR جديدًا وEvidence أن Unity لا يحقق الـGate، مع موافقة Product Owner وTech Lead.
