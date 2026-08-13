# Coding Standards

## Unity C#

- Namespace يبدأ بـ`Afareet.<Module>`؛ لا classes إنتاجية في global namespace.
- public APIs لها XML documentation عندما تعبر Module boundary.
- tuning لا يبقى hardcoded بعد blockout؛ ينقل إلى ScriptableObject/Config task.
- `Update` للقراءة/العرض، و`FixedUpdate` للفيزياء؛ لا frame-dependent simulation.
- لا `FindObjectOfType` أو string lookups في hot paths.
- تجنب allocations وLINQ في loops كل frame.
- كل MonoBehaviour مسؤول عن lifecycle واضح، ولا static mutable state إلا abstraction مقصودة ومختبرة.
- Editor-only code داخل `Assets/Afareet/Editor/` أو Editor asmdef.
- كل bug fix في gameplay/race/config يضيف regression test عندما يكون قابلًا للأتمتة.

## Flutter Legacy

- اتبع `analysis_options.yaml` و`dart format`.
- لا Feature إنتاجية جديدة دون `FLT-*`.
- لا تغير behavior مرجعي Verified دون اختبار يشرح سبب التغيير.

## General

- UTF-8، LF في المصادر الجديدة، ولا formatting شامل لملفات Unity YAML المولدة.
- لا أسرار أو absolute machine paths أو generated caches.
- أسماء الملفات والـclasses واضحة؛ ممنوع `Manager` عام بلا نطاق مسؤولية.
- TODO في الكود يحمل Task ID: `TODO(U3D-123): ...`.
- Public contract change يحتاج Task مستقلة أو توثيق migration في PR.
