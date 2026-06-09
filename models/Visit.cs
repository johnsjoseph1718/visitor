namespace visitors_mangement_system.models
{
    public class Visit
    {
        public int VisitId { get; set; }

        public int VisitorId { get; set; }

        public DateTime VisitDate { get; set; }

        public string Status { get; set; } = "Scheduled";

        public string? CancelReasonType { get; set; }

        public string? CancelReasonComment { get; set; }

        public bool CheckIn { get; set; }

        public bool CheckOut { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? DeletedDate { get; set; }

        public string? DeletedBy { get; set; }
    }
}
