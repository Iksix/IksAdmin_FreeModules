using IksAdminApi;

namespace IksAdmin_CCL;

public class PluginConfig : PluginCFG<PluginConfig>
{
    public int TimeToSendDiscord { get; set; } = 300; // SECONDS

    public int BanTime { get; set; } = 0;
    public int RejectBanTime { get; set; } = 0;

    public string[] ContactCommand = ["contact", "c"];
}