# glint-backend

ASP.NET Core 8 API for [Glint](../README.md). Handles auth, resume storage, job advertisement management, and three-engine resume analysis over SSE.

## Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local, Docker, or Azure)
- [Google Gemini API key](https://aistudio.google.com/)
- [Resend](https://resend.com/) account (email delivery)

### Install and run

```bash
dotnet restore
dotnet ef database update
dotnet run
```

Swagger UI is available at `https://localhost:7248/swagger` in development.

### Configuration

All secrets go through [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) in development. Never commit `appsettings.json` with real values — it is gitignored.

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=glint;Trusted_Connection=True;"
dotnet user-secrets set "Jwt:Key"            "your-secret-min-32-characters-long"
dotnet user-secrets set "Jwt:Issuer"         "glint-backend"
dotnet user-secrets set "Jwt:Audience"       "glint-frontend"
dotnet user-secrets set "Jwt:ExpiryMinutes"  "60"
dotnet user-secrets set "Gemini:ApiKey"      "your-gemini-api-key"
dotnet user-secrets set "Resend:ApiKey"      "your-resend-api-key"
dotnet user-secrets set "Email:From"         "noreply@yourdomain.com"
dotnet user-secrets set "Frontend:BaseUrl"   "http://localhost:3000"
dotnet user-secrets set "Cors:AllowedOrigins:0" "http://localhost:3000"
```

### Database

```bash
# Apply all pending migrations
dotnet ef database update

# Create a new migration after model changes
dotnet ef migrations add YourMigrationName

# Seed with sample data (toggle in Program.cs)
# Uncomment: await DbSeeder.SeedAsync(db);
```

---

## Architecture

The project follows a layered pattern: controllers call services, services call repositories, repositories talk to the database via EF Core.

```
Controllers/
├── AuthController.cs           # /auth — register, login, verify, refresh, reset
├── Public/
│   ├── AnalysisController.cs   # /analyze — guest + authenticated, SSE stream
│   └── PingController.cs       # /api/ping — health check
└── User/
    └── UserController.cs       # /user — history, stats, resumes, job ads, account

Services/
├── AuthService.cs              # JWT generation, OTC flows, password hashing
├── AnalysisService.cs          # Orchestrates all three analysis methods
├── ResumeService.cs            # PDF validation, upload limits, storage
├── JobAdvertisementService.cs  # Deduplication, 5-ad cap, archive logic
├── UserService.cs              # History, statistics, account deletion
├── EmailService.cs             # Resend HTTP client
├── PdfExtractionService.cs     # PdfPig text extraction
├── FileValidationService.cs    # Size, MIME, magic bytes, malicious content scan
└── AnalysisServices/
    ├── AiAnalysisService.cs         # Gemini API call, score extraction, retry on 429
    ├── KeywordAnalysisService.cs    # Vocabulary-rarity keyword extraction and scoring
    └── RuleBasedAnalysisService.cs  # Structured criteria checks
```

---

## Analysis methods

### AI semantic (`/analyze/ai`)
Sends resume text and job ad to Gemini (`gemini-3.1-flash-lite-preview`). The prompt asks for a gap analysis structured as missing elements and actionable fixes, with a `Match Score:` line the service extracts via regex. Retries once on 429 before surfacing an `AiServiceUnavailableException`.

### Keyword matching (`/analyze/keyword`)
Extracts terms from the job ad's requirements section using two signals: rarity (anything not in a ~500-word common English vocabulary list) and repetition (any token appearing 2+ times gets double weight). Bigrams require both tokens to be important. The CV is checked against extracted tokens and bigrams; result is a JSON payload with matched terms, missing terms, and a tip.

### Rule-based (`/analyze/rules`)
Checks structured criteria against the resume text: years of experience patterns, seniority indicators, required certifications, and resume structure. Returns per-rule pass/fail with weights so critical gaps score differently from minor ones.

### SSE stream (`/analyze/stream`)
All three run concurrently via `Task.WhenAll` fed into a `Channel<AnalysisStreamEvent>`. Each result is written as an SSE `data:` line as it completes, so the frontend can render scores progressively. Sends `data: [DONE]` when all three finish.

---

## Auth flow

1. **Register** — creates user, generates a 6-digit OTC, sends verification email via Resend. If email delivery fails, the user and code are rolled back.
2. **Verify email** — OTC marked used, `IsEmailVerified` set to true.
3. **Login** — bcrypt password check, returns short-lived JWT + 7-day refresh token.
4. **Refresh** — old refresh token revoked, new pair issued.
5. **Forgot / reset password** — new OTC type (`PasswordReset`), separate email template, same expiry logic.

Refresh tokens are stored in the database and revoked on use. All OTCs expire after 15 minutes.

---

## Limits

| Resource | Limit |
|---|---|
| Resumes per user | 5 |
| Job advertisements per user | 5 (oldest archived when exceeded) |
| PDF upload size | 5 MB |
| Job ad text length | 10,000 characters |
| PDF scan | Blocks `/JavaScript`, `/JS`, `/OpenAction`, `/Launch`, `/EmbeddedFile`, XFA, and common obfuscation patterns |

---

## Dependencies

| Package | Purpose |
|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | ORM + migrations |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT validation |
| `BCrypt.Net-Next` | Password hashing |
| `Google.GenAI` | Gemini API client |
| `PdfPig` | PDF text extraction |
| `Swashbuckle.AspNetCore` | Swagger UI |
