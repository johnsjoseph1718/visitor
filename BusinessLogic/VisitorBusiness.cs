using visitors_mangement_system.Models;
using visitors_mangement_system.models;
using visitors_mangement_system.Repositories;

namespace visitors_mangement_system.BusinessLogic
{
    public class VisitorBusiness
    {
        private readonly VisitorRepository _repo;

        public VisitorBusiness()
        {
            _repo = new VisitorRepository();
        }

        public (bool success, string? error) Register(RegisterVisitorRequest request)
        {
            // validations
            if (request == null)
                return (false, "Request is required.");

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                return (false, "Phone number is required");

            if (!request.PhoneNumber.All(char.IsDigit))
                return (false, "Phone number must contain only digits");

            if (request.PhoneNumber.Length != 10)
                return (false, "Phone number must be exactly 10 digits");

            if (request.BirthDate > DateTime.Today.AddYears(-18))
                return (false, "Visitor must be at least 18 years old");

            if (request.VisitDate < DateTime.Today)
                return (false, $"Visit date must be greater than {DateTime.Today:yyyy-MM-dd}");

            return _repo.Register(request);
        }

        public (int statusCode, string message) CheckIn(int visitId)
        {
            if (visitId <= 0)
                return (400, "Invalid visit id");

            return _repo.CheckIn(visitId);
        }

        public (int statusCode, string message) CheckOut(int visitId)
        {
            if (visitId <= 0)
                return (400, "Invalid visit id");

            return _repo.CheckOut(visitId);
        }

        public (List<Visitor>? visitors, string? error) GetReport()
        {
            return _repo.GetReport();
        }

        public (Visitor? visitor, string? error) SearchByPhone(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return (null, "Phone number is required");

            return _repo.SearchByPhone(phoneNumber);
        }

        public (bool success, string? error) CreateVisit(CreateVisitRequest request)
        {
            if (request == null)
                return (false, "Request required");

            return _repo.CreateVisit(request);
        }

        public (List<VisitReportResponse>? visits, string? error) GetScheduledVisits(DateTime visitDate)
        {
            return _repo.GetScheduledVisits(visitDate);
        }

        public (List<VisitReportResponse>? visits, string? error) GetCompletedVisits(DateTime visitDate)
        {
            return _repo.GetCompletedVisits(visitDate);
        }

        public (int statusCode, string message) CancelVisit(int visitId, CancelVisitRequest request)
        {
            if (visitId <= 0)
                return (400, "Invalid visit id");

            return _repo.CancelVisit(visitId, request);
        }

        public (int statusCode, string message) ApproveVisit(int visitId)
        {
            if (visitId <= 0)
                return (400, "Invalid visit id");

            return _repo.ApproveVisit(visitId);
        }

        public (int statusCode, string message) RejectVisit(int visitId)
        {
            if (visitId <= 0)
                return (400, "Invalid visit id");

            return _repo.RejectVisit(visitId);
        }

        public (List<VisitReportResponse>? visits, string? error) GetWaitingVisits(DateTime visitDate)
        {
            return _repo.GetWaitingVisits(visitDate);
        }

        public (List<VisitReportResponse>? visits, string? error) GetCancelledVisits(DateTime visitDate)
        {
            return _repo.GetCancelledVisits(visitDate);
        }

        public (DashboardResponse? dashboard, string? error) GetDashboard(DateTime visitDate)
        {
            return _repo.GetDashboard(visitDate);
        }

        public (List<VisitorHistoryResponse>? history, string? error) GetVisitorHistory(int visitorId)
        {
            return _repo.GetVisitorHistory(visitorId);
        }

        public (int statusCode, string message) UpdateVisitor(int visitorId, UpdateVisitorRequest request)
        {
            if (visitorId <= 0)
                return (400, "Invalid visitor id");

            if (request == null)
                return (400, "Request is required");

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                return (400, "Phone number is required");

            if (!request.PhoneNumber.All(char.IsDigit))
                return (400, "Phone number must contain only digits");

            if (request.PhoneNumber.Length != 10)
                return (400, "Phone number must be exactly 10 digits");

            return _repo.UpdateVisitor(visitorId, request);
        }

        public (int statusCode, string message) DeleteVisitor(int visitorId)
        {
            if (visitorId <= 0)
                return (400, "Invalid visitor id");

            return _repo.DeleteVisitor(visitorId);
        }

        public (List<VisitorAuditResponse>? audits, string? error) GetVisitorAudit(int visitorId)
        {
            return _repo.GetVisitorAudit(visitorId);
        }
    }
}
