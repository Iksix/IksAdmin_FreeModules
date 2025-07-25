using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdminApi;

namespace IksAdmin_FunCommands;

public static class Cmd
{
    private static readonly IIksAdminApi Api = AdminUtils.CoreApi;

    private static string[] GetBlockedIdentifiers(string key)
    {
        if (Api.Config.BlockedIdentifiers.TryGetValue(key, out var arr))
            return arr;
        return [];
    }
    
    public static void RConVar(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.RConVar(caller, target!, args[1], args[2], identityType);
        }, blockedArgs: GetBlockedIdentifiers("rconvar"));
    }
    
    public static void Noclip(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        bool? state = args.Count > 1 ? bool.Parse(args[1]) : null;
        
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.Noclip(caller, target!, identityType, state);
        }, blockedArgs: GetBlockedIdentifiers("noclip"));
    }
    
    public static void Slap(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        int force = args.Count > 1 ? int.Parse(args[1]) : 1;
        int damage = args.Count > 2 ? int.Parse(args[2]) : 0;
        
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.Slap(caller, target!, identityType, force, damage);
        }, blockedArgs: GetBlockedIdentifiers("slap"));
    }
    
    public static void SetHp(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.SetHp(caller, target!, int.Parse(args[1]), identityType);
        }, blockedArgs: GetBlockedIdentifiers("hp"));
    }
    
    public static void SetSpeed(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.SetSpeed(caller, target!, float.Parse(args[1]), identityType);
        }, blockedArgs: GetBlockedIdentifiers("speed"));
    }
    
    public static void SetScale(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.SetScale(caller, target!, float.Parse(args[1]), identityType);
        }, blockedArgs: GetBlockedIdentifiers("scale"));
    }
    
    public static void SetShootSpeed(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.SetShootSpeed(caller, target!, args[1] == "default" ? null : float.Parse(args[1]), identityType);
        }, blockedArgs: GetBlockedIdentifiers("shootspeed"));
    }
    
    public static void SetCustomDamage(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.SetCustomDamage(caller, target!, args[1] == "default" ? null : int.Parse(args[1]), identityType);
        }, blockedArgs: GetBlockedIdentifiers("set_damage"));
    }
    
    public static void SetBonusDamage(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.SetBonusDamage(caller, target!, args[1] == "default" ? null : int.Parse(args[1]), identityType);
        }, blockedArgs: GetBlockedIdentifiers("add_damage"));
    }
    
    public static void SetMaxAmmo(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.SetMaxAmmo(caller, target!, args[1] == "default" ? null : int.Parse(args[1]), identityType);
        }, blockedArgs: GetBlockedIdentifiers("max_ammo"));
    }
    
    public static void SavePos(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        FunFunctions.SaveTeleportPosition(caller, args[0]);
    }
    
    public static void Teleport(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.TeleportToSavedPos(caller, target!, args[1], identityType);
        }, blockedArgs: GetBlockedIdentifiers("tp"));
    }
    
    public static void TurnTeleportOnPing(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.TurnTeleportOnPing(caller, target!, bool.Parse(args[1]), identityType);
        }, blockedArgs: GetBlockedIdentifiers("pingtp"));
    }
    
    public static void SetNoRecoil(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.SetNoRecoil(caller, target!, bool.Parse(args[1]), identityType);
        }, blockedArgs: GetBlockedIdentifiers("no_recoil"));
    }
    
    public static void SetMoney(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.SetMoney(caller, target!, int.Parse(args[1]), identityType);
        }, blockedArgs: GetBlockedIdentifiers("set_money"));
    }
    
    public static void AddMoney(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.AddMoney(caller, target!, int.Parse(args[1]), identityType);
        }, blockedArgs: GetBlockedIdentifiers("add_money"));
    }
    
    public static void TakeMoney(CCSPlayerController caller, List<string> args, CommandInfo _)
    {
        Api.DoActionWithIdentity(caller, args[0], (target, identityType) =>
        {
            FunFunctions.TakeMoney(caller, target!, int.Parse(args[1]), identityType);
        }, blockedArgs: GetBlockedIdentifiers("take_money"));
    }
}