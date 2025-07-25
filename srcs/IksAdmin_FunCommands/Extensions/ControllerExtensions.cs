using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using IksAdminApi;

namespace IksAdmin_FunCommands.Extensions;

public static class ControllerExtensions
{
    public static void SetSpeed(this CCSPlayerController target, float speed)
    {
        CCSPlayerPawn? playerPawnValue = target.PlayerPawn.Value;
        if (playerPawnValue == null) return;
        playerPawnValue.VelocityModifier = speed;
    }
    
    public static void SetGravity(this CCSPlayerController target, float gravity)
    {
        CCSPlayerPawn? playerPawnValue = target.PlayerPawn.Value;
        if (playerPawnValue == null) return;

        playerPawnValue.GravityScale = gravity;
    }
    
    public static void TeleportTo(this CCSPlayerController target, TeleportPosition position)
    {
        CCSPlayerPawn? playerPawnValue = target.PlayerPawn.Value;
        if (playerPawnValue == null) return;
        
        playerPawnValue.Teleport(position.Position, position.Rotation);
    }

    public static string? GetActiveWeaponName(this CCSPlayerController player)
    {
        if (!player.PawnIsAlive) return null;
        
        var playerPawn = player.PlayerPawn.Value!;
        
        if (playerPawn.WeaponServices == null) return null;
        
        var activeWeapon = playerPawn.WeaponServices.ActiveWeapon.Value!.GetVData<CCSWeaponBaseVData>()!.Name;

        return activeWeapon;
    }
    
    public static CBasePlayerWeapon? GetActiveWeapon(this CCSPlayerController player)
    {
        if (!player.PawnIsAlive) return null;
        
        var playerPawn = player.PlayerPawn.Value!;
        
        if (playerPawn.WeaponServices == null) return null;
        
        var activeWeapon = playerPawn.WeaponServices.ActiveWeapon.Value!;

        return activeWeapon;
    }
    
    public static void SetMaxAmmo(this CBasePlayerWeapon weapon, int? ammoCount)
    {
        var weaponBase = new CCSWeaponBase(weapon.Handle);
        var weaponVData = weapon.GetVData<CCSWeaponBaseVData>()!;
        var weaponName = weapon.GetVData<CCSWeaponBaseVData>()!.Name;
        if (ammoCount == null)
        {
            if (!FunFunctions.DefaultWeaponMaxAmmo.ContainsKey(weaponName)) return;
            ammoCount = FunFunctions.DefaultWeaponMaxAmmo[weaponName];
        }

        var clip2Multiplier = weapon.ReserveAmmo[0] / weapon.VData!.DefaultClip1;
        
        Server.PrintToChatAll(weapon.ReserveAmmo[0].ToString());
        
        weaponBase.VData!.MaxClip1 = (int)ammoCount;
        weapon.Clip1 = (int)ammoCount;

        weapon.ReserveAmmo[0] = (int)ammoCount * clip2Multiplier;
        
        Utilities.SetStateChanged(weaponBase, "CBasePlayerWeaponVData", "m_iMaxClip1");
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
        
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
    }
    
    public static UserWeaponsSettings GetWeaponSettings(this CCSPlayerController player)
    {
        if (!FunFunctions.PlayersWeaponsSettings.TryGetValue(player.GetSteamId(), out var value))
        {
            var settings = new UserWeaponsSettings();
            FunFunctions.PlayersWeaponsSettings[player.GetSteamId()] = settings;
            return settings;
        }
        return value;
    }
    
    public static void SetScale(this CCSPlayerController target, float value)
    {
        var playerPawnValue = target.PlayerPawn.Value;
        if (playerPawnValue == null)
            return;

        playerPawnValue.CBodyComponent!.SceneNode!.Scale = value;
        Utilities.SetStateChanged(playerPawnValue, "CBaseEntity", "m_CBodyComponent");
    }
    
}