using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Backend.Models;

namespace Backend.Auth
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ILogger<BasicAuthenticationHandler> _logger;
        private const string VALID_USERNAME = "admin";
        private const string VALID_PASSWORD = "admin123";
        private const string VALID_EMAIL = "admin@example.com";
        private const string VALID_USER_ID = "11111111-1111-1111-1111-111111111111";
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
            : base(options, logger, encoder)
        {
            _logger = logger.CreateLogger<BasicAuthenticationHandler>();
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                _logger.LogDebug("No Authorization header found");
                return AuthenticateResult.NoResult();
            }

            var authorizationHeader = Request.Headers["Authorization"].ToString();
            
            if (!authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                if (authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return AuthenticateResult.NoResult();
                }
                return AuthenticateResult.NoResult();
            }

            try
            {
                var encodedCredentials = authorizationHeader.Substring("Basic ".Length).Trim();
                var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                
                var colonIndex = decodedCredentials.IndexOf(':');
                if (colonIndex == -1)
                {
                    _logger.LogWarning("Invalid credentials format: missing colon separator");
                    return AuthenticateResult.Fail("Invalid credentials format");
                }

                var username = decodedCredentials.Substring(0, colonIndex);
                var password = decodedCredentials.Substring(colonIndex + 1);
                if (username == VALID_USERNAME && password == VALID_PASSWORD)
                {
                    _logger.LogInformation($"User {VALID_USERNAME} authenticated successfully via Basic Auth");
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
                    
                    return AuthenticateResult.Success(ticket);
                }
                
                _logger.LogWarning($"Failed login attempt for email: {username}");
                return AuthenticateResult.Fail("Invalid email or password");
            }
            catch (FormatException)
            {
                _logger.LogWarning("Invalid Base64 encoding in Authorization header");
                return AuthenticateResult.Fail("Invalid Base64 encoding");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Basic authentication");
                return AuthenticateResult.Fail("Authentication error");
            }
        }
        
        private void SetJwtCookie(string jwtToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(24)
            };
            
            Response.Cookies.Append("jwt_token", jwtToken, cookieOptions);
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers["WWW-Authenticate"] = "Basic realm=\"FinderSecrets API\"";
            Response.StatusCode = 401;
            
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                await Response.WriteAsJsonAsync(new { error = "Unauthorized", message = "Basic authentication required" });
            }
            else
            {
                await Response.WriteAsync(@"
                    <html>
                        <body>
                            <h1>401 Unauthorized</h1>
                            <p>Please provide Basic authentication credentials.</p>
                        </body>
                    </html>
                ");
            }
            
            _logger.LogWarning("Challenge sent - requesting Basic authentication");
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 403;
            
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                await Response.WriteAsJsonAsync(new { error = "Forbidden", message = "You don't have permission to access this resource" });
            }
            else
            {
                await Response.WriteAsync("403 Forbidden - Access denied");
            }
        }
    }
}