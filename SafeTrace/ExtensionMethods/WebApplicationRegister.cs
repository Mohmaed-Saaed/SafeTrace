using SafeTrace.Application.Interfaces;

namespace SafeTrace.API.ExtensionMethods
{
    public static class WebApplicationRegister
    {
        public static async Task<WebApplication> SeedDataAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();

            var dataInitializer = scope.ServiceProvider.GetRequiredService<IDBInitializer>();

            await dataInitializer.Initialize();

            return app;
        }

        public static async Task<WebApplication> ApplyPendingMigrationsAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var pendingMigrations =
                await dbContext.Database.GetPendingMigrationsAsync();

            if (pendingMigrations.Any())
            {
                await dbContext.Database.MigrateAsync();
            }

            return app;
        }
    }
}