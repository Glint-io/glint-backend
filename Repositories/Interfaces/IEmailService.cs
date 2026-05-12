namespace glint_backend.Repositories.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody, string plainBody);
    }
}
