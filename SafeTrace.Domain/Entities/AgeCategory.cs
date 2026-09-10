namespace SafeTrace.Domain.Entities
{
    public class AgeCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int MinAge {  get; set; }
        public int MaxAge { get; set; }

        public ICollection<Case> Cases { get; set; } = new List<Case>();

    }
}