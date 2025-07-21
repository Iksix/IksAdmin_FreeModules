using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using IksAdminApi;

namespace IksAdmin_WH;

public partial class Main
{
    public readonly Dictionary</*player slot*/int, 
                    Tuple</*prop 1*/CBaseModelEntity, /*prop 2*/CBaseModelEntity>> GlowingPlayers = new();

    public readonly List<CCSPlayerController> CachedPlayers = new();
    public readonly bool[] ToggleAdminEsp = new bool[72];
    
    private void RegisterListeners()
    {

        RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);
        RegisterListener<Listeners.OnClientConnected>(OnClientConnected);
        RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnected);
        RegisterListener<Listeners.CheckTransmit>(CheckTransmitListener);

        //register event listeners
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Pre);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        
    }

    private void DeregisterListeners()
    {

        RemoveListener<Listeners.OnClientAuthorized>(OnClientAuthorized);
        RemoveListener<Listeners.OnClientConnected>(OnClientConnected);
        RemoveListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnected);
        RemoveListener<Listeners.CheckTransmit>(CheckTransmitListener);

        DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Pre);
        DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        DeregisterEventHandler<EventRoundStart>(OnRoundStart);

    }

    private void OnClientAuthorized(int slot, SteamID steamid)
    {

        var player = Utilities.GetPlayerFromSlot(slot);
        if(player == null || player.IsValid is not true) return;

        if (CachedPlayers.Contains(player) is not true)
            CachedPlayers.Add(player);
        
    }

    private void OnClientConnected(int slot)
    {

        var player = Utilities.GetPlayerFromSlot(slot);
        if(player == null || player.IsValid is not true) return;

        if (CachedPlayers.Contains(player) is not true)
            CachedPlayers.Add(player);
        
    }

    private void OnClientPutInServer(int slot)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        if (player is null || player.IsBot is not true) return;

        if (CachedPlayers.Contains(player) is not true)
            CachedPlayers.Add(player);
        
    }
    private void CheckTransmitListener(CCheckTransmitInfoList infoList)
    {

        foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
        {

            if (player is null || player.IsValid is not true) continue;

            //itereate cached players
            for (int i = 0; i < CachedPlayers.Count(); i++) {
                if (ToggleAdminEsp[player.Slot] == true)
                    continue;
                    
                //stop transmitting any entity from the glowingPlayers list
                foreach (var glowingProp in GlowingPlayers)
                {

                    if (glowingProp.Value.Item1 is not null && glowingProp.Value.Item1.IsValid is true
                    && glowingProp.Value.Item2 is not null && glowingProp.Value.Item2.IsValid is true) {

                        //prop one
                        info.TransmitEntities.Remove((int)glowingProp.Value.Item1.Index);
                        //prop two
                        info.TransmitEntities.Remove((int)glowingProp.Value.Item2.Index);

                    }
                }

            }
        }
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValid) return HookResult.Continue;
        
        if (ToggleAdminEsp[player.Slot] && !player.HasPermissions("wh.use_alive"))
        {
            ToggleAdminEsp[player.Slot] = false;
        }
        
        Server.NextFrame(() =>
        {
            if (player.PawnIsAlive && !player.HasPermissions("wh.doesnt_seen"))
                SetPlayerGlowing(CachedPlayers[player.Slot], CachedPlayers[player.Slot].TeamNum);
        });

        return HookResult.Continue;
    }
    
    public void UpdateCachedPlayers()
    {
        CachedPlayers.Clear();
        foreach (var p in PlayersUtils.GetOnlinePlayers())
        {
            CachedPlayers.Add(p);
        }
    }
    
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {

        AddTimer(1f, UpdateCachedPlayers);

        return HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player is null 
        || player.IsValid is not true 
        || player.Connected is not PlayerConnectedState.PlayerConnected) return HookResult.Continue;

        RemovePlayerGlowing(player);

        return HookResult.Continue;
    }

    private void RemovePlayerGlowing(CCSPlayerController player)
    {
        if (GlowingPlayers.ContainsKey(player.Slot)) {

            if (GlowingPlayers[player.Slot].Item1 != null! && GlowingPlayers[player.Slot].Item1.IsValid
                                                           && GlowingPlayers[player.Slot].Item2 != null! && GlowingPlayers[player.Slot].Item2.IsValid ) {
                
                GlowingPlayers[player.Slot].Item1.AcceptInput("Kill");
                GlowingPlayers[player.Slot].Item2.AcceptInput("Kill");
            }
            
            GlowingPlayers.Remove(player.Slot);
        }
    }

    private void OnClientDisconnected(int slot)
    {

        var player = Utilities.GetPlayerFromSlot(slot);
        if (player == null || player.IsValid is not true) return;

        ToggleAdminEsp[slot] = false;

        if (CachedPlayers.Contains(player))
            CachedPlayers.Remove(player);
    }
    
    private void OnWhCmd(CCSPlayerController caller, List<string> args, CommandInfo info)
    {
        if (!caller.HasPermissions("wh.use_alive") && caller.PawnIsAlive)
        {
            caller.PrintToChat(Localizer["Error.CantOnWhileAlive"]);
            return;
        }
        
        ToggleAdminEsp[caller.Slot] = !ToggleAdminEsp[caller.Slot];

        if (ToggleAdminEsp[caller.Slot])
        {
            caller.PrintToChat(Localizer["Message.WhOn"]);
        }
        else
        {
            caller.PrintToChat(Localizer["Message.WhOff"]);
        }
        
        UpdateCachedPlayers();
    }

    public void SetPlayerGlowing(CCSPlayerController player, int team)
    {

        if (player is null || player.IsValid is not true 
        || player.Connected is not PlayerConnectedState.PlayerConnected) return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn is null || playerPawn.IsValid is not true) return;

        CBaseModelEntity? modelGlow = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        CBaseModelEntity? modelRelay = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");

        if (modelGlow is null || modelRelay is null  
        || modelGlow.IsValid is not true || modelRelay.IsValid is not true) return;

        var playerCBodyComponent = playerPawn.CBodyComponent;
        if (playerCBodyComponent is null) return;

        var playerSceneMode = playerCBodyComponent.SceneNode;
        if (playerSceneMode is null) return;

        string modelName = playerSceneMode.GetSkeletonInstance().ModelState.ModelName;

        modelRelay.SetModel(modelName);
        modelRelay.Spawnflags = 256u;
        modelRelay.RenderMode = RenderMode_t.kRenderNone;
        modelRelay.DispatchSpawn();

        modelGlow.SetModel(modelName);
        modelGlow.Spawnflags = 256u;
        modelGlow.DispatchSpawn();

        switch (team) {
            case 2:
                modelGlow.Glow.GlowColorOverride = Color.Orange; //T
            break;
            case 3:
                modelGlow.Glow.GlowColorOverride = Color.SkyBlue; //CT
            break;
        }
        
        modelGlow.Glow.GlowRange = 5000;
        modelGlow.Glow.GlowTeam = -1;
        modelGlow.Glow.GlowType = 3;
        modelGlow.Glow.GlowRangeMin = 100;

        modelRelay.AcceptInput("FollowEntity", playerPawn, modelRelay, "!activator");
        modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator");

        //if player already has glowing metadata remove previous one before adding new one
        if (GlowingPlayers.ContainsKey(player.Slot) is true) {

            if (GlowingPlayers[player.Slot].Item1 is not null && GlowingPlayers[player.Slot].Item1.IsValid is true
            && GlowingPlayers[player.Slot].Item2 is not null && GlowingPlayers[player.Slot].Item2.IsValid is true) {
                
                //remove previous modelRelay prop
                GlowingPlayers[player.Slot].Item1.AcceptInput("Kill");
                //remove previous modelGlow prop
                GlowingPlayers[player.Slot].Item2.AcceptInput("Kill");
            }

            //remove player from the list
            GlowingPlayers.Remove(player.Slot);
        }

        //add player to the list
        GlowingPlayers.Add(player.Slot, new Tuple<CBaseModelEntity, CBaseModelEntity>(modelRelay,modelGlow));
        Console.WriteLine("Set glow for" + player.PlayerName);


    }

}