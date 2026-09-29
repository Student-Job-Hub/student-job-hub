# REST API Specification — Student Job Hub

**Base URL:** `http://localhost:5205/`  
**Authentication Header:** `Authorization: Bearer <jwt_token>`

---

## 🔐 1. Authentication Endpoints (`/api/auth`)

### `POST /api/auth/register`

- **Auth:** Public
- **Request Body:**
  ```json
  {
    "fullName": "Michael Obiri",
    "email": "jane@campus.edu",
    "password": "Password123!",
    "role": "Student",
    "university": "University of Ghana"
  }
  ```
- **Responses:**
  - `200 OK`: `{ "message": "Registration successful.", "token": "eyJhbG...", "refreshToken": "..." }`
  - `400 Bad Request`: `{ "message": "Invalid role selected." }`

### `POST /api/auth/login`

- **Auth:** Public
- **Request Body:**
  ```json
  {
    "email": "jane@campus.edu",
    "password": "Password123!"
  }
  ```
- **Responses:**
  - `200 OK`: `{ "message": "Login successful.", "token": "eyJhbG...", "refreshToken": "..." }`
  - `401 Unauthorized`: `{ "message": "Invalid email or password." }`

### `POST /api/auth/refresh`

- **Auth:** Public
- **Request Body:** `{ "refreshToken": "..." }`
- **Responses:**
  - `200 OK`: Returns a new access token and rotated refresh token.
  - `401 Unauthorized`: Refresh token is invalid, expired, or already used.
- Refresh tokens expire after 30 days and can only be used once.

---

## 💼 2. Job Endpoints (`/api/jobs`)

### `GET /api/jobs`

- **Auth:** Public
- **Response:** `200 OK` Array of `JobResponseDto`

### `GET /api/jobs/my`

- **Auth:** Bearer Token Required
- **Response:** `200 OK` Array of jobs posted by authenticated user

### `GET /api/jobs/{id}`

- **Auth:** Public
- **Response:** `200 OK` `JobResponseDto` or `404 Not Found`

## Direct Messages

`/hubs/messages` is an authenticated SignalR hub. The applicant and job poster for an application can call `JoinConversation(applicationId)` to receive message history and `SendMessage(applicationId, content)` to save and broadcast a message. Messages are limited to 2000 characters and stored with their application.

### `POST /api/jobs`

- **Auth:** Bearer Token Required
- **Request Body:**
  ```json
  {
    "title": "Lab Research Assistant",
    "description": "Assist with data collection in CS lab.",
    "requirements": "Basic C# and SQL",
    "budget": 250.0,
    "deadline": "2026-10-15T00:00:00Z"
  }
  ```
- **Responses:**
  - `201 Created`: Returns created job object
  - `401 Unauthorized`

### `PUT /api/jobs/{id}`

- **Auth:** Bearer Token (Owner Only)
- **Response:** `200 OK` or `404 Not Found` (if not owner)

### `PATCH /api/jobs/{id}/close`

- **Auth:** Bearer Token (Owner Only)
- **Response:** `200 OK` `{ "message": "Job closed successfully." }`

### `DELETE /api/jobs/{id}`

- **Auth:** Bearer Token (Owner Only)
- **Response:** `200 OK` or `404 Not Found`

---

## 📄 3. Application Endpoints (`/api/applications`)

### `POST /api/applications/{jobId}`

- **Auth:** Bearer Token Required
- **Request Body:** `{ "message": "I am interested in this role." }`
- **Responses:**
  - `201 Created`: Application created
  - `400 Bad Request`: Cannot apply to own job / Already applied / Job closed

### `GET /api/applications/my`

- **Auth:** Bearer Token Required
- **Response:** `200 OK` Array of current student's submitted applications

### `GET /api/applications/job/{jobId}`

- **Auth:** Bearer Token (Job Owner Only)
- **Response:** `200 OK` Array of candidate applications for specified job

### `PATCH /api/applications/{id}/status`

- **Auth:** Bearer Token (Job Owner Only)
- **Request Body:** `{ "status": "Accepted" }` _(or "Rejected")_
- **Response:** `200 OK`

### `DELETE /api/applications/{id}`

- **Auth:** Bearer Token (Applicant Only)
- **Response:** `200 OK` (Only pending applications allowed to be withdrawn)

---

## 🛠️ 4. Services Endpoints (`/api/services`)

- `GET /api/services`: Public listing
- `GET /api/services/{id}`: Public detail view
- `POST /api/services`: Bearer Token required
- `PUT /api/services/{id}`: Bearer Token required (Owner Only)
- `DELETE /api/services/{id}`: Bearer Token required (Owner Only)

---

## 🔔 5. Notification & Review Endpoints

- `GET /api/notifications`: Get user notifications
- `PATCH /api/notifications/{id}/read`: Mark notification read
- `DELETE /api/notifications/{id}`: Remove notification
- `POST /api/reviews`: Submit user review (Rating 1–5, comment)
- `GET /api/reviews/user/{userId}`: Public user reviews

---

## 👤 6. Profile Endpoints (`/api/users`)

- `GET /api/users/me`: Authenticated user profile
- `PUT /api/users/me`: Update FullName, University, Bio, ProfilePictureUrl

---

## 🛡️ 7. Admin Endpoints (`/api/admin`)

All endpoints require a JWT for a user in the `Admin` role.

- `GET /api/admin/overview`: Platform totals, open jobs, suspended users, and new users this month
- `GET /api/admin/users?search={term}`: List users, roles, join dates, and suspension state; search by name or email
- `PATCH /api/admin/users/{userId}/suspension`: Set `{ "isSuspended": true }` to suspend or `false` to reactivate; a suspended user cannot log in, refresh tokens, or use existing JWTs
- `GET /api/admin/jobs`: List job listings and their owners
- `PATCH /api/admin/jobs/{jobId}/close`: Close an open job listing
- `GET /api/admin/services`: List service listings and their providers
- `DELETE /api/admin/services/{serviceId}`: Remove a service listing

In Development, startup provisions a local administrator account without public registration. The development-only email and password are defined in `Program.cs`; this fixed credential must never be used in production or deployed environments.
