# Last verified APK released

هذا مسار Legacy للتوافق مع الروابط القديمة. المصدر الرسمي الحالي هو
[`docs/releases/LAST_VERIFIED_APK.md`](../docs/releases/LAST_VERIFIED_APK.md)، وملف APK نفسه يُرفع كـGitHub Release Asset.

## قواعد إلزامية
1. لا تضع Debug APK أو build غير مختبر.
2. يجب أن يرتبط الـAPK بـcommit SHA معروف.
3. يجب تنفيذ Prototype/Release smoke test قبل وضعه هنا.
4. الاسم المقترح: `afareet-v0.1.0-prototype-verified.apk`.
5. أضف/حدّث `VERIFICATION.md` عند نشر APK يتضمن:
   - Version
   - Commit SHA
   - Build date
   - Device / Android API
   - Tester
   - Smoke test result
   - SHA-256
6. لا تعمل commit لأي APK هنا؛ حدّث ملف المؤشر بعد رفع GitHub Release المعتمد.

> لا يوجد APK Verified عند إنشاء هذا المجلد. لا يتم إضافة ملف APK حتى ينجح أول build والتحقق على جهاز فعلي.
