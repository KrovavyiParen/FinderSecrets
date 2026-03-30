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


namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            // Добавляем DbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Регистрируем сервисы
            builder.Services.AddScoped<ISecretsFinder, SecretsFinder>();
            builder.Services.AddScoped<DatabaseService>();

            // Настройка Swagger с XML комментариями
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "FinderSecrets API",
                    Version = "v1",
                    Description = "API для поиска секретов и проверки состояния системы"
                });

                // Включение XML-комментариев для контроллеров
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                // Добавляем кастомный фильтр для описаний ответов
                c.OperationFilter<ResponseDescriptionOperationFilter>();
                c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic",
                    In = ParameterLocation.Header,
                    Description = "Basic Authentication\n\n" +
                                "Введите email и пароль в формате: email:password\n" +
                                "Пример: test@example.com:MyPassword123"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "basic"
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
                    policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "BasicAuthentication";
                options.DefaultChallengeScheme = "BasicAuthentication";
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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
            });
            //builder.Services.AddAuthentication("BasicAuthentication")
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
                "BasicAuthentication", 
                null
            );
            builder.Services.AddAuthentication();   
            builder.Services.AddHttpClient();
            //builder.Services.AddScoped<ISecretsFinder, SecretsFinder>();
            

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinderSecrets API v1");
                });
            }
            app.UseHttpsRedirection();
            app.UseCors("FrontendPolicy");

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers().RequireAuthorization();
            app.MapGet("/", () => "FinderSecrets API is running!")
               .RequireAuthorization()
               .WithName("Root")
               .WithTags("Health")
               .WithSummary("Проверка доступности API")
               .WithDescription("Возвращает статус работы основного API")
               .Produces<string>(200, "text/plain");

            app.MapGet("/api/test", () => new { message = "API is working!", status = "OK" })
               .AllowAnonymous() 
               .WithName("TestAPI")
               .WithTags("Health")
               .WithSummary("Тестовый endpoint")
               .WithDescription("Проверяет работоспособность API и возвращает тестовые данные в формате JSON")
               .Produces<object>(200, "application/json");

            app.MapGet("/api/health", () => "Healthy")
               .AllowAnonymous()  
               .WithName("HealthCheck")
               .WithTags("Health")
               .WithSummary("Проверка здоровья системы")
               .WithDescription("Возвращает статус здоровья системы в текстовом формате")
               .Produces<string>(200, "text/plain");



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