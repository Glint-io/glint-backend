// EmailTemplates.cs
// Place this in glint_backend/Services/ or a dedicated EmailTemplates/ folder.
// Call BuildVerificationEmail() from your email-sending service when a user registers
// or requests a new verification code.

namespace glint_backend.Services;

public static class EmailTemplates
{
    /// <summary>
    /// Builds the HTML body for the email-verification email.
    /// Both a direct "Verify email" button (href to your frontend) AND
    /// the raw 6-digit code are included, so users have two ways to verify.
    /// </summary>
    /// <param name="code">The 6-digit OTC code.</param>
    /// <param name="frontendBaseUrl">
    ///   Your Next.js origin, e.g. "https://glint.app".
    ///   The button will link to {frontendBaseUrl}/auth/verify-email?code={code}
    /// </param>
    /// <param name="expiresInMinutes">How long the code is valid (shown to user).</param>
    public static (string Subject, string HtmlBody, string PlainBody) BuildVerificationEmail(
        string code,
        string frontendBaseUrl,
        int expiresInMinutes = 30)
    {
        var verifyUrl = $"{frontendBaseUrl.TrimEnd('/')}/auth/verify-email?code={Uri.EscapeDataString(code)}";
        var subject = "Verify your Glint email";

        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <title>Verify your email</title>
              <style>
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background:#f9f6f0; margin:0; padding:32px 16px; }
                .card { background:#fff; max-width:480px; margin:0 auto; border-radius:16px; padding:40px 36px; box-shadow:0 2px 12px rgba(0,0,0,.08); }
                h1 { font-size:22px; color:#1a1208; margin:0 0 8px; }
                p { font-size:15px; color:#5a4e3a; line-height:1.6; margin:0 0 20px; }
                .code-box { background:#f3ede0; border:1px solid #e4d8c0; border-radius:10px; text-align:center; padding:20px; margin:24px 0; }
                .code { font-size:36px; font-weight:700; letter-spacing:10px; color:#1a1208; font-family:monospace; }
                .btn { display:inline-block; background:#e8a736; color:#2A1E0F; font-weight:600; font-size:15px; padding:14px 32px; border-radius:10px; text-decoration:none; margin:8px 0 24px; }
                .muted { font-size:13px; color:#998877; }
              </style>
            </head>
            <body>
              <div class="card">
                <h1>Verify your email</h1>
                <p>Thanks for signing up for Glint! Use one of the two options below to confirm your address.</p>
                <p><strong>Option 1 - click the button</strong> (easiest):</p>
                <a href="{{verifyUrl}}" class="btn">Verify email</a>
                <p><strong>Option 2 - enter the code</strong> in the app:</p>
                <div class="code-box">
                  <div class="code">{{code}}</div>
                </div>
                <p class="muted">This code expires in {{expiresInMinutes}} minutes. If you didn't create a Glint account, you can safely ignore this email.</p>
              </div>
            </body>
            </html>
            """;

        var plain = $"""
            Verify your Glint email
            -----------------------
            Option 1 – open this link in your browser:
            {verifyUrl}

            Option 2 – enter this 6-digit code in the app:
            {code}

            The code expires in {expiresInMinutes} minutes.
            If you didn't sign up for Glint, ignore this email.
            """;

        return (subject, html, plain);
    }
}