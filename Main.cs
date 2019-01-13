using Smod2;
using Smod2.Attributes;

namespace LaterJoin
{
	[PluginDetails(
		author = "ShingekiNoRex, storm37000",
		name = "LaterJoin",
		description = "Allow those who join just after round start to spawn",
		id = "rex.laterjoin",
		version = "1.1.13",
		SmodMajor = 3,
		SmodMinor = 2,
		SmodRevision = 0
		)]
	class Main : Plugin
	{
		public bool infAutoRespawn = false;

		public override void OnDisable()
		{
			this.Info(this.Details.name + " has been disabled.");
		}
		public override void OnEnable()
		{
			this.Info(this.Details.name + " has been enabled.");
			string[] hosts = { "https://storm37k.com/addons/", "http://74.91.115.126/addons/" };
			ushort version = ushort.Parse(this.Details.version.Replace(".", string.Empty));
			bool fail = true;
			string errorMSG = "";
			foreach (string host in hosts)
			{
				using (UnityEngine.WWW req = new UnityEngine.WWW(host + this.Details.name + ".ver"))
				{
					while (!req.isDone) { }
					errorMSG = req.error;
					if (string.IsNullOrEmpty(req.error))
					{
						ushort fileContentV = 0;
						if (!ushort.TryParse(req.text, out fileContentV))
						{
							errorMSG = "Parse Failure";
							continue;
						}
						if (fileContentV > version)
						{
							this.Error("Your version is out of date, please visit the Smod discord and download the newest version");
						}
						fail = false;
						break;
					}
				}
			}
			if (fail)
			{
				this.Error("Could not fetch latest version txt: " + errorMSG);
			}
		}

		public override void Register()
		{
			// Register Events
			this.AddEventHandlers(new EventHandler(this));
			this.AddCommands(new string[] { "lj_deathmatch" }, new CommandHandlerDeathmatch(this));

			this.AddConfig(new Smod2.Config.ConfigSetting("lj_time", 30, Smod2.Config.SettingType.NUMERIC, true, ""));
			this.AddConfig(new Smod2.Config.ConfigSetting("lj_FillerTeamQueue", new int[] {}, Smod2.Config.SettingType.NUMERIC_LIST, true, ""));
			this.AddConfig(new Smod2.Config.ConfigSetting("lj_InfAutoRespawn", false, Smod2.Config.SettingType.BOOL, true, ""));
			this.AddConfig(new Smod2.Config.ConfigSetting("lj_InfAutoRespawn_delay", 5, Smod2.Config.SettingType.NUMERIC, true, ""));
		}
	}
}
