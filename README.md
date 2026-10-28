# DriveX

**DriveX is a full-stack car-rental platform that enables multiple rental companies to manage and rent out their vehicles. Users book cars, company staff hand them over and start the rental timer, triggering an email confirmation. When users return the cars, staff submit them, apply any applicable late-return fine, and send a return-confirmation email.**

> This README is shared by both repositories. Run the two parts together for the full app.
>
> - Backend (ASP.NET Core API): https://github.com/shuvo37/dotNet_DriveX
> - Frontend (React app): https://github.com/shuvo37/DriveX_frontend

<!-- PLACEHOLDER: add badges here if you want, e.g. .NET 10, React 19, MySQL, License -->

<!-- PLACEHOLDER: add a short demo video or GIF here (30-60 seconds: browse a car, book it, ask the AI assistant, open the admin analyst) -->
<!-- ![DriveX demo](docs/demo.gif) -->

<!-- PLACEHOLDER: live demo link, if you deploy it -->

---

## Table of Contents

- [Features](#features)
- [AI Features](#ai-features)
- [Screenshots](#screenshots)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [API Overview](#api-overview)
- [Design Decisions](#design-decisions)
- [Author](#author)

---

## Features

DriveX has three roles, each with its own dashboard.

**USER (customer)**
- Register and log in with email and password, or sign in with Google
- Browse and search the car fleet
- Book a car and pay for the booking
- View your own bookings
- Reset a forgotten password with an emailed verification code

**STUFF (company staff)**
- See bookings waiting to be delivered
- Start a rental when the customer picks the car up
- End a rental and receive the car back
- late returns are fined per complete late hour

**ADMIN**
- Add and update companies and cars
- Activate or deactivate companies and cars
- Change a car's rental status (for example, put it in maintenance)
- Search bookings by car
- Change user roles
- Ask the AI analyst questions about the business

---

## AI Features

Both AI features use the Google Gemini API.

**1. Customer assistant**
Customers ask questions in plain language ("Which cars are available in Dhaka?"). The assistant answers from the real car data in the database, so it recommends only cars that exist.

**2. Admin analyst**
Admins ask business questions ("Which cars are returned late the most?"). The analyst uses tool calling: Gemini decides which function to run, and the backend executes it against the database. It has five read-only tools:

- fleet overview
- bookings
- late returns
- revenue
- car performance

The tools return business data only. Customer personal data is never sent to the model.

**Reliability:** a Gemini request is retried once. If it still fails, the backend switches to a fallback model, and if that fails too, the user gets a friendly error instead of a crash.

---

## Screenshots

<!-- PLACEHOLDER: add one screenshot per item below, saved in a docs/ folder, then replace each comment with an image line like: ![Home page](docs/home.png) -->

| Screen | Image |
|---|---|
| Home / car browsing | <!-- PLACEHOLDER --> |
| Car details and booking | <!-- PLACEHOLDER --> |
| Customer AI assistant | <!-- PLACEHOLDER --> |
| Staff dashboard (start / end rental) | <!-- PLACEHOLDER --> |
| Admin dashboard | <!-- PLACEHOLDER --> |
| Admin AI analyst | <!-- PLACEHOLDER --> |

---

## Tech Stack

| Part | Technology |
|---|---|
| Backend | ASP.NET Core (.NET 10), Entity Framework Core |
| Database | MySQL (Microting EF Core MySQL provider) |
| Authentication | JWT, Google OAuth, BCrypt password hashing |
| Frontend | React 19, Vite, React Router 7, Axios |
| AI | Google Gemini API |
| Image storage | Cloudinary |
| Email | SMTP (MailKit) |
| Deployment | Dockerfile + nginx for the frontend |

---

## Project Structure

**Backend** (`dotNet_DriveX`)

```
Controllers/   HTTP endpoints
Services/      Business logic (booking, payment, AI, auth, email)
DTOs/          Request and response models (no entity is returned directly)
Models/        Database entities
Data/          DbContext and entity configurations
Migrations/    EF Core migrations (database schema)
Options/       Typed settings (Jwt, Gemini, Cloudinary, Smtp, Frontend)
Exceptions/    Application errors mapped to clean API responses
```

**Frontend** (`DriveX_frontend`)

```
src/Pages/generalPages/   Login, register, forgot password
src/Pages/Auth/           Google sign-in callback, complete profile
src/Pages/user/           Customer pages
src/Pages/CompanyStuff/   Staff pages
src/Pages/admin/          Admin pages and AI analyst
src/services/             Auth service
src/utils/                API base URL, token helpers, car search helpers
src/hooks/                useCars data hook
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20 or newer
- MySQL 8 or newer, running locally
- The EF Core CLI tool:

```bash
dotnet tool install --global dotnet-ef
```

### 1. Clone both repositories

```bash
git clone https://github.com/shuvo37/dotNet_DriveX.git
git clone https://github.com/shuvo37/DriveX_frontend.git
```

### 2. Set up the backend

```bash
cd dotNet_DriveX
```

Store your settings with user-secrets, so no password or key is ever committed. Replace the values with your own:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=drivex;User=root;Password=YOUR_PASSWORD;"
dotnet user-secrets set "Jwt:Secret" "a-long-random-string-of-at-least-32-characters"
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY"
```

Create the database tables from the included migration:

```bash
dotnet ef database update
```

Start the API:

```bash
dotnet run
```

The API runs at `http://localhost:8080`. Check it with `http://localhost:8080/api/health`.

See [Configuration](#configuration) for the optional settings (Google login, Cloudinary, SMTP).

### 3. Set up the frontend

```bash
cd DriveX_frontend
npm install
npm run dev
```

The app runs at `http://localhost:5173`.

If your backend runs somewhere else, create a `.env` file in the frontend folder:

```
VITE_API_URL=http://localhost:8080
```

### 4. Create your first admin

Every new account starts as `USER`. Register an account in the app, then promote it in MySQL:

```sql
UPDATE users SET role = 'ADMIN' WHERE email = 'you@example.com';
```

Log in again to get a token with the new role. Create a second account and set `role = 'STUFF'` the same way to try the staff dashboard.

---

## Configuration

All backend settings are read from user-secrets (development) or environment variables (production, with `__` instead of `:`, for example `Jwt__Secret`).

| Setting | Required | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Yes | MySQL connection string |
| `Jwt:Secret` | Yes | Signing key for login tokens (32+ characters) |
| `Gemini:ApiKey` | For AI features | Google Gemini API key |
| `Gemini:Model` / `Gemini:FallbackModel` | No | Primary and fallback model names (defaults provided) |
| `Cloudinary:CloudName`, `Cloudinary:ApiKey`, `Cloudinary:ApiSecret` | For image upload | Car image storage |
| `Smtp:Host`, `Smtp:Port`, `Smtp:Username`, `Smtp:Password`, `Smtp:From` | For password reset | Sends the verification code email |
| `Authentication:Google:ClientId`, `Authentication:Google:ClientSecret` | No | Enables "Sign in with Google" (skipped when not set) |
| `Frontend:BaseUrl` | No | Frontend address for redirects and CORS (default `http://localhost:5173`) |

Frontend: `VITE_API_URL` (default `http://localhost:8080`).

---

## API Overview

| Area | Base path | Examples |
|---|---|---|
| Auth | `/api/auth` | `register`, `login`, `complete-profile`, `get-user-by-email`, `role-update` |
| Password reset | `/api/auth/forgot-password` | `check-email`, `verify-code`, `resend-code`, `reset` |
| Google login | `/oauth2` | `/oauth2/authorization/google`, `/oauth2/google/complete` |
| Cars | `/api/car` | list, add, update, rental status, active status |
| Companies | `/api/company` | list, add, update, active status |
| Bookings | `/api/booking` | create, list by email, start rent, end rent, set submitted |
| Payment | `/api/payment` | create payment for a booking |
| Submission | `/api/submission` | record a returned car |
| Images | `/api/images` | upload car images |
| AI | `/api/ai` | `ask` (customer assistant), `analyst` (admin only) |
| Health | `/api/health` | service check |

Protected endpoints expect the header `Authorization: Bearer <token>`.

---

## Design Decisions

- **DTOs instead of entities.** API responses use DTOs, so sensitive fields such as the password hash never leave the server.
- **The server is the source of truth.** Booking and payment amounts are checked on the backend against the car's real rate, so a modified browser request cannot change a price.
- **Secrets stay out of Git.** Keys and passwords live in user-secrets or environment variables, never in `appsettings.json`.
- **Migrations instead of hand-made SQL.** The database schema is versioned with EF Core migrations, so a new developer builds the same tables with one command.
- **AI with guardrails.** The analyst is limited to five read-only tools and does not send personal data to the model. Gemini calls have a retry, a fallback model, and a friendly failure message.
- **Password reset with limits.** The emailed 4-digit code allows at most 5 wrong attempts.

---

## Author

**Shuvo**: CSE graduate building toward a software engineering career.

- GitHub: [@shuvo37](https://github.com/shuvo37)
<!-- PLACEHOLDER: LinkedIn link -->
<!-- PLACEHOLDER: email or portfolio link -->

If you find this project useful, a star on the repository is appreciated.
