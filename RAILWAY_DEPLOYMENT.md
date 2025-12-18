# 🚂 دليل رفع AZ Certificates API على Railway

## 📋 المتطلبات
- حساب على [Railway](https://railway.app) (مجاني)
- المشروع على GitHub

---

## 🚀 خطوات الرفع

### 1️⃣ إنشاء حساب Railway
1. اذهب إلى: https://railway.app
2. سجل دخول بـ GitHub

### 2️⃣ إنشاء قاعدة بيانات PostgreSQL
1. في Dashboard، انقر **+ New Project**
2. اختر **Provision PostgreSQL**
3. انتظر حتى تُنشأ قاعدة البيانات
4. انقر على PostgreSQL → **Variables**
5. انسخ `DATABASE_URL` (ستحتاجه لاحقاً)

### 3️⃣ رفع الـ Backend
1. في نفس المشروع، انقر **+ New** → **GitHub Repo**
2. اختر repository الخاص بك
3. حدد المجلد: `AZ/az_backend_new/az_backend_new`
4. Railway سيكتشف الـ .NET project تلقائياً

### 4️⃣ إعداد المتغيرات (Variables)
في **Variables** tab، أضف:

```
DATABASE_URL = (سيتم ربطه تلقائياً من PostgreSQL)
ASPNETCORE_ENVIRONMENT = Production
PORT = 8080
```

**ملاحظة:** لا تحتاج لإضافة `ASPNETCORE_URLS` لأنه مُعرف في الكود

### 5️⃣ ربط PostgreSQL بالـ Backend
1. انقر على الـ Backend service
2. اذهب إلى **Variables**
3. انقر **+ Add Variable Reference**
4. اختر `DATABASE_URL` من PostgreSQL

### 6️⃣ الحصول على الرابط
1. اذهب إلى **Settings** → **Networking**
2. انقر **Generate Domain**
3. ستحصل على رابط مثل: `https://az-certificates-production.up.railway.app`

---

## ✅ اختبار الـ API

### Swagger
```
https://YOUR-APP.railway.app/swagger
```

### Endpoints
```
GET  /api/certificates                    - جميع الشهادات
POST /api/auth/login                      - تسجيل الدخول
GET  /api/certificates/search?serialNumber=AZ-VT-001  - بحث بالرقم
GET  /api/certificates/search?personName=Ahmed        - بحث بالاسم
```

### تسجيل الدخول الافتراضي
```json
{
  "email": "admin@azinternational.com",
  "password": "Admin123!"
}
```

---

## 🔧 إعدادات إضافية

### CORS للفرونت إند
إذا كان لديك Frontend على Vercel أو Netlify:

1. في **Variables**، أضف:
   ```
   CORS__AllowedOrigins__0=https://your-frontend.vercel.app
   CORS__AllowedOrigins__1=https://your-frontend.netlify.app
   ```

### إعدادات الإيميل (اختياري)
```
EmailSettings__SmtpServer=smtp.gmail.com
EmailSettings__Port=587
EmailSettings__SenderName=AZ International
EmailSettings__SenderEmail=your-email@gmail.com
EmailSettings__Username=your-email@gmail.com
EmailSettings__Password=your-app-password
```

---

## 🔍 حل المشاكل الشائعة

### ❌ خطأ: "Connection refused"
**الحل:**
1. تأكد أن PostgreSQL service موجود في نفس المشروع
2. تأكد أن `DATABASE_URL` موجود في Variables

### ❌ خطأ: "Migration failed"
**الحل:**
1. تحقق من Logs لمعرفة الخطأ المحدد
2. تأكد أن Connection String صحيح

### ❌ خطأ: "Port binding"
**الحل:**
1. تأكد من إضافة `ASPNETCORE_URLS=http://0.0.0.0:$PORT`

---

## 📊 مراقبة التطبيق

### Logs
- اذهب إلى **Deployments** → **View Logs**
- ستجد جميع رسائل التطبيق والأخطاء

### Metrics
- Railway يوفر metrics أساسية للـ CPU والذاكرة
- يمكنك مراقبة استخدام قاعدة البيانات

---

## 💰 التكلفة

- **Hobby Plan**: مجاني حتى $5 شهرياً
- **Pro Plan**: $20 شهرياً للاستخدام التجاري

---

## 🎉 تهانينا!

مشروعك الآن على السحابة ويعمل! 🚀

**الروابط المهمة:**
- API: `https://YOUR-APP.railway.app`
- Swagger: `https://YOUR-APP.railway.app/swagger`
- Database: متاح في Railway Dashboard