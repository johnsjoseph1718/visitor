using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using visitors_mangement_system.Models;
using visitors_mangement_system.models;
using visitors_mangement_system.BusinessLogic;

namespace visitors_mangement_system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisitorsController : ControllerBase
    {
        private readonly VisitorBusiness _visitorBusiness;

                public VisitorsController()
                {
                    _visitorBusiness = new VisitorBusiness();
                }

        [Authorize]
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterVisitorRequest request)
        {
            if (request == null)
            {
                return BadRequest(new CommonResponseModel<string>
                {
                    Success = false,
                    Message = "Request is required.",
                    Response = null
                });
            }

            var (success, error) = _visitorBusiness.Register(request);

            if (!success)
            {
                return StatusCode(500, error);
            }

            return Ok(new CommonResponseModel<string>
            {
                Success = true,
                Message = "Visitor Saved Successfully",
                Response = null
            });
        }

        [Authorize]
        [HttpPut("checkin/{visitId}")]
        public IActionResult CheckIn(int visitId)
        {
            var (statusCode, message) = _visitorBusiness.CheckIn(visitId);

            if (statusCode == 200)
                return Ok(message);

            if (statusCode == 404)
                return NotFound(message);

            return BadRequest(message);
        }

        [Authorize]
        [HttpPut("checkout/{visitId}")]
        public IActionResult CheckOut(int visitId)
        {
            var (statusCode, message) = _visitorBusiness.CheckOut(visitId);

            if (statusCode == 200)
                return Ok(message);

            if (statusCode == 404)
                return NotFound(message);

            return BadRequest(message);
        }
        [Authorize]
        [HttpGet("report")]
        public IActionResult Report()
        {
            var (visits, error) = _visitorBusiness.GetReport();
            if (error != null)
                return StatusCode(500, error);
            return Ok(new CommonResponseModel<List<VisitReportResponse>>
            {
                Success = true,
                Message = "Report fetched successfully",
                Response = visits
            });
        }
        [Authorize]
        [HttpGet("search/{phoneNumber}")]
        public IActionResult Search(string phoneNumber)
        {
            var (visitor, error) = _visitorBusiness.SearchByPhone(phoneNumber);

            if (visitor == null)
            {
                return NotFound(error);
            }

            return Ok(visitor);
        }
        [Authorize]
        [HttpPost("create-visit")]
        public IActionResult CreateVisit([FromBody] CreateVisitRequest request)
        {
            var (success, error) = _visitorBusiness.CreateVisit(request);

            if (!success)
            {
                return BadRequest(new CommonResponseModel<string>
                {
                    Success = false,
                    Message = error ?? "Failed",
                    Response = null
                });
            }

            return Ok(new CommonResponseModel<string>
            {
                Success = true,
                Message = "Visit created successfully",
                Response = null
            });
        }
        [Authorize]
        [HttpGet("visits/scheduled/{visitDate}")]
        public IActionResult GetScheduledVisits(DateTime visitDate)
        {
            var (visits, error) = _visitorBusiness.GetScheduledVisits(visitDate);

            if (error != null)
            {
                return StatusCode(500, error);
            }

            return Ok(new CommonResponseModel<List<VisitReportResponse>>
            {
                Success = true,
                Message = $"Scheduled visits fetched successfully. Already completed {visitDate}",
                Response = visits
            });
        }

        [Authorize]
        [HttpGet("visits/completed/{visitDate}")]
        public IActionResult GetCompletedVisits(DateTime visitDate)
        {
            var (visits, error) = _visitorBusiness.GetCompletedVisits(visitDate);

            if (error != null)
            {
                return StatusCode(500, error);
            }

            return Ok(new CommonResponseModel<List<VisitReportResponse>>
            {
                Success = true,
                Message = "Completed visits fetched successfully",
                Response = visits
            });
        }
        [Authorize]
        [HttpPut("cancel/{visitId}")]
        public IActionResult CancelVisit(int visitId, [FromBody] CancelVisitRequest request)
        {
            var (statusCode, message) = _visitorBusiness.CancelVisit(visitId, request);


    if (statusCode == 200)
                return Ok(message);

            if (statusCode == 404)
                return NotFound(message);

            return BadRequest(message);
        }
        [Authorize]
        [HttpPut("approve/{visitId}")]
        public IActionResult ApproveVisit(int visitId)
        {
            (int statusCode, string message) = _visitorBusiness.ApproveVisit(visitId);

            if (statusCode == 200)
                return Ok(message);

            if (statusCode == 404)
                return NotFound(message);

            return BadRequest(message);
        }
        [Authorize]
        [HttpPut("reject/{visitId}")]
        public IActionResult RejectVisit(int visitId)
        {
            (int statusCode, string message) = _visitorBusiness.RejectVisit(visitId);

            if (statusCode == 200)
                return Ok(message);

            if (statusCode == 404)
                return NotFound(message);

            return BadRequest(message);
        }
        [Authorize]
        [HttpGet("visits/waiting/{visitDate}")]
        public IActionResult GetWaitingVisits(DateTime visitDate)
        {
            var (visits, error) = _visitorBusiness.GetWaitingVisits(visitDate);

            if (error != null)
                return StatusCode(500, error);

            return Ok(new CommonResponseModel<List<VisitReportResponse>>
            {
                Success = true,
                Message = "Waiting visits fetched successfully",
                Response = visits
            });
        }
        [Authorize]
        [HttpGet("visits/cancelled/{visitDate}")]
        public IActionResult GetCancelledVisits(DateTime visitDate)
        {
            var (visits, error) = _visitorBusiness.GetCancelledVisits(visitDate);

            if (error != null)
                return StatusCode(500, error);

            return Ok(new CommonResponseModel<List<VisitReportResponse>>
            {
                Success = true,
                Message = "Cancelled visits fetched successfully",
                Response = visits
            });
        }
        [Authorize]
        [HttpGet("dashboard/{visitDate}")]
        public IActionResult GetDashboard(DateTime visitDate)
        {
            (DashboardResponse? dashboard, string? error) = _visitorBusiness.GetDashboard(visitDate);

            if (error != null)
                return StatusCode(500, error);

            return Ok(new CommonResponseModel<DashboardResponse>
            {
                Success = true,
                Message = "Dashboard fetched successfully",
                Response = dashboard
            });
        }
        [Authorize]
        [HttpGet("dashboard/{visitDate}/scheduled")]
        public IActionResult GetScheduledVisitors(DateTime visitDate)
        {
            (List<VisitReportResponse>? visits, string? error)
                = _visitorBusiness.GetScheduledVisits(visitDate);

            if (error != null)
                return StatusCode(500, error);

            return Ok(new CommonResponseModel<List<VisitReportResponse>>
            {
                Success = true,
                Message = "Scheduled visitors fetched successfully",
                Response = visits
            });
        }

        [Authorize]
        [HttpGet("history/{visitorId}")]
        public IActionResult GetVisitorHistory(int visitorId)
        {
            var (history, error) = _visitorBusiness.GetVisitorHistory(visitorId);

            if (error != null)
                return StatusCode(500, error);

            return Ok(new CommonResponseModel<List<VisitorHistoryResponse>>
            {
                Success = true,
                Message = "Visitor history fetched successfully",
                Response = history
            });
        }
        [Authorize]
        [HttpPut("Update{visitorId}")]
        public IActionResult UpdateVisitor(
    int visitorId,
    [FromBody] UpdateVisitorRequest request)
        {
            (int statusCode, string message) = _visitorBusiness.UpdateVisitor(visitorId, request);

            if (statusCode == 200)
                return Ok(message);

            if (statusCode == 404)
                return NotFound(message);

            return BadRequest(message);
        }
        [Authorize]
        [HttpDelete("{visitorId}")]
        public IActionResult DeleteVisitor(int visitorId)
        {
            (int statusCode, string message) = _visitorBusiness.DeleteVisitor(visitorId);

            if (statusCode == 200)
                return Ok(message);

            if (statusCode == 404)
                return NotFound(message);

            return BadRequest(message);
        }

        [Authorize]
        [HttpGet("audit/{visitorId}")]
        public IActionResult GetVisitorAudit(int visitorId)
        {
            var (audits, error) = _visitorBusiness.GetVisitorAudit(visitorId);

            if (error != null)
                return StatusCode(500, error);

            return Ok(new CommonResponseModel<List<VisitorAuditResponse>>
            {
                Success = true,
                Message = "Visitor audit fetched successfully",
                Response = audits
            });
        }
    }

}
