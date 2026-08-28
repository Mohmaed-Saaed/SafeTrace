namespace SafeTrace.Infrastructure.Options
{
    public sealed class BedrockOptions
    {
        public const string SectionName = "AWS:Bedrock";

        public string ModelId { get; set; } = "amazon.nova-pro-v1:0";
    }
}
