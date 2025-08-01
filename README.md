# WellSync Data Synchronization Application

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Entity Framework](https://img.shields.io/badge/Entity%20Framework-Core-green.svg)](https://docs.microsoft.com/en-us/ef/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-LocalDB-red.svg)](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)

## 🎯 Project Overview

A professional .NET 8 Console Application that synchronizes platform and well data from a REST API to SQL Server LocalDB using Entity Framework Core.

## ✨ Features

- 🔐 **API Authentication** with Bearer Token
- 📊 **Data Synchronization** (Insert/Update based on ID)
- 🗄️ **Entity Framework Core** Code First approach
- 🔄 **Transaction Safety** with rollback capabilities
- 📝 **Comprehensive Logging** for monitoring
- 🛡️ **Error Handling** with fallback strategies

## 🚀 Quick Start

```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/WellSyncApp.git
cd WellSyncApp

# Install EF Core tools
dotnet tool install --global dotnet-ef

# Run database migrations
dotnet ef database update

# Run the application
dotnet run
```

## 🏗️ Architecture

- **Console Application** - Single-purpose sync task
- **Clean Architecture** - Separation of concerns
- **Dependency Injection** - Professional .NET patterns
- **Repository Pattern** - Data access abstraction

## 📊 Database Schema

### Platforms Table
- Id, UniqueName, Latitude, Longitude
- CreatedAt, UpdatedAt, LastSyncedAt

### Wells Table  
- Id, PlatformId (FK), UniqueName, Latitude, Longitude
- CreatedAt, UpdatedAt, LastSyncedAt

## 🔧 Configuration

Update `appsettings.json` with your API details:
```json
{
  "ApiSettings": {
    "BaseUrl": "your-api-url",
    "Username": "your-username", 
    "Password": "your-password"
  }
}
```

## 📈 Assessment Results

This project demonstrates:
- ✅ REST API Integration
- ✅ Database Design & EF Core
- ✅ Error Handling & Logging
- ✅ Clean Code Architecture
- ✅ Professional Documentation

## 🛠️ Technologies Used

- .NET 8
- Entity Framework Core
- SQL Server LocalDB
- System.Text.Json
- Microsoft.Extensions.Logging
- Microsoft.Extensions.DependencyInjection

## 📝 License

This project is part of a technical assessment.