using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using IksAdmin_FunCommands.Extensions;
using IksAdminApi;
using Microsoft.Extensions.Localization;

namespace IksAdmin_FunCommands;

public static class Menus
{
    private static readonly IIksAdminApi Api = AdminUtils.CoreApi;
    private static readonly IStringLocalizer Localizer = Main.StringLocalizer;
    
    public static void OpenFunMenu(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        var menu = Api.CreateMenu(
            "fun_commands",
            Localizer["MenuTitle.FunCommands"],
            backMenu: backMenu
            );
        
        // menu.AddMenuOption("rconvar", Localizer["MenuOption.RConVar"],
        //     (_, _) => { RConVar(caller, menu); },
        //      viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.rconvar"));
        
        menu.AddMenuOption("noclip", Localizer["MenuOption.Noclip"], 
            (_, _) => { Noclip(caller, menu); }, 
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.noclip"));
        
        menu.AddMenuOption("slap", Localizer["MenuOption.Slap"], 
            (_, _) => { Slap(caller, menu); }, 
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.slap"));
        
        menu.AddMenuOption("set_money", Localizer["MenuOption.SetMoney"],
            (_, _) => { SetMoney(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.set_money"));
        
        menu.AddMenuOption("add_money", Localizer["MenuOption.AddMoney"],
            (_, _) => { AddMoney(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.set_money"));
        
        menu.AddMenuOption("take_money", Localizer["MenuOption.TakeMoney"],
            (_, _) => { TakeMoney(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.set_money"));
        
        menu.AddMenuOption("set_hp", Localizer["MenuOption.SetHp"],
            (_, _) => { SetHp(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.hp"));
        
        menu.AddMenuOption("set_speed", Localizer["MenuOption.SetSpeed"],
            (_, _) => { SetSpeed(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.speed"));
        
        menu.AddMenuOption("set_scale", Localizer["MenuOption.SetScale"],
            (_, _) => { SetScale(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.scale"));
        
        menu.AddMenuOption("save_position", Localizer["MenuOption.SavePosition"],
            (_, _) => { SavePosition(caller, backMenu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.tp"));
        
        menu.AddMenuOption("teleport", Localizer["MenuOption.Teleport"],
            (_, _) => { Teleport(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.tp"),
            disabled: !FunFunctions.PlayersSavedTeleportPositions.ContainsKey(caller.GetSteamId()));
        
        menu.AddMenuOption("pingtp", Localizer["MenuOption.PingTp"],
            (_, _) => { TurnPingTp(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.pingtp"));
        
        menu.AddMenuOption("shootspeed", Localizer["MenuOption.SetShootSpeed"],
            (_, _) => { SetShootSpeed(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.shootspeed"));
        
        menu.AddMenuOption("custom_damage", Localizer["MenuOption.SetCustomDamage"],
            (_, _) => { SetCustomDamage(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.set_damage"));
        
        menu.AddMenuOption("add_damage", Localizer["MenuOption.SetBonusDamage"],
            (_, _) => { SetBonusDamage(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.add_damage"));
        
        menu.AddMenuOption("max_ammo", Localizer["MenuOption.SetMaxAmmo"],
            (_, _) => { SetMaxAmmo(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.max_ammo"));
        
        menu.AddMenuOption("no_recoil", Localizer["MenuOption.SetNoRecoil"],
            (_, _) => { SetNoRecoil(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.no_recoil"));
        
        menu.AddMenuOption("freeze", Localizer["MenuOption.Freeze"],
            (_, _) => { Freeze(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.freeze"));
        
        menu.AddMenuOption("unfreeze", Localizer["MenuOption.Unfreeze"],
            (_, _) => { Unfreeze(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.freeze"));
        
        menu.AddMenuOption("give", Localizer["MenuOption.GiveWeapon"],
            (_, _) => { GiveWeapon(caller, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("fun_commands.give"));
        
        menu.Open(caller);
    }

    private static void SetMoney(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        caller.Print(Localizer["Request.MoneyAmount"]);
        
        Api.HookNextPlayerMessage(caller, amount =>
        {
            OpenSelectPlayer(caller, "set_money", (target, _) =>
            {
            
                FunFunctions.SetMoney(caller, target.Controller!, int.Parse(amount));
            }, backMenu: backMenu);
        });
        
    }
    
    private static void SavePosition(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        caller.Print(Localizer["Request.PositionKey"]);
            
        Api.HookNextPlayerMessage(caller, key =>
        {
            FunFunctions.SaveTeleportPosition(caller, key);
            OpenFunMenu(caller, backMenu);
        });
    }
    
    private static void TurnPingTp(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        var menu = Api.CreateMenu("select_tp_position", Localizer["MenuTitle.SelectPosition"], backMenu: backMenu);

        foreach (var target in PlayersUtils.GetOnlinePlayers())
        {
            if (!Api.CanDoActionWithPlayer(caller.GetSteamId(), target.GetSteamId()))
            {
                continue;
            }

            var state = FunFunctions.PlayersWithTeleportOnPing.Contains(target.Slot);
            
            menu.AddMenuOption(target.GetSteamId(), target.PlayerName + (state ? " [+]" : " [-]"), (_, _) =>
            {
                FunFunctions.TurnTeleportOnPing(caller, target, !state);
                TurnPingTp(caller, backMenu);
            });
        }

        menu.Open(caller);
    }
    
    private static void SetNoRecoil(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        var menu = Api.CreateMenu("select_player", Localizer["MenuOption.SetNoRecoil"], backMenu: backMenu);

        foreach (var target in PlayersUtils.GetOnlinePlayers())
        {
            if (!Api.CanDoActionWithPlayer(caller.GetSteamId(), target.GetSteamId()))
            {
                continue;
            }

            var settings = target.GetWeaponSettings();
            
            menu.AddMenuOption(target.GetSteamId(), target.PlayerName + (settings.NoRecoil ? " [+]" : " [-]"), (_, _) =>
            {
                FunFunctions.SetNoRecoil(caller, target, !settings.NoRecoil);
                SetNoRecoil(caller, backMenu);
            });
        }

        menu.Open(caller);
    }
    
    private static void Teleport(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        OpenSelectAlivePlayer(caller, "teleport", backMenu, target =>
        {
            OpenSelectPositionMenu(caller, target, backMenu);
        });
    }
    
    private static void Slap(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        OpenSelectAlivePlayer(caller, "slap", backMenu, target =>
        {
            FunFunctions.Slap(caller, target);
        });
    }

    private static void OpenSelectPositionMenu(CCSPlayerController caller, CCSPlayerController target, IDynamicMenu? backMenu)
    {
        var menu = Api.CreateMenu("select_tp_position", Localizer["MenuTitle.SelectPosition"]);

        menu.BackAction = _ => { Teleport(caller, backMenu); };

        var positions = FunFunctions.PlayersSavedTeleportPositions[caller.GetSteamId()];

        foreach (var position in positions)
        {
            menu.AddMenuOption(position.Key, position.Key, (_, _) =>
            {
                if (!target.PawnIsAlive)
                {
                    caller.Print(Localizer[Localizer["Error.TargetMustBeAlive"]]);
                    return;
                }
                
                FunFunctions.TeleportToSavedPos(caller, target, position.Key);
            });
        }

        menu.Open(caller);
    }
    
    private static void SetShootSpeed(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        caller.Print(Localizer["Request.ShootSpeed"]);
        
        Api.HookNextPlayerMessage(caller, amount =>
        {
            OpenSelectAlivePlayer(caller, "set_shootspeed", backMenu, target =>
            {
                var weapon = caller.GetActiveWeaponName();
            
                if (weapon == null)
                {
                    caller.Print(Localizer["Error.MustHandleWeapon"]);
                    return;
                }
            
                
                FunFunctions.SetShootSpeed(caller, target, amount == "default" ? null : float.Parse(amount));
            
            }, includeBots: false);
        });
    }
    
    private static void SetCustomDamage(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        caller.Print(Localizer["Request.CustomDamage"]);
        
        Api.HookNextPlayerMessage(caller, amount =>
        {
            OpenSelectAlivePlayer(caller, "set_custom_damage", backMenu, target =>
            {
                var weapon = caller.GetActiveWeaponName();
            
                if (weapon == null)
                {
                    caller.Print(Localizer["Error.MustHandleWeapon"]);
                    return;
                }
            
                
                FunFunctions.SetCustomDamage(caller, target, amount == "default" ? null : int.Parse(amount));
            
            }, includeBots: false);
        });
        
    }
    
    private static void SetBonusDamage(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        caller.Print(Localizer["Request.BonusDamage"]);
        
        Api.HookNextPlayerMessage(caller, amount =>
        {
            OpenSelectAlivePlayer(caller, "set_bonus_damage", backMenu, target =>
            {
                var weapon = caller.GetActiveWeaponName();
            
                if (weapon == null)
                {
                    caller.Print(Localizer["Error.MustHandleWeapon"]);
                    return;
                }
            
            
                FunFunctions.SetBonusDamage(caller, target, amount == "default" ? null : int.Parse(amount));
            
            }, includeBots: false);
        });
    }
    
    private static void SetMaxAmmo(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        caller.Print(Localizer["Request.MaxAmmo"]);
        
        Api.HookNextPlayerMessage(caller, amount =>
        {
            OpenSelectAlivePlayer(caller, "max_ammo", backMenu, target =>
            {
                var weapon = caller.GetActiveWeaponName();
            
                if (weapon == null)
                {
                    caller.Print(Localizer["Error.MustHandleWeapon"]);
                    return;
                }
            
                
                FunFunctions.SetMaxAmmo(caller, target, amount == "default" ? null : int.Parse(amount));
            
            });
        });
        
    }

    private static void SetHp(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        caller.Print(Localizer["Request.HpAmount"]);
        
        Api.HookNextPlayerMessage(caller, amount =>
        {
            OpenSelectAlivePlayer(caller, "set_hp", backMenu, target =>
            {
                FunFunctions.SetHp(caller, target, int.Parse(amount));
            });
        });
        
    }
    
    private static void SetSpeed(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        caller.Print(Localizer["Request.Speed"]);
        Api.HookNextPlayerMessage(caller, amount =>
        {
            OpenSelectAlivePlayer(caller, "set_speed", backMenu, target =>
            {
                FunFunctions.SetSpeed(caller, target, float.Parse(amount));
            });
        });
        
    }
    
    private static void SetScale(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        caller.Print(Localizer["Request.Scale"]);
        
        Api.HookNextPlayerMessage(caller, amount =>
        {
            OpenSelectAlivePlayer(caller, "set_scale", backMenu, target =>
            {
                FunFunctions.SetScale(caller, target, float.Parse(amount));
            });
        });
    }
    
    private static void AddMoney(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        Api.HookNextPlayerMessage(caller, amount =>
        {
            OpenSelectPlayer(caller, "add_money", (target, _) =>
            {
                caller.Print(Localizer["Request.MoneyAmount"]);
                
                FunFunctions.AddMoney(caller, target.Controller!, int.Parse(amount));
                
            }, backMenu: backMenu);
        });
        
    }
    
    private static void TakeMoney(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        caller.Print(Localizer["Request.MoneyAmount"]);
        
        Api.HookNextPlayerMessage(caller, amount =>
        {
            OpenSelectPlayer(caller, "take_money", (target, _) =>
            {
                FunFunctions.TakeMoney(caller, target.Controller!, int.Parse(amount));
            }, backMenu: backMenu);
        });
    }

    private static void Noclip(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        OpenSelectAlivePlayer(caller, "noclip", backMenu, target =>
        {
            FunFunctions.Noclip(caller, target);
        }, includeBots: false);
    }
    
    private static void Freeze(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        OpenSelectAlivePlayer(caller, "freeze", backMenu, target =>
        {
            FunFunctions.TurnFreeze(caller, target, true);
        }, includeBots: true);
    }

    private static void Unfreeze(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        OpenSelectAlivePlayer(caller, "unfreeze", backMenu, target =>
        {
            FunFunctions.TurnFreeze(caller, target, false);
        }, includeBots: true);
    }
    
    private static void GiveWeapon(CCSPlayerController caller, IDynamicMenu? backMenu)
    {
        var menu = Api.CreateMenu("fc_select_weapon", Localizer["MenuTitle.SelectWeapon"]);

        foreach (var weapon in FunFunctions.ValidWeapons)
        {
            menu.AddMenuOption(weapon, Localizer[weapon], (_, _) =>
            {
                OpenSelectAlivePlayer(caller, "give", backMenu, target =>
                {
                    FunFunctions.GiveWeapon(caller, target, weapon);
                }, includeBots: true);
            });
        }
        
        menu.Open(caller);
    }

    private static void RConVar(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        OpenSelectPlayer(caller, "rconvar", (target, _) =>
        {
            caller.Print(Localizer["Request.RConVar1"]);
            Api.HookNextPlayerMessage(caller, cvar =>
            {
                caller.Print(Localizer["Request.RConVar2"]);
                Main.Instance.AddTimer(0.2f, () =>
                {
                    Api.HookNextPlayerMessage(caller, value =>
                    {
                        FunFunctions.RConVar(caller, target.Controller!, cvar, value);
                    });
                });
            });
        }, backMenu: backMenu);
    }

    private static void OpenSelectAlivePlayer(CCSPlayerController caller, string prefix, IDynamicMenu? backMenu, Action<CCSPlayerController> action, bool includeBots = true)
    {
        var menu = Api.CreateMenu(prefix + "_select_alive_player", Api.Localizer["MenuTitle.Other.SelectPlayer"], backMenu: backMenu);
        
        menu.AddMenuOption("special_targets", Localizer["MenuTitle.SpecialTargets"], (_, _) =>
        {
            OpenSelectMultiAliveTargets(caller, prefix, action, includeBots, menu);
        });
        
        foreach (var player in PlayersUtils.GetOnlinePlayers(includeBots))
        {
            if (!player.IsBot && !Api.CanDoActionWithPlayer(caller.GetSteamId(), player.GetSteamId()))
            {
                continue;
            }
            
            if (!player.PawnIsAlive) continue;
            
            menu.AddMenuOption(player.Slot.ToString(), player.PlayerName, (_, _) =>
            { 
                action.Invoke(player); 
            });
        }
        
        menu.Open(caller);
    }

    private static void OpenSelectMultiAliveTargets(CCSPlayerController caller, string prefix, Action<CCSPlayerController> action, bool includeBots, IDynamicMenu? backMenu = null)
    {
        var menu = Api.CreateMenu(prefix + "_select_alive_player", Api.Localizer["MenuTitle.SpecialTargets"], backMenu: backMenu);

        var players = PlayersUtils.GetOnlinePlayers(includeBots);
        
        menu.AddMenuOption("me", Localizer["MenuOption.Me"], (_, _) =>
        {
            if (!caller.PawnIsAlive)
            {
                caller.Print(Localizer["Error.TargetMustBeAlive"]);
                return;
            }
            action.Invoke(caller);
        });

        menu.AddMenuOption("ct", Localizer["MenuOption.CT"], (_, _) =>
        {
            foreach (var player in players.Where(x => x is { TeamNum: 3, PawnIsAlive: true }))
            {
                action.Invoke(player);
            }
        });
        
        menu.AddMenuOption("t", Localizer["MenuOption.T"], (_, _) =>
        {
            foreach (var player in players.Where(x => x is { TeamNum: 2, PawnIsAlive: true }))
            {
                action.Invoke(player);
            }
        });
        
        menu.AddMenuOption("all", Localizer["MenuOption.All"], (_, _) =>
        {
            foreach (var player in players.Where(x => x is { PawnIsAlive: true }))
            {
                action.Invoke(player);
            }
        });
        
        menu.Open(caller);
    }
    
    private static void OpenSelectMultiTargets(CCSPlayerController caller, string prefix, Action<PlayerInfo, IDynamicMenu> action, bool includeBots, IDynamicMenu? backMenu = null)
    {
        var menu = Api.CreateMenu(prefix + "_select_alive_player", Api.Localizer["MenuTitle.SpecialTargets"], backMenu: backMenu);

        var players = PlayersUtils.GetOnlinePlayers(includeBots);
        
        menu.AddMenuOption("me", Localizer["MenuOption.Me"], (_, _) =>
        {
            if (!caller.PawnIsAlive)
            {
                caller.Print(Localizer["Error.TargetMustBeAlive"]);
                return;
            }
            action.Invoke(new PlayerInfo(caller), menu);
        });

        menu.AddMenuOption("ct", Localizer["MenuOption.CT"], (_, _) =>
        {
            foreach (var player in players.Where(x => x is { TeamNum: 3 }))
            {
                action.Invoke(new PlayerInfo(player), menu);
            }
        });
        
        menu.AddMenuOption("t", Localizer["MenuOption.T"], (_, _) =>
        {
            foreach (var player in players.Where(x => x is { TeamNum: 2 }))
            {
                action.Invoke(new PlayerInfo(player), menu);
            }
        });
        
        menu.AddMenuOption("all", Localizer["MenuOption.All"], (_, _) =>
        {
            foreach (var player in players)
            {
                action.Invoke(new PlayerInfo(player), menu);
            }
        });
        
        menu.Open(caller);
    }

    public static void OpenSelectPlayer(CCSPlayerController caller, string idPrefix, Action<PlayerInfo, IDynamicMenu> action, bool includeBots = false, IDynamicMenu? backMenu = null, string? customTitle = null)
    {
        var menu = Api.CreateMenu(
            idPrefix + "_select_player",
            customTitle ?? Api.Localizer["MenuTitle.Other.SelectPlayer"],
            titleColor: MenuColors.Gold,
            backMenu: backMenu
        );
        
        menu.AddMenuOption("special_targets", Localizer["MenuOption.SpecialTargets"], (_, _) =>
        {
            OpenSelectMultiTargets(caller, idPrefix, action, includeBots, menu);
        });

        var players = PlayersUtils.GetOnlinePlayers(includeBots);

        foreach (var player in players)
        {
            if (!player.IsBot && !Api.CanDoActionWithPlayer(caller.GetSteamId(), player.GetSteamId()))
            {
                continue;
            }
            
            var p = new PlayerInfo(player);
            
            menu.AddMenuOption(p.SteamId!, p.PlayerName, (_, _) =>
            {
                action.Invoke(p, menu);
            });
        }

        menu.Open(caller);
    }
}