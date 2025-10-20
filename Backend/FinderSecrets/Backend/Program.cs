using Backend.Controllers;
using Backend.Services;
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
            builder.Services.AddSwaggerGen();
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
            builder.Services.AddScoped<ISecretsFinder, SecretsFinder>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("FrontendPolicy");
            app.UseAuthorization();
            
            app.MapControllers();
            
            // Добавляем endpoint'ы ПОСЛЕ MapControllers
            app.MapGet("/", () => "FinderSecrets API is running");
            app.MapGet("/api/test", () => new { message = "API is working!", status = "OK" });
            app.MapGet("/api/health", () => "Healthy");


            app.Run();
        }
    }
}