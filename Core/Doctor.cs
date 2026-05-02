using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class Doctor : IDoctor
    {
        private readonly ISqlConnectionFactory _db;

        public Doctor(ISqlConnectionFactory db) => _db = db;

        public List<DoctorModel> GetAll()
        {
            var list = new List<DoctorModel>();
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("Doctor_GetAll", conn) { CommandType = CommandType.StoredProcedure };
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new DoctorModel
                {
                    DoctorId          = Convert.ToInt32(r["DoctorId"]),
                    DoctorName        = r["DoctorName"]        == DBNull.Value ? null : r["DoctorName"].ToString(),
                    DoctorIdNumber    = r["DoctorIdNumber"]    == DBNull.Value ? 0    : Convert.ToInt32(r["DoctorIdNumber"]),
                    SexId             = r["SexId"]             == DBNull.Value ? 0    : Convert.ToInt32(r["SexId"]),
                    SexName           = r["SexName"]           == DBNull.Value ? null : r["SexName"].ToString(),
                    DoctorMainPhone   = r["DoctorMainPhone"]   == DBNull.Value ? null : r["DoctorMainPhone"].ToString(),
                    DoctorSecondPhone = r["DoctorSecondPhone"] == DBNull.Value ? null : r["DoctorSecondPhone"].ToString(),
                    DoctorEmail       = r["DoctorEmail"]       == DBNull.Value ? null : r["DoctorEmail"].ToString(),
                    StateId           = r["StateId"]           == DBNull.Value ? 0    : Convert.ToInt32(r["StateId"]),
                    StateName         = r["StateName"]         == DBNull.Value ? null : r["StateName"].ToString(),
                    DoctorAddress     = r["DoctorAddress"]     == DBNull.Value ? null : r["DoctorAddress"].ToString(),
                    StatusId          = Convert.ToInt32(r["StatusId"]),
                    StatusName        = r["StatusName"]        == DBNull.Value ? null : r["StatusName"].ToString(),
                    SpecialtyNames    = r["SpecialtyNames"]    == DBNull.Value ? null : r["SpecialtyNames"].ToString(),
                });
            }
            return list;
        }

        public int AddOrEdit(DoctorModel model)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("Doctor_AddOrEdit", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DoctorId",          model.DoctorId);
            cmd.Parameters.AddWithValue("@DoctorName",        (object?)model.DoctorName        ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DoctorIdNumber",    model.DoctorIdNumber);
            cmd.Parameters.AddWithValue("@SexId",             model.SexId > 0 ? (object)model.SexId : DBNull.Value);
            cmd.Parameters.AddWithValue("@DoctorMainPhone",   (object?)model.DoctorMainPhone   ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DoctorSecondPhone", (object?)model.DoctorSecondPhone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DoctorEmail",       (object?)model.DoctorEmail       ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StateId",           model.StateId > 0 ? (object)model.StateId : DBNull.Value);
            cmd.Parameters.AddWithValue("@DoctorAddress",     (object?)model.DoctorAddress     ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StatusId",          model.StatusId);
            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : model.DoctorId;
        }

        public void Delete(int doctorId)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("Doctor_Delete", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DoctorId", doctorId);
            cmd.ExecuteNonQuery();
        }

        public List<SpecialtyDoctorModel> GetSpecialties(int doctorId)
        {
            var list = new List<SpecialtyDoctorModel>();
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SpecialtyDoctor_GetByDoctorId", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DoctorId", doctorId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new SpecialtyDoctorModel
                {
                    SpecialtyId   = Convert.ToInt32(r["SpecialtyId"]),
                    SpecialtyName = r["SpecialtyName"] == DBNull.Value ? null : r["SpecialtyName"].ToString(),
                    IsAssigned    = r["IsAssigned"] != DBNull.Value && Convert.ToBoolean(r["IsAssigned"]),
                });
            }
            return list;
        }

        public void SaveSpecialties(int doctorId, List<int> specialtyIds)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SpecialtyDoctor_Save", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DoctorId",      doctorId);
            cmd.Parameters.AddWithValue("@SpecialtyIds",  specialtyIds.Count > 0
                ? (object)string.Join(",", specialtyIds)
                : DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }
}
