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
        int expiresInMinutes = 15)
    {
        var verifyUrl = $"{frontendBaseUrl.TrimEnd('/')}/auth/verify-email?code={Uri.EscapeDataString(code)}";
        var subject = "Verify your Glint email";

        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background:#f4f4f5;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f5;padding:40px 0;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,0.08);">

                    <!-- Header -->
                    <tr>
                      <td style="background:#18181b;padding:28px 40px;">
                        <p style="margin:0;font-size:22px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;">✦ Glint</p>
                      </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                      <td style="padding:40px;">
                        <h1 style="margin:0 0 8px;font-size:24px;font-weight:700;color:#18181b;letter-spacing:-0.4px;">Verify your email</h1>
                        <p style="margin:0 0 28px;font-size:15px;color:#52525b;line-height:1.6;">
                          Thanks for signing up for <strong style="color:#18181b;">Glint</strong>! Use one of the two options below to confirm your address.
                        </p>

                        <!-- Option 1 -->
                        <p style="margin:0 0 14px;font-size:14px;font-weight:600;color:#18181b;">Option 1 — click the button <span style="font-weight:400;color:#71717a;">(easiest)</span></p>
                        <table cellpadding="0" cellspacing="0" style="margin-bottom:32px;">
                          <tr>
                            <td style="background:#18181b;border-radius:8px;">
                              <a href="{{verifyUrl}}" style="display:inline-block;padding:13px 28px;font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;letter-spacing:-0.1px;">Verify email →</a>
                            </td>
                          </tr>
                        </table>

                        <!-- Divider -->
                        <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:28px;">
                          <tr>
                            <td style="border-top:1px solid #e4e4e7;"></td>
                          </tr>
                        </table>

                        <!-- Option 2 -->
                        <p style="margin:0 0 14px;font-size:14px;font-weight:600;color:#18181b;">Option 2 — enter the code in the app</p>
                        <div style="background:#f4f4f5;border:1px solid #e4e4e7;border-radius:10px;padding:20px;text-align:center;margin-bottom:32px;">
                          <p style="margin:0 0 4px;font-size:12px;font-weight:500;color:#71717a;letter-spacing:0.5px;text-transform:uppercase;">Your verification code</p>
                          <p style="margin:0;font-size:36px;font-weight:700;color:#18181b;letter-spacing:8px;font-variant-numeric:tabular-nums;">{{code}}</p>
                        </div>

                        <!-- Footer note -->
                        <p style="margin:0;font-size:13px;color:#a1a1aa;line-height:1.6;">
                          This code expires in <strong style="color:#71717a;">{{expiresInMinutes}} minutes</strong>. If you didn't create a Glint account, you can safely ignore this email.
                        </p>
                      </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                      <td style="background:#fafafa;border-top:1px solid #f0f0f0;padding:20px 40px;">
                        <p style="margin:0;font-size:12px;color:#a1a1aa;">© 2026 Glint · You're receiving this because you signed up.</p>
                      </td>
                    </tr>

                  </table>
                </td></tr>
              </table>
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