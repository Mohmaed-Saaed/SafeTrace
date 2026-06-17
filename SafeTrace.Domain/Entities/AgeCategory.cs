namespace SafeTrace.Domain.Entities
{
    public class AgeCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int MinAge {  get; set; }
        public int MaxAge { get; set; }

        public ICollection<BaseCase> BaseCases { get; set; } = new List<BaseCase>();

    }
}