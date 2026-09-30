using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class LabTest : ILabTest
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public LabTest(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public List<LabTestModel> GetAll()
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                SqlCommand cmd = new(
                    "SELECT LabTestId, LabTestName " +
                    "FROM dbo.LabTest " +
                    "ORDER BY LabTestName",
                    connection)
                {
                    CommandType = CommandType.Text
                };

                List<LabTestModel> list = [];

                using SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new LabTestModel
                    {
                        LabTestId   = dr.GetInt32(dr.GetOrdinal("LabTestId")),
                        LabTestName = dr.GetString(dr.GetOrdinal("LabTestName"))
                    });
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el catálogo de exámenes", ex);
            }
        }
    }
}
