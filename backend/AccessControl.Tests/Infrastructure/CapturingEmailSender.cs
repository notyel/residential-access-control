using AccessControl.Application.Security;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace AccessControl.Tests.Infrastructure
{
    public sealed record SentEmail(string To, string Subject, string Body);

    /// <summary>Guarda los correos en memoria para que las pruebas lean los enlaces que llevan.</summary>
    public partial class CapturingEmailSender : IEmailSender
    {
        private readonly ConcurrentQueue<SentEmail> _sent = new();

        public IReadOnlyCollection<SentEmail> Sent => _sent;

        public Task SendAsync(string to, string subject, string body)
        {
            _sent.Enqueue(new SentEmail(to, subject, body));
            return Task.CompletedTask;
        }

        /// <summary>Token del último enlace enviado a ese correo cuya ruta sea la indicada; null si no hay.</summary>
        public string? LastToken(string email, string path)
        {
            var mail = _sent.LastOrDefault(m =>
                m.To.Equals(email, StringComparison.OrdinalIgnoreCase) && m.Body.Contains($"/{path}?token="));
            if (mail == null) return null;

            var match = TokenPattern().Match(mail.Body);
            return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
        }

        [GeneratedRegex(@"\?token=([^\s]+)")]
        private static partial Regex TokenPattern();
    }
}
