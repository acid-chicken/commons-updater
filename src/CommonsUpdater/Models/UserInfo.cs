namespace AcidChicken.CommonsUpdater.Models
{
    public class UserInfo
    {
        public string Nickname { get; set; }
#if use_global_hash
        public string GlobalsHash { get; set; }
#endif
    }
}
