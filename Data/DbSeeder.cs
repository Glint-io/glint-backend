using glint_backend.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace glint_backend.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDBContext context)
    {
        // Ensure DB is up to date before seeding
        await context.Database.MigrateAsync();

        if (await context.Users.AnyAsync())
            return;

        // ── Users ────────────────────────────────────────────────────────────
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var users = new[]
        {
            new User
            {
                Id             = userId1,
                Email          = "alice@example.com",
                // Replace with real BCrypt hashes in production
                PasswordHash   = BCrypt.Net.BCrypt.HashPassword("Password1!"),
                IsEmailVerified = true,
                CreatedAt      = DateTime.UtcNow
            },
            new User
            {
                Id             = userId2,
                Email          = "bob@example.com",
                PasswordHash   = BCrypt.Net.BCrypt.HashPassword("Password2!"),
                IsEmailVerified = false,
                CreatedAt      = DateTime.UtcNow
            }
        };

        await context.Users.AddRangeAsync(users);

        // ── Job Advertisements ────────────────────────────────────────────────
        var jobAdId1 = Guid.NewGuid();
        var jobAdId2 = Guid.NewGuid();

        var jobAds = new[]
        {
            new JobAdvertisement
            {
                Id        = jobAdId1,
                UserId    = userId1,
                RawText   = "We are looking for a Senior .NET Developer with 5+ years of experience in C# and ASP.NET Core. Strong knowledge of EF Core and REST APIs required.",
                CreatedAt = DateTime.UtcNow
            },
            new JobAdvertisement
            {
                Id        = jobAdId2,
                UserId    = userId2,
                RawText   = "Junior Frontend Developer position open. React, TypeScript and Tailwind CSS experience preferred. Fully remote role.",
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.JobAdvertisements.AddRangeAsync(jobAds);

        // ── Resumes ───────────────────────────────────────────────────────────
        var resumeId1 = Guid.NewGuid();
        var resumeId2 = Guid.NewGuid();

        // Minimal placeholder PDF bytes (keeps the seeder self-contained)
        var placeholderPdf = System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 placeholder resume content");

        var resumes = new[]
        {
            new Resume
            {
                Id         = resumeId1,
                UserId     = userId1,
                FileName   = "alice_resume.pdf",
                FileData   = placeholderPdf,
                UploadedAt = DateTime.UtcNow
            },
            new Resume
            {
                Id         = resumeId2,
                UserId     = userId2,
                FileName   = "bob_resume.pdf",
                FileData   = placeholderPdf,
                UploadedAt = DateTime.UtcNow
            }
        };

        await context.Resumes.AddRangeAsync(resumes);

        // ── Analyses ──────────────────────────────────────────────────────────
        var analysisId1 = Guid.NewGuid();
        var analysisId2 = Guid.NewGuid();

        var analyses = new[]
        {
            new Analysis
            {
                Id                 = analysisId1,
                UserId             = userId1,
                ResumeId           = resumeId1,
                JobAdvertisementId = jobAdId1,
                Label              = "Senior .NET role – Q1 2026",
                CreatedAt          = DateTime.UtcNow,
                Status             = AnalysisStatus.Completed
            },
            new Analysis
            {
                Id                 = analysisId2,
                UserId             = userId2,
                ResumeId           = resumeId2,
                JobAdvertisementId = jobAdId2,
                Label              = "Frontend internship attempt",
                CreatedAt          = DateTime.UtcNow,
                Status             = AnalysisStatus.Pending
            }
        };

        await context.Analyses.AddRangeAsync(analyses);

        // ── Analysis Results ──────────────────────────────────────────────────
        var analysisResults = new[]
        {
            new AnalysisResult
            {
                Id          = Guid.NewGuid(),
                AnalysisId  = analysisId1,
                Method      = AnalysisMethod.AI,
                Score       = 82.50m,
                Feedback    = "Strong technical match. Consider highlighting cloud experience more prominently.",
                CompletedAt = DateTime.UtcNow
            },
            new AnalysisResult
            {
                Id          = Guid.NewGuid(),
                AnalysisId  = analysisId1,
                Method      = AnalysisMethod.RuleBased,
                Score       = 78.00m,
                Feedback    = "Good overall fit. Leadership examples would strengthen the application.",
                CompletedAt = DateTime.UtcNow
            },
            new AnalysisResult
            {
                Id          = Guid.NewGuid(),
                AnalysisId  = analysisId2,
                Method      = AnalysisMethod.Keyword,
                Score       = null,
                Feedback    = null,
                CompletedAt = null
            }
        };

        await context.AnalysisResults.AddRangeAsync(analysisResults);

        await context.SaveChangesAsync();
    }
}