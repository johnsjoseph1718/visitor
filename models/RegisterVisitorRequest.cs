namespace visitors_mangement_system.models
{
    public class RegisterVisitorRequest
    {
        public string Name { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime BirthDate { get; set; }

        public DateTime VisitDate { get; set; }
    }
}
