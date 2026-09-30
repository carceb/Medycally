using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class PricingStructure : IPricingStructure
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public PricingStructure(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public List<PricingStructureModel> GetByClinic(int clinicId)
        {
            var list = new List<PricingStructureModel>();
            using SqlConnection connection = _connectionFactory.CreateConnection();
            connection.Open();

            using SqlCommand cmd = new(
                "SELECT PricingStructureId, ClinicId, PricingStructureName, PricingStructureValue " +
                "FROM dbo.PricingStructure " +
                "WHERE ClinicId = @ClinicId " +
                "ORDER BY PricingStructureName",
                connection)
            {
                CommandType = CommandType.Text
            };
            cmd.Parameters.AddWithValue("@ClinicId", clinicId);

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                list.Add(new PricingStructureModel
                {
                    PricingStructureId    = dr.GetInt32(dr.GetOrdinal("PricingStructureId")),
                    ClinicId              = dr.GetInt32(dr.GetOrdinal("ClinicId")),
                    PricingStructureName  = dr.GetString(dr.GetOrdinal("PricingStructureName")),
                    PricingStructureValue = dr.GetDouble(dr.GetOrdinal("PricingStructureValue"))
                });
            }
            return list;
        }

        public List<PricingStructureModel> GetAll()
        {
            var list = new List<PricingStructureModel>();
            using SqlConnection connection = _connectionFactory.CreateConnection();
            connection.Open();

            using SqlCommand cmd = new(
                "SELECT ps.PricingStructureId, ps.ClinicId, ps.PricingStructureName, " +
                "       ps.PricingStructureValue, c.ClinicName " +
                "FROM   dbo.PricingStructure ps " +
                "LEFT JOIN dbo.Clinic c ON c.ClinicId = ps.ClinicId " +
                "ORDER BY c.ClinicName, ps.PricingStructureName",
                connection)
            {
                CommandType = CommandType.Text
            };

            using SqlDataReader dr = cmd.ExecuteReader();
            int clinicNameOrd = dr.GetOrdinal("ClinicName");
            while (dr.Read())
            {
                list.Add(new PricingStructureModel
                {
                    PricingStructureId    = dr.GetInt32(dr.GetOrdinal("PricingStructureId")),
                    ClinicId              = dr.GetInt32(dr.GetOrdinal("ClinicId")),
                    PricingStructureName  = dr.GetString(dr.GetOrdinal("PricingStructureName")),
                    PricingStructureValue = dr.GetDouble(dr.GetOrdinal("PricingStructureValue")),
                    ClinicName            = dr.IsDBNull(clinicNameOrd) ? null : dr.GetString(clinicNameOrd)
                });
            }
            return list;
        }

        public int AddOrEdit(PricingStructureModel model)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection();
            connection.Open();

            string sql = model.PricingStructureId == 0
                ? "INSERT INTO dbo.PricingStructure (ClinicId, PricingStructureName, PricingStructureValue) " +
                  "VALUES (@ClinicId, @Name, @Value); SELECT CAST(SCOPE_IDENTITY() AS INT);"
                : "UPDATE dbo.PricingStructure " +
                  "SET ClinicId = @ClinicId, PricingStructureName = @Name, PricingStructureValue = @Value " +
                  "WHERE PricingStructureId = @PricingStructureId; SELECT @PricingStructureId;";

            using SqlCommand cmd = new(sql, connection) { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@ClinicId", model.ClinicId);
            cmd.Parameters.AddWithValue("@Name",     model.PricingStructureName ?? string.Empty);
            cmd.Parameters.AddWithValue("@Value",    model.PricingStructureValue);
            if (model.PricingStructureId != 0)
                cmd.Parameters.AddWithValue("@PricingStructureId", model.PricingStructureId);

            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : model.PricingStructureId;
        }

        public void Delete(int pricingStructureId)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection();
            connection.Open();

            using SqlCommand cmd = new(
                "DELETE FROM dbo.PricingStructure WHERE PricingStructureId = @Id",
                connection)
            {
                CommandType = CommandType.Text
            };
            cmd.Parameters.AddWithValue("@Id", pricingStructureId);
            cmd.ExecuteNonQuery();
        }
    }
}
