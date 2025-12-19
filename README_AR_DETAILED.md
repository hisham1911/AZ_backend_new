# 🏗️ نظام إدارة الشهادات - AZ Certificates Backend

## 📋 فهرس المحتويات
1. [نظرة عامة على المشروع](#نظرة-عامة-على-المشروع)
2. [هيكل المشروع](#هيكل-المشروع)
3. [المفاهيم الأساسية](#المفاهيم-الأساسية)
4. [قاعدة البيانات](#قاعدة-البيانات)
5. [طبقات التطبيق](#طبقات-التطبيق)
6. [المشاكل الشائعة وحلولها](#المشاكل-الشائعة-وحلولها)
7. [النشر والتشغيل](#النشر-والتشغيل)
8. [التحضير للمقابلات](#التحضير-للمقابلات)

---

## 🎯 نظرة عامة على المشروع

هذا المشروع هو **نظام إدارة الشهادات** لشركة AZ International المتخصصة في اختبارات المواد.

### ما يفعله النظام:
- إدارة شهادات الاختبارات المختلفة (VT, PT, MT, RT, UT)
- البحث في الشهادات بالاسم أو الرقم التسلسلي
- رفع ملفات Excel لاستيراد شهادات متعددة
- إدارة المستخدمين والصلاحيات
- إرسال إشعارات بالبريد الإلكتروني

### التقنيات المستخدمة:
- **Backend**: ASP.NET Core 8.0
- **Database**: PostgreSQL (Railway)
- **Authentication**: JWT Tokens
- **ORM**: Entity Framework Core
- **Architecture**: Clean Architecture
- **Deployment**: Railway + Docker
- **Frontend**: Next.js 14 + TypeScript

### الروابط المهمة:
- **Backend API**: https://azbackendnew-production-817b.up.railway.app
- **Frontend**: يعمل محلياً على http://localhost:3000
- **Database**: PostgreSQL على Railway

---

## 🏗️ هيكل المشروع

```
az_backend_new/
├── Controllers/          # نقاط النهاية للـ API
│   ├── CertificatesController.cs  # إدارة الشهادات
│   ├── AuthController.cs          # تسجيل الدخول
│   └── EmailController.cs         # إرسال الإيميلات
├── Models/              # نماذج البيانات
│   ├── Certificate.cs             # نموذج الشهادة
│   ├── User.cs                    # نموذج المستخدم
│   ├── ServiceMethod.cs           # أنواع الاختبارات
│   └── CertificateType.cs         # أنواع الشهادات
├── Repositories/        # طبقة الوصول للبيانات
│   ├── CertificateRepository.cs   # عمليات قاعدة البيانات للشهادات
│   └── UserRepository.cs          # عمليات قاعدة البيانات للمستخدمين
├── Services/           # منطق العمل
│   ├── JwtService.cs              # إدارة الـ JWT
│   └── EmailService.cs            # خدمة الإيميل
├── DTOs/              # نماذج نقل البيانات
│   ├── CertificateDto.cs          # نماذج API للشهادات
│   ├── AuthDto.cs                 # نماذج تسجيل الدخول
│   └── EmailDto.cs                # نماذج الإيميل
├── Data/              # إعدادات قاعدة البيانات
│   └── AzDbContext.cs             # سياق قاعدة البيانات
└── Migrations/        # تحديثات قاعدة البيانات
    └── InitialCreate.cs           # إنشاء الجداول الأولية
```
---

## 🧠 المفاهيم الأساسية

### 1. Clean Architecture (العمارة النظيفة)

```
┌─────────────────────────────────────────────────────────────┐
│                    طبقة العرض                               │
│  ┌─────────────────┐  ┌─────────────────┐                  │
│  │   Controllers   │  │      DTOs       │                  │
│  │ (نقاط النهاية)  │  │ (نقل البيانات)  │                  │
│  └─────────────────┘  └─────────────────┘                  │
├─────────────────────────────────────────────────────────────┤
│                    طبقة منطق العمل                          │
│  ┌─────────────────┐  ┌─────────────────┐                  │
│  │    Services     │  │   Repositories  │                  │
│  │ (منطق العمل)    │  │ (الوصول للبيانات)│                  │
│  └─────────────────┘  └─────────────────┘                  │
├─────────────────────────────────────────────────────────────┤
│                     طبقة البيانات                          │
│  ┌─────────────────┐  ┌─────────────────┐                  │
│  │     Models      │  │    DbContext    │                  │
│  │ (نماذج البيانات) │  │ (سياق قاعدة البيانات)│              │
│  └─────────────────┘  └─────────────────┘                  │
└─────────────────────────────────────────────────────────────┘
```

**لماذا نستخدم Clean Architecture؟**
- **فصل الاهتمامات**: كل طبقة لها مسؤولية واحدة
- **سهولة الاختبار**: يمكن اختبار كل طبقة منفصلة
- **المرونة**: يمكن تغيير قاعدة البيانات دون تأثير على منطق العمل
- **القابلية للصيانة**: الكود منظم وسهل الفهم

### 2. Repository Pattern (نمط المستودع)

**❌ بدون Repository Pattern:**
```csharp
public class CertificatesController : ControllerBase
{
    private readonly AzDbContext _context; // مرتبط مباشرة بقاعدة البيانات
    
    public async Task<Certificate> GetCertificate(int id)
    {
        // كود قاعدة البيانات مختلط مع منطق التحكم
        return await _context.Certificates.FindAsync(id);
    }
}
```

**✅ مع Repository Pattern:**
```csharp
public class CertificatesController : ControllerBase
{
    private readonly ICertificateRepository _repo; // يعتمد على التجريد
    
    public async Task<Certificate> GetCertificate(int id)
    {
        return await _repo.GetByIdAsync(id); // منطق عمل واضح
    }
}
```

**فوائد Repository Pattern:**
1. **فصل الاهتمامات** - منطق البيانات منفصل عن منطق التحكم
2. **سهولة الاختبار** - يمكن محاكاة Repository للاختبارات
3. **القابلية للصيانة** - تغيير قاعدة البيانات لا يؤثر على Controllers
4. **إعادة الاستخدام** - نفس Repository يُستخدم في عدة Controllers

---

## 📊 قاعدة البيانات

### تصميم الجداول

```sql
-- جدول الشهادات
┌─────────────────────────────────────────────────────────────┐
│                    CERTIFICATES                             │
├─────────────────────────────────────────────────────────────┤
│ Id (PK)          │ int          │ المفتاح الأساسي            │
│ SerialNumber     │ varchar(50)  │ فهرس فريد (5070-VT)       │
│ PersonName       │ varchar(100) │ اسم صاحب الشهادة          │
│ ServiceMethod    │ int          │ نوع الاختبار (1=VT, 2=PT) │
│ CertificateType  │ int          │ نوع الشهادة (1=أولي, 2=تجديد)│
│ ExpiryDate       │ datetime     │ تاريخ انتهاء الصلاحية      │
│ Country          │ varchar(50)  │ البلد (اختياري)           │
│ State            │ varchar(50)  │ المحافظة (اختياري)        │
│ StreetAddress    │ varchar(200) │ العنوان (اختياري)         │
│ CreatedAt        │ datetime     │ تاريخ الإنشاء             │
│ UpdatedAt        │ datetime     │ تاريخ آخر تحديث           │
└─────────────────────────────────────────────────────────────┘

-- جدول المستخدمين
┌─────────────────────────────────────────────────────────────┐
│                        USERS                                │
├─────────────────────────────────────────────────────────────┤
│ Id (PK)          │ int          │ المفتاح الأساسي            │
│ Email            │ varchar(100) │ فهرس فريد                 │
│ PasswordHash     │ text         │ كلمة المرور مشفرة بـ BCrypt │
│ Role             │ int          │ الدور (1=مستخدم, 2=مدير)  │
│ CreatedAt        │ datetime     │ تاريخ الإنشاء             │
│ UpdatedAt        │ datetime     │ تاريخ آخر تحديث           │
└─────────────────────────────────────────────────────────────┘
```

### نموذج الشهادة (Certificate Model)

```csharp
public class Certificate
{
    // المفتاح الأساسي
    public int Id { get; set; }
    
    // المفتاح التجاري (مثل: 5070-VT)
    [Required, StringLength(50)]
    public string SerialNumber { get; set; }
    
    // اسم صاحب الشهادة
    [Required, StringLength(100)]
    public string PersonName { get; set; }
    
    // نوع الاختبار (VT, PT, MT, RT, UT)
    public ServiceMethod ServiceMethod { get; set; }
    
    // نوع الشهادة (أولي، تجديد)
    public CertificateType CertificateType { get; set; }
    
    // تاريخ انتهاء الصلاحية
    public DateTime ExpiryDate { get; set; }
    
    // حقول الموقع (اختيارية)
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? StreetAddress { get; set; }
    
    // حقول المراجعة
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // خاصية محسوبة
    public bool IsExpired => ExpiryDate < DateTime.UtcNow;
}
```

### أنواع الاختبارات (ServiceMethod)

```csharp
public enum ServiceMethod
{
    [Display(Name = "Visual Testing")]
    VisualTesting = 1,           // VT - الفحص البصري
    
    [Display(Name = "Liquid Penetrant Testing")]
    LiquidPenetrantTesting = 2,  // PT - اختبار السائل النافذ
    
    [Display(Name = "Magnetic Particle Testing")]
    MagneticParticleTesting = 3, // MT - اختبار الجسيمات المغناطيسية
    
    [Display(Name = "Radiographic Testing")]
    RadiographicTesting = 4,     // RT - الاختبار الإشعاعي
    
    [Display(Name = "Ultrasonic Testing")]
    UltrasonicTesting = 5        // UT - الاختبار فوق الصوتي
}
```

---

## 🎛️ طبقات التطبيق

### 1. Controllers Layer (طبقة التحكم)

```csharp
[ApiController]
[Route("api/[controller]")]
public class CertificatesController : ControllerBase
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly ILogger<CertificatesController> _logger;

    // GET /api/certificates - الحصول على جميع الشهادات
    [HttpGet]
    public async Task<ActionResult<PagedResult<CertificateDto>>> GetCertificates(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    
    // GET /api/certificates/5 - الحصول على شهادة محددة
    [HttpGet("{id}")]
    public async Task<ActionResult<CertificateDto>> GetCertificate(int id)
    
    // POST /api/certificates - إنشاء شهادة جديدة
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CertificateDto>> CreateCertificate(CreateCertificateDto createDto)
    
    // PUT /api/certificates/5 - تحديث شهادة
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CertificateDto>> UpdateCertificate(int id, UpdateCertificateDto updateDto)
    
    // DELETE /api/certificates/5 - حذف شهادة
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCertificate(int id)
    
    // GET /api/certificates/search - البحث في الشهادات
    [HttpGet("search")]
    public async Task<ActionResult<List<CertificateDto>>> SearchCertificates([FromQuery] CertificateSearchDto searchDto)
    
    // POST /api/certificates/import - رفع ملف Excel
    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ImportResultDto>> ImportFromExcel(IFormFile file)
}
```

### 2. Repository Layer (طبقة المستودع)

```csharp
public interface ICertificateRepository
{
    Task<Certificate?> GetByIdAsync(int id);
    Task<Certificate?> GetBySerialNumberAsync(string serialNumber);
    Task<PagedResult<Certificate>> GetAllAsync(int page, int pageSize);
    Task<List<Certificate>> SearchAsync(CertificateSearchDto searchDto);
    Task<Certificate> CreateAsync(Certificate certificate);
    Task<Certificate> UpdateAsync(Certificate certificate);
    Task<bool> DeleteAsync(int id);
    Task<bool> SerialNumberExistsAsync(string serialNumber, int? excludeId = null);
    Task<List<Certificate>> GetExpiringCertificatesAsync(int daysFromNow = 30);
}
```

### 3. Services Layer (طبقة الخدمات)

#### JWT Service (خدمة الرموز المميزة)
```csharp
public interface IJwtService
{
    string GenerateToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
}

public class JwtService : IJwtService
{
    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        
        // إنشاء الرمز المميز مع انتهاء صلاحية 24 ساعة
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

## 🚨 المشاكل الشائعة وحلولها

### المشكلة 1: DateTime مع PostgreSQL

**❌ المشكلة:**
```csharp
var certificate = new Certificate 
{
    ExpiryDate = DateTime.Parse("2024-12-25") // Kind = Unspecified
};
// النتيجة: أخطاء PostgreSQL timestamp
```

**✅ الحل:**
```csharp
// الحل الأول: تفعيل السلوك القديم
// Program.cs
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// الحل الثاني: تحديد UTC صراحة
ExpiryDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc)
```

### المشكلة 2: عدم تطابق Enum بين Frontend/Backend

**❌ المشكلة:**
```csharp
// Backend ServiceMethod.cs
VisualTesting = 1,
LiquidPenetrantTesting = 2,

// Frontend enums.ts (كان خطأ)
MagneticParticleTesting = 1,  // خطأ!
LiquidPenetrantTesting = 2,
```

**✅ الحل:**
```typescript
// Frontend يجب أن يطابق Backend تماماً
export const ServiceMethod = {
  VisualTesting: 1,
  LiquidPenetrantTesting: 2,
  MagneticParticleTesting: 3,
  RadiographicTesting: 4,
  UltrasonicTesting: 5,
} as const;
```

### المشكلة 3: رفع ملف Excel معقد التنسيق

**التحدي:** ملف Excel يحتوي على أعمدة متعددة لكل شخص
```
التنسيق: S/N | Name | VT_Type | VT_Date | PT_Type | PT_Date | MT_Type | MT_Date | ...
```

**✅ الحل:**
```csharp
// تكرار على كل نوع اختبار وإنشاء شهادة منفصلة
var methodColumns = new List<(ServiceMethod method, int typeCol, int dateCol, string code)>
{
    (ServiceMethod.VisualTesting, 2, 3, "VT"),
    (ServiceMethod.LiquidPenetrantTesting, 4, 5, "PT"),
    (ServiceMethod.MagneticParticleTesting, 6, 7, "MT"),
    (ServiceMethod.RadiographicTesting, 8, 9, "RT"),
    (ServiceMethod.UltrasonicTesting, 10, 11, "UT")
};

foreach (var (method, typeCol, dateCol, methodCode) in methodColumns)
{
    // إنشاء رقم تسلسلي فريد: "5070-VT", "5070-PT", إلخ
    var uniqueSerialNumber = $"{serialNumber}-{methodCode}";
    
    // إنشاء شهادة منفصلة لكل نوع اختبار
    var certificate = new Certificate
    {
        SerialNumber = uniqueSerialNumber,
        PersonName = personName,
        ServiceMethod = method,
        // ... باقي الخصائص
    };
}
```

### المشكلة 4: مشاكل CORS

**✅ الحل:**
```csharp
// إعداد CORS صحيح
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

app.UseCors("AllowSpecificOrigins");
```

---

## 🚀 النشر والتشغيل

### إعداد Railway

#### 1. إعداد قاعدة البيانات
```csharp
// Program.cs - اتصال PostgreSQL على Railway
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_PUBLIC_URL");

if (!string.IsNullOrEmpty(databaseUrl) && databaseUrl.StartsWith("postgresql://"))
{
    // تحليل postgresql://user:password@host:port/database
    var uri = new Uri(databaseUrl);
    var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={uri.UserInfo.Split(':')[0]};Password={uri.UserInfo.Split(':')[1]};SSL Mode=Require;Trust Server Certificate=true";
}
```

#### 2. Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY az_backend_new/az_backend_new.csproj ./az_backend_new/
RUN dotnet restore ./az_backend_new/az_backend_new.csproj
COPY az_backend_new/ ./az_backend_new/
RUN dotnet publish ./az_backend_new/az_backend_new.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "az_backend_new.dll"]
```

#### 3. متغيرات البيئة
```bash
DATABASE_PUBLIC_URL=postgresql://user:pass@host:port/db
JWT_SECRET_KEY=your-secret-key-here
JWT_ISSUER=AzCertificates
JWT_AUDIENCE=AzCertificatesUsers
```

### تشغيل محلي

```bash
# استنساخ المشروع
git clone https://github.com/your-repo/az_backend_new.git
cd az_backend_new

# تثبيت التبعيات
dotnet restore

# تشغيل قاعدة البيانات (PostgreSQL محلي)
# تأكد من تحديث connection string في appsettings.json

# تطبيق migrations
dotnet ef database update

# تشغيل التطبيق
dotnet run
```

---

## 🎯 التحضير للمقابلات

### المفاهيم التقنية الأساسية

#### 1. Clean Architecture
**السؤال:** "لماذا استخدمت Clean Architecture؟"
**الإجابة:** "Clean Architecture يوفر فصل واضح للاهتمامات، مما يجعل الكود أكثر قابلية للاختبار والصيانة. كل طبقة لها مسؤولية محددة، والطبقات الداخلية لا تعتمد على الطبقات الخارجية."

#### 2. Repository Pattern
**السؤال:** "ما فائدة Repository Pattern؟"
**الإجابة:** "Repository Pattern يوفر تجريد فوق طبقة الوصول للبيانات، مما يجعل الكود أكثر قابلية للاختبار ويسمح بتغيير مصدر البيانات دون تأثير على منطق العمل."

#### 3. Entity Framework Core
**السؤال:** "كيف تتعامل مع database migrations في الإنتاج؟"
**الإجابة:** "نستخدم `context.Database.MigrateAsync()` في Program.cs لتطبيق migrations تلقائياً عند بدء التطبيق. للإنتاج، يمكن أيضاً استخدام scripts منفصلة."

#### 4. JWT Authentication
**السؤال:** "لماذا اخترت JWT للمصادقة؟"
**الإجابة:** "JWT يوفر مصادقة stateless، مما يجعل التطبيق أكثر قابلية للتوسع. الرموز تحتوي على معلومات المستخدم والصلاحيات، ولا نحتاج لتخزين sessions في الخادم."

#### 5. API Design
**السؤال:** "كيف صممت الـ API؟"
**الإجابة:** "اتبعت مبادئ RESTful design مع استخدام HTTP methods المناسبة (GET, POST, PUT, DELETE) وstatus codes واضحة. استخدمت DTOs لفصل نماذج API عن نماذج قاعدة البيانات."

### أسئلة شائعة وإجاباتها

**س: كيف تضمن consistency البيانات؟**
ج: "استخدم database constraints (unique indexes)، وmodel validation attributes، وbusiness logic validation في repositories. EF Core transactions تضمن atomicity للعمليات المعقدة."

**س: كيف تتعامل مع الأخطاء؟**
ج: "استخدم try-catch blocks مع logging مفصل، وإرجاع HTTP status codes مناسبة مع رسائل خطأ واضحة للمستخدم."

**س: كيف تحسن أداء التطبيق؟**
ج: "استخدم pagination للبيانات الكبيرة، وindexes في قاعدة البيانات، وasync/await للعمليات I/O، وcaching عند الحاجة."

---

## 📚 مصادر إضافية

- [Clean Architecture by Robert Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Repository Pattern في .NET](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [JWT Authentication في ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)

---

## 🤝 المساهمة في المشروع

عند العمل على هذا المشروع:
1. اتبع مبادئ Clean Architecture
2. اكتب unit tests للميزات الجديدة
3. حدث هذه الوثائق للتغييرات المهمة
4. استخدم Git commit messages واضحة
5. اختبر جيداً قبل النشر

---

## 📈 الحالة الحالية للمشروع

### ✅ ما تم إنجازه:
- ✅ إعداد Clean Architecture مع فصل واضح للطبقات
- ✅ تطبيق Repository Pattern لفصل منطق البيانات
- ✅ إنشاء CertificatesController مع جميع العمليات CRUD
- ✅ تطبيق JWT Authentication للأمان
- ✅ إعداد PostgreSQL على Railway مع اتصال آمن
- ✅ تطبيق Excel Import مع معالجة الأعمدة المتعددة
- ✅ إنشاء Serial Numbers فريدة لكل نوع اختبار (5070-VT, 5070-PT, إلخ)
- ✅ مزامنة Enums بين Frontend وBackend
- ✅ إضافة endpoints للتنظيف والإحصائيات
- ✅ تحديث Frontend ليستخدم API الجديد
- ✅ نشر Backend على Railway بنجاح
- ✅ إنشاء وثائق شاملة للمشروع
- ✅ حل مشاكل DateTime مع PostgreSQL
- ✅ إصلاح مشاكل CORS والتزامن

### 🔄 ما يمكن تحسينه مستقبلاً:
- إضافة Unit Tests شاملة
- تطبيق Caching لتحسين الأداء
- إضافة Rate Limiting للحماية
- تحسين Error Handling والرسائل
- إضافة Logging أكثر تفصيلاً
- تطبيق Health Checks للمراقبة
- إضافة API Documentation مع Swagger
- تطبيق Background Jobs للمهام الطويلة

### 🎯 نقاط القوة في المشروع:
1. **العمارة النظيفة**: فصل واضح للمسؤوليات
2. **قابلية الصيانة**: كود منظم وسهل الفهم
3. **الأمان**: JWT authentication مع role-based authorization
4. **المرونة**: Repository pattern يسمح بتغيير مصدر البيانات
5. **التوافق**: مزامنة كاملة بين Frontend وBackend
6. **الوثائق**: شرح شامل لكل جزء في المشروع

### 🚀 كيفية استخدام المشروع:

#### للمطورين الجدد:
1. اقرأ هذا الـ README بالكامل
2. افهم Clean Architecture والطبقات
3. تعلم Repository Pattern وفوائده
4. اختبر الـ API endpoints
5. تدرب على Excel import functionality

#### للمقابلات:
1. اشرح Clean Architecture وفوائدها
2. وضح Repository Pattern والتجريد
3. اشرح كيف تعاملت مع مشاكل DateTime
4. وضح Excel import وتعقيداته
5. اشرح JWT authentication وفوائده

---

*هذا المشروع يمثل تطبيق عملي لأفضل الممارسات في تطوير .NET Core، ويمكن استخدامه كمرجع تقني ومادة للتحضير للمقابلات التقنية.*