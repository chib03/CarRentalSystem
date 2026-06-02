using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CarRentalSystem.Services
{
    public class EmailService
    {
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            try
            {
                var message = new MimeMessage();
                
                message.From.Add(new MailboxAddress("Car Rental", "jacobambat03@gmail.com"));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlContent };
                message.Body = bodyBuilder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync("jacobambat03@gmail.com", "dnhpalczwntnecaj");
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> SendOtpEmailAsync(string toEmail, string name, string otp)
        {
            string subject = "Your Verification Code";
            string body = $"<h3>Hello {name},</h3><p>Your OTP is: <b>{otp}</b></p><p>This expires in 10 minutes.</p>";

            return await SendEmailAsync(toEmail, subject, body);
        }
    }
}