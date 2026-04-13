using Backend.Controllers;
using Backend.Services;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;


namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<ISecretsFinder, SecretsFinder>();
            builder.Services.AddScoped<DatabaseService>();

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "FinderSecrets API",
                    Version = "v1",
                    Description = "API для поиска секретов и проверки состояния системы"
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                c.OperationFilter<ResponseDescriptionOperationFilter>();
                c.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic",
                    In = ParameterLocation.Header,
                    Description = "Basic Authentication\n\n" +
                                "Введите email и пароль в формате: email:password\n" +
                                "Пример: test@example.com:MyPassword123"
                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme."
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Basic"
                            }
                        },
                        new List<string>()
                    }
                });
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FrontendPolicy", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "https://localhost:3000", "http://localhost:5173", "https://localhost:5173")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .WithExposedHeaders("WWW-Authenticate", "Content-Type");
                });
            });

           builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Basic";
                options.DefaultChallengeScheme = "Basic";
                options.DefaultScheme = "Basic";;
            })
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null)
            .AddJwtBearer("Bearer", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured")))
              };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Cookies["jwt_token"];
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder("Basic", "Bearer")
                    .RequireAuthenticatedUser()
                    .Build();
                options.AddPolicy("BasicOnly", new AuthorizationPolicyBuilder("Basic")
                    .RequireAuthenticatedUser()
                    .Build());
                options.AddPolicy("JWTOnly", new AuthorizationPolicyBuilder("Bearer")
                    .RequireAuthenticatedUser()
                    .Build());

            });     
            
            builder.Services.AddHttpClient();
            
            var app = builder.Build();
            
            app.UseCors("FrontendPolicy"); 
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinderSecrets API v1");
                });
            }
            app.UseHttpsRedirection();          

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            // app.Use(async (context, next) =>
            // {
            //     var path = context.Request.Path.Value ?? "";
            //     var isApiPath = path.StartsWith("/api") || 
            //                     path.StartsWith("/swagger") || 
            //                     path.StartsWith("/swagger.json");
            //     var isStaticFile = path.Contains(".") && 
            //                     (path.EndsWith(".css") || path.EndsWith(".js") || 
            //                         path.EndsWith(".png") || path.EndsWith(".jpg") ||
            //                         path.EndsWith(".html"));
                
            //     if (!isApiPath && !isStaticFile)
            //     {
            //         if (!context.User.Identity?.IsAuthenticated ?? true)
            //         {
            //             context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"FinderSecrets\"";
            //             context.Response.StatusCode = 401;
            //             return;
            //         }
            //     }
                
            //     await next();
            // });

            app.MapControllers();
            app.MapGet("/", () => "FinderSecrets API is running!").RequireAuthorization("BasicOnly");
            app.MapFallbackToFile("index.html").RequireAuthorization("BasicOnly");
            app.Run();
        }
    }

    public class ResponseDescriptionOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Responses.ContainsKey("200"))
            {
                var methodAttributes = context.MethodInfo.GetCustomAttributes(true);
                var endpointName = context.MethodInfo.Name.ToLower();

                operation.Responses["200"].Description = endpointName switch
                {
                    var name when name.Contains("root") || name.Contains("main") =>
                        "API успешно запущен и готов к работе",
                    var name when name.Contains("test") =>
                        "API функционирует корректно, все компоненты работают нормально",
                    var name when name.Contains("health") =>
                        "Система полностью работоспособна, все сервисы функционируют в штатном режиме",
                    _ => "Успешный запрос"
                };

                if (operation.Summary != null)
                {
                    operation.Responses["200"].Description = operation.Summary.ToLower() switch
                    {
                        var summary when summary.Contains("проверка доступности") =>
                            "API успешно запущен и готов к работе",
                        var summary when summary.Contains("тестовый") =>
                            "API функционирует корректно, все компоненты работают нормально",
                        var summary when summary.Contains("проверка здоровья") =>
                            "Система полностью работоспособна, все сервисы функционируют в штатном режиме",
                        _ => operation.Responses["200"].Description
                    };
                }
            }
        }
    }
}