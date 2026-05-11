# 📦 FileStorage Microservice

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=.net)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](LICENSE.md)
[![Tests](https://img.shields.io/badge/tests-passing-success)](tests/)
[![Coverage](https://img.shields.io/badge/coverage-80%25-green)]()
[![Docker](https://img.shields.io/badge/docker-ready-2496ED?logo=docker)](Dockerfile)

A production-ready microservice for managing file uploads, downloads, and storage operations built with .NET 9. Implements Clean Architecture, DDD, and CQRS patterns with support for multiple cloud storage providers (Azure Blob, Azure File, Local Storage).

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Key Features](#-key-features)
- [Technology Stack](#️-technology-stack)
- [Prerequisites](#️-prerequisites)
- [Getting Started](#-getting-started)
- [API Endpoints](#-api-endpoints)
- [Configuration](#️-configuration)
- [Use Cases & Scenarios](#-use-cases--scenarios)
- [Architecture](#️-architecture)
- [Testing](#-testing)
- [Best Practices](#-best-practices)
- [Troubleshooting](#-troubleshooting)
- [Metrics & Monitoring](#-metrics--monitoring)
- [Roadmap](#️-roadmap)
- [FAQ](#-faq)
- [Contributing](#-contributing)
- [License](#-license)

---

## 🎯 Overview

The FileStorage microservice provides a unified API for file management operations across different storage providers. It abstracts the complexity of cloud storage interactions, offering features like:

- **Multi-provider support**: Azure Blob Storage, Azure File Storage, Local File System
- **Secure file operations**: Upload, download, and delete with authentication
- **Temporary signed URLs**: Generate time-limited URLs for direct cloud access
- **File metadata management**: Track file details, versions, and custom metadata
- **Multi-file aggregates**: Group multiple files under a single identifier
- **Renowned files**: Mark files as public/special with custom handling
- **Storage transparency**: Clients only persist aggregate IDs, never cloud URLs

### 🚀 Quick Start

```bash
# 1. Start infrastructure services
git clone https://github.com/codedesignplus/CodeDesignPlus.Environment.Dev
cd CodeDesignPlus.Environment.Dev/resources
docker-compose up -d

# 2. Configure Vault secrets
cd ../../tools/vault
./config-vault.sh

# 3. Run the microservice
dotnet run --project src/entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.Rest

# 4. Access Swagger UI
open http://localhost:5000/swagger
```

### 📊 High-Level Architecture

```
┌─────────────┐
│   Client    │
│ Application │
└──────┬──────┘
       │ HTTPS + JWT
       │
┌──────▼──────────────────────────────────────────────┐
│          FileStorage Microservice (REST API)        │
│  ┌──────────────┐  ┌─────────────┐  ┌────────────┐ │
│  │ Controllers  │  │  MediatR    │  │  Handlers  │ │
│  │   (API)      │─▶│   (CQRS)    │─▶│ (Business) │ │
│  └──────────────┘  └─────────────┘  └────────────┘ │
└───────┬──────────────────┬──────────────────┬───────┘
        │                  │                  │
        │                  │                  │
   ┌────▼────┐      ┌──────▼──────┐    ┌─────▼─────┐
   │ MongoDB │      │Azure Storage│    │ RabbitMQ  │
   │(Metadata)      │(Files/Blobs)│    │ (Events)  │
   └─────────┘      └─────────────┘    └───────────┘
```

## 🚀 Key Features

### Core Capabilities

- ✅ **Multiple Storage Providers**: Seamlessly switch between Azure Blob, Azure File, or Local storage
- ✅ **Secure Upload/Download**: Stream files through API proxy with authentication
- ✅ **Signed URL Generation**: Create temporary URLs (5-60 min) for direct cloud access
- ✅ **Multi-file Aggregates**: Upload multiple files to the same aggregate ID
- ✅ **File Versioning**: Track file versions and metadata
- ✅ **Content-Type Detection**: Automatic MIME type detection and handling
- ✅ **View in Browser**: Support for inline file viewing vs. download
- ✅ **Pagination & Filtering**: OData-style queries for file listing
- ✅ **Problem Details**: RFC 7807 compliant error responses

### Technical Features

- Clean Architecture with DDD and CQRS
- Domain events for file operations
- MongoDB for aggregate persistence
- RabbitMQ for event publishing
- Redis for distributed caching
- OAuth2/OpenID Connect security
- Swagger/OpenAPI documentation
- Multi-tenancy support
- Docker containerization
- Comprehensive test coverage (Unit, Integration, E2E)

## 🛠️ Technology Stack

### Core
- **.NET 9** - Runtime and framework
- **ASP.NET Core** - Web API framework
- **C# 13** - Programming language

### Storage & Data
- **MongoDB** - Aggregate persistence and queries
- **Azure Blob Storage** - Cloud file storage (primary)
- **Azure File Storage** - Cloud file storage (alternative)
- **Local File System** - Development/testing storage

### Messaging & Caching
- **RabbitMQ** - Event publishing and message broker
- **Redis** - Distributed caching and session storage

### Architecture & Patterns
- **MediatR** - CQRS command/query handling
- **FluentValidation** - Input validation
- **Mapster** - Object mapping
- **NodaTime** - Date/time handling

### Security & Configuration
- **Vault** - Secret management
- **OAuth2/OpenID Connect** - Authentication
- **JWT Bearer** - Token-based security

### DevOps & Testing
- **Docker** - Containerization
- **Playwright** - E2E testing
- **xUnit** - Unit/integration testing
- **k6** - Load testing
- **Swagger/OpenAPI** - API documentation

## ⚙️ Prerequisites

### Required
- **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker & Docker Compose** - For infrastructure services
- **MongoDB 6.0+** - Document database
- **Redis 7.0+** - Caching layer
- **RabbitMQ 3.12+** - Message broker

### Optional
- **Vault** - Secret management (can use appsettings for local dev)
- **Azure Storage Account** - For cloud storage providers
- **Node.js 18+** - For Playwright E2E tests

## 🚀 Getting Started

The following instructions will help you set up the project on your local machine for development and testing purposes.

1. Clone the repository:
```bash
git clone <repository-url>
```

2. Run the MongoDB, Redis, and RabbitMQ services using Docker Compose. Clone this repository [CodeDesignPlus.Environment.Dev](https://github.com/codedesignplus/CodeDesignPlus.Environment.Dev) and run the following command:

```bash
cd resources

docker-compose up -d
```

3. Run the script to config the vault:

```bash
cd tools/vault

./config-vault.sh
```

4. Build the solution:
```bash
dotnet build
```

5. Run the desired entry point:
   
   - For REST API:
      ```bash
      dotnet run --project src/entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.Rest
      ```

   - For gRPC:
      ```bash
      dotnet run --project src/entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.gRpc
      ```

   - For Worker:
      ```bash
      dotnet run --project src/entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.AsyncWorker
      ```

## 📡 API Endpoints

### File Management

#### Upload File
```http
POST /api/filestorage/upload
Content-Type: multipart/form-data
Authorization: Bearer {token}
X-Tenant: {tenant-id}

Form fields (must be in this order):
- Id: {guid}              # Aggregate ID (use same ID for multiple files)
- Target: {string}        # Target directory/container
- Renowned: {boolean}     # Mark as public/special file
- file: {binary}          # File content (must be last)
```

**Response**: `200 OK` with upload confirmation

**Example** (using curl):
```bash
curl -X POST "http://localhost:5000/api/v1/filestorage/upload" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "X-Tenant: 9588813a-7bc0-4be4-a169-293061881cc3" \
  -F "Id=550e8400-e29b-41d4-a716-446655440000" \
  -F "Target=documents" \
  -F "Renowned=false" \
  -F "file=@/path/to/document.pdf"
```

#### Get FileStorage by ID
```http
GET /api/filestorage/{id}
Authorization: Bearer {token}
X-Tenant: {tenant-id}
```

**Response**: `200 OK` with FileStorage aggregate
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "files": [
    {
      "success": true,
      "message": "File uploaded successfully",
      "fileDetail": {
        "extension": ".pdf",
        "fullName": "document.pdf",
        "name": "document",
        "metadata": {
          "file": "document.pdf",
          "target": "documents",
          "uri": "https://storage.blob.core.windows.net/...",
          "uriDownload": "http://localhost:5000/api/v1/filestorage/download/1?file=document.pdf&target=documents",
          "uriViewInBrowser": "http://localhost:5000/api/v1/filestorage/download/1?viewInBrowser=true&file=document.pdf&target=documents",
          "provider": "AzureBlobProvider"
        },
        "size": 1048576,
        "version": "1.0.0",
        "renowned": false,
        "mime": {
          "type": "application/pdf",
          "extension": ".pdf"
        }
      },
      "provider": "AzureBlobProvider"
    }
  ],
  "isActive": true
}
```

#### List FileStorages (Paginated)
```http
GET /api/filestorage?limit=50&skip=0&filter=isActive eq true&orderby=createdAt desc
Authorization: Bearer {token}
X-Tenant: {tenant-id}
```

**Query Parameters**:
- `limit` (optional): Number of items per page (default: 100)
- `skip` (optional): Number of items to skip (default: 0)
- `filter` (optional): OData filter expression
- `orderby` (optional): OData order expression

**Response**: `200 OK` with paginated results
```json
{
  "data": [...],
  "totalCount": 250,
  "limit": 50,
  "skip": 0
}
```

#### Download File (via API Proxy)
```http
GET /api/filestorage/download/{id}?file={filename}&target={target}&viewInBrowser=false
Authorization: Bearer {token}
X-Tenant: {tenant-id}
```

**Query Parameters**:
- `file` (required): File name
- `target` (required): Target directory
- `viewInBrowser` (optional): `true` for inline, `false` for download (default: `false`)

**Response**: `200 OK` with file stream
- Content-Type: Detected MIME type
- Content-Disposition: `attachment` or `inline`

**Example**:
```bash
curl -X GET "http://localhost:5000/api/v1/filestorage/download/550e8400-e29b-41d4-a716-446655440000?file=document.pdf&target=documents" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "X-Tenant: 9588813a-7bc0-4be4-a169-293061881cc3" \
  -o downloaded-document.pdf
```

#### Generate Signed URL
```http
GET /api/filestorage/signed-url/{id}?expirationMinutes=5
Authorization: Bearer {token}
X-Tenant: {tenant-id}
```

**Query Parameters**:
- `expirationMinutes` (optional): Expiration time in minutes (default: 5, max: 60)

**Response**: `200 OK` with signed URLs in metadata
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "files": [
    {
      "fileDetail": {
        "metadata": {
          "signedUrl": "https://storage.blob.core.windows.net/container/file?sv=2021-06-08&se=2026-05-11T17%3A00%3A00Z&sr=b&sp=r&sig=SIGNATURE",
          "signedUrlExpiration": "2026-05-11T17:00:00Z",
          ...
        }
      }
    }
  ]
}
```

**Use the signed URL** (no authentication required):
```bash
curl -X GET "https://storage.blob.core.windows.net/container/file?sv=2021-06-08&se=2026-05-11T17%3A00%3A00Z&sr=b&sp=r&sig=SIGNATURE" \
  -o direct-download.pdf
```

### Error Responses

All errors follow RFC 7807 Problem Details format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "The file storage does not exist.",
  "extensions": {
    "layer": "Application",
    "error_code": "202",
    "traceId": "0HMVJ3K7S5Q2K:00000001"
  }
}
```

**Common Status Codes**:
- `200 OK` - Success
- `400 Bad Request` - Invalid input or business rule violation
- `401 Unauthorized` - Missing or invalid token
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

## 🧪 Testing

### Unit & Integration Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/unit/CodeDesignPlus.Net.Microservice.FileStorage.Rest.Test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=./coverage
```

### E2E Tests (Playwright)
```bash
cd ms-filestorage-playwright

# Install dependencies
npm install

# Run all tests
npm test

# Run smoke tests only
npm test -- --grep "@smoke"

# Run E2E tests only
npm test -- --grep "@e2e"

# Run with UI
npm test -- --ui
```

### Load Tests (k6)
```bash
cd tests/load

# Run load test
k6 run load-rest.js

# Run with custom parameters
k6 run --vus 100 --duration 5m load-rest.js
```

## 📦 Update Packages
To update the NuGet packages, run the following script:

```bash
cd tools/update-packages

./update-packages.sh
```

## 📦 Upgrading .NET Version

To upgrade the .NET version, run the following script:

```bash
cd tools/upgrade-dotnet

./upgrade-dotnet.sh
```

## 🧪 SonarQube Analysis

To run the SonarQube analysis, follow the instructions in the sonarqube directory.

1. Replace the SonarQube URL and token in the sonarqube.sh script to analyze with SonarQube locally.
2. Run the script:

   ```bash
   cd tools/sonarqube

   ./sonarqube.sh
   ```

🐳 Docker Support

To build and run the application using Docker, follow these steps:

1. Build the Docker image using the Dockerfile in the REST API entry point:
   ```bash
   docker build -t ms-filestorage-rest . -f src/entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.Rest/Dockerfile

   docker run -d -p 5000:5000 --network=backend -e ASPNETCORE_ENVIRONMENT=Docker --name ms-filestorage-rest ms-filestorage-rest
   ```

2. Build the Docker image using the Dockerfile in the gRPC entry point:
   ```bash
   docker build -t ms-filestorage-grpc . -f src/entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.gRpc/Dockerfile

   docker run -d -p 5001:5001 --network=backend -e ASPNETCORE_ENVIRONMENT=Docker --name ms-filestorage-grpc ms-filestorage-grpc
   ```

3. Build the Docker image using the Dockerfile in the Worker entry point:
   ```bash
   docker build -t ms-filestorage-worker . -f src/entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.AsyncWorker/Dockerfile

   docker run -d -p 5002:5002 --network=backend -e ASPNETCORE_ENVIRONMENT=Docker --name ms-filestorage-worker ms-filestorage-worker
   ```

## ⚙️ Configuration

### Storage Provider Setup

The microservice supports multiple storage providers configured in `appsettings.json`:

#### Azure Blob Storage (Recommended for Production)
```json
{
  "FileStorage": {
    "AzureBlob": {
      "Enable": true,
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;",
      "Container": "filestorage",
      "UriDownload": "http://localhost:5000/api/v1/filestorage/download"
    },
    "AzureFile": {
      "Enable": false
    },
    "Local": {
      "Enable": false
    }
  }
}
```

#### Azure File Storage
```json
{
  "FileStorage": {
    "AzureFile": {
      "Enable": true,
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;",
      "Share": "filestorage",
      "UriDownload": "http://localhost:5000/api/v1/filestorage/download"
    }
  }
}
```

#### Local Storage (Development Only)
```json
{
  "FileStorage": {
    "Local": {
      "Enable": true,
      "Path": "C:/temp/filestorage",
      "UriDownload": "http://localhost:5000/api/v1/filestorage/download"
    }
  }
}
```

### Security Configuration

```json
{
  "Security": {
    "Authority": "https://your-identity-server.com",
    "Audience": "filestorage-api",
    "RequireHttpsMetadata": true,
    "ValidateIssuer": true,
    "ValidateAudience": true
  }
}
```

### Multi-tenancy

The microservice supports multi-tenancy through the `X-Tenant` header. Each request must include a tenant ID:

```http
X-Tenant: 9588813a-7bc0-4be4-a169-293061881cc3
```

Files are isolated by tenant at the repository level.

### Environment Variables

Key environment variables for Docker deployment:

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
MONGO_CONNECTION_STRING=mongodb://mongo:27017
REDIS_CONNECTION_STRING=redis:6379
RABBITMQ_HOST=rabbitmq
VAULT_ADDRESS=http://vault:8200
VAULT_TOKEN=your-vault-token
```

## 🎯 Use Cases & Scenarios

### 1. Document Management System
Upload user documents, generate signed URLs for temporary access:

```bash
# User uploads their ID document
POST /api/filestorage/upload
- Id: user-123-documents
- Target: identity-docs
- file: passport.pdf

# System retrieves document for verification
GET /api/filestorage/signed-url/user-123-documents?expirationMinutes=10

# Verifier accesses document directly from signed URL (no auth needed)
GET https://storage.blob.core.windows.net/...?sig=...
```

### 2. Multi-file Invoice System
Store invoice PDF + attachments under single aggregate:

```bash
# Upload invoice PDF
POST /api/filestorage/upload
- Id: invoice-2024-001
- Target: invoices/2024
- file: invoice.pdf

# Upload supporting documents to same aggregate
POST /api/filestorage/upload
- Id: invoice-2024-001
- Target: invoices/2024
- file: receipt.jpg

POST /api/filestorage/upload
- Id: invoice-2024-001
- Target: invoices/2024
- file: contract.pdf

# Retrieve all files for invoice
GET /api/filestorage/invoice-2024-001
# Returns aggregate with 3 files
```

### 3. Public Asset Management
Store and serve public files (logos, images):

```bash
# Upload company logo as renowned file
POST /api/filestorage/upload
- Id: company-assets
- Target: public/logos
- Renowned: true
- file: logo.png

# Generate long-lived signed URL for CDN
GET /api/filestorage/signed-url/company-assets?expirationMinutes=60

# Use signed URL in public website (cached by CDN)
<img src="https://storage.blob.core.windows.net/...?sig=..." alt="Logo" />
```

### 4. Secure File Sharing
Share files with time-limited access:

```bash
# User uploads file
POST /api/filestorage/upload
- Id: share-xyz789
- Target: shared-files
- file: presentation.pptx

# Generate 30-minute signed URL for recipient
GET /api/filestorage/signed-url/share-xyz789?expirationMinutes=30

# Send signed URL to recipient via email
# URL expires automatically after 30 minutes
```

### 5. Report Generation & Download
Generate reports and serve through proxy:

```bash
# System generates report and uploads
POST /api/filestorage/upload
- Id: report-monthly-2024-05
- Target: reports/monthly
- file: sales-report.xlsx

# User downloads report through authenticated proxy
GET /api/filestorage/download/report-monthly-2024-05?file=sales-report.xlsx&target=reports/monthly
# No cloud URLs exposed to client
```

## 🏗️ Architecture

### Clean Architecture Layers

```
src/
├── domain/                          # Domain Layer
│   ├── Domain/                      # Aggregates, Entities, Value Objects
│   │   ├── FileStorageAggregate.cs # Main aggregate root
│   │   ├── ValueObjects/           # File, FileDetail, Metadata
│   │   ├── DomainEvents/           # FileStorageCreated, FileStorageAdded
│   │   └── Repositories/           # IFileStorageRepository
│   ├── Application/                 # Application Layer
│   │   ├── Commands/               # CreateFileStorage
│   │   ├── Queries/                # GetById, GetAll, Download, SignedUrl
│   │   ├── DTOs/                   # FileStorageDto
│   │   └── Validators/             # FluentValidation rules
│   └── Infrastructure/              # Infrastructure Layer
│       └── Repositories/           # MongoDB implementation
└── entrypoints/                     # Presentation Layer
    └── Rest/                        # REST API
        ├── Controllers/            # FileStorageController
        └── Program.cs              # Startup configuration
```

### CQRS Pattern

**Commands** (Write operations):
- `CreateFileStorageCommand` - Upload files

**Queries** (Read operations):
- `GetFileStorageByIdQuery` - Get aggregate by ID
- `GetAllFileStorageQuery` - List with pagination
- `DownloadQuery` - Download file
- `GetFileStorageWithSignedUrlsQuery` - Generate signed URLs

### Domain Events

Published to RabbitMQ after successful operations:
- `FileStorageCreatedDomainEvent` - New aggregate created
- `FileStorageAddedDomainEvent` - File added to existing aggregate
- `FileStorageDeletedDomainEvent` - Aggregate soft-deleted

### Data Flow

```
Client Request
     ↓
[Controller] → Validates & Routes
     ↓
[MediatR] → Dispatches Command/Query
     ↓
[Handler] → Business Logic
     ↓
[Repository] → Persists to MongoDB
     ↓
[IFileStorage] → Cloud Storage SDK
     ↓
[IPubSub] → Publishes Events to RabbitMQ
     ↓
Response to Client
```

## 📚 Documentation

### API Documentation
- **Swagger UI**: Available at `http://localhost:5000/swagger`
- **OpenAPI Spec**: Available at `http://localhost:5000/swagger/v1/swagger.json`

### Additional Resources
- **CLAUDE.md**: AI assistant guidance for working with this codebase
- **Project Documentation**: See `docs/` directory (if available)
- **CodeDesignPlus SDK**: [Documentation Site](https://codedesignplus.github.io/)

## 💡 Best Practices

### Client Integration

#### ✅ DO: Store Aggregate IDs
```csharp
// Good: Store only the aggregate ID
public class Invoice
{
    public Guid Id { get; set; }
    public Guid FileStorageId { get; set; }  // Store this
    // Don't store: CloudUrl, SignedUrl, etc.
}
```

#### ✅ DO: Use Signed URLs for Direct Access
```csharp
// Generate signed URL for temporary direct access
var signedUrlResponse = await httpClient.GetAsync(
    $"/api/filestorage/signed-url/{fileStorageId}?expirationMinutes=15"
);

// Use the signed URL directly (no auth needed)
var directDownload = await httpClient.GetAsync(signedUrlResponse.SignedUrl);
```

#### ✅ DO: Handle Multi-part Upload Correctly
```csharp
using var form = new MultipartFormDataContent();

// IMPORTANT: Fields must be in this order
form.Add(new StringContent(fileId.ToString()), "Id");
form.Add(new StringContent("documents"), "Target");
form.Add(new StringContent("false"), "Renowned");
form.Add(new StreamContent(fileStream), "file", fileName);  // File MUST be last

var response = await httpClient.PostAsync("/api/filestorage/upload", form);
```

#### ❌ DON'T: Store Cloud URLs
```csharp
// Bad: URLs change when moving providers
public class Document
{
    public string AzureBlobUrl { get; set; }  // ❌ Don't do this
    public string SignedUrl { get; set; }      // ❌ Expires quickly
}
```

#### ❌ DON'T: Skip Authentication Headers
```csharp
// Bad: Missing required headers
var request = new HttpRequestMessage(HttpMethod.Get, "/api/filestorage/...");
// Missing: Authorization header
// Missing: X-Tenant header
```

### Performance Optimization

#### Use Signed URLs for Large Files
For files > 10MB, use signed URLs to avoid proxy overhead:

```csharp
// Step 1: Get signed URL from API
var signedUrl = await GetSignedUrlAsync(fileStorageId);

// Step 2: Download directly from cloud storage
var fileBytes = await httpClient.GetByteArrayAsync(signedUrl);
```

#### Batch Operations
Upload multiple files to the same aggregate to reduce round trips:

```csharp
foreach (var file in files)
{
    // All use same aggregate ID
    await UploadAsync(aggregateId, file);
}

// Single query returns all files
var allFiles = await GetByIdAsync(aggregateId);
```

### Security Considerations

1. **Always validate file types** before upload
2. **Scan for malware** after upload (external service)
3. **Use short expiration times** for signed URLs (5-15 minutes)
4. **Rotate storage account keys** regularly
5. **Enable HTTPS only** in production
6. **Implement rate limiting** on upload endpoints

## 🐛 Troubleshooting

### Common Issues

#### Issue: "The process cannot access the file because it is being used by another process"
**Cause**: Service is running while trying to rebuild.

**Solution**:
```bash
# Stop the service
taskkill //F //IM "CodeDesignPlus.Net.Microservice.FileStorage.Rest.exe"

# Or find and kill the process
netstat -ano | findstr :5000
taskkill //PID <process_id> //F

# Rebuild
dotnet build
```

#### Issue: Upload returns 500 error when adding second file to aggregate
**Cause**: Fixed in latest version. Handler now uses `UpdateAsync` for existing aggregates.

**Solution**: Update to latest version or ensure you're using the corrected `CreateFileStorageCommandHandler`.

#### Issue: Signed URLs return 403 Forbidden
**Cause**: Incorrect storage account permissions or expired SAS token.

**Solution**:
```bash
# Check Azure Storage Account permissions
az storage account show --name <account-name> --query primaryEndpoints

# Verify connection string in appsettings.json
# Ensure account key is valid and not rotated
```

#### Issue: MongoDB connection timeout
**Cause**: MongoDB not accessible or wrong connection string.

**Solution**:
```bash
# Test MongoDB connectivity
mongosh "mongodb://localhost:27017"

# Check Docker containers
docker ps | grep mongo

# Verify connection string in appsettings.json
"Mongo": {
  "ConnectionString": "mongodb://localhost:27017"
}
```

#### Issue: Files not appearing in Azure Storage Explorer
**Cause**: Files uploaded to wrong container or storage account.

**Solution**:
1. Verify container name in `appsettings.json` matches Azure
2. Check `FileStorage.AzureBlob.Container` configuration
3. Ensure storage account connection string is correct

### Debug Mode

Enable detailed logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "CodeDesignPlus": "Trace",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Health Checks

Check service health:
```bash
curl http://localhost:5000/health
```

Expected response:
```json
{
  "status": "Healthy",
  "checks": [
    { "name": "MongoDB", "status": "Healthy" },
    { "name": "Redis", "status": "Healthy" },
    { "name": "RabbitMQ", "status": "Healthy" }
  ]
}
```

## 🤝 Contributing

We welcome contributions! Please follow these guidelines:

### Development Workflow

1. **Fork the repository**
2. **Create a feature branch**
   ```bash
   git checkout -b feature/amazing-feature
   ```

3. **Make your changes**
   - Follow existing code style
   - Add tests for new features
   - Update documentation

4. **Run tests**
   ```bash
   dotnet test
   cd ms-filestorage-playwright && npm test
   ```

5. **Commit with conventional commits**
   ```bash
   git commit -m "feat: add support for AWS S3 provider"
   git commit -m "fix: resolve upload timeout issue"
   git commit -m "docs: update API examples"
   ```

6. **Push and create Pull Request**
   ```bash
   git push origin feature/amazing-feature
   ```

### Code Standards

- **C# Coding Style**: Follow .editorconfig rules
- **Test Coverage**: Aim for >80% coverage
- **Documentation**: Update README.md and CLAUDE.md
- **Naming Conventions**:
  - Commands: `{Action}Command` (e.g., `CreateFileStorageCommand`)
  - Queries: `{Action}Query` (e.g., `GetFileStorageByIdQuery`)
  - Handlers: `{CommandOrQuery}Handler`
  - Tests: `{MethodName}_{Scenario}_{ExpectedResult}`

### Pull Request Checklist

- [ ] Code compiles without warnings
- [ ] All tests pass (unit, integration, E2E)
- [ ] New features have tests
- [ ] Documentation updated
- [ ] CHANGELOG.md updated (if applicable)
- [ ] No breaking changes (or documented with migration guide)
- [ ] Follows SOLID principles and Clean Architecture

## 📊 Metrics & Monitoring

### Key Performance Indicators

Monitor these metrics in production:

- **Upload Success Rate**: Target > 99.5%
- **Download Response Time**: Target < 500ms (proxy), < 200ms (signed URL)
- **Signed URL Generation Time**: Target < 100ms
- **Storage Provider Availability**: Target > 99.9%
- **Event Publishing Rate**: Monitor for backpressure

### Application Insights

The service includes built-in telemetry:

```csharp
// Automatic tracking
- HTTP request duration
- Dependency calls (MongoDB, Redis, Azure Storage)
- Exception logging
- Custom events for file operations
```

### Example Queries

```kusto
// Failed uploads in last hour
requests
| where timestamp > ago(1h)
| where url contains "/upload"
| where success == false
| summarize count() by resultCode

// Average download time by file size
dependencies
| where name contains "AzureBlob"
| where operation_Name == "Download"
| summarize avg(duration), max(duration) by bin(customDimensions.fileSize, 1MB)
```

## 🗺️ Roadmap

### Planned Features

- [ ] **AWS S3 Provider** - Support for Amazon S3 storage
- [ ] **Google Cloud Storage Provider** - Support for GCS
- [ ] **File Compression** - Automatic compression for supported types
- [ ] **Image Thumbnails** - Generate thumbnails for images
- [ ] **Virus Scanning Integration** - ClamAV or similar
- [ ] **File Encryption at Rest** - Encrypt files before storage
- [ ] **Resumable Uploads** - Support for chunked uploads
- [ ] **WebDAV Support** - WebDAV protocol for file access
- [ ] **File Versioning** - Keep multiple versions of files
- [ ] **Audit Trail** - Detailed access logs
- [ ] **Storage Quotas** - Per-tenant storage limits
- [ ] **CDN Integration** - Automatic CDN distribution for public files

### Version History

#### v2.0.0 (Current)
- ✅ Signed URL generation feature
- ✅ Multi-file aggregate support
- ✅ Fixed CreateAsync/UpdateAsync bug
- ✅ Enhanced E2E test coverage
- ✅ Comprehensive API documentation

#### v1.0.0
- ✅ Azure Blob Storage provider
- ✅ Azure File Storage provider
- ✅ Local Storage provider
- ✅ Upload/Download via proxy
- ✅ Multi-tenancy support
- ✅ Domain events publishing
- ✅ OAuth2 authentication

## ❓ FAQ

### General Questions

**Q: Can I use multiple storage providers simultaneously?**
A: No, only one provider can be enabled at a time. Configure the desired provider in `appsettings.json`.

**Q: What's the maximum file size?**
A: By default, there's no hard limit set by the service. Azure Blob supports files up to 5TB. Configure `Kestrel.MaxRequestBodySize` if needed.

**Q: How do I migrate from Local to Azure Blob?**
A: Files must be manually migrated to Azure Blob Storage, then update `appsettings.json` to enable AzureBlob provider. The MongoDB data remains unchanged.

**Q: Are signed URLs secure?**
A: Yes, signed URLs are time-limited (5-60 minutes) and generated with Azure SAS tokens. They expire automatically and cannot be extended.

**Q: Can I delete files permanently?**
A: Currently, the service implements soft delete (sets `isActive=false`). Physical deletion is not exposed via API but can be implemented.

### Technical Questions

**Q: Why use aggregate IDs instead of cloud URLs?**
A: This provides **storage transparency**. You can switch providers (Azure → AWS → GCS) without updating client code or database records. Clients only know the aggregate ID.

**Q: How does multi-file aggregates work?**
A: Upload multiple files using the **same aggregate ID**. The handler detects if the aggregate exists and uses `UpdateAsync` to add files to the existing aggregate.

**Q: What happens if upload fails?**
A: The transaction is rolled back. The file is not saved to cloud storage, and no MongoDB record is created. Clients receive a Problem Details response.

**Q: Can I access files without authentication?**
A: Direct downloads via `/download` endpoint require authentication. Use signed URLs for time-limited unauthenticated access.

**Q: How is multi-tenancy enforced?**
A: The `X-Tenant` header is mandatory. Repository queries automatically filter by tenant. Files from different tenants are completely isolated.

**Q: What's the difference between `Renowned` files?**
A: `Renowned=true` is a marker for public/special files. You can implement custom logic based on this flag (e.g., longer signed URL expiration, public access).

### Troubleshooting Questions

**Q: Why do I get 401 Unauthorized?**
A: Ensure you're sending a valid JWT token in the `Authorization: Bearer {token}` header and the `X-Tenant` header.

**Q: Upload works but I can't see the file in Azure Storage Explorer?**
A: Verify the container name in your configuration matches the Azure container. Check the connection string and ensure proper permissions.

**Q: Signed URLs return 403 Forbidden?**
A: Azure Storage Account must allow SAS token access. Check firewall rules and ensure the account key hasn't been rotated.

**Q: MongoDB connection fails in Docker?**
A: Ensure services are on the same Docker network. Use service names (e.g., `mongodb://mongo:27017`) instead of `localhost`.

## 📞 Support & Resources

### Getting Help

- **GitHub Issues**: [Report bugs or request features](https://github.com/codedesignplus/CodeDesignPlus.Net.Microservice.FileStorage/issues)
- **Discussions**: [Ask questions and share ideas](https://github.com/codedesignplus/CodeDesignPlus.Net.Microservice.FileStorage/discussions)
- **Documentation**: [CodeDesignPlus Docs](https://codedesignplus.github.io/)
- **Email**: support@codedesignplus.com

### Related Projects

- **CodeDesignPlus.Net.Sdk**: Core SDK with shared abstractions
- **CodeDesignPlus.Environment.Dev**: Local development environment setup
- **Template Repository**: Microservice scaffolding template

## 📄 License

This project is licensed under the **GNU Lesser General Public License v3.0** - see the [LICENSE.md](LICENSE.md) file for details.

### What This Means

- ✅ **Commercial use**: Use in commercial applications
- ✅ **Modification**: Modify the source code
- ✅ **Distribution**: Distribute the software
- ✅ **Private use**: Use privately
- ⚠️ **Disclose source**: Must disclose source for derivative works
- ⚠️ **License and copyright notice**: Include license and copyright
- ⚠️ **Same license**: Derivative works must use LGPL v3.0

## 🙏 Acknowledgments

Built with:
- **CodeDesignPlus SDK** - Core abstractions and utilities
- **.NET 9** - Microsoft's modern development platform
- **Azure Storage** - Scalable cloud storage
- **MongoDB** - Flexible document database
- **Open Source Community** - For all the amazing tools and libraries

---

**Made with ❤️ by CodeDesignPlus**

*For questions, suggestions, or contributions, please open an issue or pull request.*

## 🔧 Tools
The repository includes several utility scripts in the tools directory:

- `convert-crlf-to-lf.sh`: Converts line endings
- `update-packages/`: Updates NuGet packages
- `upgrade-dotnet/`: Upgrades .NET version
- `vault/`: Vault configuration scripts
- `sonarqube/`: SonarQube analysis configuration

## 📦 CodeDesignPlus Packages
This template use the `CodeDesignPlus.Net.Sdk` package to simplify the development process. For more information, visit the [Doc Site](https://codedesignplus.github.io/).