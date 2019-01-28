namespace AcidChicken.CommonsUpdater.Models
{
    public class ContentInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }

        public override string ToString() =>
            Id == "none" ? "オリジナル作品" :
            Title is null ? Id : $"{Title} ({Id})";
    }
}
