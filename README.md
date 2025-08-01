# WellSync Data Synchronization Application

## Overview

This .NET Core Console Application synchronizes platform and well data from a REST API to SQL Server LocalDB using Entity Framework Core.

## Features

- **API Authentication**: Login with Bearer Token
- **Data Fetching**: GetPlatformWellActual and GetPlatformWellDummy endpoints
- **Database Operations**: Insert/Update operations based on ID matching
- **Dynamic Key Handling**: Gracefully handles missing or extra keys in API responses
- **Transaction Safety**: All operations wrapped in database transactions
- **Comprehensive Logging**: Detailed console output for monitoring

## API Configuration

The application is configured to work with:
- **API Base URL**: `http://test-demo.aemenersol.com`
- **Swagger Documentation**: `http://test-demo.aemenersol.com/index.html`
- **Credentials**: 
  - Username: `user@aemenersol.com`
  - Password: `Test@123`

## Database Schema

### Platforms Table
- Id (Primary Key from API)
- UniqueName
- Latitude, Longitude
- CreatedAt, UpdatedAt
- LastSyncedAt (tracking field)

### Wells Table
- Id (Primary Key from API)
- PlatformId (Foreign Key)
- UniqueName
- Latitude, Longitude
- CreatedAt, UpdatedAt
- LastSyncedAt (tracking field)

## Running the Application

1. **Prerequisites**:
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. **Database Setup**:
   ```bash
   dotnet ef database update
   ```

3. **Run Application**:
   ```bash
   dotnet run
   ```

## Expected Console Output

```
Starting WellSync Data Sync Application...
Database connection verified.
Database before sync: 0 platforms, 0 wells
Starting data synchronization with actual API data...
Login successful. Token acquired.
Fetching platform and well data...
5 platforms processed. 2 inserted, 3 updated.
12 wells processed. 4 inserted, 8 updated.
Sync completed successfully.
Database after sync: 5 platforms, 12 wells
```

## Error Handling

The application includes robust error handling:
- API connectivity issues
- Authentication failures
- Database transaction rollbacks
- JSON parsing errors
- Network timeouts

## Sync Process

1. **Authentication**: Login to API with Bearer Token
2. **Data Fetching**: Get platform/well data from API
3. **Database Transaction**: Begin atomic transaction
4. **Platform Sync**: Insert new or update existing platforms
5. **Well Sync**: Insert new or update existing wells
6. **Commit**: Save all changes atomically
7. **Statistics**: Report sync results

## Configuration

All settings are in `appsettings.json`:
- Database connection string
- API base URL and credentials
- Endpoint configurations
- Logging levels

The application is ready for production use with the provided API endpoints.