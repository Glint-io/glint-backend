using glint_backend.Data;
using glint_backend.Interfaces;
using glint_backend.Repositories;
using glint_backend.Repositories.Interfaces;
using glint_backend.Services;
using glint_backend.Services.Interfaces;
using glint_backend.Services.AnalysisServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace glint_backend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            if (builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddUserSecrets<Program>(optional: true);
            }

            // ── Database ──────────────────────────────────────────────────────────
            builder.Services.AddDbContext<AppDBContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ── Services & Repositories ───────────────────────────────────────────
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IOtcRepository, OtcRepository>();
            builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            // ── Authentication (JWT) ──────────────────────────────────────────────
            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT key is not configured.");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization();

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetRateLimitPartitionKey(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 120,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true,
                        }));

                options.AddPolicy("auth", context =>
                {
                    var key = GetRateLimitPartitionKey(context);
                    return RateLimitPartition.GetFixedWindowLimiter(
                        key,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 8,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true,
                        });
                });

                options.AddPolicy("analysis", context =>
                {
                    var key = GetRateLimitPartitionKey(context);
                    var permitLimit = context.User.Identity?.IsAuthenticated == true ? 10 : 4;

                    return RateLimitPartition.GetFixedWindowLimiter(
                        key,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromMinutes(3),
                            QueueLimit = 0,
                            AutoReplenishment = true,
                        });
                });
            });

            // ── Controllers & Swagger ─────────────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new() { Title = "Glint API", Version = "v1" });

                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter your JWT token. Example: Bearer eyJhbGci..."
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id   = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ── CORS ──────────────────────────────────────────────────────────────
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // ── Email (Resend) ────────────────────────────────────────────────────
            var resendApiKey = builder.Configuration["Resend:ApiKey"];
            if (!string.IsNullOrWhiteSpace(resendApiKey))
            {
                builder.Services.AddHttpClient("Resend", client =>
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", resendApiKey);
                });
            }
            else
            {
                builder.Services.AddHttpClient("Resend");
            }

            // ── Repositories ──────────────────────────────────────────────────────
            builder.Services.AddScoped<IResumeRepository, ResumeRepository>();
            builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();
            builder.Services.AddScoped<IJobAdvertisementRepository, JobAdvertisementRepository>();
            builder.Services.AddSingleton<IAnalysisRunLockService, AnalysisRunLockService>();

            // ── Analysis sub-services ─────────────────────────────────────────────
            builder.Services.AddScoped<IAiAnalysisService, AiAnalysisService>();
            builder.Services.AddScoped<IRuleBasedAnalysisService, RuleBasedAnalysisService>();
            builder.Services.AddScoped<IKeywordAnalysisService, KeywordAnalysisService>();

            // ── Services ─────────────────────────────────────────────────────────
            builder.Services.AddScoped<IFileValidationService, FileValidationService>();
            builder.Services.AddScoped<IResumeService, ResumeService>();
            builder.Services.AddScoped<IJobAdvertisementService, JobAdvertisementService>();
            builder.Services.AddScoped<IAnalysisService, AnalysisService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IPdfExtractionService, PdfExtractionService>();

            // ── Build ─────────────────────────────────────────────────────────────
            var app = builder.Build();

            // Run migrations automatically on startup (safe for Railway deployments)
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
                await db.Database.MigrateAsync();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Glint API v1"));
            }

            app.UseExceptionHandler(err => err.Run(async ctx =>
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.ContentType = "application/json";
                var error = ctx.Features.Get<IExceptionHandlerFeature>();
                await ctx.Response.WriteAsJsonAsync(new { error = error?.Error.Message });
            }));

            app.UseCors("AllowFrontend");
            app.UseRateLimiter();
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // ── Bind to Railway's PORT ────────────────────────────────────────────
            var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
            app.Run($"http://0.0.0.0:{port}");
        }

        private static string GetRateLimitPartitionKey(HttpContext context)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"user:{userId}";
            }

            return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        }
    }
}