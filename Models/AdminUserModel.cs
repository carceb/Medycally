namespace Medycally.Models
{
    public class AdminUserModel
    {
        public int SecurityUserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public int UserIdNumber { get; set; }
        public int SecurityRoleId { get; set; }
        public string? RoleName { get; set; }
        public int StatusId { get; set; }
        public string? UserPassword { get; set; }
        public int? DoctorId { get; set; }
        public string? DoctorName { get; set; }
    }
}
