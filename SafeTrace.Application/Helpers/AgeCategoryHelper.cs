using SafeTrace.Application.Exceptions;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;

namespace SafeTrace.Application.Helpers
{
    public static class AgeCategoryHelper
    {
        public static AgeCategoryEnum GetCategory(int age)
        {
            if (age <= 2) return AgeCategoryEnum.Infant;
            if (age <= 12) return AgeCategoryEnum.Child;
            if (age <= 17) return AgeCategoryEnum.Teenager;
            if (age <= 35) return AgeCategoryEnum.YoungAdult;
            if (age <= 59) return AgeCategoryEnum.Adult;

            return AgeCategoryEnum.Senior;
        }

        public static (int Min, int Max) GetRange(AgeCategoryEnum category) =>
            category switch
            {
                AgeCategoryEnum.Infant => (0, 2),
                AgeCategoryEnum.Child => (3, 12),
                AgeCategoryEnum.Teenager => (13, 17),
                AgeCategoryEnum.YoungAdult => (18, 35),
                AgeCategoryEnum.Adult => (36, 59),
                AgeCategoryEnum.Senior => (60, 120),
                _ => (0, 120)
            };

        public static async Task<int> ResolveAgeCategoryIdAsync(
            IUnitOfWork unitOfWork,
            int age)
        {
            var category = await unitOfWork.Repository<AgeCategory>().GetOneAsync(
                c => age >= c.MinAge && age <= c.MaxAge,
                tracked: false);

            if (category is null)
                throw new NotFoundException(
                    $"No age category configured for age {age}.");

            return category.Id;
        }
    }
}