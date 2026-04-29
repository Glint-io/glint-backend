using glint_backend.Repositories.Interfaces;
using System.Text;
using System.Text.Json;

namespace glint_backend.Services
{
    public sealed class EmailDeliveryException : Exception
    {
        public int SmtpStatusCode { get; }

        public EmailDeliveryException(string message, int smtpStatusCode, Exception? inner = null)
            : base(message, inner)
        {
            SmtpStatusCode = smtpStatusCode;
        }
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public EmailService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _http = httpClientFactory.CreateClient("Resend");
        }

        public async Task SendAsync(string to, string subject, string htmlBody, string plainBody)
        {
            var payload = new
            {
                from = _config["Email:From"],
                to = new[] { to },
                subject,
                html = htmlBody,
                text = plainBody
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload, _json),
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage res;

            try
            {
                res = await _http.PostAsync("https://api.resend.com/emails", content);
            }
            catch (HttpRequestException ex)
            {
                throw new EmailDeliveryException(
                    "An unexpected error occurred while communicating with the mail server.",
                    0,
                    ex);
            }

            if (res.IsSuccessStatusCode) return;

            // Resend returns structured JSON errors — surface them properly.
            var body = await res.Content.ReadAsStringAsync();
            var errorMessage = TryExtractResendError(body, to, (int)res.StatusCode);

            throw new EmailDeliveryException(errorMessage, (int)res.StatusCode);
        }

        private static string TryExtractResendError(string body, string to, int statusCode)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var message = root.TryGetProperty("message", out var m) ? m.GetString() : null;

                // Return Resend's actual message so nothing gets lost in translation
                return message ?? $"The email could not be delivered to '{to}' (status {statusCode}).";
            }
            catch
            {
                return $"The email could not be delivered to '{to}' (status {statusCode}).";
            }
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_config["Email:From"]));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            // Port 465 = SSL, not StartTls
            await smtp.ConnectAsync(_config["Email:Host"], int.Parse(_config["Email:Port"]!), SecureSocketOptions.SslOnConnect);
            await smtp.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        public static string BuildVerificationEmail(string verifyUrl, string code)
        {
            return $"""
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
                              <a href="{verifyUrl}" style="display:inline-block;padding:13px 28px;font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;letter-spacing:-0.1px;">Verify email →</a>
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
                          <p style="margin:0;font-size:36px;font-weight:700;color:#18181b;letter-spacing:8px;font-variant-numeric:tabular-nums;">{code}</p>
                        </div>

                        <!-- Footer note -->
                        <p style="margin:0;font-size:13px;color:#a1a1aa;line-height:1.6;">
                          This code expires in <strong style="color:#71717a;">15 minutes</strong>. If you didn't create a Glint account, you can safely ignore this email.
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
        }
    }
}