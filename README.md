# 🏗️ AZ International - Certificate Management System (Backend API)

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Railway](https://img.shields.io/badge/Railway-Deployed-0B0D0E?style=for-the-badge&logo=railway)](https://railway.app/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)

A comprehensive **NDT (Non-Destructive Testing) Certificate Management System** built with **ASP.NET Core 8.0** and **Clean Architecture**. Manages trainees and their multiple certifications across different testing methods.

## 🌐 Live Demo

| Service | URL |
|---------|-----|
| **API** | https://azbackendnew-production-817b.up.railway.app |
| **Swagger Docs** | https://azbackendnew-production-817b.up.railway.app/swagger |
| **Frontend** | https://azinternational.vercel.app |

---

## ✨ Key Features

### 🎯 Core Functionality
- **Trainee Management** - One trainee can have multiple certificates
- **Multi-Method Certificates** - VT, PT, MT, RT, UT testing methods
- **Excel Bulk Import** - Smart column detection for various Excel formats
- **Advanced Search** - Search by name or serial number
- **Expiry Tracking** - Automatic certificate expiry status

### 🔐 Security
- **JWT Authentication** - Secure token-based auth
- **Role-Based Access** - Admin and User roles
- **Password Hashing** - BCrypt encryption

### 📊 Analytics
- **Statistics Dashboard** - Certificate counts by method/type
- **Expiry Reports** - Track expired vs active certificates

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|------------|
| **Framework** | ASP.NET Core 8.0 |
| **Database** | PostgreSQL 15 |
| **ORM** | Entity Framework Core |
| **Auth** | JWT Bearer Tokens |
| **Docs** | Swagger/OpenAPI |
| **Excel** | ExcelDataReader |
| **Hosting** | Railway |

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   API Layer (Controllers)               │
├─────────────────────────────────────────────────────────┤
│                   Business Layer (Services)             │
├─────────────────────────────────────────────────────────┤
│                   Data Layer (Repositories)             │
├─────────────────────────────────────────────────────────┤
│                   Database (PostgreSQL)                 │
└─────────────────────────────────────────────────────────┘
```

### Project Structure
```
az_backend_new/
├── Controllers/        # API endpoints
│   ├── TraineesController.cs
│   ├── AuthController.cs
│   └── EmailController.cs
├── Models/            # Domain entities
│   ├── Trainee.cs
│   ├── Certificate.cs
│   └── User.cs
├── Repositories/      # Data access
│   ├── TraineeRepository.cs
│   └── UserRepository.cs
├── Services/          # Business logic
│   ├── JwtService.cs
│   └── EmailService.cs
├── DTOs/              # Data transfer objects
├── Data/              # DbContext
└── Migrations/        # EF migrations
```

---

## 📚 API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Auth/login` | User login |
| POST | `/api/Auth/register` | Register new user |

### Trainees & Certificates
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/Trainees` | Get all trainees (paginated) |
| GET | `/api/Trainees/{id}` | Get trainee by ID |
| GET | `/api/Trainees/search` | Search trainees |
| POST | `/api/Trainees` | Create trainee with certificates |
| PUT | `/api/Trainees/{id}` | Update trainee |
| DELETE | `/api/Trainees/{id}` | Delete trainee |
| POST | `/api/Trainees/{id}/certificates` | Add certificate |
| PUT | `/api/Trainees/{id}/certificates/{certId}` | Update certificate |
| DELETE | `/api/Trainees/{id}/certificates/{certId}` | Delete certificate |
| POST | `/api/Trainees/import` | Import from Excel |
| GET | `/api/Trainees/stats` | Get statistics |

---

## 🗄️ Database Schema

### Trainees
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| SerialNumber | varchar(50) | Unique identifier |
| PersonName | varchar(100) | Trainee name |
| Country | varchar(50) | Country (optional) |
| CreatedAt | datetime | Created timestamp |

### Certificates
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| TraineeId | int | Foreign key to Trainee |
| ServiceMethod | int | 1=VT, 2=PT, 3=MT, 4=RT, 5=UT |
| CertificateType | int | 1=Initial, 2=Recertificate |
| ExpiryDate | datetime | Expiry date |

---

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- PostgreSQL 13+

### Local Development
```bash
# Clone repository
git clone https://github.com/hisham1911/AZ_backend_new.git
cd AZ_backend_new

# Restore packages
dotnet restore

# Update connection string in appsettings.json

# Run migrations
dotnet ef database update

# Start server
dotnet run
```

### Default Admin
- **Email**: `admin@azinternational.com`
- **Password**: `Admin123!`

---

## 🐳 Docker Deployment

```bash
docker build -t az-backend .
docker run -p 8080:8080 az-backend
```

---

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

---

<div align="center">

**Built with ❤️ using ASP.NET Core 8.0**

[⬆ Back to Top](#-az-international---certificate-management-system-backend-api)

</div>
