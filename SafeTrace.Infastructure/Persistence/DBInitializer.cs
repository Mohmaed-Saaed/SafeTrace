using Microsoft.AspNetCore.Identity;
using SafeTrace.Application.Interfaces;
using SafeTrace.Domain.Interfaces.IUnitOfWork;

namespace SafeTrace.Infrastructure.Persistence
{
    public class DBInitializer : IDBInitializer
    {
        public readonly IUnitOfWork _unitOfWork;
        private readonly RoleManager<IdentityRole> _roleManager;
        public DBInitializer(IUnitOfWork unitOfWork, RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }

        public async Task Initialize()
        {
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                var userRole = new IdentityRole("User");
                await _roleManager.CreateAsync(userRole);
            }

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                var adminRole = new IdentityRole("Admin");
                await _roleManager.CreateAsync(adminRole);
            }

            if (!await _roleManager.RoleExistsAsync("VerifiedUser"))
            {
                var verifiedUserRole = new IdentityRole("VerifiedUser");
                await _roleManager.CreateAsync(verifiedUserRole);
            }
        }
    }
}