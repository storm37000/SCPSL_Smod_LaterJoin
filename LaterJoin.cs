using Smod2;
using Smod2.Attributes;
using Smod2.Events;
using Smod2.EventHandlers;

namespace Smod.TestPlugin
{
    [PluginDetails(
        author = "ShingekiNoRex, storm37000",
        name = "LaterJoin",
        description = "Allow those who join just after round start to spawn",
        id = "rex.later.join",
        version = "1.1.5",
        SmodMajor = 3,
        SmodMinor = 1,
        SmodRevision = 7
        )]
    class LaterJoin : Plugin
    {
        public override void OnDisable()
        {
            this.Info(this.Details.name + " has been disabled.");
        }
        public override void OnEnable()
        {
            bool SSLerr = false;
            this.Info(this.Details.name + " has been enabled.");
            while (true)
            {
                try
                {
                    string host = "https://storm37000.tk/addons/";
                    if (SSLerr) { host = "http://74.91.115.126/addons/"; }
                    ushort version = ushort.Parse(this.Details.version.Replace(".", string.Empty));
                    ushort fileContentV = ushort.Parse(new System.Net.WebClient().DownloadString(host + this.Details.name + ".ver"));
                    if (fileContentV > version)
                    {
                        this.Info("Your version is out of date, please visit the Smod discord and download the newest version");
                    }
                    break;
                }
                catch (System.Exception e)
                {
                    if (SSLerr == false && e.Message.Contains("The authentication or decryption has failed."))
                    {
                        SSLerr = true;
                        continue;
                    }
                    this.Error("Could not fetch latest version txt: " + e.Message);
                    break;
                }
            }
        }

        public override void Register()
        {
            // Register Events
            EventHandler events = new EventHandler(this);
            this.AddEventHandler(typeof(IEventHandlerPlayerJoin), events, Priority.High);
            this.AddEventHandler(typeof(IEventHandlerRoundStart), events, Priority.High);
            this.AddEventHandler(typeof(IEventHandlerRoundEnd), events, Priority.High);
            this.AddEventHandler(typeof(IEventHandlerWarheadDetonate), events, Priority.Highest);
            this.AddConfig(new Smod2.Config.ConfigSetting("lj_time", 30, Smod2.Config.SettingType.NUMERIC, true, ""));
            this.AddConfig(new Smod2.Config.ConfigSetting("lj_queue", new int[] { }, Smod2.Config.SettingType.NUMERIC_LIST, true, ""));
        }
    }
}