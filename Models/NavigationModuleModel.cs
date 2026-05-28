namespace Medycally.Models
{
    public class NavigationModuleModel
    {
        public int SecurityModuleId { get; set; }
        public int? ParentSecurityModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string? ModuleUrl { get; set; }
        public string? ModuleIcon { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
