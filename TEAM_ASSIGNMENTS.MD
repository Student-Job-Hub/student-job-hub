# Team Feature Assignments & Guidelines

Here are feature assignments and development workflow guidelines for the team.

---
## If a feature is already implemented just skip it or modify it were necessary or if you see that you can implement it better go ahed and implement it, but be sure to test you self if it works before merging(Very Important!)

## Feature Assignments

### 1. JEFFREY ESHUN (`1general`)
* **Feature #18: Messaging / Chat** — Implement direct messaging between job posters and applicants using SignalR.
* **Feature #21: Admin Dashboard** — Build the admin panel to manage users, moderate jobs/services, and view platform analytics.
* **Feature #27: Refresh Tokens** — Implement JWT refresh token rotation mechanism for extended user sessions.

### 2. Christian Agyapong (`ChristianAgyapong`)
* **Feature #19: Job Bookmarks / Saved Jobs** — Enable users to save/bookmark interesting jobs for quick access later.
* **Feature #24: Service Request / Booking System** — Build the flow allowing users to directly request or book services.
* **Feature #29: Export Data** — Build data export functionality for users to download their job/application history as CSV/PDF.

### 3. d-dumasp (`d-dumasp`)
* **Feature #20: Application CV/Resume Upload** — Implement PDF resume attachments for job applications via `InputFile`.
* **Feature #25: Email Notifications** — Set up external email triggers when application statuses change.
* **Feature #30: SEO & Open Graph** — Add meta tags and Open Graph tags for rich link previews across social media.

### 4. DONBEINAA JULIANA DOME-IM (`DONJULI233`)
* **Feature #22: Dark Mode** — Implement a dark mode toggle using Blazor's `IPreferences` and `IJSRuntime` to save and apply theme preference.
* **Feature #26: Modify the Jobs card** — Modify the jobs and services card to display more information about the job and services.

### 5. Niiboy (`Niiboy88`)
* **Feature #23: Implement the Notification** — Implement the notification system to notify users of new messages, job applications, and service requests.
* **Feature #28: Audit Logging** — Implement backend audit logs tracking key actions (logins, job CRUD operations, status changes).

### 6. Amos Fiifi Addo(`afaddo1`)
* **Feature #31: implement settings menu** - implement settings menu for the app, including notifications, preferences, app and privacy settings(Just minimal-no need to over do it!)
* **Feature #32: implement reviews and ratings** - implement reviews and ratings for both jobs and services

### 7. SUN123-wrld (`SUN123-wrld`)
* **Feature #33: Profile Pictures on Cards** — Update job, saved job, and service detail cards to display the user's uploaded profile picture instead of generic initials (fallback to initials if no picture exists).
* **Feature #34: Email Verification** — Send verification email on registration before allowing full access.

### 8. Kelvin Annor (`jukebox5`)
* **Feature #35: Forgot Password / Reset** — Standard password reset flow with email link.
* **Task brief:** Implement a secure password reset flow where a user can request a reset link via email, validate the token, and update their password. The work includes backend API endpoints for requesting reset and setting a new password, email integration or mock email trigger, UI pages for requesting and resetting the password, validation for invalid/expired tokens, and basic testing to confirm success and failure scenarios.
---

## Team Development & Workflow Guidelines

 **IMPORTANT:** All team members must thoroughly review `ACTION_PLAN.md` in the project root repository before writing or modifying any code!

### 1. Branch Naming Standard
Create a new feature branch for your work using your **GitHub username** and **Feature Name**.
* **Format:** `feature/<your-firstname>/<feature-name>`
* **Examples:** 
  * `feature/jeff/messaging-chat`
  * `feature/christian/saved-jobs`

### 2. File Isolation & Clean Commits
* **ONLY** stage, commit, and push the SPECIFIC files you created or directly edited for your assigned feature AND PUSH ONLY THAT/THOSE FILES (NOT ALL THE FILES IN THE PROJECT!)
* **DO NOT** commit shared setup files, local test configs, or untracked temporary files outside your scope.

### 3. Pull Requests (PRs)
* Set **`dev`** as your base branch for all Pull Requests.
* Include a clear description of the work completed and any testing steps in your PR summary.

### 4. Strict Protection Rule
* **NEVER push directly to the `main` branch under any circumstances!** All updates to `main` must go through PR review via the `dev` branch.