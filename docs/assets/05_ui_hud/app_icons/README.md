# App Icons

## Current received reference
- `references/app_icon_platform_sizes_reference.jpg` — لوحة المقاسات المستلمة من مالك المشروع؛ مرجع بصري فقط.
- `exports_candidate/app_icon_candidate_256.jpg` — قصّة نظيفة مبدئية من نفس المرجع لتسهيل المعاينة فقط؛ ليست Production master.

## Production gate
المطلوب قبل `VERIFIED`:
1. Master clean artwork 1024x1024 بدون annotations أو نصوص المقاسات.
2. Android adaptive icon foreground/background + Play Store 512x512.
3. iOS AppIcon set من master نظيف.
4. فحص readability في الأحجام الصغيرة وعدم قص العناصر داخل mask.
5. تحديث `docs/MISSED_ASSETS.md` وذكر PR/Owner.

لا تقم بتكبير `app_icon_candidate_256.jpg` واستخدامه كـ1024 master.
