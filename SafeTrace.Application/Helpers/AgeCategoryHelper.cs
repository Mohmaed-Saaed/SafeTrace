using SafeTrace.Application.Common.Enums;
using SafeTrace.Application.Exceptions;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using System;
using System.Threading.Tasks;

namespace SafeTrace.Application.Common.Helpers
{
    public static class AgeCategoryHelper
    {
        public static AgeCategory GetCategory(int age)
        {
            if (age <= 2) return AgeCategory.Infant;
            if (age <= 12) return AgeCategory.Child;
            if (age <= 17) return AgeCategory.Teenager;
            if (age <= 35) return AgeCategory.YoungAdult;
            if (age <= 59) return AgeCategory.Adult;
            return AgeCategory.Senior;
        }

        public static (int Min, int Max) GetRange(AgeCategory category) => category switch
        {
            AgeCategory.Infant => (0, 2),
            AgeCategory.Child => (3, 12),
            AgeCategory.Teenager => (13, 17),
            AgeCategory.YoungAdult => (18, 35),
            AgeCategory.Adult => (36, 59),
            AgeCategory.Senior => (60, 120),
            _ => (0, 120)
        };

        /// <summary>
        /// Resolves the AgeCategories.Id (FK on Cases.AgeCategoryId) for a given age,
        /// by querying the DB table directly. Used by every Case-creating/updating service
        /// (LongTerm, ShortTerm, UnknownPerson...) so the FK is always consistent with
        /// the actual AgeCategories table, not a hardcoded mapping.
        /// </summary>
        public static async Task<int> ResolveAgeCategoryIdAsync(IUnitOfWork unitOfWork, int age)
        {
            var category = await unitOfWork.Repository<SafeTrace.Domain.Entities.AgeCategory>()
                .GetOneAsync(
                    c => age >= c.MinAge && age <= c.MaxAge,
                    tracked: false);

            if (category is null)
                throw new NotFoundException($"No age category configured for age {age}.");

            return category.Id;
        }
    }
}