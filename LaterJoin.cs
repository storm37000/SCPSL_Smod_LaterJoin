using Smod2;
using Smod2.Attributes;
using Smod2.Events;

namespace Smod.TestPlugin
{
    [PluginDetails(
        author = "ShingekiNoRex, storm37000",
        name = "LaterJoin",
        description = "Allow those who join just after round start to spawn",
        id = "rex.later.join",
        version = "1.1.0c",
        SmodMajor = 2,
        SmodMinor = 2,
        SmodRevision = 0
        )]
    class LaterJoin : Plugin
    {
        public override void OnDisable()
        {
        }

        public override void OnEnable()
        {
            Info("Later Join has loaded :)");
        }

        public override void Register()
        {
            // Register Events
            EventHandler events = new EventHandler(this);
            AddEventHandler(typeof(IEventPlayerJoin), events, Priority.High);
            AddEventHandler(typeof(IEventRoundStart), events, Priority.High);
            AddEventHandler(typeof(IEventRoundEnd), events, Priority.High);
            AddConfig(new Smod2.Config.ConfigSetting("lj_time", 30, Smod2.Config.SettingType.NUMERIC, true, ""));
            AddConfig(new Smod2.Config.ConfigSetting("lj_queue", new int[] { }, Smod2.Config.SettingType.NUMERIC_LIST, true, ""));
        }
    }
}