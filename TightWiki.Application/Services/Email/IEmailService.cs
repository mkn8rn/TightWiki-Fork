namespace BLL.Services.Email
{
    /// <summary>
    /// Service interface for email operations.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email asynchronously.
        /// </summary>
        /// <param name="email">The recipient email address.</param>
        /// <param name="subject">The email subject.</param>
        /// <param name="htmlMessage">The HTML body of the email.</param>
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
