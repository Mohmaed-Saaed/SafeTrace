using SafeTrace.Application.DTOs.FacebookImportedPosts.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IFacebookPostRequirementChecker
    {
        FacebookPostRequirementResultDto Evaluate(FacebookImportedPost post);
    }
}
