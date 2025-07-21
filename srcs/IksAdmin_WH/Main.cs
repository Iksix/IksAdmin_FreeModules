using IksAdminApi;

namespace IksAdmin_WH;

public partial class Main : AdminModule
{
    public override string ModuleName => "IksAdmin_WH";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "iks__";

    public override void Ready()
    {
        Api.RegisterPermission("wh.use", "w");
        Api.RegisterPermission("wh.use_alive", "z");
        Api.RegisterPermission("wh.doesnt_seen", "z");
        
        RegisterListeners();
    }

    public override void Unload(bool hotReload)
    {
        DeregisterListeners();
    }

    public override void InitializeCommands()
    {
        Api.AddNewCommand(
            "wh",
            "Turns wh",
            "wh.use",
            "css_wh",
            OnWhCmd
            );
    }
}