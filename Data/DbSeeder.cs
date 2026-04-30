using glint_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace glint_backend.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDBContext context)
    {
        await context.Database.MigrateAsync();

        await context.AnalysisResults.ExecuteDeleteAsync();
        await context.Analyses.ExecuteDeleteAsync();
        await context.JobAdvertisements.ExecuteDeleteAsync();
        await context.Resumes.ExecuteDeleteAsync();
        await context.RefreshTokens.ExecuteDeleteAsync();
        await context.OneTimeCodes.ExecuteDeleteAsync();
        await context.Users.ExecuteDeleteAsync();

        // ── Users ─────────────────────────────────────────────────────────────
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();

        await context.Users.AddRangeAsync(
            new User
            {
                Id = userId1,
                Email = "alice@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            },
            new User
            {
                Id = userId2,
                Email = "bob@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password2!"),
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new User
            {
                Id = userId3,
                Email = "null@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password3!"),
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        );

        // ── Resumes ───────────────────────────────────────────────────────────
        var resumeId1 = Guid.NewGuid();
        var resumeId2 = Guid.NewGuid();
        var resumeId3 = Guid.NewGuid();

        var placeholderPdf = System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 placeholder resume content");

        await context.Resumes.AddRangeAsync(
            new Resume { Id = resumeId1, UserId = userId1, FileName = "alice_resume_v1.pdf", FileData = placeholderPdf, UploadedAt = DateTime.UtcNow.AddDays(-55) },
            new Resume { Id = resumeId2, UserId = userId1, FileName = "alice_resume_v2.pdf", FileData = placeholderPdf, UploadedAt = DateTime.UtcNow.AddDays(-20) },
            new Resume { Id = resumeId3, UserId = userId2, FileName = "bob_resume.pdf", FileData = placeholderPdf, UploadedAt = DateTime.UtcNow.AddDays(-28) }
        );

        // ── Job Advertisements ────────────────────────────────────────────────
        var jobAdIds = new[]
        {
            (Id: Guid.NewGuid(), UserId: userId1, Text: "Senior .NET Developer – 5+ years C#, ASP.NET Core, EF Core and REST APIs required. Azure experience a plus."),
            (Id: Guid.NewGuid(), UserId: userId1, Text: "Backend Engineer at a fintech startup. C#, Kafka, PostgreSQL, Docker. Strong system design skills needed."),
            (Id: Guid.NewGuid(), UserId: userId1, Text: "Lead Software Engineer. Team leadership, .NET 8, microservices architecture, CI/CD pipelines."),
            (Id: Guid.NewGuid(), UserId: userId1, Text: "Software Engineer – full stack. React, TypeScript, .NET, SQL Server. Agile environment."),
            (Id: Guid.NewGuid(), UserId: userId1, Text: "Platform Engineer. Kubernetes, Terraform, .NET services, strong DevOps background required."),
            (Id: Guid.NewGuid(), UserId: userId2, Text: "Junior Frontend Developer – React, TypeScript and Tailwind CSS. Fully remote role."),
            (Id: Guid.NewGuid(), UserId: userId2, Text: "Frontend Engineer – Next.js, REST API integration, responsive design, Git workflows."),
        };

        await context.JobAdvertisements.AddRangeAsync(jobAdIds.Select(j => new JobAdvertisement
        {
            Id = j.Id,
            UserId = j.UserId,
            RawText = j.Text,
            CreatedAt = DateTime.UtcNow.AddDays(-50)
        }));

        // ── Analyses + Results ────────────────────────────────────────────────
        // (daysAgo, resumeId, jobAdIndex, label, aiScore, ruleScore, keywordScore)
        var aliceRuns = new[]
        {
            (DaysAgo: 50, ResumeId: resumeId1, JobAdIndex: 0, Label: "Senior .NET – first attempt",    AiScore: 55m, RuleScore: 50m, KwScore: 30m),
            (DaysAgo: 44, ResumeId: resumeId1, JobAdIndex: 0, Label: "Senior .NET – second attempt",   AiScore: 62m, RuleScore: 58m, KwScore: 38m),
            (DaysAgo: 38, ResumeId: resumeId1, JobAdIndex: 1, Label: "Fintech backend role",           AiScore: 59m, RuleScore: 54m, KwScore: 33m),
            (DaysAgo: 31, ResumeId: resumeId1, JobAdIndex: 2, Label: "Lead Engineer application",      AiScore: 66m, RuleScore: 61m, KwScore: 42m),
            (DaysAgo: 25, ResumeId: resumeId2, JobAdIndex: 2, Label: "Lead Engineer – updated CV",     AiScore: 74m, RuleScore: 70m, KwScore: 55m),
            (DaysAgo: 18, ResumeId: resumeId2, JobAdIndex: 3, Label: "Full stack position",            AiScore: 78m, RuleScore: 72m, KwScore: 58m),
            (DaysAgo: 12, ResumeId: resumeId2, JobAdIndex: 0, Label: "Senior .NET – third attempt",    AiScore: 80m, RuleScore: 76m, KwScore: 62m),
            (DaysAgo:  7, ResumeId: resumeId2, JobAdIndex: 4, Label: "Platform Engineer stretch role", AiScore: 69m, RuleScore: 65m, KwScore: 48m),
            (DaysAgo:  3, ResumeId: resumeId2, JobAdIndex: 3, Label: "Full stack – final polish",      AiScore: 83m, RuleScore: 79m, KwScore: 67m),
            (DaysAgo:  2, ResumeId: resumeId2, JobAdIndex: 0, Label: "Senior .NET – best version",     AiScore: 82.5m, RuleScore: 78m, KwScore: 40m),
            (DaysAgo:  0, ResumeId: resumeId2, JobAdIndex: 0, Label: "Senior .NET – best version",     AiScore: 100m, RuleScore: 100m, KwScore: 100m),
        };

        var bobRuns = new[]
        {
            (DaysAgo: 28, ResumeId: resumeId3, JobAdIndex: 5, Label: "Junior Frontend – first try",    AiScore: 48m, RuleScore: 44m, KwScore: 35m),
            (DaysAgo: 21, ResumeId: resumeId3, JobAdIndex: 5, Label: "Junior Frontend – revised",      AiScore: 57m, RuleScore: 52m, KwScore: 41m),
            (DaysAgo: 14, ResumeId: resumeId3, JobAdIndex: 6, Label: "Next.js role application",       AiScore: 63m, RuleScore: 59m, KwScore: 46m),
            (DaysAgo:  7, ResumeId: resumeId3, JobAdIndex: 6, Label: "Next.js – updated keywords",     AiScore: 68m, RuleScore: 63m, KwScore: 52m),
        };

        foreach (var r in aliceRuns)
        {
            await AddAnalysisWithResults(context, userId1, r.ResumeId, jobAdIds[r.JobAdIndex].Id, r.Label, r.DaysAgo, r.AiScore, r.RuleScore, r.KwScore);
        }

        foreach (var r in bobRuns)
        {
            await AddAnalysisWithResults(context, userId2, r.ResumeId, jobAdIds[r.JobAdIndex].Id, r.Label, r.DaysAgo, r.AiScore, r.RuleScore, r.KwScore);
        }

        await context.SaveChangesAsync();
    }

    private static async Task AddAnalysisWithResults(
        AppDBContext context,
        Guid userId,
        Guid resumeId,
        Guid jobAdId,
        string label,
        int daysAgo,
        decimal aiScore,
        decimal ruleScore,
        decimal kwScore)
    {
        var analysisId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-daysAgo);
        var completedAt = createdAt.AddSeconds(8);

        await context.Analyses.AddAsync(new Analysis
        {
            Id = analysisId,
            UserId = userId,
            ResumeId = resumeId,
            JobAdvertisementId = jobAdId,
            Label = label,
            CreatedAt = createdAt,
            Status = AnalysisStatus.Completed
        });

        await context.AnalysisResults.AddRangeAsync(
            new AnalysisResult { Id = Guid.NewGuid(), AnalysisId = analysisId, Method = AnalysisMethod.AI, Score = aiScore, Feedback = FeedbackFor(AnalysisMethod.AI, aiScore), CompletedAt = completedAt },
            new AnalysisResult { Id = Guid.NewGuid(), AnalysisId = analysisId, Method = AnalysisMethod.RuleBased, Score = ruleScore, Feedback = FeedbackFor(AnalysisMethod.RuleBased, ruleScore), CompletedAt = completedAt },
            new AnalysisResult { Id = Guid.NewGuid(), AnalysisId = analysisId, Method = AnalysisMethod.Keyword, Score = kwScore, Feedback = FeedbackFor(AnalysisMethod.Keyword, kwScore), CompletedAt = completedAt }
        );
    }

    private static string FeedbackFor(AnalysisMethod method, decimal score) => (method, score) switch
    {
        (AnalysisMethod.AI, >= 80) => "Strong semantic match. Profile aligns well with the role requirements.",
        (AnalysisMethod.AI, >= 65) => "Good conceptual fit. Consider expanding on relevant project experience.",
        (AnalysisMethod.AI, _) => "Moderate alignment. Tailor your summary section more closely to the job description.",

        (AnalysisMethod.RuleBased, >= 75) => "Meets most rule-based criteria. Leadership examples would strengthen the application.",
        (AnalysisMethod.RuleBased, >= 60) => "Several criteria met. Missing some expected qualifications — consider addressing them directly.",
        (AnalysisMethod.RuleBased, _) => "Key criteria gaps detected. Review the job requirements and address missing areas explicitly.",

        (AnalysisMethod.Keyword, >= 60) => "Good keyword coverage. A few domain-specific terms are still missing.",
        (AnalysisMethod.Keyword, >= 45) => "Moderate keyword overlap. Add more specific technical terms from the job ad.",
        (AnalysisMethod.Keyword, _) => "Low keyword match. Mirror the job ad language more closely in your CV.",

        _ => "Analysis complete."
    };
}