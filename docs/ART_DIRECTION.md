# عفاريت الأسفلت — Premium 3D Art Direction

**Status:** Mandatory visual constitution  
**Priority:** P0 for current Unity vertical slice  
**Updated:** 2026-08-30

الصور المرجعية التي يقدمها مالك المشروع هي **مرجع إحساس وجودة فقط**. المطلوب ليس نسخ لعبة أخرى، وليس Cartoon Mobile تقليديًا، وليس Cyberpunk neon مفرطًا. المطلوب لعبة سباق 3D أصلية لها شخصية **3FAREET / Cairo After Midnight**.

## الهوية البصرية

- القاهرة ليلًا بطابع سينمائي واقعي-مُصمم، مع لمسات supernatural/fantasy خفيفة تخدم الهوية ولا تبتلع السباق.
- Palette أساسية: Midnight Navy/Black + asphalt neutrals + warm street/shop amber + cool moon/cyan accents.
- Cyan/Turquoise وGold/Amber تستخدمان كهوية accent، وليس كغسيل neon على كل المشهد.
- Magenta/Orange يمكن استخدامهما في Spirit/Nitro/VFX فقط عند الحاجة.
- السيارات 3D أصلية stylized-realistic: stance قوي، عجلات مقروءة، خامات معدنية/طلاء محسوبة، انعكاسات مضبوطة.
- Drift/Nitro/VFX لهما signature واضح لكن bounded للأداء.
- UI داكن premium بسيط لا يغطي الطريق.

## أهم مبدأ بصري — Perceptual 3D

وجود Meshes ثلاثية الأبعاد لا يكفي. كل لقطة لعب رئيسية يجب أن تحتوي على مؤشرات عمق واضحة:

1. Perspective chase camera منخفضة نسبيًا خلف السيارة.
2. Foreground قريب يمر بسرعة: curb/poles/props/parked cars.
3. Midground: shops/buildings/traffic/intersections.
4. Background: skyline/bridges/distant buildings/haze.
5. Vehicle contact shadow + wheel/suspension response.
6. Road/curb/sidewalk بارتفاعات وخامات مختلفة.
7. Lighting hierarchy يظهر حجم العناصر.

إذا أخفيت الـHUD، يجب أن تبقى الصورة واضحة كلعبة **third-person 3D racing**.

## Camera look

- Perspective، وليس orthographic/flat composition.
- Normal FOV تقريبًا 58–64° كنقطة بداية.
- Speed FOV قد يصل تقريبًا 68–74°.
- Damped chase motion؛ الكاميرا ليست child صلبًا للعربية.
- Subtle acceleration/brake/drift feedback.
- Camera collision.
- Shake محدود جدًا وقابل للتقليل/التعطيل.

## Vehicle presentation

- Hero car هي مركز القراءة البصرية أثناء القيادة والكراج.
- Wheel rotation/steering واضحان.
- Suspension/body roll/pitch مقروءة بدون مبالغة كرتونية.
- Contact مع الأرض مقنع.
- Brake/reverse/head lights تعمل بصريًا.
- Tire smoke/skid marks عند الانزلاق.
- ممنوع استخدام تصميم سيارة إنتاجية معروفة بصورة قابلة للتعرف أو شعارات تجارية محمية.

## Cairo world language

المدينة يجب أن تحيط بالطريق، لا أن تكون صف مبانٍ ملتصقًا بحلبة.

استخدم مزيجًا أصليًا من:
- apartment facades;
- balconies;
- rooftop rooms;
- satellite dishes;
- water tanks;
- AC units;
- shopfronts/shutters;
- fictional Arabic signage;
- awnings;
- concrete walls/gates;
- street lamps;
- utility poles/wires;
- kiosks;
- bins;
- medians/barriers;
- flyover/bridge silhouettes;
- visual-only side streets.

لا ترتب المباني في صف متكرر مثالي. غيّر الارتفاع/العمق/العرض/الواجهة بشكل deterministic.

## Road look

- Dark asphalt ليس flat grey plane.
- Roughness/normal variation حسب الميزانية.
- Patches/cracks/stains/skid marks.
- Manholes/drains.
- Lane markings/crosswalks.
- Curbs/sidewalks بارتفاع واضح.
- Intersections والـroad edges لها تنوع.

## Lighting

Moon/directional light هو base فقط، وليس مصدر الإضاءة الوحيد.

المطلوب:
- pools of warm street light;
- shop/window practical light;
- cooler moon/ambient fill;
- vehicle headlights/taillights;
- selective emissive signage;
- dark alleys/shadow zones;
- contact/soft shadows بميزانية موبايل.

يجب أن توجد مناطق bright / medium / dark. Flat global lighting غير مقبول.

## Atmosphere / Post

مسموح بصورة restrained:
- tonemapping;
- color grading;
- subtle bloom;
- subtle vignette;
- fog/haze;
- AO عند ملاءمة الأداء;
- anti-aliasing.

ممنوع bloom قوي يجعل كل شيء مضيئًا أو يحول القاهرة إلى Cyberpunk generic.

## Main Menu

- Hero vehicle كبيرة وواضحة.
- Cairo night cinematic background.
- CTA واضح.
- Navigation premium، أصلية وغير منسوخة من المرجع.

## Garage

- 3D showroom مظلم فاخر.
- السيارة هي مركز الشاشة.
- Rotate/inspect.
- Stats/customization/event entry بدون grid افتراضي ممل.

## Race HUD

- Minimal.
- Speed/gear/position/timer/objective عند الأطراف.
- Drift/Spirit/Nitro عند الحاجة.
- Touch controls واضحة لكن لا تسيطر على الصورة.
- Safe-area aware.
- لا يتم نسخ HUD لعبة مرجعية.

## ممنوع بصريًا

- Cartoon UI طفولي/مسطح.
- primitive placeholder look قرب الكاميرا في النسخة المقبولة بصريًا.
- خلفيات عامة فارغة حول الطريق.
- صفوف مبانٍ متكررة بلا عمق.
- سيارة تبدو كأنها تنزلق فوق Plane.
- Bloom/Glow مفرط.
- UI مزدحم يغطي اللعب.
- نسخ مباشر من game reference.
- Assets غير مرخصة أو real-brand vehicle/logo replication.

## Prototype / Vertical Slice Visual Acceptance Gate

لا يمكن إعلان `VERIFIED` إلا إذا تحقق الآتي في screenshots/device review:

1. أول لقطة تقرأ فورًا كلعبة سباق 3D حديثة.
2. Chase camera تعطي perspective/depth قويًا.
3. Hero vehicle مقنعة ولها وزن واتصال بالأرض.
4. Foreground/Midground/Background واضحة.
5. Track/road لا يبدو flat primitive strip.
6. Cairo-inspired city density تحيط باللاعب.
7. Traffic/parked cars تعطي scale.
8. Lighting/shadows تظهر الحجم.
9. Drift/Nitro لها feedback أصلي ومضبوط.
10. HUD أصلي ومحدود ولا يخفي ضعف المشهد.
11. الأداء مقبول على target Android device class.

إذا الكود والأداء ينجحان لكن الصورة لا تحقق هذه النقاط، تكون المهمة `DONE` وليست `VERIFIED`.

## Performance Guardrails

- baked/faked/mixed lighting عندما يكون أوفر.
- LOD للسيارات والبيئة.
- pooling للtraffic/VFX.
- shared materials / instancing حيث يفيد.
- quality tiers للshadows/reflections/post/particles/traffic.
- تقليل overdraw والشفافية الثقيلة.
- قياس frame time في أكثر مشهد ازدحامًا.
