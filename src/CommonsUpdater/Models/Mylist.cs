namespace AcidChicken.CommonsUpdater.Models
{
    public class Mylist
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public MylistItem[] Mylistitem { get; set; }

        public override string ToString() =>
            $"{Name} ({Id})";
    }
}
