using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;
using IksAdminApi;
using MySqlConnector;
using System.Numerics;
using VipCoreApi;

namespace IksAdmin_GroupToVip;

public class PluginConfig : PluginCFG<PluginConfig>
{
    // Для VIP PISEXA
    public bool VipByPisex {get; set;} = false;
    public string Host { get; set; } = "host";
	public string Database { get; set; } = "db";
	public string User { get; set; } = "user";
	public string Pass { get; set; } = "pass";
	public uint Port { get; set; } = 3306;
	public int Sid { get; set; } = 0;
    // ===
    public Dictionary<string, string> AGroupToVip {get; set;} = new() {
        ["Admin"] = "VIP"
    };
    public int VipGiveTime { get; set; } = 1440; // Время на которое выдаётся випка (Минуты или секунды зависит от вашей вип систеы и конфигурации)
}

public class Main : AdminModule
{
    public override string ModuleName => "IksAdmin_GroupToVip";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "iks__";

    public PluginConfig Config = null!;
    public IVipCoreApi VipApi = null!;
    private PluginCapability<IVipCoreApi> _capability = new("vipcore:core"); 
    private List<CCSPlayerController> _vipGived = new();
    private string _dbConnString = "";

    public override void Ready()
    {
        base.Ready();
        Config = new PluginConfig().ReadOrCreate(AdminUtils.ConfigsDir + "/IksAdmin_Modules/group_to_vip.json", new PluginConfig());
        var builder = new MySqlConnectionStringBuilder();
        builder.Server = Config.Host;
        builder.UserID = Config.User;
        builder.Password = Config.Pass;
        builder.Port = Config.Port;
        builder.Database = Config.Database;
        _dbConnString = builder.ToString();

        if (!Config.VipByPisex)
            VipApi = _capability.Get()!;

        Api.OnFullConnect += OnFullConnect;
        Api.OnDynamicEvent += OnDynamicEvent;
    }

    private HookResult OnDynamicEvent(EventData data)
    {
        Server.NextFrame(() =>
        {
            if (data.EventKey == "admin_delete_post")
            {
                OnAdminDelete(data.Get<Admin>("new_admin"));
                return;
            }
            if (data.EventKey == "admin_create_post")
            {
                OnAdminCreate(data.Get<Admin>("new_admin"));
                return;
            }
        });
        


        return HookResult.Continue;
    }
    private void GiveAdminVip(Admin admin)
    {
        if (!Config.AGroupToVip.TryGetValue(admin.Group.Name, out var vipGroup))
        {
            return;
        }
        var player = PlayersUtils.GetControllerBySteamIdUnsafe(admin.SteamId);
        // Если VIP cssharp ТО:
        if (!Config.VipByPisex)
        {
            GiveVipCssharp(player, vipGroup);
            return;
        }
        // Если VIP by Pisex ТО:
        var accountId = player.AuthorizedSteamID!.AccountId;
        var name = player.PlayerName;
        Task.Run(async () =>
        {
            try
            {
                var conn = new MySqlConnection(_dbConnString);
                await conn.OpenAsync();

                Console.WriteLine("Check vip group");
                Console.WriteLine("accountId: " + accountId);
                Console.WriteLine("name: " + name);
                bool isVip = await conn.QuerySingleAsync<int>(
                    @"select count(*) from vip_users where account_id=@accountId and sid=@sid",
                    new
                    {
                        accountId,
                        sid = Config.Sid
                    }
                ) > 0;
                Console.WriteLine(7);

                Console.WriteLine("IsVip: " + isVip);
                if (isVip) return;

                // Выдаём випку
                await conn.QueryAsync(@"insert into vip_users 
            (account_id, name, lastvisit, sid, `group`, expires)
            values
            (@accountId, @name, @lastvisit, @sid, @group, @expires);
            ", new
                {
                    accountId,
                    name,
                    lastvisit = AdminUtils.CurrentTimestamp(),
                    sid = Config.Sid,
                    group = vipGroup,
                    expires = AdminUtils.CurrentTimestamp() + Config.VipGiveTime
                });
                Server.NextFrame(() =>
                {
                    _vipGived.Add(player);
                    Server.ExecuteCommand("mm_reload_vip " + accountId);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        });
    }
    private void OnAdminCreate(Admin admin)
    {
        if (admin.Controller == null) return;
        GiveAdminVip(admin);
    }

    private void OnAdminDelete(Admin admin)
    {
        Console.WriteLine("Deleting admin " + admin.CurrentName);
        if (admin.Controller == null) return;
        Console.WriteLine("Controller exists!");
        var controller = admin.Controller;
        if (!_vipGived.Contains(controller)) return;
        Console.WriteLine("Vip was gived");
        var accountId = controller.AuthorizedSteamID!.AccountId;
        Console.WriteLine("AccountID: " + accountId);
        Task.Run(async () => {
            var conn = new MySqlConnection(_dbConnString);
            await conn.OpenAsync();
            await conn.QueryAsync("delete from vip_users where account_id = @accountId and sid = @sid", new
            {
                accountId,
                sid = Config.Sid
            });
            Console.WriteLine("Vip Removed: " + accountId);
            Server.NextFrame(() => {
                _vipGived.Remove(controller);
                Server.ExecuteCommand("mm_reload_vip " + accountId);
            });
        });
    }

    private void OnFullConnect(string steamId, string ip)
    {
        var player = PlayersUtils.GetControllerBySteamIdUnsafe(steamId);

        if (player == null || !player.IsValid) return;

        var admin = player.Admin();
        if (admin == null || admin.Group == null) return;

        GiveAdminVip(admin);
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.AuthorizedSteamID == null) return HookResult.Continue;

        if (player.Admin() == null || player.Admin()?.Group == null) return HookResult.Continue;
        if (!_vipGived.Contains(player)) return HookResult.Continue;

        if (!Config.VipByPisex)
        {
            RemoveVipCssharp(player);
        } else {
            var accountId = player.AuthorizedSteamID.AccountId;
            Task.Run(async () => {
                var conn = new MySqlConnection(_dbConnString);
                await conn.OpenAsync();
                await conn.QueryAsync("delete from vip_users where account_id=@accountId and sid=@sid", new {
                    accountId = accountId,
                    sid = Config.Sid
                });
            });
        }
        _vipGived.Remove(player);
        return HookResult.Continue;
    }
    

    private void GiveVipCssharp(CCSPlayerController player, string vipGroup)
    {
        if (VipApi.IsClientVip(player))
        {
            // Если игрок уже VIP то мы не выдаём ему её ещё раз
            return;
        }
        VipApi.GiveClientTemporaryVip(player, vipGroup, 60*60*24);
        _vipGived.Add(player);
    }
    private void RemoveVipCssharp(CCSPlayerController player)
    {
        if (!VipApi.IsClientVip(player)) return;
        VipApi.RemoveClientVip(player);
        _vipGived.Remove(player);
    }
}
