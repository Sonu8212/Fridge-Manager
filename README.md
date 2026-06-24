# 🧊 FridgeManager API

A smart fridge & pantry management REST API built with **ASP.NET Core 8**, following industry-standard clean architecture patterns.

Track your fridge items, get expiry notifications by email and real-time push, receive recipe suggestions for expiring food, auto-populate your shopping list, and forecast what you'll need next week or month — all scoped per authenticated user.

---

## ✨ Features

| Feature | Details |
|---|---|
| **Fridge Items** | Add items with name, category, quantity, unit, cost, purchase & expiry date |
| **Expiry Notifications** | Configurable reminder (X days before expiry) via email + SignalR push |
| **Recipe Suggestions** | Suggests meals using your expiring ingredients (Spoonacular API) |
| **Mark as Used** | Partial or full consumption tracking with auto shopping-list population |
| **Shopping List** | Auto-populated when items are fully used; manual additions supported |
| **Wastage Report** | Monthly report of expired/wasted items and total cost lost |
| **Consumption Forecast** | Predicts weekly/monthly quantity and cost based on consumption history |
| **Multi-user** | Every user sees only their own data — full data isolation |
| **Email Verification** | Account activation via email link before login is allowed |

---

## 🏗️ Architecture & Tech Stack

```
FridgeManager.Api/
├── Controllers/        # HTTP layer — thin, delegates to services
├── Services/           # Business logic (IFridgeItemService, IShoppingListService, ...)
├── Repositories/       # Data access layer (IFridgeItemRepository, ...)
├── Models/             # EF Core domain models + Identity user
├── DTOs/               # Request/response shapes
├── Validators/         # FluentValidation rules for all inputs
├── Jobs/               # Hangfire background jobs (ExpiryCheckJob)
├── Hubs/               # SignalR notification hub
├── Middleware/         # Global exception handler, Hangfire auth
├── Common/             # IDateTimeProvider, PagedResult<T>, Errors
└── Data/               # AppDbContext, EF migrations
```

**Key packages:**
- **EF Core + SQL Server** — persistence
- **ASP.NET Core Identity + JWT Bearer** — auth
- **MailKit** — email sending
- **Hangfire** — scheduled background jobs
- **SignalR** — real-time push notifications
- **FluentValidation** — input validation
- **ErrorOr** — Result pattern (no null returns)
- **Serilog** — structured logging with rolling file sink
- **Asp.Versioning** — API versioning (`/api/v1/`)
- **Swashbuckle** — Swagger UI with JWT support

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server or LocalDB (ships with Visual Studio)

### 1. Clone & configure

```bash
git clone https://github.com/Sonu8212/Fridge-Manager.git
cd Fridge-Manager
```

Open `appsettings.json` and fill in your values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FridgeManagerDb;Trusted_Connection=True;"
  },
  "Jwt": {
    "Secret": "YOUR_SECRET_KEY_MIN_32_CHARACTERS_LONG",
    "Issuer": "FridgeManager",
    "Audience": "FridgeManagerUsers",
    "ExpiryMinutes": "60"
  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "Username": "your-email@gmail.com",
    "Password": "your-gmail-app-password",
    "From": "your-email@gmail.com",
    "DisplayName": "FridgeManager"
  }
}
```

> **Gmail tip:** Enable 2FA on your Google account → go to [myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords) → generate an App Password → paste it as `Password`.

> **Spoonacular (optional):** Get a free API key at [spoonacular.com/food-api](https://spoonacular.com/food-api) for real recipe suggestions. Without it, fallback suggestions are used.

### 2. Run

```bash
dotnet run
```

The database is created and migrated automatically on first run (development mode).

Open **Swagger UI** at: `http://localhost:5058/swagger`

---

## 🔐 Authentication Flow

```
POST /api/v1/auth/register     →  creates account, sends verification email
GET  /api/v1/auth/verify-email →  click link from email to confirm account
POST /api/v1/auth/login        →  returns JWT (only works after email verified)
POST /api/v1/auth/resend-verification  →  resend the verification email
```

**Using the JWT in Swagger:**
1. Call `POST /auth/login` → copy the `accessToken`
2. Click **Authorize** (top right in Swagger)
3. Enter: `Bearer <your_token>`
4. All protected endpoints now work

---

## 📡 API Reference

All routes are versioned under `/api/v1/`. All endpoints except `/auth/*` require a valid JWT.

### Fridge Items

| Method | Route | Description |
|---|---|---|
| `GET` | `/fridge-items?page=1&pageSize=20` | List all active items (paginated) |
| `GET` | `/fridge-items/{id}` | Get single item |
| `POST` | `/fridge-items` | Add new item |
| `PUT` | `/fridge-items/{id}` | Update item |
| `DELETE` | `/fridge-items/{id}` | Delete item |
| `POST` | `/fridge-items/{id}/mark-used` | Record consumption (auto-adds to shopping list when empty) |
| `GET` | `/fridge-items/expiring?withinDays=7` | Items expiring soon |
| `GET` | `/fridge-items/expiring/recipes` | Recipe suggestions for expiring items |
| `GET` | `/fridge-items/reports/wastage?month=6&year=2026` | Monthly wastage report |
| `GET` | `/fridge-items/forecast` | Weekly/monthly consumption forecast |

#### Add item — example request body
```json
{
  "name": "Milk",
  "category": "Dairy",
  "quantity": 2,
  "unit": "L",
  "costPerUnit": 1.50,
  "purchaseDate": "2026-06-20T00:00:00Z",
  "expiryDate": "2026-07-05T00:00:00Z",
  "expiryReminderDays": 3
}
```

### Shopping List

| Method | Route | Description |
|---|---|---|
| `GET` | `/shopping-list` | All pending items |
| `POST` | `/shopping-list` | Add item manually |
| `POST` | `/shopping-list/{id}/purchased` | Mark as purchased (removes from list) |
| `DELETE` | `/shopping-list/{id}` | Remove item |

### Notifications

| Method | Route | Description |
|---|---|---|
| `GET` | `/notifications` | Last 50 notifications |
| `POST` | `/notifications/{id}/read` | Mark as read |
| `POST` | `/notifications/check-expiry` | Manually trigger expiry check (for testing) |

---

## 📧 Email Notifications

The daily expiry check job runs automatically at **08:00 UTC** via Hangfire.

When items are within the reminder window, each user receives:
- A **real-time push** via SignalR (if connected)
- A **single consolidated HTML email** listing all expiring items with a recipe suggestion per item

To trigger manually without waiting: `POST /api/v1/notifications/check-expiry`

---

## 🔔 Real-time Notifications (SignalR)

Connect to the hub at `/hubs/notifications` — pass your JWT as a query parameter:

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/notifications?access_token=YOUR_JWT")
  .build();

connection.on("ReceiveNotification", (title, message) => {
  console.log(title, message);
});

await connection.start();
```

---

## 📊 Hangfire Dashboard

Available at `/hangfire` (localhost-only in production).

View scheduled jobs, job history, and manually trigger jobs.

---

## 🏥 Health Check

```
GET /health
```

Returns `Healthy` / `Unhealthy` — checks SQL Server connectivity. Used by load balancers and Kubernetes probes.

---

## 🧪 Running Tests

```bash
cd ../FridgeManager.Tests
dotnet test
```

13 unit tests covering `FridgeItemService` and `ShoppingListService` — business logic is tested independently of the database using NSubstitute mocks.

---

## 📁 Supported Units of Measure

Any string is accepted. Common examples:

`kg` · `g` · `L` · `ml` · `pcs` · `dozen` · `box` · `bag` · `bottle` · `can`

---

## 🗺️ Roadmap

- [ ] Refresh tokens
- [ ] Password reset via email
- [ ] Push notifications (FCM / APNs)
- [ ] Barcode scanning integration
- [ ] Frontend (React / Vue)
- [ ] Docker + docker-compose setup
