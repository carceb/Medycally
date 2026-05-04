using System.Net;
using System.Net.Mail;

namespace Medycally.Core
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config) => _config = config;

        public async Task SendActivationEmailAsync(string toEmail, string userName, string activationUrl)
        {
            var fromEmail = _config["Email:FromEmail"]!;
            var fromName  = _config["Email:FromName"] ?? "Medycally";
            var password  = _config["Email:Password"]!;
            var smtpHost  = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort  = int.Parse(_config["Email:SmtpPort"] ?? "587");

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl   = true,
                Credentials = new NetworkCredential(fromEmail, password)
            };

            using var mail = new MailMessage
            {
                From       = new MailAddress(fromEmail, fromName),
                Subject    = "Bienvenido a Medycally – Activa tu cuenta",
                Body       = BuildHtml(userName, activationUrl),
                IsBodyHtml = true
            };
            mail.To.Add(new MailAddress(toEmail, userName));

            await client.SendMailAsync(mail);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetUrl)
        {
            var fromEmail = _config["Email:FromEmail"]!;
            var fromName  = _config["Email:FromName"] ?? "Medycally";
            var password  = _config["Email:Password"]!;
            var smtpHost  = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort  = int.Parse(_config["Email:SmtpPort"] ?? "587");

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl   = true,
                Credentials = new NetworkCredential(fromEmail, password)
            };

            using var mail = new MailMessage
            {
                From       = new MailAddress(fromEmail, fromName),
                Subject    = "Restablece tu contraseña — Medycally",
                Body       = BuildResetHtml(userName, resetUrl),
                IsBodyHtml = true
            };
            mail.To.Add(new MailAddress(toEmail, userName));

            await client.SendMailAsync(mail);
        }

        private static string BuildResetHtml(string userName, string resetUrl) => $"""
            <!DOCTYPE html>
            <html lang="es">
            <body style="font-family:Inter,Arial,sans-serif;background:#f1f5f9;margin:0;padding:40px 0;">
              <div style="max-width:560px;margin:0 auto;background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.08);">
                <div style="background:linear-gradient(135deg,#10b981,#059669);padding:40px 48px;text-align:center;">
                  <div style="font-size:2rem;font-weight:700;color:#fff;">
                    <span style="margin-right:8px;">&#10084;&#65039;</span>Medycally
                  </div>
                </div>
                <div style="padding:40px 48px;">
                  <h2 style="color:#1e293b;margin:0 0 8px;">Restablecer contraseña</h2>
                  <p style="color:#64748b;margin:0 0 8px;">Hola <strong>{EscapeHtml(userName)}</strong>,</p>
                  <p style="color:#64748b;margin:0 0 24px;">Recibimos una solicitud para restablecer la contraseña de tu cuenta. Haz clic en el botón para crear una nueva.</p>
                  <div style="text-align:center;margin:32px 0;">
                    <a href="{resetUrl}"
                       style="background:linear-gradient(135deg,#10b981,#059669);color:#fff;padding:14px 32px;border-radius:10px;text-decoration:none;font-weight:600;font-size:1rem;display:inline-block;">
                      Restablecer contraseña
                    </a>
                  </div>
                  <p style="color:#94a3b8;font-size:.85rem;margin:0;">Este enlace expira en <strong>2 horas</strong>. Si no solicitaste este cambio, ignora este correo — tu contraseña no se modificará.</p>
                </div>
                <div style="background:#f8fafc;padding:20px 48px;text-align:center;color:#94a3b8;font-size:.8rem;">
                  © {DateTime.Now.Year} Medycally. Todos los derechos reservados.
                </div>
              </div>
            </body>
            </html>
            """;

        private static string BuildHtml(string userName, string activationUrl) => $"""
            <!DOCTYPE html>
            <html lang="es">
            <body style="font-family:Inter,Arial,sans-serif;background:#f1f5f9;margin:0;padding:40px 0;">
              <div style="max-width:560px;margin:0 auto;background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.08);">
                <div style="background:linear-gradient(135deg,#10b981,#059669);padding:40px 48px;text-align:center;">
                  <div style="font-size:2rem;font-weight:700;color:#fff;">
                    <span style="margin-right:8px;">&#10084;&#65039;</span>Medycally
                  </div>
                </div>
                <div style="padding:40px 48px;">
                  <h2 style="color:#1e293b;margin:0 0 8px;">¡Bienvenido, {EscapeHtml(userName)}!</h2>
                  <p style="color:#64748b;margin:0 0 24px;">Tu cuenta en <strong>Medycally</strong> ha sido creada. Haz clic en el botón para activarla y establecer tu contraseña.</p>
                  <div style="text-align:center;margin:32px 0;">
                    <a href="{activationUrl}"
                       style="background:linear-gradient(135deg,#10b981,#059669);color:#fff;padding:14px 32px;border-radius:10px;text-decoration:none;font-weight:600;font-size:1rem;display:inline-block;">
                      Activar mi cuenta
                    </a>
                  </div>
                  <p style="color:#94a3b8;font-size:.85rem;margin:0;">Este enlace expira en <strong>7 días</strong>. Si no esperabas este correo, puedes ignorarlo.</p>
                </div>
                <div style="background:#f8fafc;padding:20px 48px;text-align:center;color:#94a3b8;font-size:.8rem;">
                  © {DateTime.Now.Year} Medycally. Todos los derechos reservados.
                </div>
              </div>
            </body>
            </html>
            """;

        private static string EscapeHtml(string text) =>
            text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
