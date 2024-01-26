namespace Shared
{
    public class GroupEntity : BaseEntity
    {
        public string Description { get; set; } = string.Empty;
        public List<Guid> Members { get; set; }= new List<Guid>();
    }
}