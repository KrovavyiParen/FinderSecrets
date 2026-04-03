using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Backend.Auth
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ILogger<BasicAuthenticationHandler> _logger;
        
        private const string VALID_USERNAME = "admin";
        private const string VALID_PASSWORD = "admin123";
        private const string VALID_EMAIL = "admin@example.com";
        private const string VALID_USER_ID = "11111111-1111-1111-1111-111111111111";

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
            _logger = logger.CreateLogger<BasicAuthenticationHandler>();
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                _logger.LogWarning("Запрос без заголовка Authorization");
                return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));
            }

            var authorizationHeader = Request.Headers["Authorization"].ToString();
            
            if (string.IsNullOrEmpty(authorizationHeader) || 
                !authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Неверный формат заголовка Authorization");
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header format"));
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
                return Task.FromResult(AuthenticateResult.Fail("Invalid Base64 encoding"));
            }

            var colonIndex = decodedCredentials.IndexOf(':');
            if (colonIndex == -1)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid credentials format"));
            }

            var username = decodedCredentials.Substring(0, colonIndex);
            var password = decodedCredentials.Substring(colonIndex + 1);

            if (username != VALID_USERNAME || password != VALID_PASSWORD)
            {
                _logger.LogWarning($"Неудачная попытка входа для username: {username}");
                return Task.FromResult(AuthenticateResult.Fail("Invalid username or password"));
            }

            _logger.LogInformation($"Пользователь {username} успешно аутентифицирован");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, VALID_USER_ID),
                new Claim(ClaimTypes.Name, VALID_USERNAME),
                new Claim(ClaimTypes.Email, VALID_EMAIL),
                new Claim(ClaimTypes.AuthenticationMethod, "Basic")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers["WWW-Authenticate"] = "Basic realm=\"FinderSecrets API\"";
            Response.StatusCode = 401;
            await Response.WriteAsync("Unauthorized.");
            _logger.LogWarning("HandleChallengeAsync called");
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 403;
            await Response.WriteAsync("Forbidden.");
        }
    }
}