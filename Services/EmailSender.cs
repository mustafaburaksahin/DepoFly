using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace DepoFly.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var mail = "depofly.destek@gmail.com";
            var sifre = "zhyd tsni jgyr qhtx";

            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false; 
                client.Credentials = new NetworkCredential(mail, sifre);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;

                var mesaj = new MailMessage
                {
                    From = new MailAddress(mail, "DepoFly Destek"), 
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mesaj.To.Add(email);

                await client.SendMailAsync(mesaj);
            }
            catch (Exception ex)
            {
                
                System.Diagnostics.Debug.WriteLine("Mail Hatası: " + ex.Message);
                throw; 
            }
        }
    }
}