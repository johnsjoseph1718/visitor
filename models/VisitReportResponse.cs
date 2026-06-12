namespace visitors_mangement_system.models
{
    public class VisitReportResponse
    {
        public int VisitId { get; set; }

        public int VisitorId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime BirthDate { get; set; }

        public DateTime VisitDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? ReasonType { get; set; }

        public string? Comments { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool CheckIn { get; set; }

        public bool CheckOut { get; set; }
    }
}