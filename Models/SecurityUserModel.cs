namespace Medycally.Models
{
    public class SecurityUserModel
    {
        public int SecurityUserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int UserIdNumber { get; set; }
        public int SecurityRoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
        public int? DoctorId { get; set; }
        public string? ResetToken { get; set; }
    }
}
