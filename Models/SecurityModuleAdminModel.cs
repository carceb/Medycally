namespace Medycally.Models
{
    public class SecurityModuleAdminModel
    {
        public int     SecurityModuleId       { get; set; }
        public int?    ParentSecurityModuleId { get; set; }
        public string? ParentModuleName       { get; set; }
        public string  ModuleName             { get; set; } = string.Empty;
        public string? ModuleUrl              { get; set; }
        public string? ModuleIcon             { get; set; }
        public int     ModuleOrder            { get; set; }
        public bool    IsActive               { get; set; }
    }
}
