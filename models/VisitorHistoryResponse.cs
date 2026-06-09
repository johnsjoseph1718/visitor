namespace visitors_mangement_system.models
{
    public class VisitorHistoryResponse
    {
        public int VisitId { get; set; }

        public DateTime VisitDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool CheckIn { get; set; }

        public bool CheckOut { get; set; }
    }
}