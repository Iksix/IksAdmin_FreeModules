using CounterStrikeSharp.API;
using IksAdminApi;

namespace IksAdmin_AllSpecs;

public class Main : AdminModule
{
    public override string ModuleName { get; } = "IksAdmin_AllSpecs";
    
    public override string ModuleVersion { get; } = "1.0.0";

    public override string ModuleDescription { get; } = "iks__";

    public override void Ready()
    {
        Api.RegisterPermission("specs.all", "b");
        Api.OnFullConnect += OnFullConnect;
        
        AddCommand("css_fc", "", (player, info) =>
        {
            player!.ReplicateConVar("mp_forcecamera", "0");
            Server.PrintToChatAll("111");
        });
    }

    private void OnFullConnect(string steamId, string ip)
    {
        var player = PlayersUtils.GetControllerBySteamIdUnsafe(steamId);
        
        if (player == null) return;
        
        if (!player.HasPermissions("specs.all")) return;
        
        player.ReplicateConVar("mp_forcecamera", "0");
    }
    
}