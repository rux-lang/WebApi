using Npgsql;
using WebApi.Repositories;
using WebApi.Services;

namespace WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            var connectionString = builder.Configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
            builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
            builder.Services.AddSingleton<PackageRepository>();
            builder.Services.Configure<PlaygroundOptions>(builder.Configuration.GetSection("Playground"));
            builder.Services.AddSingleton<PlaygroundService>();
            var app = builder.Build();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
