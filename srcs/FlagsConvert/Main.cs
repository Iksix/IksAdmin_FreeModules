using System.Runtime.CompilerServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using IksAdminApi;

namespace AdminsConvert;

public class Main : AdminModule
{
    public override string ModuleName => "FlagsConvert";
    public override string ModuleVersion => "1.0.2";
    public override string ModuleAuthor => "iks__";
    public override string ModuleDescription => "for IksAdmin 3.0";
    Dictionary<CCSPlayerController, List<string>> _deleteFlags = new();
    Dictionary<CCSPlayerController, uint> _defaultImmunities = new();
    public override void Ready()
    {
        Instance = this;
        PluginConfig.Set();

        Api.OnFullConnect += OnFullConnect;
    }

    public override void Unload(bool hotReload)
    {
        base.Unload(hotReload);
        Api.OnFullConnect -= OnFullConnect;
    }

    private void OnFullConnect(string steamId, string ip)
    {
        var player = PlayersUtils.GetControllerBySteamIdUnsafe(steamId);

        if (player == null) return;

        var admin = player.Admin();

        if (admin == null) return;

        foreach (var item in PluginConfig.Config.FlagsConvert)
        {
            var flag = item.Key;
            var cssFlags = item.Value;
            if (admin.CurrentFlags.Contains(flag))
            {
                AdminUtils.LogDebug(cssFlags.ToString()!);
                AdminManager.AddPlayerPermissions(player, cssFlags);
                if (!_deleteFlags.ContainsKey(player))
                    _deleteFlags.Add(player, cssFlags.ToList());
                else {
                    foreach (var f in cssFlags)
                    {
                        _deleteFlags[player].Add(f);
                    }
                }
            }   
        }
        
        var cssGroupName = $"#css/{admin.Group?.Name ?? ""}";
        
        if (PluginConfig.Config.ConvertGroup && admin.Group != null && !AdminManager.PlayerInGroup(player, cssGroupName))
        {
            AdminManager.AddPlayerToGroup(player, [cssGroupName]);
            
            Console.WriteLine($"Adding {player.PlayerName} to {cssGroupName}");
        }

        if (PluginConfig.Config.ConvertImmunity)
        {
            _defaultImmunities.Add(player, AdminManager.GetPlayerImmunity(player));
            AdminManager.SetPlayerImmunity(new SteamID(ulong.Parse(steamId)), (uint)admin.CurrentImmunity);
        }

        Console.WriteLine($"Group: {AdminManager.PlayerInGroup(player, cssGroupName)}");
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;
        var admin = player.Admin();
        if (admin == null) return HookResult.Continue;
        foreach (var item in PluginConfig.Config.FlagsConvert)
        {
            var flag = item.Key;
            var cssFlags = item.Value;
            if (admin.CurrentFlags.Contains(flag))
            {
                AdminUtils.LogDebug(cssFlags.ToString()!);
                AdminManager.AddPlayerPermissions(player, cssFlags);
                if (!_deleteFlags.ContainsKey(player))
                    _deleteFlags.Add(player, cssFlags.ToList());
                else {
                    foreach (var f in cssFlags)
                    {
                        _deleteFlags[player].Add(f);
                    }
                }
            }   
        }
        
        var cssGroupName = $"#css/{admin.Group?.Name ?? ""}";
        
        if (PluginConfig.Config.ConvertGroup && admin.Group != null  && !AdminManager.PlayerInGroup(player, cssGroupName))
        {
            AdminManager.AddPlayerToGroup(player, [cssGroupName]);
            
            Console.WriteLine($"Adding {player.PlayerName} to {cssGroupName}");
        }
        
        if (PluginConfig.Config.ConvertImmunity)
        {
            _defaultImmunities[player] = AdminManager.GetPlayerImmunity(player);
            AdminUtils.LogDebug($"Set {player.PlayerName} immunity to {admin.CurrentImmunity}" );
            AdminManager.SetPlayerImmunity(player, (uint)admin.CurrentImmunity);
        }
        
        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsBot || !player.IsValid || player.AuthorizedSteamID == null) return HookResult.Continue;

        var admin = player.Admin();
        if (admin == null) return HookResult.Continue;
        if (_defaultImmunities.TryGetValue(player, out var defaultImmunity))
        {
            AdminManager.SetPlayerImmunity(player, defaultImmunity);
            _defaultImmunities.Remove(player);
        }
        if (PluginConfig.Config.ConvertGroup && admin.Group != null)
        {
            AdminManager.RemovePlayerFromGroup(player, true, [$"#css/{admin.Group.Name}"]);
        }
        if (_deleteFlags.TryGetValue(player, out var flagsForDelete))
        {
            AdminManager.RemovePlayerPermissions(player, flagsForDelete.ToArray());
            _deleteFlags.Remove(player);
        }   
        return HookResult.Continue;
    }
    
}
