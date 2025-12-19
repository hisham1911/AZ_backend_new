# 🏗️ AZ Certificates Management System - Backend API

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-13+-blue.svg)](https://www.postgresql.org/)
[![Railway](https://img.shields.io/badge/Deployed%20on-Railway-blueviolet.svg)](https://railway.app/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A comprehensive certificate management system for AZ International, specializing in Non-Destructive Testing (NDT) certifications. Built with ASP.NET Core 8.0 and Clean Architecture principles.

## 🚀 Live Demo

- **API Base URL**: https://azbackendnew-production-817b.up.railway.app
- **API Documentation**: Available via Swagger UI at `/swagger`

## 📋 Table of Contents

- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [Architecture](#-architecture)
- [Getting Started](#-getting-started)
- [API Endpoints](#-api-endpoints)
- [Database Schema](#-database-schema)
- [Deployment](#-deployment)
- [Contributing](#-contributing)

## ✨ Features

### Core Functionality
- **Certificate Management**: Full CRUD operations for NDT certificates
- **Multi-Method Support**: Visual Testing (VT), Liquid Penetrant (PT), Magnetic Particle (MT), Radiographic (RT), Ultrasonic (UT)
- **Advanced Search**: Search by person name or serial number
- **Excel Import**: Bulk import certificates from Excel files with complex multi-column format
- **User Authentication**: JWT-based authentication with role-based authorization
- **Email Notifications**: Automated email system for certificate updates

### Advanced Features
- **Pagination**: Efficient data loading with configurable page sizes
- **Data Validation**: Comprehensive input validation and error handling
- **Audit Trail**: Created/Updated timestamps for all records
- **Expiry Tracking**: Automatic expiry status calculation
- **Statistics Dashboard**: Certificate analytics and reporting
- **Data Cleanup**: Tools for managing legacy data formats

## 🛠 Tech Stack

### Backend
- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL 13+
- **ORM**: Entity Framework Core
- **Authentication**: JWT Tokens
- **Documentation**: Swagger/OpenAPI
- **File Processing**: ExcelDataReader

### Infrastructure
- **Hosting**: Railway
- **Database Hosting**: Railway PostgreSQL
- **Containerization**: Docker
- **CI/CD**: GitHub Actions (via Railway)

### Architecture Patterns
- **Clean Architecture**: Separation of concerns across layers
- **Repository Pattern**: Data access abstraction
- **Dependency Injection**: Built-in ASP.NET Core DI container
- **DTO Pattern**: Data transfer objects for API contracts

## 🏗 Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  ┌─────────────────┐  ┌─────────────────┐                  │
│  │   Controllers   │  │      DTOs       │                  │
│  │   (API Layer)   │  │ (Data Transfer) │                  │
│  └─────────────────┘  └─────────────────┘                  │
├─────────────────────────────────────────────────────────────┤
│                    Business Layer                           │
│  ┌─────────────────┐  ┌─────────────────┐                  │
│  │    Services     │  │   Repositories  │                  │
│  │ (Business Logic)│  │ (Data Access)   │                  │
│  └─────────────────┘  └─────────────────┘                  │
├─────────────────────────────────────────────────────────────┤
│                    Data Layer                               │
│  ┌─────────────────┐  ┌─────────────────┐                  │
│  │     Models      │  │    DbContext    │                  │
│  │ (Domain Models) │  │ (EF Core)       │                  │
│  └─────────────────┘  └─────────────────┘                  │
└─────────────────────────────────────────────────────────────┘
```

### Project Structure

```
az_backend_new/
├── Controllers/          # API Controllers
│   ├── CertificatesController.cs
│   ├── AuthController.cs
│   └── EmailController.cs
├── Models/              # Domain Models
│   ├── Certificate.cs
│   ├── User.cs
│   ├── ServiceMethod.cs
│   └── CertificateType.cs
├── Repositories/        # Data Access Layer
│   ├── CertificateRepository.cs
│   └── UserRepository.cs
├── Services/           # Business Logic
│   ├── JwtService.cs
│   └── EmailService.cs
├── DTOs/              # Data Transfer Objects
│   ├── CertificateDto.cs
│   ├── AuthDto.cs
│   └── EmailDto.cs
├── Data/              # Database Context
│   └── AzDbContext.cs
└── Migrations/        # EF Core Migrations
    └── InitialCreate.cs
```

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 13+](https://www.postgresql.org/download/)
- [Git](https://git-scm.com/)

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/hisham1911/AZ_backend_new.git
   cd AZ_backend_new
   ```

2. **Install dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure database connection**
   
   Update `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=az_certificates;Username=your_username;Password=your_password"
     }
   }
   ```

4. **Run database migrations**
   ```bash
   dotnet ef database update
   ```

5. **Start the application**
   ```bash
   dotnet run
   ```

6. **Access the API**
   - API: `http://localhost:5167`
   - Swagger UI: `http://localhost:5167/swagger`

### Default Admin Account

- **Email**: `admin@azinternational.com`
- **Password**: `Admin123!`

## 📚 API Endpoints

### Authentication
```http
POST /api/Auth/login          # User login
POST /api/Auth/register       # User registration (Admin only)
```

### Certificates
```http
GET    /api/Certificates                    # Get all certificates (paginated)
GET    /api/Certificates/{id}               # Get certificate by ID
POST   /api/Certificates                    # Create new certificate
PUT    /api/Certificates/{id}               # Update certificate
DELETE /api/Certificates/{id}               # Delete certificate
GET    /api/Certificates/search             # Search certificates
POST   /api/Certificates/import             # Import from Excel
GET    /api/Certificates/stats              # Get statistics
POST   /api/Certificates/cleanup-old-format # Clean legacy data
```

### Email
```http
POST /api/Email/SendEmail     # Send email notification
```

### Example API Usage

**Search Certificates by Name:**
```bash
curl -X GET "https://azbackendnew-production-817b.up.railway.app/api/Certificates/search?personName=john" \
  -H "Accept: application/json"
```

**Get Certificate Statistics:**
```bash
curl -X GET "https://azbackendnew-production-817b.up.railway.app/api/Certificates/stats" \
  -H "Accept: application/json"
```

## 🗄 Database Schema

### Certificates Table
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| SerialNumber | varchar(50) | Unique identifier (e.g., "5070-VT") |
| PersonName | varchar(100) | Certificate holder name |
| ServiceMethod | int | Testing method (1=VT, 2=PT, 3=MT, 4=RT, 5=UT) |
| CertificateType | int | Certificate type (1=Initial, 2=Recertificate) |
| ExpiryDate | datetime | Certificate expiry date |
| Country | varchar(50) | Location (optional) |
| State | varchar(50) | State/Province (optional) |
| StreetAddress | varchar(200) | Street address (optional) |
| CreatedAt | datetime | Record creation timestamp |
| UpdatedAt | datetime | Last update timestamp |

### Users Table
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| Email | varchar(100) | Unique email address |
| PasswordHash | text | BCrypt hashed password |
| Role | int | User role (1=User, 2=Admin) |
| CreatedAt | datetime | Account creation timestamp |
| UpdatedAt | datetime | Last update timestamp |

## 🚀 Deployment

### Railway Deployment

The application is configured for automatic deployment on Railway:

1. **Environment Variables**
   ```bash
   DATABASE_PUBLIC_URL=postgresql://user:pass@host:port/db
   JWT_SECRET_KEY=your-secret-key-here
   JWT_ISSUER=AzCertificates
   JWT_AUDIENCE=AzCertificatesUsers
   ```

2. **Docker Configuration**
   
   The project includes a `Dockerfile` for containerized deployment:
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
   # ... build steps
   FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
   # ... runtime configuration
   ```

3. **Automatic Migrations**
   
   Database migrations run automatically on startup in production.

### Manual Deployment

For other hosting providers:

1. Build the application:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. Configure environment variables
3. Deploy the `./publish` folder to your hosting provider

## 🧪 Testing

### Running Tests
```bash
dotnet test
```

### API Testing
Use the included Swagger UI or tools like Postman to test API endpoints.

## 🔧 Configuration

### Key Configuration Options

**JWT Settings:**
```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "Issuer": "AzCertificates",
    "Audience": "AzCertificatesUsers",
    "ExpiryHours": 24
  }
}
```

**CORS Settings:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

## 🤝 Contributing

We welcome contributions! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow Clean Architecture principles
- Write unit tests for new features
- Update documentation for API changes
- Use meaningful commit messages
- Ensure code passes all existing tests

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built for AZ International NDT Services
- Inspired by Clean Architecture principles
- Uses Railway for reliable hosting

## 📞 Support

For support and questions:

- Create an issue in this repository
- Contact: [Your Contact Information]

---

**Built with ❤️ using ASP.NET Core and Clean Architecture**