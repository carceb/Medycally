namespace Medycally.Models
{
    public class SecurityRoleModuleModel
    {
        public int SecurityModuleId { get; set; }
        public int? ParentSecurityModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string? ModuleUrl { get; set; }
        public string? ModuleIcon { get; set; }
        public int ModuleOrder { get; set; }
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
