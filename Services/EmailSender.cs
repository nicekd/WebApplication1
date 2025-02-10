using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");

            using (var client = new SmtpClient(smtpSettings["Server"], int.Parse(smtpSettings["Port"])))
            {
                client.Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]);
                client.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpSettings["SenderEmail"]),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
