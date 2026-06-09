namespace visitors_mangement_system.models
{
    public class SearchVisitorResponse
    {
        public int VisitorId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime BirthDate { get; set; }
    }
}
