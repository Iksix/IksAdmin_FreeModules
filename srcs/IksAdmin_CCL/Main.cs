using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdminApi;

namespace IksAdmin_CCL;

public class ActiveCheck
{
    public required CCSPlayerController Admin;
    public required CCSPlayerController Target;
    public int Timer;
    public string? Discord;
}

public class Main : AdminModule
{
    public override string ModuleName => "IksAdmin_CCL";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "iks__";

    private List<ActiveCheck> _activeChecks = [];

    private PluginConfig _config = null!;

    public override void Ready()
    {
        _config = new PluginConfig();

        _config = _config.ReadOrCreate(AdminUtils.ConfigsDir + "/IksAdmin_Modules/ccl.json", _config);

        Api.RegisterPermission("ccl.check", "b");
    }

    public override void InitializeCommands()
    {
        Api.AddNewCommand(
            "check",
            "check player",
            "ccl.check",
            "css_check [#uid/#steamId/name/@...]",
            OnCheckCmd
        );
    }

    private void OnCheckCmd(CCSPlayerController caller, List<string> list, CommandInfo info)
    {
        var menu = Api.CreateMenu("ccl.main", Localizer["MenuTitle.Main"]);

        var players = PlayersUtils.GetOnlinePlayers();

        foreach (var target in players)
        {
            if (!Api.CanDoActionWithPlayer(caller.GetSteamId(), target.GetSteamId()))
                continue;

            menu.AddMenuOption(target.GetSteamId(), target.PlayerName, (_, _) =>
            {
                StartCheck(caller, target);   
            });            
        }

        menu.Open(caller);
    }

    private void StartCheck(CCSPlayerController caller, CCSPlayerController target)
    {
        var newCheck = new ActiveCheck()
        {
            Admin = caller,
            Target = target
        };

        newCheck.Timer = _config.TimeToSendDiscord;

        _activeChecks.Add(newCheck);
    }
}
