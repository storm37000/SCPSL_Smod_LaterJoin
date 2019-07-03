using Smod2;
using Smod2.Attributes;
using System.Collections.Generic;
using UnityEngine.Networking;
using MEC;
using System.Linq;

namespace LaterJoin
{
	[PluginDetails(
		author = "ShingekiNoRex and storm37000",
		name = "LaterJoin",
		description = "Allow those who join just after round start to spawn",
		id = "s37k.laterjoin",
		version = "1.1.17",
		SmodMajor = 3,
		SmodMinor = 2,
		SmodRevision = 0
		)]
	class Main : Plugin
	{
		public bool infAutoRespawn = false;
		public bool LJenabled = true;

		public override void OnDisable()
		{
			this.Info(this.Details.name + " has been disabled.");
		}
		public override void OnEnable()
		{
			this.Info(this.Details.name + " has been enabled.");
		}

		public bool UpToDate { get; private set; } = true;

		public void outdatedmsg()
		{
			this.Error("Your version is out of date, please update the plugin and restart your server when it is convenient for you.");
		}

		IEnumerator<float> UpdateChecker()
		{
			string[] hosts = { "https://storm37k.com/addons/", "http://74.91.115.126/addons/" };
			bool fail = true;
			string errorMSG = "";
			foreach (string host in hosts)
			{
				using (UnityWebRequest webRequest = UnityWebRequest.Get(host + this.Details.name + ".ver"))
				{
					// Request and wait for the desired page.
					yield return Timing.WaitUntilDone(webRequest.SendWebRequest());

					if (webRequest.isNetworkError || webRequest.isHttpError)
					{
						errorMSG = webRequest.error;
					}
					else
					{
						if (webRequest.downloadHandler.text != this.Details.version)
						{
							outdatedmsg();
							UpToDate = false;
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
			this.AddCommands(new string[] { "lj_toggle" }, new CommandHandlerToggle(this));

			this.AddConfig(new Smod2.Config.ConfigSetting("lj_time", 30, Smod2.Config.SettingType.NUMERIC, true, ""));
			this.AddConfig(new Smod2.Config.ConfigSetting("lj_FillerTeamQueue", new int[] {}, Smod2.Config.SettingType.NUMERIC_LIST, true, ""));
			this.AddConfig(new Smod2.Config.ConfigSetting("lj_InfAutoRespawn", false, Smod2.Config.SettingType.BOOL, true, ""));
			this.AddConfig(new Smod2.Config.ConfigSetting("lj_InfAutoRespawn_delay", 5, Smod2.Config.SettingType.NUMERIC, true, ""));
			this.AddConfig(new Smod2.Config.ConfigSetting("lj_InfAutoRespawn_queue", new int[] { }, Smod2.Config.SettingType.NUMERIC_LIST, true, ""));

			try
			{
				string file = System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(Smod2.ConfigManager.Manager.Config.GetConfigPath()), "s37k_g_disableVcheck*", System.IO.SearchOption.TopDirectoryOnly).FirstOrDefault();
				if (file == null)
				{
					Timing.RunCoroutine(UpdateChecker());
				}
				else
				{
					this.Info("Version checker is disabled.");
				}
			}
			catch(System.Exception)
			{
				Timing.RunCoroutine(UpdateChecker());
			}
		}
	}
}
