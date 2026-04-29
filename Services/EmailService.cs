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
        }

    }
}