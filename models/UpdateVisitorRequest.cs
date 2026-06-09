namespace visitors_mangement_system.models
{
    public class UpdateVisitorRequest
    {
        public string? Name { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; } = string.Empty;

        public DateTime?  BirthDate { get; set; }
    }
}