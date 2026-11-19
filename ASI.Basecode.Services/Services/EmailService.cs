using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;

namespace ASI.Basecode.Services.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Send(string to, string subject, string htmlBody)
        {
            var host = _configuration["Smtp:Host"];
            var port = int.Parse(_configuration["Smtp:Port"]);
            var from = _configuration["Smtp:From"];

            using var client = new SmtpClient(host, port) { EnableSsl = false };
            using var message = new MailMessage(from, to, subject, htmlBody) { IsBodyHtml = true };
            client.Send(message);
        }
    }
}