﻿using Klinika.Core;
using Klinika.Core.Database;
using Klinika.Requests.Interfaces;
using Klinika.Requests.Models;

namespace Klinika.Requests.Repositories
{
    public class VacationRequestRepository : Repository, IVacationRequestRepo
    {
        public VacationRequestRepository() : base() { }
        public List<VacationRequest> GetAll(int doctorID)
        {
            string getAllQuerry = "SELECT * " +
                                  "FROM [VacationRequest] " +
                                  "WHERE DoctorID = @DoctorID";

            var result = database.ExecuteSelectCommand(getAllQuerry, ("@DoctorID", doctorID));
            return GenerateList(result);
        }
        public List<VacationRequest> GetAll()
        {
            string getAllQuerry = "SELECT * " +
                                  "FROM [VacationRequest]";

            var result = DatabaseConnection.GetInstance().ExecuteSelectCommand(getAllQuerry);
            return GenerateList(result);
        }
        public int Create(VacationRequest vacationRequest)
        {
            string createQuery = "INSERT INTO [VacationRequest] " +
                "(DoctorID,FromDate,ToDate,Reason,Status,Emergency,DenyReason) " +
                "OUTPUT INSERTED.ID " +
                "VALUES (@DoctorID,@FromDate,@ToDate,@Reason,@Status,@Emergency,@DenyReason)";

            var id = (int)database.ExecuteNonQueryScalarCommand(
                createQuery,
                ("@DoctorID", vacationRequest.doctorID),
                ("@FromDate", vacationRequest.fromDate),
                ("@ToDate", vacationRequest.toDate),
                ("@Reason", vacationRequest.reason),
                ("@Status", vacationRequest.status),
                ("@Emergency", vacationRequest.emergency),
                ("@DenyReason", vacationRequest.denyReason));

            return id;
        }
        private List<VacationRequest> GenerateList(List<object> input)
        {
            var output = new List<VacationRequest>();
            foreach (object row in input)
            {
                var vacationRequest = new VacationRequest
                (
                    id: Convert.ToInt32(((object[])row)[0].ToString()),
                    doctorID: Convert.ToInt32(((object[])row)[1].ToString()),
                    fromDate: Convert.ToDateTime(((object[])row)[2].ToString()),
                    toDate: Convert.ToDateTime(((object[])row)[3].ToString()),
                    reason: DatabaseConnection.CheckNull<string>(((object[])row)[4]),
                    status: Convert.ToChar(((object[])row)[5].ToString()),
                    emergency: DatabaseConnection.CheckNull<bool>(((object[])row)[6]),
                    denyReason: DatabaseConnection.CheckNull<string>(((object[])row)[7])
                );
                output.Add(vacationRequest);
            }
            return output;
        }
        public void Deny(VacationRequest request)
        {
            string denyQuery = "UPDATE [VacationRequest] SET Status = 'D', DenyReason = @reason " +
                               "WHERE ID = @id";
            DatabaseConnection.GetInstance().ExecuteNonQueryCommand(denyQuery, ("@id", request.id), ("@reason", request.denyReason));
        }
        public void Approve(VacationRequest request)
        {
            string approveQuery = "UPDATE [VacationRequest] SET Status = 'A' WHERE ID = @id";
            DatabaseConnection.GetInstance().ExecuteNonQueryCommand(approveQuery, ("@id", request.id));
        }
    }
}
