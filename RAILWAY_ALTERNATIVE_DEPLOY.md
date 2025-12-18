# 🚂 طريقة بديلة لنشر AZ Certificates على Railway

## 🎯 المشكلة
Railway لا يجد الـ Dockerfile في المسار المحدد.

## ✅ الحل البديل: استخدام Auto-Detection

### 1️⃣ إعداد المشروع بدون Dockerfile

#### أ) في Railway Dashboard:
1. **+ New Project**
2. **Deploy from GitHub repo**
3. اختر repository الخاص بك
4. **Root Directory**: اتركه فارغ أو ضع `AZ/az_backend_new/az_backend_new`

#### ب) Railway سيكتشف .NET تلقائياً:
- سيجد ملف `.csproj`
- سيستخدم .NET buildpack تلقائياً
- لا حاجة لـ Dockerfile

### 2️⃣ إعداد المتغيرات المطلوبة

في **Variables** tab، أضف:

```bash
# متغيرات أساسية
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT

# قاعدة البيانات (تلقائي)
DATABASE_URL=${{Postgres.DATABASE_URL}}

# JWT Settings (نسخ من appsettings.json)
JwtSettings__SecretKey=AZ_International_Certificates_System_Secret_Key_2024_Very_Long_And_Secure
JwtSettings__Issuer=AZ International
JwtSettings__Audience=AZ Certificates System
JwtSettings__ExpirationHours=24

# CORS (اختياري)
CORS__AllowedOrigins__0=http://localhost:3000
CORS__AllowedOrigins__1=https://azinternational-eg.com
CORS__AllowedOrigins__2=https://az-international.vercel.app
```

### 3️⃣ إضافة PostgreSQL Database

1. في نفس المشروع: **+ New** → **Database** → **Add PostgreSQL**
2. Railway سيربط `DATABASE_URL` تلقائياً

### 4️⃣ اختبار التطبيق

#### أ) Health Check:
```bash
curl https://YOUR-APP.railway.app/health
```
**المتوقع:**
```json
{
  "status": "Healthy",
  "timestamp": "2024-12-18T..."
}
```

#### ب) Root Endpoint:
```bash
curl https://YOUR-APP.railway.app/
```
**المتوقع:**
```json
{
  "status": "OK",
  "message": "AZ Certificates API is running",
  "timestamp": "2024-12-18T...",
  "environment": "Production"
}
```

#### ج) Swagger UI:
```
https://YOUR-APP.railway.app/swagger
```

#### د) API Test:
```bash
curl https://YOUR-APP.railway.app/api/certificates
```

---

## 🔧 الطريقة الثانية: إصلاح مسار Dockerfile

إذا كنت تريد استخدام Dockerfile:

### 1️⃣ نقل Dockerfile إلى Root Directory:

```bash
# انسخ الـ Dockerfile إلى مجلد المشروع الرئيسي
cp AZ/az_backend_new/az_backend_new/Dockerfile AZ/az_backend_new/
```

### 2️⃣ تحديث Dockerfile للمسار الجديد:

```dockerfile
# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["az_backend_new/az_backend_new.csproj", "az_backend_new/"]
RUN dotnet restore "az_backend_new/az_backend_new.csproj"
COPY az_backend_new/ az_backend_new/
WORKDIR "/src/az_backend_new"
RUN dotnet build "az_backend_new.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "az_backend_new.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "az_backend_new.dll"]
```

### 3️⃣ في Railway:
- **Root Directory**: `AZ/az_backend_new`
- **Dockerfile Path**: `Dockerfile`

---

## 🎯 التوصية

**استخدم الطريقة الأولى (Auto-Detection)** لأنها:
- ✅ أسهل وأسرع
- ✅ لا تحتاج Dockerfile
- ✅ Railway يدير كل شيء تلقائياً
- ✅ أقل عرضة للأخطاء

---

## 🚨 نصائح مهمة

### 1. تأكد من المتغيرات:
جميع المتغيرات في `appsettings.json` يجب أن تكون في Railway Variables

### 2. تحقق من Logs:
```bash
# ابحث عن هذه الرسائل:
✅ "Application started"
✅ "Now listening on: http://0.0.0.0:$PORT"
✅ "No migrations were applied. The database is already up to date"
```

### 3. اختبر محلياً أولاً:
```bash
# تأكد أن التطبيق يعمل محلياً
cd AZ/az_backend_new/az_backend_new
dotnet run
# اختبر: http://localhost:8080/health
```

---

## 📞 للمساعدة

إذا احتجت مساعدة:
1. أرسل رابط المشروع على Railway
2. أرسل لقطة شاشة من Variables
3. أرسل لقطة شاشة من Logs