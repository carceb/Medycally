namespace Medycally.Models
{
    public class SecurityRoleModel
    {
        public int SecurityRoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int RoleLevel { get; set; }
    }
}
