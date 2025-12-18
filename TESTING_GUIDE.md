# 🧪 دليل اختبار AZ Certificates API

## ✅ حالة التطبيق
- **التطبيق يعمل بنجاح على:** `http://localhost:5167`
- **Swagger UI:** `http://localhost:5167/swagger`
- **قاعدة البيانات:** PostgreSQL متصلة بنجاح
- **المستخدم الافتراضي:** admin@azinternational.com / Admin123!

---

## 🔐 اختبار المصادقة

### تسجيل الدخول
```powershell
$body = @{
    email = "admin@azinternational.com"
    password = "Admin123!"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5167/api/auth/login" -Method POST -Body $body -ContentType "application/json"
$token = $response.token
Write-Host "Token: $token"
```

---

## 📜 اختبار إدارة الشهادات

### إنشاء شهادة جديدة
```powershell
$certificateBody = @{
    serialNumber = "AZ-VT-002"
    personName = "Sara Ahmed Ali"
    serviceMethod = 2  # LiquidPenetrantTesting
    certificateType = 1  # Initial
    expiryDate = "2025-06-30T00:00:00Z"
    country = "Egypt"
    state = "Alexandria"
    streetAddress = "Smouha"
} | ConvertTo-Json

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

$newCert = Invoke-RestMethod -Uri "http://localhost:5167/api/certificates" -Method POST -Body $certificateBody -Headers $headers
$newCert
```

### البحث عن شهادة
```powershell
# البحث بالرقم التسلسلي
$searchBySerial = Invoke-RestMethod -Uri "http://localhost:5167/api/certificates/search?serialNumber=AZ-VT-001" -Method GET

# البحث بالاسم
$searchByName = Invoke-RestMethod -Uri "http://localhost:5167/api/certificates/search?personName=Ahmed" -Method GET

# البحث بنوع الخدمة
$searchByService = Invoke-RestMethod -Uri "http://localhost:5167/api/certificates/search?serviceMethod=1" -Method GET
```

### عرض جميع الشهادات
```powershell
$allCerts = Invoke-RestMethod -Uri "http://localhost:5167/api/certificates" -Method GET
$allCerts
```

---

## 📧 اختبار الإيميل

### إرسال إيميل
```powershell
$emailBody = @{
    to = "test@example.com"
    subject = "Test Email"
    body = "This is a test email from AZ Certificates System"
} | ConvertTo-Json

$emailResponse = Invoke-RestMethod -Uri "http://localhost:5167/api/email/send" -Method POST -Body $emailBody -Headers $headers
```

### عرض الشهادات المنتهية الصلاحية قريباً
```powershell
$expiringCerts = Invoke-RestMethod -Uri "http://localhost:5167/api/email/expiring-certificates?days=30" -Method GET -Headers $headers
```

---

## 🔧 ServiceMethod Values
- `1` = Visual Testing
- `2` = Liquid Penetrant Testing  
- `3` = Magnetic Particle Testing
- `4` = Radiographic Testing
- `5` = Ultrasonic Testing

## 🏷️ CertificateType Values
- `1` = Initial
- `2` = Recertificate

## 👤 Role Values
- `1` = Admin
- `2` = User

---

## 🚀 الخطوات التالية

1. **اختبر جميع الـ endpoints في Swagger UI**
2. **اختبر Excel Import** (إذا كان متوفراً)
3. **اختبر Error Handling** بإدخال بيانات خاطئة
4. **اختبر Authorization** بمحاولة الوصول بدون token

---

## 📝 ملاحظات

- التطبيق جاهز للرفع على Railway أو أي منصة أخرى
- تأكد من تحديث `CORS:AllowedOrigins` في الإنتاج
- تأكد من تحديث `EmailSettings` للإيميل الفعلي