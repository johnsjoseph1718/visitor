namespace visitors_mangement_system.models
{
    public class VisitorAuditResponse
    {
        public int AuditId { get; set; }

        public int VisitorId { get; set; }

        public int VisitId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime BirthDate { get; set; }

        public DateTime VisitDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool CheckIn { get; set; }

        public bool CheckOut { get; set; }

        public DateTime CreatedDate { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }
}