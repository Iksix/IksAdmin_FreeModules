using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using IksAdmin_FunCommands.Extensions;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin_FunCommands;

public class UserWeaponsSettings
{
    public Dictionary<string, float> CustomShootSpeed { get; set; } = []; 
    
    /// <summary>
    /// В ивенте будет по формуле: CustomDamage
    /// </summary>
    public Dictionary<string, int> CustomDamage { get; set; } = []; 
    
    /// <summary>
    /// В ивенте будет по формуле: Урон + CustomDamage
    /// </summary>
    public Dictionary<string, int> DamageBonus { get; set; } = []; 
    
    public bool NoRecoil { get; set; } = false; 
}

public static class FunFunctions
{
    private static readonly IIksAdminApi Api = AdminUtils.CoreApi;
    
    private static readonly IStringLocalizer Localizer = Main.StringLocalizer;
    
    /// <summary>
    /// Key = Slot <br/>
    /// Value = Speed
    /// </summary>
    private static readonly Dictionary<int, float> PlayersSpeed = [];
    
    /// <summary>
    /// Слоты игроков у которых включена функция телепорта на метку
    /// </summary>
    public static readonly List<int> PlayersWithTeleportOnPing = [];
    
    /// <summary>
    /// Сохранённые позиции для телепорта <br/>
    /// Key = SteamId
    /// </summary>
    public static readonly Dictionary<string, Dictionary<string, TeleportPosition>> PlayersSavedTeleportPositions = [];
    
    public static readonly Dictionary<string, UserWeaponsSettings> PlayersWeaponsSettings = [];

    public static Dictionary<string, int> DefaultWeaponMaxAmmo = [];

    public static string[] ValidWeapons =
    [
        "deagle", "elite", "fiveseven", "glock", "ak47", "aug", "awp", "famas", "g3sg1", "galilar", "m249", "m4a1",
        "mac10", "p90", "mp5sd", "ump45", "xm1014", "bizon", "mag7", "negev", "sawedoff", "tec9", "hkp2000", "mp7",
        "mp9", "nova", "p250", "scar20", "sg556", "ssg08", "m4a1_silencer", "usp_silencer", "cz75a", "revolver",
        "flashbang", "hegrenade", "smokegrenade", "molotov", "decoy", "incgrenade", "taser", "healthshot", "tagrenade",
        "shield",
        "knife", "knifegg", "bayonet", "knife_flip", "knife_gut", "knife_karambit", "knife_m9_bayonet",
        "knife_tactical", "knife_falchion", "knife_survival_bowie", "knife_butterfly", "knife_push", "knife_ursus",
        "knife_gypsy_jackknife", "knife_stiletto", "knife_widowmaker", "knife_skeleton", "knife_outdoor", "knife_canis",
        "knife_cord", "knife_css"
    ];

    
    public static void RConVar(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        string convar, 
        string value, 
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateTarget(caller, target, identityType))
            return;

        target.ReplicateConVar(convar, value);
        
        caller.Print(Localizer["Message.RConVar"].AReplace(["target"], [target.PlayerName]));
    }

    public static void Noclip(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        IdentityType identityType = IdentityType.SteamId,
        bool? state = null
    )
    {
        if (!ValidateAliveTarget(caller, target, identityType))
            return;
        
        var pawn = target.PlayerPawn.Value;
        if (pawn == null) return;
        
        if (state == null)
        {
            
            if (pawn.MoveType == MoveType_t.MOVETYPE_NOCLIP)
            {
                pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2); // walk
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
            }
            else
            {
                pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
                Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 8); // noclip
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
            }
        }
        else
        {
            if ((bool)state)
            {
                pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
                Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 8); // noclip
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
            } 
            
            pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 8); // noclip
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }
        
        caller.Print(Localizer["Message.Noclip"].AReplace(
            ["target", "value"], 
            [target.PlayerName, pawn.MoveType == MoveType_t.MOVETYPE_NOCLIP]
            ));
    }
    
    public static void TeleportToSavedPos(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        string positionKey,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateAliveTarget(caller, target, identityType))
            return;
        
        var pawn = target.PlayerPawn.Value;
        if (pawn == null) return;

        if (!PlayersSavedTeleportPositions.TryGetValue(caller.GetSteamId(), out var positions))
        {
            caller.Print(Localizer["Error.UndefinedPosition"]);
            return;
        }
        
        if (!positions.TryGetValue(positionKey, out var position))
        {
            caller.Print(Localizer["Error.UndefinedPosition"]);
            return;
        }
        
        target.TeleportTo(position);
        
        caller.Print(Localizer["Message.TeleportTo"].AReplace(
            ["target", "key"],
            [target.PlayerName, positionKey]
            ));
    }
    
    public static void SaveTeleportPosition(
        CCSPlayerController caller, 
        string positionKey
    )
    {
        var origin = caller.Pawn.Value!.AbsOrigin!.Clone();
        var angle = caller.Pawn.Value!.AbsRotation!.Clone();

        if (!PlayersSavedTeleportPositions.ContainsKey(caller.GetSteamId()))
            PlayersSavedTeleportPositions[caller.GetSteamId()] = [];
        
        PlayersSavedTeleportPositions[caller.GetSteamId()][positionKey] = new TeleportPosition(origin, angle);
        
        caller.Print(Localizer["Message.PositionSaved"].AReplace(
            ["key"],
            [positionKey]
        ));
    }
    
    public static void TurnFreeze(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        bool? state,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateAliveTarget(caller, target, identityType))
            return;

        if (state == null)
        {
            state = target.PlayerPawn.Value!.MoveType != MoveType_t.MOVETYPE_OBSOLETE;
        }
        
        if ((bool)state)
        {
            target.PlayerPawn.Value!.Freeze();
            caller.Print(Localizer["Message.Freeze"].AReplace(
                ["name"],
                [target.PlayerName]
            ));
        }
        else
        {
            target.PlayerPawn.Value!.Unfreeze();
            caller.Print(Localizer["Message.Unfreeze"].AReplace(
                ["name"],
                [target.PlayerName]
            ));
        }
    }
    
    public static void GiveWeapon(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        string weaponId,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateAliveTarget(caller, target, identityType))
            return;

        if (!ValidWeapons.Contains(weaponId))
        {
            caller.Print(Localizer["Error.WeaponNotFound"].AReplace(["value"], [weaponId]));
            return;
        }
        
        target.GiveNamedItem($"weapon_{weaponId}");
        
        caller.Print(Localizer["Message.GiveWeapon"].AReplace(
            ["name", "value"],
            [target.PlayerName, Localizer[weaponId]]
        ));
    }

    
    public static void TurnTeleportOnPing(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        bool state,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateTarget(caller, target, identityType))
            return;
        var slot = target.Slot;
        
        PlayersWithTeleportOnPing.Remove(slot);
        
        if (state)
        {
            PlayersWithTeleportOnPing.Add(slot);
        }
        
        caller.Print(Localizer["Message.TurnTeleportOnPing"].AReplace(
            ["target", "value"],
            [target.PlayerName, state]
        ));
    }
    
    public static void SetNoRecoil(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        bool state,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (target.IsBot) return;
        
        var slot = target.Slot;

        var settings = target.GetWeaponSettings().NoRecoil = state;
        
        caller.Print(Localizer["Message.NoRecoil"].AReplace(
            ["target", "value"],
            [target.PlayerName, state]
        ));
    }
    
    public static void SetMoney(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        int moneyAmount,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateTarget(caller, target, identityType))
            return;

        if (target.InGameMoneyServices == null)
        {
            caller.Print($"For some reason, set money for player {target.PlayerName} is impossible");
            return;
        }

        target.InGameMoneyServices.Account = moneyAmount;
        
        Utilities.SetStateChanged(target, "CCSPlayerController", "m_pInGameMoneyServices");
        
        caller.Print(Localizer["Message.SetMoney"].AReplace(
            ["target", "value"], 
            [target.PlayerName, moneyAmount]
        ));
    }
    
    public static void SetSpeed(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        float speed,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateAliveTarget(caller, target, identityType))
            return;

        target.SetSpeed(speed);
        
        caller.Print(Localizer["Message.SetSpeed"].AReplace(
            ["target", "value"], 
            [target.PlayerName, speed]
        ));
    }
    
    public static void SetScale(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        float scale,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateAliveTarget(caller, target, identityType))
            return;

        target.SetScale(scale);
        
        caller.Print(Localizer["Message.SetScale"].AReplace(
            ["target", "value"], 
            [target.PlayerName, scale]
        ));
    }
    
    public static void AddMoney(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        int moneyAmount,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateTarget(caller, target, identityType))
            return;

        if (target.InGameMoneyServices == null)
        {
            caller.Print($"For some reason, set money for player {target.PlayerName} is impossible");
            return;
        }

        target.InGameMoneyServices.Account += Math.Abs(moneyAmount);
        
        Utilities.SetStateChanged(target, "CCSPlayerController", "m_pInGameMoneyServices");
        
        caller.Print(Localizer["Message.AddMoney"].AReplace(
            ["target", "value"], 
            [target.PlayerName, moneyAmount]
        ));
    }
    
    public static void TakeMoney(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        int moneyAmount,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateTarget(caller, target, identityType))
            return;

        if (target.InGameMoneyServices == null)
        {
            caller.Print($"For some reason, set money for player {target.PlayerName} is impossible");
            return;
        }

        if (target.InGameMoneyServices.Account - Math.Abs(moneyAmount) < 0)
            target.InGameMoneyServices.Account = 0;
        else target.InGameMoneyServices.Account -= Math.Abs(moneyAmount);
        
        Utilities.SetStateChanged(target, "CCSPlayerController", "m_pInGameMoneyServices");
        
        caller.Print(Localizer["Message.TakeMoney"].AReplace(
            ["target", "value"], 
            [target.PlayerName, moneyAmount]
        ));
    }
    
    public static void SetHp(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        int health,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateAliveTarget(caller, target, identityType))
            return;

        if (health > target.PlayerPawn.Value!.MaxHealth)
        {
            target.PlayerPawn.Value.MaxHealth = health;
            Utilities.SetStateChanged(target.PlayerPawn.Value, "CBaseEntity", "m_iMaxHealth");
        }
        
        if (health < 1) health = 1;
        
        target.PlayerPawn.Value.Health = health;
        
        Utilities.SetStateChanged(target.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
        
        caller.Print(Localizer["Message.SetHp"].AReplace(
            ["target", "value"],
            [target.PlayerName, health]
        ));
    }
    
    public static void SetBonusDamage(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        int? damageBonus,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateAliveTarget(caller, target, identityType))
            return;
        
        if (target.IsBot) return;
        
        var weapon = target.GetActiveWeaponName();

        if (weapon == null)
        {
            caller.Print(Localizer["Error.MustHandleWeapon"]);
            return;
        }

        var settings = target.GetWeaponSettings();

        if (damageBonus == null)
        {
            settings.DamageBonus.Remove(weapon);
            
            caller.Print(Localizer["Message.BonusDamage"].AReplace(
                ["target", "value", "weapon"],
                [target.PlayerName, "default", weapon]
            ));
            
            return;
        }
        
        settings.DamageBonus[weapon] = (int)damageBonus;
        
        caller.Print(Localizer["Message.BonusDamage"].AReplace(
            ["target", "value", "weapon"],
            [target.PlayerName, damageBonus, weapon]
        ));
    }
    
    public static void SetMaxAmmo(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        int? ammo,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateAliveTarget(caller, target, identityType))
            return;
        
        if (target.IsBot) return;
        
        var weapon = target.GetActiveWeaponName();

        if (weapon == null)
        {
            caller.Print(Localizer["Error.MustHandleWeapon"]);
            return;
        }
        
        var weaponBase = target.GetActiveWeapon()!;


        if (!DefaultWeaponMaxAmmo.ContainsKey(weapon))
        {

            DefaultWeaponMaxAmmo[weapon] = weaponBase.VData!.MaxClip1;
        }

        if (ammo == null)
        {
            caller.Print(Localizer["Message.MaxAmmo"].AReplace(
                ["target", "value", "weapon"],
                [target.PlayerName, "default", weapon]
            ));
            
            return;
        }
        
        weaponBase.SetMaxAmmo(ammo);
        
        caller.Print(Localizer["Message.MaxAmmo"].AReplace(
            ["target", "value", "weapon"],
            [target.PlayerName, ammo, weapon]
        ));
    }
    
    public static void SetCustomDamage(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        int? damage,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateAliveTarget(caller, target, identityType))
            return;
        
        if (target.IsBot) return;
        
        var weapon = target.GetActiveWeaponName();

        if (weapon == null)
        {
            caller.Print(Localizer["Error.MustHandleWeapon"]);
            return;
        }

        var settings = target.GetWeaponSettings();

        if (damage == null)
        {
            settings.CustomDamage.Remove(weapon);
            
            caller.Print(Localizer["Message.CustomDamage"].AReplace(
                ["target", "value", "weapon"],
                [target.PlayerName, "default", weapon]
            ));
            
            return;
        }
        
        settings.CustomDamage[weapon] = (int)damage;
        
        caller.Print(Localizer["Message.CustomDamage"].AReplace(
            ["target", "value", "weapon"],
            [target.PlayerName, damage, weapon]
        ));
    }
    
    public static void SetShootSpeed(
        CCSPlayerController caller, 
        CCSPlayerController target, 
        float? shotSpeed,
        IdentityType identityType = IdentityType.SteamId
    )
    {
        if (!ValidateAliveTarget(caller, target, identityType))
            return;
        
        if (target.IsBot) return;
        
        var weapon = target.GetActiveWeaponName();

        if (weapon == null)
        {
            caller.Print(Localizer["Error.MustHandleWeapon"]);
            return;
        }

        var settings = target.GetWeaponSettings();

        if (shotSpeed == null)
        {
            settings.CustomShootSpeed.Remove(weapon);
            
            caller.Print(Localizer["Message.ShootSpeed"].AReplace(
                ["target", "value", "weapon"],
                [target.PlayerName, "default", weapon]
            ));
            
            return;
        }
        
        settings.CustomShootSpeed[weapon] = (float)shotSpeed;
        
        caller.Print(Localizer["Message.ShootSpeed"].AReplace(
            ["target", "value", "weapon"],
            [target.PlayerName, shotSpeed, weapon]
        ));
    }
    
    public static void Slap(
        CCSPlayerController caller, 
        CCSPlayerController target,
        IdentityType identityType = IdentityType.SteamId,
        int force = 1, int damage = 0
    )
    {
        if (!ValidateAliveTarget(caller, target, identityType))
            return;
        
        var pawn = target.PlayerPawn.Value;
        if (pawn!.LifeState != (int)LifeState_t.LIFE_ALIVE)
            return;

        /* Teleport in a random direction - thank you, Mani!*/
        /* Thank you AM & al!*/
        var random = new Random();
        var vel = new Vector(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z);

        vel.X += ((random.Next(180) + 50) * ((random.Next(2) == 1) ? -1 : 1));
        vel.Y += ((random.Next(180) + 50) * ((random.Next(2) == 1) ? -1 : 1));
        vel.Z += random.Next(200) + 100;

        pawn.AbsVelocity.X = vel.X * force;
        pawn.AbsVelocity.Y = vel.Y * force;
        pawn.AbsVelocity.Z = vel.Z * force;

        if (damage <= 0)
            return;

        pawn.Health -= damage;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        if (pawn.Health <= 0)
            pawn.CommitSuicide(true, true);

        caller.Print(Localizer["Message.Slap"].AReplace(
            ["target"], 
            [target.PlayerName]
        ));
    }
    
    private static bool ValidateTarget(
        CCSPlayerController caller, 
        CCSPlayerController? target, 
        IdentityType identityType
    )
    {
        if (target == null || !target.IsValid)
        {
            if (identityType is IdentityType.Name or IdentityType.UserId or IdentityType.SteamId)
                caller.Print(Localizer["Error.TargetNotFound"]);
            
            return false;
        }

        if (!target.IsBot && !Api.CanDoActionWithPlayer(caller.GetSteamId(), target.GetSteamId()))
        {
            if (identityType is IdentityType.Name or IdentityType.UserId or IdentityType.SteamId)
                caller.Print(Localizer["Error.NotEnoughPermission"].AReplace(["target"], [target.PlayerName]));
            
            return false;
        }

        return true;
    }
    
    private static bool ValidateAliveTarget(
        CCSPlayerController caller, 
        CCSPlayerController? target, 
        IdentityType identityType
    )
    {
        if (!ValidateTarget(caller, target, identityType))
            return false;

        if (!target!.PawnIsAlive)
        {
            if (identityType is IdentityType.Name or IdentityType.UserId or IdentityType.SteamId)
                caller.Print(Localizer["Error.TargetMustBeAlive"]);
            
            return false;
        }

        return true;
    }
    

    public static HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (player == null || !player.PawnIsAlive) return HookResult.Continue;
        
        var slot = player.Slot;
        
        if (!PlayersSpeed.TryGetValue(slot, out var speed)) return HookResult.Continue;
        
        player.SetSpeed(speed);
        
        return HookResult.Continue;
    }

    public static HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (Main.Config.CleanSpeedAfterRound)
            PlayersSpeed.Clear();
            
        return HookResult.Continue;
    }
    
    public static HookResult OnPlayerPing(EventPlayerPing @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValid || player.IsBot || !player.PawnIsAlive) return HookResult.Continue;
        
        if (!PlayersWithTeleportOnPing.Contains(player.Slot)) return HookResult.Continue;
        
        var vector = new Vector(@event.X, @event.Y, @event.Z);
        
        player.PlayerPawn.Value!.Teleport(vector);
        
        info.DontBroadcast = true;
        return HookResult.Stop;
    }

    public static void OnClientDisconnect(int playerSlot)
    {
        PlayersSpeed.Remove(playerSlot);
        PlayersWithTeleportOnPing.Remove(playerSlot);
        
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        
        if (player == null || player.AuthorizedSteamID == null) return;

        PlayersWeaponsSettings.Remove(player.GetSteamId());
    }
    
    public static HookResult OnTakeDamage(DynamicHook hook)
    {
        
        var entity = hook.GetParam<CEntityInstance>(0);
        var info = hook.GetParam<CTakeDamageInfo>(1);

        if (!entity.IsValid || !info.Attacker.IsValid)
            return HookResult.Continue;

        if (entity.DesignerName != "player" && info.Attacker.Value?.DesignerName != "player")
            return HookResult.Continue;

        CCSPlayerPawn? playerPawn = entity.As<CCSPlayerPawn>();
        CCSPlayerPawn? attackerPawn = info.Attacker.Value?.As<CCSPlayerPawn>();

        if (attackerPawn == null) return HookResult.Continue;

        CCSPlayerController? player = playerPawn.OriginalController.Value;
        CCSPlayerController? attacker = attackerPawn.OriginalController.Value;

        if (player == null) return HookResult.Continue;
        if (attacker == null || attacker.AuthorizedSteamID == null) return HookResult.Continue;

        var settings = attacker.GetWeaponSettings();

        var weapon = attacker.GetActiveWeaponName();
        
        if (weapon == null) return HookResult.Continue;
        
        if (settings.CustomDamage.TryGetValue(weapon, out var customDamage))
        {
            info.Damage = customDamage;
        }
        else if (settings.DamageBonus.TryGetValue(weapon, out var damageBonus))
        {
            info.Damage += damageBonus;
        }
        
        return HookResult.Continue;
    }

    public static HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (player == null || !player.IsValid || player.IsBot || player.AuthorizedSteamID == null) return HookResult.Continue;

        var weaponName = @event.Weapon;
        var settings = player.GetWeaponSettings();
        
        if (settings.NoRecoil)
        {
            Server.NextFrame(() =>
            {
                NoRecoil(player);
            });
        }
        
        if (!settings.CustomShootSpeed.TryGetValue(weaponName, out var speed))
            return HookResult.Continue;
        
        Server.NextFrame(() =>
        {
            var weapon = player.GetActiveWeapon();

            var tickCount = Server.TickCount;
        
            int rateOfFire;
        
            int nextTickAttack = weapon!.NextPrimaryAttackTick;
        
            rateOfFire = nextTickAttack - tickCount;
        
            rateOfFire = (int)(rateOfFire / speed);
            weapon.NextPrimaryAttackTick = tickCount + rateOfFire + 1;

            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
            if (settings.NoRecoil)
            {
                NoRecoil(player);
            }
        });
        
        return HookResult.Continue;
    }

    public static void OnClientAuthorized(int playerSlot, SteamID steamId)
    {
        PlayersWeaponsSettings[steamId.SteamId64.ToString()] = new ();
    }
    
    public static void NoRecoil(CCSPlayerController client)
    {
        if (client == null! || client.IsBot || !client.PawnIsAlive) 
        { 
            return; 
        } 
        try 
        { 
            var weapon = client.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value; 
            
            if (weapon == null) return;
            
            var weaponBase = new CCSWeaponBase(weapon.Handle); 
            
            if (!weaponBase.IsValid) return;
            
            var weaponData = weaponBase.VData!; 
            weaponBase.FlRecoilIndex = 0;
            weaponBase.IRecoilIndex = 0;
            weaponBase.AccuracyPenalty = 0;
            weaponData.InaccuracyFire.Values[0] = 0;
            weaponData.InaccuracyFire.Values[1] = 0;
            weaponData.InaccuracyMove.Values[0] = 0;
            weaponData.InaccuracyLand.Values[0] = 0;
            weaponData.InaccuracyJump.Values[0] = 0;
            weaponData.InaccuracyMove.Values[1] = 0;
            weaponData.InaccuracyLand.Values[1] = 0;
            weaponData.InaccuracyJump.Values[1] = 0;

            client.PlayerPawn.Value.AimPunchAngle.X = 0;
            client.PlayerPawn.Value.AimPunchAngle.Y = 0;
            client.PlayerPawn.Value.AimPunchAngle.Z = 0;
            client.PlayerPawn.Value.AimPunchAngleVel.X = 0;
            client.PlayerPawn.Value.AimPunchAngleVel.Y = 0;
            client.PlayerPawn.Value.AimPunchAngleVel.Z = 0;

            Server.NextFrame(() => {
                Utilities.SetStateChanged(client.PlayerPawn.Value, "CCSPlayerPawn", "m_aimPunchAngle"); 
                Utilities.SetStateChanged(client.PlayerPawn.Value, "CCSPlayerPawn", "m_aimPunchAngleVel"); 

        
                Utilities.SetStateChanged(weaponBase, "CCSWeaponBase", "m_flRecoilIndex"); 
                Utilities.SetStateChanged(weaponBase, "CCSWeaponBase", "m_flRecoilIndex"); 
                Utilities.SetStateChanged(weaponBase, "CCSWeaponBase", "m_fAccuracyPenalty"); 
            }) ;
        } 
        catch (Exception e) 
        { 
            Console.WriteLine(e.ToString());
            throw; 
        } 
    }
}