using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class DoctorPricingStructure : IDoctorPricingStructure
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public DoctorPricingStructure(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public List<DoctorPricingStructureModel> GetByDoctor(int doctorId)
        {
            var list = new List<DoctorPricingStructureModel>();
            using SqlConnection connection = _connectionFactory.CreateConnection();
            connection.Open();

            using SqlCommand cmd = new(
                "SELECT dps.DoctorPricingStructureId, dps.DoctorId, dps.PricingStructureId, dps.StatusId, " +
                "       ps.PricingStructureName, ps.PricingStructureValue, " +
                "       c.ClinicName, s.StatusName " +
                "FROM   dbo.DoctorPricingStructure dps " +
                "INNER JOIN dbo.PricingStructure ps ON ps.PricingStructureId = dps.PricingStructureId " +
                "LEFT  JOIN dbo.Clinic           c  ON c.ClinicId            = ps.ClinicId " +
                "LEFT  JOIN dbo.Status           s  ON s.StatusId            = dps.StatusId " +
                "WHERE  dps.DoctorId = @DoctorId " +
                "ORDER BY c.ClinicName, ps.PricingStructureName",
                connection)
            {
                CommandType = CommandType.Text
            };
            cmd.Parameters.AddWithValue("@DoctorId", doctorId);

            using SqlDataReader dr = cmd.ExecuteReader();
            int clinicNameOrd = dr.GetOrdinal("ClinicName");
            int statusNameOrd = dr.GetOrdinal("StatusName");
            while (dr.Read())
            {
                list.Add(new DoctorPricingStructureModel
                {
                    DoctorPricingStructureId = dr.GetInt32(dr.GetOrdinal("DoctorPricingStructureId")),
                    DoctorId                 = dr.GetInt32(dr.GetOrdinal("DoctorId")),
                    PricingStructureId       = dr.GetInt32(dr.GetOrdinal("PricingStructureId")),
                    StatusId                 = dr.GetInt32(dr.GetOrdinal("StatusId")),
                    PricingStructureName     = dr.GetString(dr.GetOrdinal("PricingStructureName")),
                    PricingStructureValue    = dr.GetDouble(dr.GetOrdinal("PricingStructureValue")),
                    ClinicName               = dr.IsDBNull(clinicNameOrd) ? null : dr.GetString(clinicNameOrd),
                    StatusName               = dr.IsDBNull(statusNameOrd) ? null : dr.GetString(statusNameOrd)
                });
            }
            return list;
        }

        public int AddOrEdit(DoctorPricingStructureModel model)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection();
            connection.Open();

            string sql = model.DoctorPricingStructureId == 0
                ? "INSERT INTO dbo.DoctorPricingStructure (DoctorId, PricingStructureId, StatusId) " +
                  "VALUES (@DoctorId, @PricingStructureId, @StatusId); SELECT CAST(SCOPE_IDENTITY() AS INT);"
                : "UPDATE dbo.DoctorPricingStructure " +
                  "SET DoctorId = @DoctorId, PricingStructureId = @PricingStructureId, StatusId = @StatusId " +
                  "WHERE DoctorPricingStructureId = @DoctorPricingStructureId; SELECT @DoctorPricingStructureId;";

            using SqlCommand cmd = new(sql, connection) { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@DoctorId",           model.DoctorId);
            cmd.Parameters.AddWithValue("@PricingStructureId", model.PricingStructureId);
            cmd.Parameters.AddWithValue("@StatusId",           model.StatusId);
            if (model.DoctorPricingStructureId != 0)
                cmd.Parameters.AddWithValue("@DoctorPricingStructureId", model.DoctorPricingStructureId);

            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : model.DoctorPricingStructureId;
        }

        public void Delete(int doctorPricingStructureId)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection();
            connection.Open();

            using SqlCommand cmd = new(
                "DELETE FROM dbo.DoctorPricingStructure WHERE DoctorPricingStructureId = @Id",
                connection)
            {
                CommandType = CommandType.Text
            };
            cmd.Parameters.AddWithValue("@Id", doctorPricingStructureId);
            cmd.ExecuteNonQuery();
        }
    }
}
