# 🔧 حل مشاكل النشر على Railway

## 🚨 المشاكل الشائعة والحلول

### ❌ المشكلة: "This page can't be found" أو HTTP 404

#### الأسباب المحتملة:
1. **Port Binding خاطئ**
2. **متغيرات البيئة مفقودة**
3. **مشكلة في الـ Dockerfile**
4. **مشكلة في قاعدة البيانات**

#### ✅ الحلول:

#### 1. تحقق من Variables في Railway:
```
DATABASE_URL = (يجب أن يكون موجود تلقائياً)
ASPNETCORE_ENVIRONMENT = Production
PORT = 8080
```

#### 2. تحقق من Logs:
1. اذهب إلى **Deployments** → **View Logs**
2. ابحث عن:
   - `Now listening on: http://0.0.0.0:8080` ✅
   - `Application started` ✅
   - أي رسائل خطأ ❌

#### 3. اختبر Health Check:
```
GET https://YOUR-APP.railway.app/health
```
يجب أن يرجع:
```json
{
  "status": "Healthy",
  "timestamp": "2024-12-18T..."
}
```

#### 4. اختبر Root Endpoint:
```
GET https://YOUR-APP.railway.app/
```
يجب أن يرجع:
```json
{
  "status": "OK",
  "message": "AZ Certificates API is running",
  "timestamp": "2024-12-18T...",
  "environment": "Production"
}
```

---

### ❌ المشكلة: Database Connection Error

#### الأعراض:
- التطبيق يبدأ لكن يفشل في الـ API calls
- رسائل خطأ في Logs عن قاعدة البيانات

#### ✅ الحلول:

#### 1. تحقق من PostgreSQL Service:
- تأكد أن PostgreSQL service موجود في نفس المشروع
- تأكد أن `DATABASE_URL` موجود في Variables

#### 2. تحقق من Connection String:
في Logs، ابحث عن:
```
Host=...;Port=5432;Database=...;Username=...;Password=...
```

#### 3. إعادة تشغيل Services:
1. اذهب إلى PostgreSQL service → **Settings** → **Restart**
2. اذهب إلى Backend service → **Settings** → **Restart**

---

### ❌ المشكلة: Build Failure

#### الأعراض:
- Deployment يفشل أثناء Build
- رسائل خطأ في Build Logs

#### ✅ الحلول:

#### 1. تحقق من Dockerfile:
تأكد أن الـ Dockerfile في المكان الصحيح:
```
AZ/az_backend_new/az_backend_new/Dockerfile
```

#### 2. تحقق من .csproj:
تأكد أن جميع الـ PackageReferences صحيحة

#### 3. تحقق من Railway Configuration:
تأكد أن Railway يكتشف الـ .NET project تلقائياً

---

### ❌ المشكلة: CORS Error

#### الأعراض:
- Frontend لا يستطيع الوصول للـ API
- رسائل CORS في Browser Console

#### ✅ الحلول:

#### 1. أضف Frontend URL في Variables:
```
CORS__AllowedOrigins__0=https://your-frontend.vercel.app
CORS__AllowedOrigins__1=https://your-frontend.netlify.app
```

#### 2. أو استخدم Wildcard (للتطوير فقط):
```
CORS__AllowedOrigins__0=*
```

---

## 🔍 خطوات التشخيص

### 1. تحقق من Service Status:
- اذهب إلى Railway Dashboard
- تأكد أن جميع Services تظهر **"Running"**

### 2. تحقق من Domain:
- اذهب إلى Backend Service → **Settings** → **Networking**
- تأكد أن Domain مُولد ومتاح

### 3. تحقق من Logs:
```bash
# ابحث عن هذه الرسائل في Logs:
✅ "Application started"
✅ "Now listening on: http://0.0.0.0:8080"
✅ "No migrations were applied. The database is already up to date"
❌ أي رسائل Exception أو Error
```

### 4. اختبر محلياً:
```bash
# تأكد أن التطبيق يعمل محلياً أولاً
cd AZ/az_backend_new/az_backend_new
dotnet run
# ثم اختبر: http://localhost:5167
```

---

## 📞 طلب المساعدة

إذا استمرت المشكلة، أرسل:

1. **رابط التطبيق على Railway**
2. **لقطة شاشة من Logs**
3. **لقطة شاشة من Variables**
4. **رسالة الخطأ الكاملة**

---

## ✅ علامات النجاح

عندما يعمل التطبيق بشكل صحيح، ستجد:

1. **Root Endpoint يعمل:**
   ```
   GET https://your-app.railway.app/
   → Status 200 OK
   ```

2. **Swagger يعمل:**
   ```
   GET https://your-app.railway.app/swagger
   → Swagger UI يظهر
   ```

3. **API Endpoints تعمل:**
   ```
   GET https://your-app.railway.app/api/certificates
   → يرجع قائمة فارغة أو بيانات
   ```

4. **Login يعمل:**
   ```
   POST https://your-app.railway.app/api/auth/login
   → يرجع JWT token
   ```