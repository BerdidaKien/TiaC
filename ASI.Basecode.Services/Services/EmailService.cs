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
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public void Send(string to, string subject, string htmlBody)
        {
            var host = _config["Smtp:Host"];
            var port = int.Parse(_config["Smtp:Port"]);
            var from = _config["Smtp:From"];

            using var client = new SmtpClient(host, port) { EnableSsl = false };
            using var message = new MailMessage(from, to, subject, htmlBody) { IsBodyHtml = true };
            client.Send(message);
        }
    }
}