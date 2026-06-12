using Microsoft.Data.SqlClient;
using System.Data;
using visitors_mangement_system.Models;
using visitors_mangement_system.models;

namespace visitors_mangement_system.Repositories
{
    public class VisitorRepository
    {
        private readonly string _connectionString;

        public VisitorRepository()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            _connectionString = config.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        public (bool success, string? error) Register(RegisterVisitorRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (false, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                con.Open();

                int visitorId;

                string findVisitorQuery = @"
        SELECT VisitorId
        FROM Visitors
        WHERE PhoneNumber = @PhoneNumber
        AND IsActive = 1";

                using (SqlCommand findCmd = new SqlCommand(findVisitorQuery, con))
                {
                    findCmd.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber);

                    object? result = findCmd.ExecuteScalar();

                    if (result != null)
                    {
                        visitorId = Convert.ToInt32(result);
                    }
                    else
                    {
                        string insertVisitorQuery = @"
                INSERT INTO Visitors
                (
                    Name,
                    PhoneNumber,
                    BirthDate,
                    CreatedBy
                )
                VALUES
                (
                    @Name,
                    @PhoneNumber,
                    @BirthDate,
                    'admin'
                );

                SELECT SCOPE_IDENTITY();";

                        using SqlCommand insertVisitorCmd =
                            new SqlCommand(insertVisitorQuery, con);

                        insertVisitorCmd.Parameters.AddWithValue("@Name", request.Name);
                        insertVisitorCmd.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber);
                        insertVisitorCmd.Parameters.AddWithValue("@BirthDate", request.BirthDate);

                        visitorId = Convert.ToInt32(insertVisitorCmd.ExecuteScalar());

                    }
                }

                string countQuery = @"
        SELECT COUNT(*)
        FROM Visits
        WHERE VisitDate = @VisitDate
        AND Status = 'Scheduled'
        AND IsActive = 1";

                int scheduledCount;

                using (SqlCommand countCmd = new SqlCommand(countQuery, con))
                {
                    countCmd.Parameters.AddWithValue("@VisitDate", request.VisitDate.Date);

                    scheduledCount = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                string status = scheduledCount < 10
                    ? "Scheduled"
                    : "Waiting";

                string insertVisitQuery = @"
        INSERT INTO Visits
        (
            VisitorId,
            VisitDate,
            Status,
            CreatedBy
        )
        VALUES
        (
            @VisitorId,
            @VisitDate,
            @Status,
            'admin'
        )";

                using SqlCommand visitCmd = new SqlCommand(insertVisitQuery, con);

                visitCmd.Parameters.AddWithValue("@VisitorId", visitorId);
                visitCmd.Parameters.AddWithValue("@VisitDate", request.VisitDate.Date);
                visitCmd.Parameters.AddWithValue("@Status", status);

                visitCmd.ExecuteNonQuery();

                con.Close();

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (int statusCode, string message) CheckIn(int visitId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (500, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                con.Open();

                string query = @"
            SELECT VisitDate, CheckIn, Status
            FROM Visits
            WHERE VisitId = @VisitId
            AND IsActive = 1";

                using SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@VisitId", visitId);

                using SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return (404, "Visit not found");

                DateTime visitDate = Convert.ToDateTime(reader["VisitDate"]);

                bool alreadyCheckedIn = Convert.ToBoolean(reader["CheckIn"]);

                string status = reader["Status"].ToString() ?? "";

                reader.Close();

                if (status == "Cancelled")
                    return (400, "Visit is cancelled");

                if (alreadyCheckedIn)
                    return (400, "Already checked in");

                if (visitDate.Date != DateTime.Today)
                    return (400, "Check-in allowed only on visit date");

                string updateQuery = @"
            UPDATE Visits
            SET CheckIn = 1,
                UpdatedDate = GETDATE(),
                UpdatedBy = @UpdatedBy
            WHERE VisitId = @VisitId";

                using SqlCommand updateCmd = new SqlCommand(updateQuery, con);

                updateCmd.Parameters.AddWithValue("@VisitId", visitId);

                updateCmd.ExecuteNonQuery();

                // Create an audit record for this successful check-in
                string auditInsert = @"
    INSERT INTO VisitorAudit
    (
        VisitorId,
        VisitId,
        Name,
        PhoneNumber,
        BirthDate,
        VisitDate,
        Status,
        CheckIn,
        CheckOut,
        CreatedDate,
        CreatedBy
    )
    SELECT
        v.VisitorId,
        vt.VisitId,
        v.Name,
        v.PhoneNumber,
        v.BirthDate,
        vt.VisitDate,
        vt.Status,
        vt.CheckIn,
        vt.CheckOut,
        GETDATE(),
        'Admin'
    FROM Visitors v
    INNER JOIN Visits vt ON v.VisitorId = vt.VisitorId
    WHERE vt.VisitId = @VisitId";

                using SqlCommand auditCmd = new SqlCommand(auditInsert, con);
                auditCmd.Parameters.AddWithValue("@VisitId", visitId);
                auditCmd.ExecuteNonQuery();

                return (200, "Check-in successful");
            }
            catch (Exception ex)
            {
                return (500, ex.Message);
            }
        }

        public (int statusCode, string message) CheckOut(int visitId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (500, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                con.Open();

                string query = @"
            SELECT CheckIn, CheckOut
            FROM Visits
            WHERE VisitId = @VisitId
            AND IsActive = 1";

                using SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@VisitId", visitId);

                using SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return (404, "Visit not found");

                bool checkedIn = Convert.ToBoolean(reader["CheckIn"]);

                bool checkedOut = Convert.ToBoolean(reader["CheckOut"]);

                reader.Close();

                if (!checkedIn)
                    return (400, "Visitor has not checked in");

                if (checkedOut)
                    return (400, "Already checked out");

                string updateQuery = @"
            UPDATE Visits
            SET CheckOut = 1,
                Status = 'Completed',
                UpdatedDate = GETDATE(),
                UpdatedBy = @UpdatedBy
            WHERE VisitId = @VisitId";

                using SqlCommand updateCmd = new SqlCommand(updateQuery, con);

                string updatedBy = "Admin";
                updateCmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);

                updateCmd.Parameters.AddWithValue("@VisitId", visitId);

                updateCmd.ExecuteNonQuery();

                return (200, "Check-out successful");
            }
            catch (Exception ex)
            {
                return (500, ex.Message);
            }
        }

        public (List<VisitReportResponse>? visits, string? error) GetReport()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (null, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                string query = @"
            SELECT 
                v.VisitId,
                v.VisitorId,
                vis.Name,
                vis.PhoneNumber,
                vis.BirthDate,
                v.VisitDate,
                v.Status,
                v.CancelReasonType,
                v.CancelReasonComment,
                v.CreatedDate,
                v.CheckIn,
                v.CheckOut
            FROM Visits v
            INNER JOIN Visitors vis ON v.VisitorId = vis.VisitorId
            WHERE v.IsActive = 1 AND vis.IsActive = 1
            ORDER BY v.VisitDate DESC";

                using SqlCommand cmd = new SqlCommand(query, con);

                con.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                List<VisitReportResponse> visits = new();

                while (reader.Read())
                {
                    visits.Add(new VisitReportResponse
                    {
                        VisitId = Convert.ToInt32(reader["VisitId"]),
                        VisitorId = Convert.ToInt32(reader["VisitorId"]),
                        Name = reader["Name"].ToString() ?? string.Empty,
                        PhoneNumber = reader["PhoneNumber"].ToString() ?? string.Empty,
                        BirthDate = Convert.ToDateTime(reader["BirthDate"]),
                        VisitDate = Convert.ToDateTime(reader["VisitDate"]),
                        Status = reader["Status"].ToString() ?? string.Empty,
                        ReasonType = reader["CancelReasonType"].ToString() ?? string.Empty,
                        Comments = reader["CancelReasonComment"].ToString() ?? string.Empty,
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        CheckIn = Convert.ToBoolean(reader["CheckIn"]),
                        CheckOut = Convert.ToBoolean(reader["CheckOut"])
                    });
                }

                return (visits, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public (Visitor? visitor, string? error) SearchByPhone(string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (null, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                string query = @"
            SELECT VisitorId,
                   Name,
                   PhoneNumber,
                   BirthDate
            FROM Visitors
            WHERE PhoneNumber = @PhoneNumber
            AND IsActive = 1";

                using SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);

                con.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return (null, "Visitor not found");

                Visitor visitor = new Visitor
                {
                    VisitorId = Convert.ToInt32(reader["VisitorId"]),

                    Name = reader["Name"].ToString() ?? "",
                    PhoneNumber = reader["PhoneNumber"].ToString() ?? "",
                    BirthDate = Convert.ToDateTime(reader["BirthDate"])
                };

                return (visitor, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public (bool success, string? error) CreateVisit(CreateVisitRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (false, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                con.Open();

                string visitorQuery = @"
            SELECT COUNT(*)
            FROM Visitors
            WHERE VisitorId = @VisitorId
            AND IsActive = 1";

                using SqlCommand visitorCmd = new SqlCommand(visitorQuery, con);

                visitorCmd.Parameters.AddWithValue("@VisitorId", request.VisitorId);

                int visitorCount = Convert.ToInt32(visitorCmd.ExecuteScalar());

                if (visitorCount == 0)
                    return (false, "Visitor not found");

                string duplicateQuery = @"
            SELECT COUNT(*)
            FROM Visits
            WHERE VisitorId = @VisitorId
            AND VisitDate = @VisitDate
            AND IsActive = 1";

                using SqlCommand duplicateCmd = new SqlCommand(duplicateQuery, con);

                duplicateCmd.Parameters.AddWithValue("@VisitorId", request.VisitorId);
                duplicateCmd.Parameters.AddWithValue("@VisitDate", request.VisitDate.Date);

                int duplicateCount = Convert.ToInt32(duplicateCmd.ExecuteScalar());

                if (duplicateCount > 0)
                    return (false, "Visitor already has a visit on this date");

                string waitingCountQuery = @"
            SELECT COUNT(*)
            FROM Visits
            WHERE VisitDate = @VisitDate
            AND Status = 'Waiting'
            AND IsActive = 1";

                using SqlCommand waitingCountCmd = new SqlCommand(waitingCountQuery, con);
                waitingCountCmd.Parameters.AddWithValue("@VisitDate", request.VisitDate.Date);

                int waitingCount = Convert.ToInt32(waitingCountCmd.ExecuteScalar());

                string status;

                if (waitingCount > 0)
                {
                    // Apply queue protection: if there are waiting visitors for this date, new visits join waiting
                    // But enforce waiting list limit: max 4
                    if (waitingCount >= 4)
                    {
                        return (false, "Daily capacity reached. Please select another date.");
                    }

                    status = "Waiting";
                }
                else
                {
                    string countQuery = @"
            SELECT COUNT(*)
            FROM Visits
            WHERE VisitDate = @VisitDate
            AND Status = 'Scheduled'
            AND IsActive = 1";

                    using SqlCommand countCmd = new SqlCommand(countQuery, con);

                    countCmd.Parameters.AddWithValue("@VisitDate", request.VisitDate.Date);

                    int scheduledCount = Convert.ToInt32(countCmd.ExecuteScalar());

                    status = scheduledCount < 10 ? "Scheduled" : "Waiting";
                }

                string insertQuery = @"
            INSERT INTO Visits
            (
                VisitorId,
                VisitDate,
                Status,
                CreatedBy
            )
            VALUES
            (
                @VisitorId,
                @VisitDate,
                @Status,
                'admin'
            )";

                using SqlCommand insertCmd = new SqlCommand(insertQuery, con);

                insertCmd.Parameters.AddWithValue("@VisitorId", request.VisitorId);
                insertCmd.Parameters.AddWithValue("@VisitDate", request.VisitDate.Date);
                insertCmd.Parameters.AddWithValue("@Status", status);

                insertCmd.ExecuteNonQuery();

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (List<VisitReportResponse>? visits, string? error) GetScheduledVisits(DateTime visitDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (null, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                string query = @"
            SELECT
                vt.VisitorId,       
                vt.VisitId,
                v.Name,
                v.PhoneNumber,
                vt.VisitDate,
                vt.Status,
                vt.CheckIn,
                vt.CheckOut
            FROM Visits vt
            INNER JOIN Visitors v
                ON vt.VisitorId = v.VisitorId
            WHERE vt.VisitDate = @VisitDate
            AND vt.Status = 'Scheduled'
            AND vt.IsActive = 1";

                using SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@VisitDate", visitDate.Date);

                con.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                List<VisitReportResponse> visits = new();

                while (reader.Read())
                {
                    visits.Add(new VisitReportResponse
                    {
                        VisitId = Convert.ToInt32(reader["VisitId"]),
                        VisitorId = Convert.ToInt32(reader["VisitorId"]),
                        Name = reader["Name"].ToString() ?? "",
                        PhoneNumber = reader["PhoneNumber"].ToString() ?? "",
                        VisitDate = Convert.ToDateTime(reader["VisitDate"]),
                        Status = reader["Status"].ToString() ?? "",
                        CheckIn = Convert.ToBoolean(reader["CheckIn"]),
                        CheckOut = Convert.ToBoolean(reader["CheckOut"])
                    });
                }

                return (visits, null );
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public (List<VisitReportResponse>? visits, string? error) GetCompletedVisits(DateTime visitDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (null, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                string query = @"
            SELECT
                vt.VisitId,
                vt.VisitorId,
                v.Name,
                v.PhoneNumber,
                vt.VisitDate,
                vt.Status,
                vt.CheckIn,
                vt.CheckOut
            FROM Visits vt
            INNER JOIN Visitors v
                ON vt.VisitorId = v.VisitorId
            WHERE vt.VisitDate = @VisitDate
            AND vt.CheckIn = 1
            AND vt.CheckOut = 1
            AND vt.IsActive = 1";

                using SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@VisitDate", visitDate.Date);

                con.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                List<VisitReportResponse> visits = new();

                while (reader.Read())
                {
                    visits.Add(new VisitReportResponse
                    {
                        VisitId = Convert.ToInt32(reader["VisitId"]),
                        VisitorId = Convert.ToInt32(reader["VisitorId"]),
                        Name = reader["Name"].ToString() ?? "",
                        PhoneNumber = reader["PhoneNumber"].ToString() ?? "",
                        VisitDate = Convert.ToDateTime(reader["VisitDate"]),
                        Status = reader["Status"].ToString() ?? "",
                        CheckIn = Convert.ToBoolean(reader["CheckIn"]),
                        CheckOut = Convert.ToBoolean(reader["CheckOut"])
                    });
                }

                return (visits, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public (int statusCode, string message) CancelVisit(int visitId, CancelVisitRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (500, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                con.Open();

                string checkQuery = @"
            SELECT CheckIn, Status
            FROM Visits
            WHERE VisitId = @VisitId
            AND IsActive = 1";

                using SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@VisitId", visitId);

                using SqlDataReader reader = checkCmd.ExecuteReader();

                if (!reader.Read())
                    return (404, "Visit not found");

                bool checkedIn = Convert.ToBoolean(reader["CheckIn"]);

                string status = reader["Status"].ToString() ?? "";

                reader.Close();

                if (checkedIn)
                    return (400, "Cannot cancel after check-in");

                if (status == "Cancelled")
                    return (400, "Visit already cancelled");

                string updateQuery = @"
            UPDATE Visits
            SET Status = 'Cancelled',
                CancelReasonType = @ReasonType,
                CancelReasonComment = @Comments,
                UpdatedDate = GETDATE(),
                UpdatedBy = @UpdatedBy
            WHERE VisitId = @VisitId";

                using SqlCommand updateCmd = new SqlCommand(updateQuery, con);

                updateCmd.Parameters.AddWithValue("@ReasonType", request.ReasonType);
                updateCmd.Parameters.AddWithValue("@Comments", request.Comments);

                string updatedBy = "Admin";
                updateCmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);
                updateCmd.Parameters.AddWithValue("@VisitId", visitId);

                updateCmd.ExecuteNonQuery();

                return (200, "Visit cancelled successfully");
            }
            catch (Exception ex)
            {
                return (500, ex.Message);
            }
        }

        public (int statusCode, string message) ApproveVisit(int visitId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (500, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                con.Open();

                string visitQuery = @"
            SELECT VisitDate, Status
            FROM Visits
            WHERE VisitId = @VisitId
            AND IsActive = 1";

                using SqlCommand visitCmd = new SqlCommand(visitQuery, con);

                visitCmd.Parameters.AddWithValue("@VisitId", visitId);

                using SqlDataReader reader = visitCmd.ExecuteReader();

                if (!reader.Read())
                    return (404, "Visit not found");

                DateTime visitDate = Convert.ToDateTime(reader["VisitDate"]);

                string status = reader["Status"].ToString() ?? "";

                reader.Close();

                if (status != "Waiting")
                    return (400, "Only waiting visits can be approved");

                string countQuery = @"
            SELECT COUNT(*)
            FROM Visits
            WHERE VisitDate = @VisitDate
            AND Status = 'Scheduled'
            AND IsActive = 1";

                using SqlCommand countCmd = new SqlCommand(countQuery, con);

                countCmd.Parameters.AddWithValue("@VisitDate", visitDate.Date);

                int scheduledCount = Convert.ToInt32(countCmd.ExecuteScalar());

                if (scheduledCount >= 10)
                    return (400, "Daily limit reached");

                string updateQuery = @"
            UPDATE Visits
            SET Status = 'Scheduled'
            WHERE VisitId = @VisitId";

                using SqlCommand updateCmd = new SqlCommand(updateQuery, con);

                updateCmd.Parameters.AddWithValue("@VisitId", visitId);

                updateCmd.ExecuteNonQuery();

                return (200, "Visit approved successfully");
            }
            catch (Exception ex)
            {
                return (500, ex.Message);
            }
        }

        public (int statusCode, string message) RejectVisit(int visitId, RejectVisitRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (500, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                con.Open();

                string checkQuery = @"
            SELECT Status
            FROM Visits
            WHERE VisitId = @VisitId
            AND IsActive = 1";

                using SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@VisitId", visitId);

                object? result = checkCmd.ExecuteScalar();

                if (result == null)
                    return (404, "Visit not found");

                string status = result.ToString() ?? "";

                if (status != "Waiting")
                    return (400, "Only waiting visits can be rejected");

                string updateQuery = @"
            UPDATE Visits
            SET Status = 'Rejected',
                CancelReasonType = @ReasonType,
                CancelReasonComment = @Comments,
                UpdatedDate = GETDATE(),
                UpdatedBy = @UpdatedBy
            WHERE VisitId = @VisitId";

                using SqlCommand updateCmd = new SqlCommand(updateQuery, con);

                updateCmd.Parameters.AddWithValue("@ReasonType", request.ReasonType);
                updateCmd.Parameters.AddWithValue("@Comments", request.Comments);

                // prefer authenticated user if available; fallback to 'Admin' if not retrievable
                string updatedBy = "Admin";
                updateCmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);

                updateCmd.Parameters.AddWithValue("@VisitId", visitId);

                updateCmd.ExecuteNonQuery();

                return (200, "Visit rejected successfully");
            }
            catch (Exception ex)
            {
                return (500, ex.Message);
            }
        }

        public (List<VisitReportResponse>? visits, string? error) GetWaitingVisits(DateTime visitDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (null, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                string query = @"
            SELECT
                vt.VisitId,
                vt.VisitorId,
                v.Name,
                v.PhoneNumber,
                vt.VisitDate,
                vt.Status,
                vt.CheckIn,
                vt.CheckOut,
                vt.CreatedDate
            FROM Visits vt
            INNER JOIN Visitors v
                ON vt.VisitorId = v.VisitorId
            WHERE vt.VisitDate = @VisitDate
            AND vt.Status = 'Waiting'
            AND vt.IsActive = 1
            ORDER BY vt.CreatedDate ASC";

                using SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@VisitDate", visitDate.Date);

                con.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                List<VisitReportResponse> visits = new();

                while (reader.Read())
                {
                    visits.Add(new VisitReportResponse
                    {
                        VisitId = Convert.ToInt32(reader["VisitId"]),
                        VisitorId = Convert.ToInt32(reader["VisitorId"]),
                        Name = reader["Name"].ToString() ?? "",
                        PhoneNumber = reader["PhoneNumber"].ToString() ?? "",
                        VisitDate = Convert.ToDateTime(reader["VisitDate"]),
                        Status = reader["Status"].ToString() ?? "",
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        CheckIn = Convert.ToBoolean(reader["CheckIn"]),
                        CheckOut = Convert.ToBoolean(reader["CheckOut"])
                    });
                }

                return (visits, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public (List<VisitReportResponse>? visits, string? error) GetRejectedVisits()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (null, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                string query = @"
            SELECT
                vt.VisitId,
                vt.VisitorId,
                v.Name,
                v.PhoneNumber,
                vt.VisitDate,
                vt.Status,
                vt.CancelReasonType,
                vt.CancelReasonComment,
                vt.CreatedDate,
                vt.CheckIn,
                vt.CheckOut
            FROM Visits vt
            INNER JOIN Visitors v
                ON vt.VisitorId = v.VisitorId
            WHERE vt.Status = 'Rejected'
            AND vt.IsActive = 1
            ORDER BY vt.CreatedDate DESC";

                using SqlCommand cmd = new SqlCommand(query, con);

                con.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                List<VisitReportResponse> visits = new();

                while (reader.Read())
                {
                    visits.Add(new VisitReportResponse
                    {
                        VisitId = Convert.ToInt32(reader["VisitId"]),
                        VisitorId = Convert.ToInt32(reader["VisitorId"]),
                        Name = reader["Name"].ToString() ?? "",
                        PhoneNumber = reader["PhoneNumber"].ToString() ?? "",
                        VisitDate = Convert.ToDateTime(reader["VisitDate"]),
                        Status = reader["Status"].ToString() ?? "",
                        ReasonType = reader["CancelReasonType"].ToString() ?? "",
                        Comments = reader["CancelReasonComment"].ToString() ?? "",
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        CheckIn = Convert.ToBoolean(reader["CheckIn"]),
                        CheckOut = Convert.ToBoolean(reader["CheckOut"]) 
                    });
                }

                return (visits, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public (List<VisitReportResponse>? visits, string? error) GetCancelledVisits(DateTime visitDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (null, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                string query = @"
            SELECT
                vt.VisitId,
                vt.VisitorId,
                v.Name,
                v.PhoneNumber,
                vt.VisitDate,
                vt.Status,
                vt.CancelReasonType,
                vt.CancelReasonComment,
                vt.CreatedDate,
                vt.CheckIn,
                vt.CheckOut
            FROM Visits vt
            INNER JOIN Visitors v
                ON vt.VisitorId = v.VisitorId
            WHERE vt.VisitDate = @VisitDate
            AND vt.Status = 'Cancelled'
            AND vt.IsActive = 1
            ORDER BY vt.CreatedDate ASC";

                using SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@VisitDate", visitDate.Date);

                con.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                List<VisitReportResponse> visits = new();

                while (reader.Read())
                {
                    visits.Add(new VisitReportResponse
                    {
                        VisitId = Convert.ToInt32(reader["VisitId"]),
                        VisitorId = Convert.ToInt32(reader["VisitorId"]),
                        Name = reader["Name"].ToString() ?? "",
                        PhoneNumber = reader["PhoneNumber"].ToString() ?? "",
                        VisitDate = Convert.ToDateTime(reader["VisitDate"]),
                        Status = reader["Status"].ToString() ?? "",
                        ReasonType = reader["CancelReasonType"].ToString() ?? "",
                        Comments = reader["CancelReasonComment"].ToString() ?? "",
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                        CheckIn = Convert.ToBoolean(reader["CheckIn"]),
                        CheckOut = Convert.ToBoolean(reader["CheckOut"]) 
                    });
                }

                return (visits, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public (DashboardResponse? dashboard, string? error) GetDashboard(DateTime visitDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (null, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                con.Open();

                DashboardResponse dashboard = new();

                string scheduledQuery = @"
            SELECT COUNT(*)
            FROM Visits
            WHERE VisitDate = @VisitDate
            AND Status = 'Scheduled'
            AND IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(scheduledQuery, con))
                {
                    cmd.Parameters.AddWithValue("@VisitDate", visitDate.Date);
                    dashboard.Scheduled = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string waitingQuery = @"
            SELECT COUNT(*)
            FROM Visits
            WHERE VisitDate = @VisitDate
            AND Status = 'Waiting'
            AND IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(waitingQuery, con))
                {
                    cmd.Parameters.AddWithValue("@VisitDate", visitDate.Date);
                    dashboard.Waiting = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string completedQuery = @"
            SELECT COUNT(*)
            FROM Visits
            WHERE VisitDate = @VisitDate
            AND CheckIn = 1
            AND CheckOut = 1
            AND IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(completedQuery, con))
                {
                    cmd.Parameters.AddWithValue("@VisitDate", visitDate.Date);
                    dashboard.Completed = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string cancelledQuery = @"
            SELECT COUNT(*)
            FROM Visits
            WHERE VisitDate = @VisitDate
            AND Status = 'Cancelled'
            AND IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(cancelledQuery, con))
                {
                    cmd.Parameters.AddWithValue("@VisitDate", visitDate.Date);
                    dashboard.Cancelled = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string rejectedQuery = @"
            SELECT COUNT(*)
            FROM Visits
            WHERE VisitDate = @VisitDate
            AND Status = 'Rejected'
            AND IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(rejectedQuery, con))
                {
                    cmd.Parameters.AddWithValue("@VisitDate", visitDate.Date);
                    dashboard.Rejected = Convert.ToInt32(cmd.ExecuteScalar());
                }

                return (dashboard, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public (List<VisitorHistoryResponse>? history, string? error) GetVisitorHistory(int visitorId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (null, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                string query = @"
            SELECT
                VisitId,
                VisitDate,
                Status,
                CheckIn,
                CheckOut
            FROM Visits
            WHERE VisitorId = @VisitorId
            AND IsActive = 1
            ORDER BY VisitDate DESC";

                using SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@VisitorId", visitorId);

                con.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                List<VisitorHistoryResponse> history = new();

                while (reader.Read())
                {
                    history.Add(new VisitorHistoryResponse
                    {
                        VisitId = Convert.ToInt32(reader["VisitId"]),
                        VisitDate = Convert.ToDateTime(reader["VisitDate"]),
                        Status = reader["Status"].ToString() ?? "",
                        CheckIn = Convert.ToBoolean(reader["CheckIn"]),
                        CheckOut = Convert.ToBoolean(reader["CheckOut"]) 
                    });
                }

                return (history, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public (int statusCode, string message) UpdateVisitor(int visitorId, UpdateVisitorRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (500, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                con.Open();

                string existsQuery = @"
            SELECT COUNT(*)
            FROM Visitors
            WHERE VisitorId = @VisitorId
            AND IsActive = 1";

                using SqlCommand existsCmd = new SqlCommand(existsQuery, con);

                existsCmd.Parameters.AddWithValue("@VisitorId", visitorId);

                int exists = Convert.ToInt32(existsCmd.ExecuteScalar());

                if (exists == 0)
                    return (404, "Visitor not found");

                string duplicateQuery = @"
            SELECT COUNT(*)
            FROM Visitors
            WHERE PhoneNumber = @PhoneNumber
            AND VisitorId <> @VisitorId
            AND IsActive = 1";

                using SqlCommand duplicateCmd = new SqlCommand(duplicateQuery, con);

                duplicateCmd.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber);
                duplicateCmd.Parameters.AddWithValue("@VisitorId", visitorId);

                int duplicate = Convert.ToInt32(duplicateCmd.ExecuteScalar());

                if (duplicate > 0)
                    return (400, "Phone number already exists");

                string getOldDataQuery = @"
    SELECT Name, PhoneNumber, BirthDate
    FROM Visitors
    WHERE VisitorId = @VisitorId";

                string oldName = "";
                string oldPhone = "";
                DateTime oldBirthDate = DateTime.MinValue;

                using (SqlCommand oldCmd = new SqlCommand(getOldDataQuery, con))
                {
                    oldCmd.Parameters.AddWithValue("@VisitorId", visitorId);

                    using SqlDataReader reader = oldCmd.ExecuteReader();

                    if (reader.Read())
                    {
                        oldName = reader["Name"].ToString() ?? "";
                        oldPhone = reader["PhoneNumber"].ToString() ?? "";
                        oldBirthDate = Convert.ToDateTime(reader["BirthDate"]);
                    }
                }


                string updateQuery = @"
            UPDATE Visitors
            SET
                Name = @Name,
                PhoneNumber = @PhoneNumber,
                BirthDate = @BirthDate,
                UpdatedDate = GETDATE(),
                UpdatedBy = 'Admin'
            WHERE VisitorId = @VisitorId";

                using SqlCommand updateCmd = new SqlCommand(updateQuery, con);

                updateCmd.Parameters.AddWithValue("@Name", request.Name);
                updateCmd.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber);
                updateCmd.Parameters.AddWithValue("@BirthDate", request.BirthDate);
                updateCmd.Parameters.AddWithValue("@VisitorId", visitorId);

                updateCmd.ExecuteNonQuery();

                return (200, "Visitor updated successfully");
            }
            catch (Exception ex)
            {
                return (500, ex.Message);
            }
        }

        public (int statusCode, string message) DeleteVisitor(int visitorId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (500, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                con.Open();

                string checkQuery = @"
            SELECT COUNT(*)
            FROM Visitors
            WHERE VisitorId = @VisitorId
            AND IsActive = 1";

                using SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@VisitorId", visitorId);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count == 0)
                    return (404, "Visitor not found");

                string deleteQuery = @"
            UPDATE Visitors
            SET
                IsActive = 0,
                DeletedDate = GETDATE(),
                DeletedBy = 'Admin',
                UpdatedDate = GETDATE(),
                UpdatedBy = 'Admin'
            WHERE VisitorId = @VisitorId";

                using SqlCommand deleteCmd = new SqlCommand(deleteQuery, con);

                deleteCmd.Parameters.AddWithValue("@VisitorId", visitorId);

                deleteCmd.ExecuteNonQuery();

                return (200, "Visitor deleted successfully");
            }
            catch (Exception ex)
            {
                return (500, ex.Message);
            }
        }

        public (List<VisitorAuditResponse>? audits, string? error) GetVisitorAudit(int visitorId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (null, "Database connection string is not configured.");

                using SqlConnection con = new SqlConnection(_connectionString);

                string query = @"
SELECT
    AuditId,
    VisitorId,
    VisitId,
    Name,
    PhoneNumber,
    BirthDate,
    VisitDate,
    Status,
    CheckIn,
    CheckOut,
    CreatedDate,
    CreatedBy
FROM VisitorAudit
WHERE VisitorId = @VisitorId
ORDER BY CreatedDate DESC";

                using SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@VisitorId", visitorId);

                con.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                List<VisitorAuditResponse> audits = new();

                while (reader.Read())
                {
                    audits.Add(new VisitorAuditResponse
                    {
                        AuditId = Convert.ToInt32(reader["AuditId"]),
                        VisitorId = Convert.ToInt32(reader["VisitorId"]),
                        VisitId = Convert.ToInt32(reader["VisitId"]),

                        Name = reader["Name"].ToString() ?? "",

                        PhoneNumber = reader["PhoneNumber"].ToString() ?? "",

                        BirthDate = Convert.ToDateTime(reader["BirthDate"]),

                        VisitDate = Convert.ToDateTime(reader["VisitDate"]),

                        Status = reader["Status"].ToString() ?? "",

                        CheckIn = Convert.ToBoolean(reader["CheckIn"]),

                        CheckOut = Convert.ToBoolean(reader["CheckOut"]),

                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),

                        CreatedBy = reader["CreatedBy"].ToString() ?? ""
                    });
                }

                return (audits, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }
    }
}
