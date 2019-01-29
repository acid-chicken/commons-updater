namespace AcidChicken.CommonsUpdater.Models
{
    public class TreeSummary
    {
        public string Countparent { get; set; }
        public string Countchild { get; set; }
        public TreeContent[] Parent { get; set; }
    }
}
