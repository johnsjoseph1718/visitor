public class DashboardResponse
{
    public int Scheduled { get; set; }

    public int Waiting { get; set; }

    public int Completed { get; set; }

    public int Cancelled { get; set; }

    public int Rejected { get; set; }
}