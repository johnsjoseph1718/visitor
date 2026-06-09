namespace visitors_mangement_system.models
{
    public class CommonResponseModel<T>
    {
        public bool Success { get; set; }
        public required string Message { get; set; }
        public required T? Response { get; set; }
    
    }
}
