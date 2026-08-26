using SafeTrace.Application.DTOs.FacebookImportedPosts.Response;

namespace SafeTrace.Application.Services
{
    public sealed class FacebookPostRequirementChecker : IFacebookPostRequirementChecker
    {
        public FacebookPostRequirementResultDto Evaluate(FacebookImportedPost post)
        {
            ArgumentNullException.ThrowIfNull(post);

            if (!post.Classification.HasValue)
                return Incomplete("Classification");

            if (post.Classification == SocialPostClassification.Found)
            {
                return Incomplete(
                    "Found cases require matching with an existing case.");
            }

            if (post.Classification == SocialPostClassification.NotRelevant)
            {
                return Incomplete(
                    "Not relevant posts cannot be published as cases.");
            }

            var missing = new List<string>();

            if (!post.Gender.HasValue)
                missing.Add("Gender");

            if (string.IsNullOrWhiteSpace(post.Government))
                missing.Add("Government");

            if (string.IsNullOrWhiteSpace(post.City))
                missing.Add("City");

            if (!post.Age.HasValue || post.Age is < 1 or > 120)
                missing.Add("Age");

            if (!post.EventDate.HasValue)
                missing.Add("EventDate");

            if (!post.Relation.HasValue || !Enum.IsDefined(post.Relation.Value))
                missing.Add("Relation");

            if (post.FacebookPage is null ||
                string.IsNullOrWhiteSpace(post.FacebookPage.UserId))
            {
                missing.Add("UserId");
            }

            if (!post.Files.Any(file => IsValidImageSource(file.FileUrl)))
                missing.Add("Image");

            if (post.Classification == SocialPostClassification.Urgent)
            {
                if (!post.Latitude.HasValue || post.Latitude is < -90 or > 90)
                    missing.Add("Latitude");

                if (!post.Longitude.HasValue || post.Longitude is < -180 or > 180)
                    missing.Add("Longitude");
            }

            return new FacebookPostRequirementResultDto(missing.Count == 0, missing);
        }

        private static bool IsValidImageSource(string? fileUrl)
        {
            return Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri) &&
                   string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }

        private static FacebookPostRequirementResultDto Incomplete(string reason)
        {
            return new FacebookPostRequirementResultDto(false, [reason]);
        }
    }
}
