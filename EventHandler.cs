using Smod2;
using Smod2.API;
using Smod2.Events;
using Smod2.EventHandlers;
using System.Collections.Generic;
using MEC;

namespace LaterJoin
{

	class EventHandler : IEventHandlerSetServerName, IEventHandlerRoundStart, IEventHandlerRoundEnd, IEventHandlerPlayerJoin, IEventHandlerWarheadDetonate, IEventHandlerLCZDecontaminate, IEventHandlerWaitingForPlayers, IEventHandlerPlayerDie, IEventHandlerSetConfig
	{

		private Main plugin;
		private bool roundstarted = false;
		private List<byte> FilledTeams = new List<byte>();
		private List<string> blacklist = new List<string>();
		private List<byte> enabledSCPs = new List<byte>();
		private bool decond = false;
		private static readonly System.Random getrandom = new System.Random();
		private int time = 0;
		private bool detonated = false;
		private byte fillerTeam;

		Queue<byte> spqueue = new Queue<byte>();
		Queue<byte> infqueue = new Queue<byte>();

		private byte[] queue;
		private int autoRespawnDelay = 5;

		public EventHandler(Main plugin)
		{
			this.plugin = plugin;
		}

		public void OnDetonate()
		{
			detonated = true;
		}

		public void OnDecontaminate()
		{
			decond = true;
		}

		public void OnRoundEnd(RoundEndEvent ev)
		{
			if (ev.Round.Duration >= 3)
			{
				FilledTeams.Clear();
				blacklist.Clear();
				enabledSCPs.Clear();
				spqueue.Clear();
				infqueue.Clear();
				roundstarted = false;
				decond = false;
				detonated = false;
				plugin.infAutoRespawn = false;
			}
		}

		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			if (!plugin.UpToDate)
			{
				plugin.outdatedmsg();
			}
			if (ConfigManager.Manager.Config.GetBoolValue("scp049_disable", false) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp049_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_049); } }
			if (ConfigManager.Manager.Config.GetBoolValue("scp096_disable", false) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp096_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_096); } }
			if (ConfigManager.Manager.Config.GetBoolValue("scp106_disable", false) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp106_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_106); } }
			if (ConfigManager.Manager.Config.GetBoolValue("scp173_disable", false) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp173_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_173); } }
			if (ConfigManager.Manager.Config.GetBoolValue("scp939_53_disable", false) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp939_53_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_939_53); } }
			if (ConfigManager.Manager.Config.GetBoolValue("scp939_89_disable", false) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp939_89_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_939_89); } }
			if (ConfigManager.Manager.Config.GetBoolValue("scp079_disable", true) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp079_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_079); } }

			time = plugin.GetConfigInt("lj_time");
			if (time < -1)
			{
				plugin.Error("Config for lj_time of " + time + " is not a valid value! Using default instead.");
				time = 30;
			}

			plugin.infAutoRespawn = plugin.GetConfigBool("lj_InfAutoRespawn");

			autoRespawnDelay = plugin.GetConfigInt("lj_InfAutoRespawn_delay");
			if (autoRespawnDelay < 1)
			{
				plugin.Error("Config for lj_InfAutoRespawn_delay of " + autoRespawnDelay + " is not a valid value! Using default instead.");
				autoRespawnDelay = 5;
			}

			fillerTeam = (byte)ConfigManager.Manager.Config.GetIntValue("filler_team_id", (byte)Smod2.API.Team.CLASSD);
			if (!System.Enum.IsDefined(typeof(Smod2.API.Team), (int)fillerTeam))
			{
				plugin.Error("your filler_team_id contains an invalid value!  The default will be used.");
				fillerTeam = (byte)Smod2.API.Team.CLASSD;
			}

			foreach (int v in plugin.GetConfigIntList("lj_FillerTeamQueue"))
			{
				if (System.Enum.IsDefined(typeof(Smod2.API.Team), v))
				{
					spqueue.Enqueue((byte)v);
				}
				else
				{
					plugin.Error("your lj_FillerTeamQueue contains an invalid value of " + v + "!  It will not be used.");
				}
			}

			foreach (int v in plugin.GetConfigIntList("lj_InfAutoRespawn_queue"))
			{
				if (System.Enum.IsDefined(typeof(Smod2.API.Team), v))
				{
					infqueue.Enqueue((byte)v);
				}
				else
				{
					plugin.Error("your lj_InfAutoRespawn_queue contains an invalid value of " + v + "!  It will not be used.");
				}
			}

			string[] queuestr = ConfigManager.Manager.Config.GetListValue("team_respawn_queue");
			try
			{
				char[] queuechar = queuestr[0].ToCharArray();
				queue = System.Array.ConvertAll(queuechar, c => (byte)System.Char.GetNumericValue(c));
			}catch(System.Exception e)
			{
				plugin.Error("Your team_respawn_queue contains invalid data! The LaterJoin function cannot continue!");
				plugin.LJenabled = false;
			}

			if (plugin.Server.MaxPlayers > queue.Length)
			{
				plugin.Info("Your team_respawn_queue is too small for your player count!  Filler will be used when the queue is exhausted!");
			}
		}

		public void OnRoundStart(RoundStartEvent ev)
		{
			roundstarted = true;
			foreach (Player player in ev.Server.GetPlayers())
			{
				blacklist.Add(player.SteamId);
				FilledTeams.Add((byte)player.TeamRole.Team);
				if (enabledSCPs.Contains((byte)player.TeamRole.Role)) { enabledSCPs.Remove((byte)player.TeamRole.Role); }
			}
		}

		private string RemoveSpecialCharacters(string str)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			foreach (char c in str)
			{
				if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == ',' || c == '_' || c == ' ' || c == '[' || c == ']' || c == '{' || c == '}')
				{
					sb.Append(c);
				}
			}
			return sb.ToString();
		}

		private byte TeamIDtoClassID(byte TeamID)
		{
			if (decond && (!detonated) && (TeamID == (byte)Smod2.API.Team.SCIENTIST || TeamID == (byte)Smod2.API.Team.CLASSD || TeamID == (byte)Smod2.API.Team.SCP))
			{
				plugin.Debug("Tried to select a team that cant spawn because LCZ DECON has occurred!");
				return 255;
			}
			if (detonated && (TeamID == (byte)Smod2.API.Team.SCIENTIST || TeamID == (byte)Smod2.API.Team.CLASSD || TeamID == (byte)Smod2.API.Team.SCP))
			{
				plugin.Debug("Tried to select a team that cant spawn because warhead detonation has occurred!");
				return 255;
			}
			switch (TeamID)
			{
				case (byte)Smod2.API.Team.SCP:
					byte chosenscp = 255;
					if (enabledSCPs.Count > 0)
					{
						chosenscp = enabledSCPs[getrandom.Next(0, enabledSCPs.Count)];
					}
					else { plugin.Debug("Attempted to choose an SCP but there are no more SCP slots left!"); }
					return chosenscp;
				case (byte)Smod2.API.Team.NINETAILFOX:
					if (detonated)
					{
						return (byte)Smod2.API.Role.NTF_LIEUTENANT;
					}
					else
					{
						return (byte)Smod2.API.Role.FACILITY_GUARD;
					}
				case (byte)Smod2.API.Team.CHAOS_INSURGENCY:
					return (byte)Role.CHAOS_INSURGENCY;
				case (byte)Smod2.API.Team.SCIENTIST:
					return (byte)Role.SCIENTIST;
				case (byte)Smod2.API.Team.CLASSD:
					return (byte)Role.CLASSD;
				case (byte)Smod2.API.Team.SPECTATOR:
					return (byte)Role.SPECTATOR;
				case (byte)Smod2.API.Team.TUTORIAL:
					return (byte)Role.TUTORIAL;
				default:
					plugin.Debug("Tried to select an invalid team!");
					return 255;
			}
		}

		private byte getfiller()
		{
			byte chosenclass = TeamIDtoClassID(fillerTeam);
			if (spqueue.Count > 0)
			{
				byte temp = spqueue.Dequeue();
				spqueue.Enqueue(temp);
				chosenclass = TeamIDtoClassID(temp);
			}
			return chosenclass;
		}

		private string spawnPlayer(Player player)
		{
			byte attempt = 0;
			byte chosenclass = 255;
			while (chosenclass == 255 && attempt < 10)
			{
				attempt++;
				plugin.Debug("Choosing class... attempt #" + attempt);
				if (FilledTeams.Count < queue.Length)
				{
					plugin.Debug("is within index");
					byte chosenteam = queue[FilledTeams.Count];
					chosenclass = TeamIDtoClassID(chosenteam);
					FilledTeams.Add(chosenteam);
				}
				else
				{
					plugin.Debug("is outside index, using filler");
					chosenclass = getfiller();
				}
			}
			if (chosenclass != 255)
			{
				plugin.Debug("Choosing class finished on attempt #" + attempt);
				player.ChangeRole((Role)chosenclass);
				if (enabledSCPs.Contains(chosenclass)) { enabledSCPs.Remove(chosenclass); }
				return "" + (Smod2.API.Role)chosenclass;
			} else
			{
				return "Error, could not select a spawnable class!";
			}
			
		}

		private string respawnPlayer(Player player)
		{
			byte attempt = 0;
			byte chosenclass = 255;
			while (chosenclass == 255 && attempt < 10)
			{
				attempt++;
				plugin.Debug("Choosing class... attempt #" + attempt);
				if (infqueue.Count > 0 )
				{
					byte temp = infqueue.Dequeue();
					infqueue.Enqueue(temp);
					chosenclass = TeamIDtoClassID(temp);
				}
				else
				{
					plugin.Debug("lj_InfAutoRespawn_queue is empty! Using fillers");
					chosenclass = getfiller();
				}
			}
			if (chosenclass != 255)
			{
				plugin.Debug("Choosing class finished on attempt #" + attempt);
				player.ChangeRole((Role)chosenclass);
				if (enabledSCPs.Contains(chosenclass)) { enabledSCPs.Remove(chosenclass); }
				return "" + (Smod2.API.Role)chosenclass;
			}
			else
			{
				return "Error, could not select a spawnable class!";
			}

		}

		public void OnPlayerJoin(PlayerJoinEvent ev)
		{
			if (plugin.LJenabled)
			{
				if (roundstarted && ((time >= PluginManager.Manager.Server.Round.Duration) || time == -1) && (!ev.Player.OverwatchMode) && (!blacklist.Contains(ev.Player.SteamId)) && (ev.Player.TeamRole.Team == Smod2.API.Team.NONE || ev.Player.TeamRole.Team == Smod2.API.Team.SPECTATOR))
				{
					plugin.Info("Player " + RemoveSpecialCharacters(ev.Player.Name) + " joined late!  Setting their class to " + spawnPlayer(ev.Player));
					blacklist.Add(ev.Player.SteamId);
				}
			}
		}

		IEnumerator<float> autoRespawn(Player player)
		{
			plugin.Debug("Player " + RemoveSpecialCharacters(player.Name) + " died!  Attempting to respawning them in " + autoRespawnDelay + " seconds!");
			yield return Timing.WaitForSeconds(autoRespawnDelay);
			if (!player.OverwatchMode && player.TeamRole.Role == Role.SPECTATOR)
			{
				plugin.Debug("Respawning " + RemoveSpecialCharacters(player.Name) + " as a class of " + respawnPlayer(player));
			}
			else
			{
				plugin.Debug("Could not respawn them, they were already set as a role or in overwatch mode when the timer ran out.");
			}
		}

		public void OnPlayerDie(PlayerDeathEvent ev)
		{
			if (plugin.infAutoRespawn && ev.Player.Name != "Server")
			{
				Timing.RunCoroutine(autoRespawn(ev.Player));
			}
		}
		public void OnSetConfig(SetConfigEvent ev)
		{
			if (plugin.LJenabled && ev.Key == "smart_class_picker" && (bool)ev.Value == true)
			{
				plugin.Info("smart_class_picker is set to true!  Disabling it as we are unable to support it.");
				ev.Value = false;
			}
		}

		public void OnSetServerName(SetServerNameEvent ev)
		{
			ev.ServerName += "<size=1>" + plugin.Details.name + plugin.Details.version + "</size>";
		}
	}
}
