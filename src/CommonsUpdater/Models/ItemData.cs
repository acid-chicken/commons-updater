namespace AcidChicken.CommonsUpdater.Models
{
    public class ItemData
    {
        public string VideoId { get; set; }
        public string Title { get; set; }
        public string Deleted { get; set; }
        public override string ToString() =>
            $"{Title} ({VideoId})";
    }
}