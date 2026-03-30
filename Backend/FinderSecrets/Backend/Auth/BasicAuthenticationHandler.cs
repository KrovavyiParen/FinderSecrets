using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Auth
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BasicAuthenticationHandler> _logger;

        /// <summary>
        /// Конструктор обработчика
        /// </summary>
        /// <param name="options">Настройки аутентификации</param>
        /// <param name="logger">Логгер для записи событий</param>
        /// <param name="encoder">Кодировщик URL</param>
        /// <param name="clock">Системные часы</param>
        /// <param name="context">Контекст базы данных</param>
        public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, AppDbContext context)
            : base(options, logger, encoder)
        {
            _context = context;
            _logger = logger.CreateLogger<BasicAuthenticationHandler>();
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                _logger.LogWarning("Запрос без заголовка Authorization");
                return AuthenticateResult.Fail("Missing Authorization header");
            }

            var authorizationHeader = Request.Headers["Authorization"].ToString();
            
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Неверный формат заголовка Authorization");
                return AuthenticateResult.Fail("Invalid Authorization header format");
            }

            var encodedCredentials = authorizationHeader.Substring("Basic ".Length).Trim();
            
            string decodedCredentials;
            
            try
            {
                decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            }
            catch (FormatException)
            {
                _logger.LogWarning("Некорректная Base64 строка");
                return AuthenticateResult.Fail("Invalid Base64 encoding");
            }

            var colonIndex = decodedCredentials.IndexOf(':');
            if (colonIndex == -1)
            {
                return AuthenticateResult.Fail("Invalid credentials format");
            }

            var email = decodedCredentials.Substring(0, colonIndex);
            var password = decodedCredentials.Substring(colonIndex + 1);


            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                _logger.LogWarning($"Неудачная попытка входа для email: {email}");
                return AuthenticateResult.Fail("Invalid username or password");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.AuthenticationMethod, "Basic")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}