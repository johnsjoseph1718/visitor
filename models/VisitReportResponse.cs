namespace visitors_mangement_system.models
{
    public class VisitReportResponse
    {
        public int VisitId { get; set; }

        public int VisitorId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime VisitDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool CheckIn { get; set; }

        public bool CheckOut { get; set; }
    }
}