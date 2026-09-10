# لقاء (Liqaa) — Backend API

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-EF_Core-CC2927?logo=microsoftsqlserver&logoColor=white)
![AWS](https://img.shields.io/badge/AWS-S3_%26_Rekognition-FF9900?logo=amazonaws&logoColor=white)

**لقاء** is an Arabic-first social-impact platform that helps communities in Egypt reconnect missing people with their families. This repository contains the **.NET 10 REST API**: authentication and permissions, case management, AI face matching, file storage, real-time chat and notifications, donations, reporting, audit logging, and background jobs.

The Angular SPA is maintained in a **separate repository**. This API is independently runnable and exposes the REST and SignalR contracts it consumes.

## Contents

- [Business capabilities](#business-capabilities)
- [Roles and case lifecycle](#roles-and-case-lifecycle)
- [Architecture](#architecture)
- [Project structure](#project-structure)
- [Technology](#technology)
- [API surface](#api-surface)
- [Security and operations](#security-and-operations)
- [Local setup](#local-setup)
- [Configuration](#configuration)
- [Future work](#future-work)

## Business capabilities

- Registration, email-confirmation OTP, login, Google login, password reset/change, JWT access tokens, and refresh tokens.
- Five roles and granular permission-based authorization: `User`, `VerifiedUser`, `Moderator`, `Admin`, and `SuperAdmin`.
- National-ID verification workflow that promotes an approved user to `VerifiedUser`.
- Urgent, Long-Term, and Unknown case workflows, plus public found-person records.
- AWS Rekognition face indexing/search, duplicate detection, similarity matching, and Unknown-case grouping.
- Amazon S3 media storage; SignalR chat/messages/notifications; administrative chat monitoring.
- Guest-capable donation flow with payment webhook, statistics, and PDF export.
- Complaints, dashboards, user/role administration, PDF/Excel reports, and audit logs.

## Roles and case lifecycle

| Case type | Eligible creator | Publication and limits |
| --- | --- | --- |
| Urgent | Any authenticated user | Publishes immediately, expires after 48 hours, and is limited to one case every 14 days per user. |
| Long-Term | `VerifiedUser` | Requires administrator approval before publication. |
| Unknown | `VerifiedUser` | Requires administrator approval; similar cases can be placed in a `DuplicateGroup`, with the newest update shown publicly. |

The API searches the AWS Rekognition collection before creating a case. A match with an active Urgent/Long-Term case blocks a duplicate. A match with an Unknown case warns the user and can allow a force-create path where the business rules permit it. Self-duplicates are blocked; matches with pending cases return a pending-approval notice without revealing the case.

Case attachments allow up to **four images** (`jpg`, `jpeg`, `png`, `webp`), at **5 MB** each, and **one video** (`mp4`, `mov`, `webm`) at **50 MB**.

## Architecture

The solution follows Clean Architecture. The API host composes the application and infrastructure layers; domain entities and contracts remain dependency-free.

```text
SafeTrace API
  ├─ Controllers, middleware, Swagger, SignalR hubs, Hangfire dashboard
  │       ↓
  ├─ Application: services, DTOs, interfaces, validation, mappings
  │       ↓
  ├─ Infrastructure: EF Core, SQL Server, Identity, AWS, email, payments, reports
  │       ↓
  └─ Domain: entities, enums, result types, repository/unit-of-work contracts

External services: SQL Server · Amazon S3 · AWS Rekognition · SMTP · Paymob
```

## Project structure

```text
SafeTrace.sln
├─ SafeTrace/                         API host
│  ├─ Controllers/                    HTTP endpoints by business domain
│  ├─ ExceptionHandlers/              ProblemDetails exception mapping
│  ├─ ExtensionMethods/               database/AWS startup registration
│  ├─ Hubs/                           ChatHub and SignalR notifier
│  ├─ Properties/launchSettings.json  local launch profiles
│  ├─ wwwroot/                        PDF fonts and static assets
│  └─ Program.cs                      application composition and middleware
├─ SafeTrace.Application/
│  ├─ Common/, Constants/, DTOs/, Hubs/, Interfaces/
│  ├─ Mapping/                        AutoMapper profiles
│  └─ Services/                       case, chat, dashboard, complaint, and AI use cases
├─ SafeTrace.Infrastructure/
│  ├─ Authorization/                  dynamic permission policies/handler
│  ├─ DataAccess/                     DbContext and EF configurations
│  ├─ DependencyInjection/            Identity, JWT, AWS, and rate limits
│  ├─ Migrations/, Options/, Persistence/, Reports/, Services/
└─ SafeTrace.Domain/
   ├─ Common/, Entities/, Enums/, Interfaces/
```

## Technology

| Area | Implementation |
| --- | --- |
| Web API | ASP.NET Core 10, controllers, Swagger in Development |
| Persistence | EF Core 10, SQL Server, NetTopologySuite |
| Identity | ASP.NET Core Identity, JWT bearer, refresh tokens |
| Authorization | Dynamic permission policy provider and permission handler |
| Cloud | AWS SDK, Amazon S3, AWS Rekognition |
| Real time | ASP.NET Core SignalR |
| Jobs | Hangfire with SQL Server storage |
| Reporting | QuestPDF and ClosedXML |
| Observability | Serilog/Seq, Elmah SQL error log, Audit.EntityFramework |
| Integrations | MailKit SMTP and Paymob |

## API surface

All controller routes begin with `/api/{ControllerName}`; for example, `AccountController` is rooted at `/api/Account`. While running in Development, use `/swagger` for complete request schemas, multipart requirements, response models, authorization policies, and status codes.

| Controller | Key operations |
| --- | --- |
| `Account` | Register/login/Google login, OTP confirmation/resend, password reset/change, refresh/revoke token |
| `UserProfile` | Current/visited profile, ID images, profile/location/phone updates, personal cases |
| `Users` | List/details/statistics, approve/reject, block, role/permission assignment, admin registration, PDF report |
| `Roles` | List/create/delete roles and update role permissions |
| `UrgentCase` | Lists/details, creation status, create/update/delete, mark found, permanent deletion, administration views |
| `LongTermCase` and `UnKnownCase` | Public/admin lists and details, create/update, approval/rejection, mark found, deletion |
| `AiMatching` | `POST /api/AiMatching/search` |
| `Chats` and `Messages` | Context/create/list chats, send/read/delete messages, admin monitoring/statistics |
| `Notification` | List/send/delete notifications; mark one/all read |
| `Payment` | Create donation, webhook, result, lists/statistics, PDF export |
| `Complaints` | List/details/statistics, create, resolve, delete, PDF report |
| `Dashboard` and `Founded` | Statistics/audit logs/reporting and public found-person records |

### SignalR hubs

| Endpoint | Purpose |
| --- | --- |
| `/chatHub` | Live chat connection and events. |
| `/SafeTrace.Application/Hubs/notifications` | User-notification connection. |

For these two hub paths only, JWT bearer authentication accepts `access_token` in the query string. Standard HTTP requests use `Authorization: Bearer <token>`.

## Security and operations

- Access tokens expire after **15 minutes**. Refresh tokens expire after **7 days** and are sent in an HTTP-only secure cookie.
- Passwords require eight or more characters including uppercase, lowercase, number, and non-alphanumeric characters. Accounts lock after five failed attempts for 15 minutes.
- Authorization combines role assignment with granular seeded permissions; never treat a frontend check as a security boundary.
- Global rate limiting is **100 requests/minute**. Authentication is **10 requests/15 minutes**; dedicated policies also protect AI, OTP, chat, and other sensitive paths. Rejections return `429` Problem Details.
- Global exception handling produces Problem Details. Elmah logs server errors at `/elmah`, protected by Basic authentication. Audit logging records qualifying administrative state changes with old/new values.
- CORS is configured in `Program.cs`; add only trusted frontend origins, especially when allowing credentials.

### Scheduled maintenance

Hangfire uses the primary SQL Server connection and serves its Basic-authenticated dashboard at `/hangfire`.

| Job | Schedule | Purpose |
| --- | --- | --- |
| `CleanupExpiredOtps` | Daily | Remove expired OTP records. |
| `CleanupOldRefreshTokens` | Daily | Remove old/revoked refresh tokens. |
| `CleanupExpiredUrgentCases` | Hourly | Expire Urgent cases after their 48-hour window. |

## Local setup

### Prerequisites

- .NET SDK 10
- SQL Server
- AWS IAM credentials permitted to use S3 and Rekognition
- SMTP account for OTP/notification emails
- Paymob credentials to use payments

### Restore, configure, migrate, and run

```bash
git clone <your-backend-repository-url>
cd SafeTrace
dotnet restore
dotnet user-secrets init --project SafeTrace/SafeTrace.API.csproj
dotnet ef database update --project SafeTrace.Infrastructure --startup-project SafeTrace
dotnet run --project SafeTrace/SafeTrace.API.csproj --launch-profile https
```

The HTTPS profile runs at `https://localhost:7041` (and `http://localhost:5041`) and opens Swagger at `/swagger`. Startup seeds roles/permissions, applies pending migrations, and ensures the configured Rekognition collection exists.

> Run the EF command from the solution directory. If required, install the EF CLI once: `dotnet tool install --global dotnet-ef`.

## Configuration

No production secrets are included. The API reads its local secrets from the .NET **User Secrets** store; use a managed secret store or environment variables in production. Initialize the local store once:

```bash
dotnet user-secrets init --project SafeTrace/SafeTrace.API.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<SQL Server connection string>" --project SafeTrace/SafeTrace.API.csproj
```

Add the following shape to User Secrets. The placeholder values are intentional—do not place real credentials in this README or in committed `appsettings*.json` files.

```json
{
  "ConnectionStrings": { "DefaultConnection": "<SQL Server connection string>" },
  "Jwt": {
    "SecretKey": "<long random signing key>",
    "Issuer": "<issuer>",
    "Audience": "<audience>",
    "DurationInMinutes": 15,
    "RefreshTokenDurationInDays": 7
  },
  "AWS": {
    "Region": "<AWS region>", "AccessKey": "<access key>", "SecretKey": "<secret key>",
    "CollectionId": "<Rekognition collection>", "FilesBucketName": "<S3 bucket>"
  },
  "MailSettings": {
    "Email": "<SMTP sender email>", "DisplayName": "<sender display name>",
    "Password": "<SMTP password or app password>", "Host": "<SMTP host>", "Port": "<SMTP port>"
  },
  "Paymob": {
    "SecretKey": "<secret>", "PublicKey": "<public key>", "PaymentMethodId": "<method id>",
    "NotificationUrl": "<webhook URL>", "RedirectionUrl": "<browser return URL>",
    "BaseUrl": "<Paymob base URL>", "WebhookSecret": "<HMAC secret>"
  },
  "Hangfire": { "Username": "<dashboard user>", "Password": "<dashboard password>" },
  "Elmah": { "Username": "<error-log user>", "Password": "<error-log password>" }
}
```

`MailSettings` is the mail configuration used by the current MailKit email service. `SendGrid` is retained in the local secret shape for deployment/integration use; it is not consumed by the current API code. Keep `appsettings.Development.json` for non-sensitive local settings such as logging.

Never commit real database strings, JWT/AWS/SMTP/payment secrets, or Hangfire/Elmah credentials. If any secret is shared in a chat, ticket, screenshot, or commit, rotate it immediately and update the relevant User Secret or deployment secret.

## Future work

| Feature | Priority | Notes |
| --- | --- | --- |
| Governmental integration | High | Connect with local police missing-persons databases for automatic cross-checking. |
| Progressive Web App (PWA) | Medium | Installable mobile access with push notifications. |

## 👥 Team Members

| Name                   | GitHub                                                           |
| ---------------------- | ---------------------------------------------------------------- |
| Esraa Taha             | [@Gargera](https://github.com/Gargera)                           |
| Yousef Abdullah        | [@yousef721](https://github.com/yousef721)                       |
| Basma Allaa            | [@basmaallaa](https://github.com/basmaallaa)                     |
| Mohamed Gamal Elemam   | [@Mohamed-Gamal-Elemam](https://github.com/Mohamed-Gamal-Elemam) |
| Amany Hisham           | [@amanyhisham](https://github.com/amanyhisham)                   |
| Yousef Farouk          | [@ysfrk10](https://github.com/ysfrk10)                           |
| Toqa Mahmoud           | [@ToqaMahmoud787](https://github.com/ToqaMahmoud787)             |
| Mohamed Saeed          | [@Mohmaed-Saaed](https://github.com/Mohmaed-Saaed)               |

---

ITI Full Stack .NET & Generative AI graduation project, 2026.
