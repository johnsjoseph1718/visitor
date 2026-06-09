namespace visitors_mangement_system.models
{
    public class CancelVisitRequest
    {
        public string ReasonType { get; set; } = string.Empty;

        public string Comments { get; set; } = string.Empty;
    }
}