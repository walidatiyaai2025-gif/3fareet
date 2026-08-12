# Last verified APK released

هذا المجلد مخصص **لآخر APK Android اجتاز التحقق الفعلي فقط**.

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
6. عند نجاح نسخة أحدث، تكون هي المرجع الأخير الواضح.

> لا يوجد APK Verified عند إنشاء هذا المجلد. لا يتم إضافة ملف APK حتى ينجح أول build والتحقق على جهاز فعلي.
