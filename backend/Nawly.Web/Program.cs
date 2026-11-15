using System.Text.Json.Serialization;
using Nawly.Services;
using Scalar.AspNetCore;

namespace Nawly.Web
{
    /// <summary>
    /// Nawly API – composition root.
    /// Service registrations will be added through extension methods in
    /// Nawly.Web/Extensions and Nawly.Persistence/ProgramExtensions as features are built.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ==========================================
            // 1. Database & Identity          (TODO: Persistence extensions)
            // 2. Application Services
            // 3. Security, CORS, Rate limiting (TODO: Web extensions)
            // ==========================================
            builder.Services.AddApplicationServices();

            // ==========================================
            // 4. Controllers (from Nawly.Presentation)
            // ==========================================
            builder.Services.AddControllers()
                .AddApplicationPart(typeof(Nawly.Presentation.AssemblyReference).Assembly)
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
                });

            // ==========================================
            // 5. OpenAPI (Development only, per SRS §3.2)
            // ==========================================
            builder.Services.AddOpenApi();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
