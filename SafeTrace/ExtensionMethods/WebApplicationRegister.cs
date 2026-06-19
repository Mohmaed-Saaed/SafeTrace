using SafeTrace.Application.Interfaces;

namespace SafeTrace.API.ExtensionMethods
{
    public static class WebApplicationRegister
    {
        public static async Task<WebApplication> SeedDataAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dataIntializer = scope.ServiceProvider.GetRequiredService<IDBInitializer>();
            await dataIntializer.Initialize();

            return app;
        }

        public static async Task<WebApplication> ApplyPendingMigrationsAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

            if (pendingMigrations.Any())
            {
                await dbContext.Database.MigrateAsync();
            }
            return app;
        }
    }
}