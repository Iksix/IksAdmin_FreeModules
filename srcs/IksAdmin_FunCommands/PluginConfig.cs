using IksAdminApi;

namespace IksAdmin_FunCommands;

public class PluginConfig : PluginCFG<PluginConfig>
{
    public string DefaultFlag { get; set; } = "f";
    
    /// <summary>
    /// Возврат скоростей к дефолтным значениям в следующем раунде
    /// </summary>
    public bool CleanSpeedAfterRound { get; set; } = true; 
     
}