namespace glint_backend.Services;

public static class EmailTemplates
{
    public static (string Subject, string HtmlBody, string PlainBody) BuildVerificationEmail(
        string code, string frontendBaseUrl, int expiresInMinutes = 15)
    {
        var verifyUrl = $"{frontendBaseUrl.TrimEnd('/')}/auth/verify-email?code={Uri.EscapeDataString(code)}";
        var subject = "Verify your Glint email";

        var html = $$"""
            <!DOCTYPE html><html lang="en"><head><meta charset="UTF-8"></head>
            <body style="margin:0;padding:0;background:#f4f4f5;font-family:-apple-system,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f5;padding:40px 0;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,0.08);">
                    <tr><td style="background:#18181b;padding:28px 40px;">
                      <p style="margin:0;font-size:22px;font-weight:700;color:#fff;">✦ Glint</p>
                    </td></tr>
                    <tr><td style="padding:40px;">
                      <h1 style="margin:0 0 8px;font-size:24px;font-weight:700;color:#18181b;">Verify your email</h1>
                      <p style="margin:0 0 28px;font-size:15px;color:#52525b;line-height:1.6;">Use the button or code below to confirm your address.</p>
                      <table cellpadding="0" cellspacing="0" style="margin-bottom:32px;">
                        <tr><td style="background:#18181b;border-radius:8px;">
                          <a href="{{verifyUrl}}" style="display:inline-block;padding:13px 28px;font-size:15px;font-weight:600;color:#fff;text-decoration:none;">Verify email →</a>
                        </td></tr>
                      </table>
                      <div style="background:#f4f4f5;border:1px solid #e4e4e7;border-radius:10px;padding:20px;text-align:center;margin-bottom:32px;">
                        <p style="margin:0 0 4px;font-size:12px;color:#71717a;text-transform:uppercase;letter-spacing:0.5px;">Your code</p>
                        <p style="margin:0;font-size:36px;font-weight:700;color:#18181b;letter-spacing:8px;">{{code}}</p>
                      </div>
                      <p style="margin:0;font-size:13px;color:#a1a1aa;">Expires in {{expiresInMinutes}} minutes. Ignore if you didn't sign up.</p>
                    </td></tr>
                    <tr><td style="background:#fafafa;border-top:1px solid #f0f0f0;padding:20px 40px;">
                      <p style="margin:0;font-size:12px;color:#a1a1aa;">© 2026 Glint</p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;

        var plain = $"Verify your Glint email\n\nOpen this link: {verifyUrl}\n\nOr enter code: {code}\n\nExpires in {expiresInMinutes} minutes.";
        return (subject, html, plain);
    }

    public static (string Subject, string HtmlBody, string PlainBody) BuildPasswordResetEmail(
        string code, string frontendBaseUrl, int expiresInMinutes = 15)
    {
        var resetUrl = $"{frontendBaseUrl.TrimEnd('/')}/auth/reset-password?code={Uri.EscapeDataString(code)}";
        var subject = "Reset your Glint password";

        var html = $$"""
            <!DOCTYPE html><html lang="en"><head><meta charset="UTF-8"></head>
            <body style="margin:0;padding:0;background:#f4f4f5;font-family:-apple-system,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f5;padding:40px 0;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,0.08);">
                    <tr><td style="background:#18181b;padding:28px 40px;">
                      <p style="margin:0;font-size:22px;font-weight:700;color:#fff;">✦ Glint</p>
                    </td></tr>
                    <tr><td style="padding:40px;">
                      <h1 style="margin:0 0 8px;font-size:24px;font-weight:700;color:#18181b;">Reset your password</h1>
                      <p style="margin:0 0 28px;font-size:15px;color:#52525b;line-height:1.6;">Click the button or enter the code on the reset page.</p>
                      <table cellpadding="0" cellspacing="0" style="margin-bottom:32px;">
                        <tr><td style="background:#18181b;border-radius:8px;">
                          <a href="{{resetUrl}}" style="display:inline-block;padding:13px 28px;font-size:15px;font-weight:600;color:#fff;text-decoration:none;">Reset password →</a>
                        </td></tr>
                      </table>
                      <div style="background:#f4f4f5;border:1px solid #e4e4e7;border-radius:10px;padding:20px;text-align:center;margin-bottom:32px;">
                        <p style="margin:0 0 4px;font-size:12px;color:#71717a;text-transform:uppercase;letter-spacing:0.5px;">Your code</p>
                        <p style="margin:0;font-size:36px;font-weight:700;color:#18181b;letter-spacing:8px;">{{code}}</p>
                      </div>
                      <p style="margin:0;font-size:13px;color:#a1a1aa;">Expires in {{expiresInMinutes}} minutes. If you didn't request this, ignore it — your password won't change.</p>
                    </td></tr>
                    <tr><td style="background:#fafafa;border-top:1px solid #f0f0f0;padding:20px 40px;">
                      <p style="margin:0;font-size:12px;color:#a1a1aa;">© 2026 Glint</p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;

        var plain = $"Reset your Glint password\n\nOpen this link: {resetUrl}\n\nOr enter code: {code}\n\nExpires in {expiresInMinutes} minutes. Ignore if you didn't request this.";
        return (subject, html, plain);
    }
}