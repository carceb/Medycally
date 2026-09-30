namespace Medycally.Models
{
    public class SecurityModuleActionModel
    {
        public int     SecurityModuleActionId { get; set; }
        public int     SecurityModuleId       { get; set; }
        public string  ActionKey              { get; set; } = string.Empty;
        public string  ActionName             { get; set; } = string.Empty;
        public int     ActionOrder            { get; set; }
        public bool    IsAllowed              { get; set; }
    }
}
