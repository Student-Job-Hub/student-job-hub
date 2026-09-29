# 🎓 Student Job Hub

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor WASM](https://img.shields.io/badge/Frontend-Blazor%20WebAssembly-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![ASP.NET Core API](https://img.shields.io/badge/Backend-ASP.NET%20Core%20Web%20API-512BD4?logo=dotnet)](https://asp.net/)
[![EF Core](https://img.shields.io/badge/ORM-Entity%20Framework%20Core%2010.0-512BD4)](https://docs.microsoft.com/ef/)
[![MudBlazor](https://img.shields.io/badge/UI-MudBlazor%209.10.0-7460EE)](https://mudblazor.com/)
[![Tests](https://img.shields.io/badge/Tests-23%20Passing-brightgreen?logo=xunit)](tests/StudentJobHub.Tests)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

> A modern, full-stack campus marketplace connecting **Students**, **Lecturers**, and **Businesses**. Discover flexible campus jobs, manage applications with resume file uploads, offer freelance services, book appointments, track real-time SignalR notifications, export data reports, and manage administrative workflows.

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Key Features](#-key-features)
- [Technology Stack](#-technology-stack)
- [System Architecture & Directory Structure](#-system-architecture--directory-structure)
- [Getting Started](#-getting-started)
  - [Prerequisites](#prerequisites)
  - [Database Configuration](#database-configuration)
  - [Running the Application](#running-the-application)
- [API Reference](#-api-reference)
- [Automated Testing](#-automated-testing)
- [Contributing & Team](#-contributing--team)

---

## 🚀 Overview

**Student Job Hub** provides a comprehensive digital campus ecosystem designed for student employment, freelance services, and administrative management:
- **Students** can explore part-time job postings, bookmark favorites, submit job applications with resume attachments (PDF/DOCX), offer freelance services, request service bookings, and track real-time application and booking statuses.
- **Businesses & Lecturers** can publish job openings, review applicant resumes, accept or reject candidates, offer campus services, manage incoming booking requests, and leave peer reviews.
- **Admins** have access to a dedicated Admin Control Panel to monitor system-wide metrics, manage users, suspend accounts, and moderate jobs and services.
- **Security & Infrastructure** are powered by JWT Bearer authentication, ASP.NET Core Identity with role-based access control (`Student`, `Business`, `Lecturer`, `Admin`), SignalR web sockets, and EF Core 10 with SQL Server.

---

## ✨ Key Features

### 🔐 1. Authentication, Authorization & Roles
- **JWT Bearer Authentication**: Secure token-based auth with client-side `localStorage` persistence and token refresh (`/api/auth/refresh`).
- **Role-Based Access Control**: Granular permission enforcement across `Student`, `Business`, `Lecturer`, and `Admin` roles.
- **Admin Bootstrapping**: Automatic role initialization and administrator account seeding (`AdminBootstrapService`).

### 💼 2. Job Marketplace & Bookmarks
- **Job Discovery (`/jobs`)**: Filter and search active or closed job opportunities, view budgets in GH₵, and track application deadlines.
- **Job Details & Management (`/jobs/{id}`)**: Owner actions (`Edit`, `Close`, `Delete`, `View Applications`) and student actions (`Apply`, `Bookmark`).
- **Job Bookmarks (`/saved-jobs`)**: Save opportunities for quick access with persistent bookmark tracking.

### 📄 3. Job Application System & Resume Uploads
- **Application Submission**: Students submit job applications with custom pitch messages and optional resume attachments (PDF/DOCX validation).
- **Application Tracking (`/my-applications`)**: Track submission status (`Pending`, `Accepted`, `Rejected`) and withdraw pending applications.
- **Owner Review Panel (`/job-applications/{jobId}`)**: Job creators inspect applicant profiles, download attached resumes, and update application statuses with instant SignalR notifications.

### 🛠️ 4. Services Marketplace & Service Bookings
- **Service Listings (`/services`)**: Browse freelance campus services (e.g., tutoring, tech repair, graphic design) with category filtering and pricing.
- **Service Management (`/my-services`, `/create-service`, `/edit-service/{id}`)**: Create, update, and manage offered campus services.
- **Service Bookings (`/my-bookings`)**: Book campus services, track requested bookings, and allow providers to accept, reject, or mark bookings as completed.

### 🔔 5. Real-Time SignalR Notifications & Peer Reviews
- **SignalR Notification Hub**: Instant real-time alerts delivered via `/hubs/notifications` for application status changes and booking updates.
- **Notification Center (`/notifications`)**: Inbox view with read/unread toggling, batch clearing, and individual deletion.
- **Email Alerts**: Background email notification dispatch (`EmailNotificationService`).
- **Peer Reviews & Ratings (`/reviews/{userId}`)**: 1–5 star rating system with feedback comments and self-review protections.

### 🛡️ 6. Admin Control Panel
- **Dashboard Metrics (`/admin`)**: System-wide overview tracking total users, active jobs, listed services, and application volume.
- **User Moderation**: Search users, inspect account details, and toggle account suspension (`/api/admin/users/{userId}/suspension`).
- **Content Moderation**: Administrative oversight to close jobs or remove inappropriate service listings.

### 📊 7. Data Export & Analytics Reports
- **Export Center (`/export-data`)**: Export applications, job listings, and service bookings in CSV or JSON summary formats.
- **Printable HTML Reports**: Generate printable summary reports formatted for physical printing or PDF output (`/api/export/report/html`).

### 🎨 8. User Profile, Custom Themes & Dynamic SEO
- **User Profile (`/profile`)**: Manage personal details (Full Name, Phone Number, University, Bio, Avatar URL) and update account passwords.
- **Theme Support (`/settings`)**: Theme switcher with persistent Light and Dark mode styles (`ThemeService`).
- **Dynamic SEO Head**: Dynamic OpenGraph meta tag rendering for social share previews (`SeoHead.razor`).

---

## 🛠️ Technology Stack

| Layer | Technology | Package / Version | Description |
|---|---|---|---|
| **Frontend Framework** | Blazor WebAssembly | .NET 10.0 (`net10.0`) | Client-side Single Page Application (SPA) |
| **Frontend UI Components** | MudBlazor & Custom CSS | `MudBlazor` v9.10.0 | Modern UI components, dialogs, dark theme support |
| **Backend Framework** | ASP.NET Core Web API | .NET 10.0 (`net10.0`) | RESTful Web API controllers & services |
| **Authentication & Security** | ASP.NET Core Identity & JWT | `Microsoft.AspNetCore.Authentication.JwtBearer` v10.0.11 | Token-based auth, role authorization & password hashing |
| **Real-Time Communication** | ASP.NET Core SignalR | `Microsoft.AspNetCore.SignalR.Client` v10.0.9 | WebSockets hub for instant notification delivery |
| **Database & ORM** | Microsoft SQL Server / LocalDB | `Microsoft.EntityFrameworkCore.SqlServer` v10.0.11 | Code-first migrations and relational data mapping |
| **Testing Framework** | xUnit & EF Core InMemory | `xunit` v2.9.3, `Microsoft.EntityFrameworkCore.InMemory` v10.0.11 | Unit & integration test suite |

---

## 📂 System Architecture & Directory Structure

```text
StudentJobHub/
├── src/
│   ├── StudentJobHub.Api/                  # ASP.NET Core Web API (.NET 10.0)
│   │   ├── Controllers/                    # API Endpoints (Admin, Auth, Applications, Bookings,
│   │   │                                   #  Bookmarks, Export, Jobs, Notifications, Reviews,
│   │   │                                   #  Services, Share, Users)
│   │   ├── Data/                           # ApplicationDbContext & EF Core Migrations
│   │   ├── DTOs/                           # Data Transfer Objects (Admin, Applications, Auth,
│   │   │                                   #  Bookings, Jobs, Reviews, Services, Users)
│   │   ├── Hubs/                           # SignalR NotificationHub (/hubs/notifications)
│   │   ├── Models/                         # Domain Entities (ApplicationUser, Job, JobApplication,
│   │   │                                   #  JobBookmark, Notification, Review, Service, ServiceBooking)
│   │   ├── Properties/                     # Launch settings & environment profiles
│   │   └── Services/                       # Core Business Logic (Admin, Auth, Application, Booking,
│   │                                       #  Bookmark, Email, Export, Job, Notification, Review, Service)
│   │
│   └── StudentJobHub.Client/               # Blazor WebAssembly Frontend (.NET 10.0)
│       ├── Components/                     # Reusable Razor Components (ProfileCard, ReviewSection,
│       │                                   #  SeoHead, StarRating, ToastContainer, ConfirmModal)
│       ├── Layout/                         # Layouts & Navigation (MainLayout, NavMenu, FooterLayout)
│       ├── Models/                         # Client-side View Models & DTO mappings
│       ├── Pages/                          # Blazor Pages (Admin, Dashboard, DirectMessages, Jobs,
│       │                                   #  JobDetails, MyApplications, MyBookings, MyJobs,
│       │                                   #  MyServices, Notifications, PostJob, Profile, Register,
│       │                                   #  Reviews, SavedJobs, Services, ServiceDetails, Settings, etc.)
│       ├── Services/                       # API Services, JwtAuthorizationHandler, ThemeService, ToastService
│       └── wwwroot/                        # Static web assets, styles (app.css), and JavaScript interop
│
└── tests/
    └── StudentJobHub.Tests/                # xUnit Automated Test Suite (23 Unit & Integration Tests)
```

---

## ⚙️ Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or higher
- Microsoft SQL Server or SQL Server Express / LocalDB

---

### Database Configuration

Verify or update the connection string in `src/StudentJobHub.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=StudentJobHubDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

> [!NOTE]
> Database migrations and default roles (`Student`, `Lecturer`, `Business`, `Admin`) will automatically execute and apply on API startup.

---

### Running the Application

1. **Clone the Repository**:
   ```powershell
   git clone https://github.com/Student-Job-Hub/student-jub-hub.git
   cd StudentJobHub
   ```

2. **Start the API Backend**:
   ```powershell
   dotnet run --project src/StudentJobHub.Api
   ```
   *The API will restore dependencies, apply pending EF migrations, seed roles, and listen at `http://localhost:5205`.*

3. **Start the Blazor WebAssembly Frontend**:
   ```powershell
   dotnet run --project src/StudentJobHub.Client
   ```
   *Open your browser and navigate to the local client URL displayed in your terminal (e.g. `http://localhost:5000` or `https://localhost:7071`).*

---

## 📡 API Reference

### 🔐 Authentication & Accounts (`/api/auth`)
| Endpoint | Method | Auth | Description |
|---|---|---|---|
| `/api/auth/register` | `POST` | Public | Register a new user (`Student`, `Business`, `Lecturer`) |
| `/api/auth/login` | `POST` | Public | Authenticate user credentials and return JWT bearer token |
| `/api/auth/refresh` | `POST` | Public | Refresh an active JWT session |

### 💼 Jobs & Bookmarks (`/api/jobs`, `/api/bookmarks`)
| Endpoint | Method | Auth | Description |
|---|---|---|---|
| `/api/jobs` | `GET` | Public | Search and list all active job postings |
| `/api/jobs/my` | `GET` | Bearer | Retrieve job postings created by the authenticated user |
| `/api/jobs/{id}` | `GET` | Public | Fetch detailed information for a specific job |
| `/api/jobs` | `POST` | Bearer | Create and publish a new job opportunity |
| `/api/jobs/{id}` | `PUT` | Bearer | Update an existing job posting |
| `/api/jobs/{id}` | `DELETE` | Bearer | Delete a job posting |
| `/api/jobs/{id}/close` | `PATCH` | Bearer | Toggle job status to closed |
| `/api/bookmarks` | `GET` | Bearer | List saved jobs for the current user |
| `/api/bookmarks/{jobId}` | `POST` | Bearer | Bookmark a job posting |
| `/api/bookmarks/{jobId}` | `DELETE` | Bearer | Remove a job from bookmarks |

### 📄 Job Applications (`/api/applications`)
| Endpoint | Method | Auth | Description |
|---|---|---|---|
| `/api/applications/{jobId}` | `POST` | Bearer | Submit job application with pitch & resume attachment |
| `/api/applications/my` | `GET` | Bearer | Fetch job applications submitted by the current student |
| `/api/applications/job/{jobId}` | `GET` | Bearer | Fetch applicants for a job owned by the user |
| `/api/applications/{id}/resume` | `GET` | Bearer | Download resume file attachment for an application |
| `/api/applications/{id}/status` | `PATCH` | Bearer | Update applicant status (`Accepted`, `Rejected`) |
| `/api/applications/{id}` | `DELETE` | Bearer | Withdraw a pending job application |

### 🛠️ Services & Bookings (`/api/services`, `/api/bookings`)
| Endpoint | Method | Auth | Description |
|---|---|---|---|
| `/api/services` | `GET` | Public | Browse campus freelance services |
| `/api/services/my` | `GET` | Bearer | Retrieve services created by the current provider |
| `/api/services/{id}` | `GET` | Public | Fetch service details |
| `/api/services` | `POST` | Bearer | List a new campus service |
| `/api/services/{id}` | `PUT` | Bearer | Update service details |
| `/api/services/{id}` | `DELETE` | Bearer | Remove a service listing |
| `/api/bookings` | `POST` | Bearer | Book a campus service |
| `/api/bookings/my-requests` | `GET` | Bearer | View service bookings requested by student |
| `/api/bookings/received` | `GET` | Bearer | View incoming service booking requests for provider |
| `/api/bookings/{id}/status` | `PATCH` | Bearer | Update booking status (`Accepted`, `Rejected`, `Completed`, `Cancelled`) |

### 🔔 Notifications, Reviews & Users (`/api/notifications`, `/api/reviews`, `/api/users`)
| Endpoint | Method | Auth | Description |
|---|---|---|---|
| `/api/notifications` | `GET` | Bearer | Retrieve inbox notifications for authenticated user |
| `/api/notifications/{id}/read` | `PATCH` | Bearer | Mark a notification as read |
| `/api/notifications/{id}` | `DELETE` | Bearer | Delete a notification |
| `/api/reviews` | `POST` | Bearer | Submit a peer review and rating (1–5 stars) |
| `/api/reviews/user/{userId}` | `GET` | Public | Fetch reviews and average rating score for a user |
| `/api/users/me` | `GET` | Bearer | Fetch current user profile details |
| `/api/users/me` | `PUT` | Bearer | Update user profile information & avatar |

### 🛡️ Admin & Export Data (`/api/admin`, `/api/export`)
| Endpoint | Method | Auth | Description |
|---|---|---|---|
| `/api/admin/overview` | `GET` | Admin | Fetch system-wide administrative statistics |
| `/api/admin/users` | `GET` | Admin | Search and list registered users |
| `/api/admin/users/{userId}/suspension` | `PATCH` | Admin | Suspend or reactivate a user account |
| `/api/export/applications/csv` | `GET` | Bearer | Download CSV export of applications |
| `/api/export/jobs/csv` | `GET` | Bearer | Download CSV export of jobs |
| `/api/export/bookings/csv` | `GET` | Bearer | Download CSV export of service bookings |
| `/api/export/report/html` | `GET` | Bearer | Generate printable HTML summary report |

---

## 🧪 Automated Testing

The project includes an automated **xUnit** integration and unit test suite covering authentication, application workflow, job service, review service, notification service, service bookings, and bookmark management using an in-memory EF Core database.

To execute the test suite:

```powershell
dotnet test
```

**Expected Output**:
```text
Passed!  - Failed: 0, Passed: 23, Skipped: 0, Total: 23
```

---

## 👥 Contributing & Team

Built for the **DCIT318 Group Project** at the University of Ghana under the leadership of Addo Michael Obiri and team contributors. Refer to [TEAM_ASSIGNMENTS.MD](TEAM_ASSIGNMENTS.MD) and [ACTION_PLAN.md](ACTION_PLAN.md) for team development workflows.
