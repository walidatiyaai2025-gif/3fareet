# عفاريت الأسفلت — Premium Visual Direction

**Status:** Mandatory visual constitution  
**Priority:** P0 for Prototype

الصور المرجعية التي قدمها مالك المشروع هي المرجع البصري الملزم. المطلوب ليس Cartoon Mobile تقليديًا، بل **Premium Neon Egyptian Fantasy Racing** بطابع سينمائي فخم.

## الهوية البصرية
- القاهرة ليلًا/وقت الغروب بطابع Fantasy: أهرامات، قباب، مآذن، نخيل، طرق مرتفعة، وواجهات مدينة مضاءة.
- Palette: Midnight Navy/Black + Cyan/Turquoise Neon + Warm Gold/Amber، مع Magenta/Orange كطاقة ثانوية.
- سيارات Stylized 3D بجسم عريض وstance قوي وعجلات واضحة وخامات لامعة وانعكاسات محسوبة.
- Rim light + under-glow + emissive accents.
- Drift/Nitro/VFX جزء أساسي من الهوية، وليس polish لاحقًا.
- UI داكن شبه زجاجي/معدني، Cyan outlines، Gold highlights، ومساحات سوداء مريحة.

## Main Menu
- Hero Car كبيرة ومركزية.
- Cairo cinematic background كاملة الشاشة.
- CTA واضح مثل Play / Continue.
- Currency/Profile في الأعلى.
- Navigation premium بدون Material widgets خام.

## Garage
- Showroom مظلم فاخر بمنصة مضاءة.
- السيارة هي مركز الشاشة.
- Car carousel/cards في الأسفل أو الجانب.
- Stats/Customize/Select بعناصر UI مخصصة.
- لا يستخدم Grid تقليدي ممل كواجهة رئيسية للكراج.

## Race HUD
- Minimal أثناء السباق.
- Position/Lap/Timer في الأعلى.
- Drift/Spirit/Nitro/Power-ups عند الأطراف.
- لا يغطي الطريق.
- أزرار التحكم لها skin خاص ومتناسق مع اللغة البصرية.

## Track look
- Night/sunset cinematic Egypt.
- Dark asphalt مع neon/gold practical lighting.
- Landmarks مصرية مقروءة لكن Fantasy وليست محاكاة حرفية.
- البيئة تبقى مقروءة أثناء السرعة العالية ولا تعتمد على التفاصيل الصغيرة فقط.

## ممنوع بصريًا
- Cartoon UI طفولي أو مسطح.
- خلفيات بيضاء/رمادية عامة داخل اللعبة النهائية.
- Buttons/Material widgets افتراضية ظاهرة.
- HUD مزدحم أو نصوص كثيرة أثناء السباق.
- Bloom/Glow مفرط يجعل الصورة مغسولة.
- Assets غير مرخصة أو نسخ مباشرة من علامات سيارات تجارية.

## Prototype Visual Acceptance Gate
لا يمكن إعلان `P1 VERIFIED` إلا إذا تحقق الآتي:
1. أول Screenshot من الـPrototype تبدو كلعبة سباق Premium وليست Technical Demo.
2. Hero car مقنعة بصريًا بما يكفي للحكم على الإضاءة والكاميرا.
3. Track prototype يحتوي على Cairo fantasy landmarks/lighting/neon language.
4. Magic Drift وNitro Spirit لهما Signature VFX واضحة.
5. HUD يستخدم skin مخصصًا متوافقًا مع هذه الوثيقة.
6. Team Lead + Owner يوافقان على Visual Review.

إذا كان الأداء ناجحًا لكن الصورة بعيدة بوضوح عن هذه الهوية، تكون المرحلة `DONE` وليست `VERIFIED`.

## Performance Guardrails
- Baked/faked lighting حين يكون أوفر من الإضاءة الديناميكية.
- LOD للسيارات والبيئة.
- Texture atlases + object pooling للـVFX.
- Quality tiers لـBloom/particles/shadows/reflections.
- تقليل overdraw والشفافية الثقيلة أثناء Drift/Nitro.
- قياس frame time في أكثر مشهد ازدحامًا.
