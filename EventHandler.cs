using Smod2;
using Smod2.API;
using Smod2.Events;
using Smod2.EventHandlers;
using System.Collections.Generic;
using System.Linq;
using Smod2.EventSystem.Events;

namespace LaterJoin
{

	class EventHandler : IEventHandlerPlayerJoin, IEventHandlerRoundStart, IEventHandlerRoundEnd, IEventHandlerWarheadDetonate, IEventHandlerLCZDecontaminate, IEventHandlerWaitingForPlayers, IEventHandlerDecideTeamRespawnQueue, IEventHandlerPlayerDie
	{

		private Main plugin;
		private bool roundstarted = false;
		private int number = 0;
		private List<byte> FilledTeams = new List<byte>();
		private List<string> blacklist = new List<string>();
		private List<byte> enabledSCPs = new List<byte>();
		private bool decond = false;
		private static readonly System.Random getrandom = new System.Random();
		private int time = 0;
		private bool detonated = false;
		private byte fillerTeam = 4;
		private bool hasSmartCPicker = true;
		private int[] spqueue;
		private byte[] queue;
		private bool infAutoRespawn = false;
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
				number = 0;
				roundstarted = false;
				decond = false;
				detonated = false;
			}
		}

		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			if (ConfigManager.Manager.Config.GetBoolValue("smart_class_picker", true))
			{
				plugin.Info("smart_class_picker is enabled! the addon will behave unexpectedly with it enabled, it is recommended to turn it off.");
				hasSmartCPicker = true;
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

			infAutoRespawn = plugin.GetConfigBool("lj_InfAutoRespawn");

			autoRespawnDelay = plugin.GetConfigInt("lj_InfAutoRespawn_delay");
			if (time < 1)
			{
				plugin.Error("Config for lj_InfAutoRespawn_delay of " + time + " is not a valid value! Using default instead.");
				autoRespawnDelay = 5;
			}

			fillerTeam = (byte)ConfigManager.Manager.Config.GetIntValue("filler_team_id", (byte)Smod2.API.Team.NINETAILFOX);
			if (!System.Enum.IsDefined(typeof(Team), fillerTeam))
			{
				plugin.Error("your filler_team_id contains an invalid value!  The default will be used.");
				fillerTeam = (byte)Smod2.API.Team.NINETAILFOX;
			}

			spqueue = plugin.GetConfigIntList("lj_queue");
			foreach (int v in spqueue)
			{
				if (!System.Enum.IsDefined(typeof(Role), v))
				{
					plugin.Error("your lj_queue contains an invalid value!  It will not be used.");
					spqueue = new int[] { };
					break;
				}
			}

			string[] queuestr = ConfigManager.Manager.Config.GetListValue("team_respawn_queue");
			char[] queuechar = queuestr[0].ToCharArray();
			queue = System.Array.ConvertAll(queuechar, c => (byte)System.Char.GetNumericValue(c));
		}

		public void OnDecideTeamRespawnQueue(DecideRespawnQueueEvent ev)
		{
			foreach (Team team in ev.Teams)
			{
				FilledTeams.Add((byte)team);
			}
		}

		public void OnRoundStart(RoundStartEvent ev)
		{
			plugin.Info("LJ: round start!");
			roundstarted = true;
			foreach (Player player in ev.Server.GetPlayers())
			{
				blacklist.Add(player.SteamId);
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
			if (decond && (!detonated) && (TeamID == 3 || TeamID == 4 || TeamID == 0 || TeamID == 6))
			{
				plugin.Debug("Tried to select a team that cant spawn because LCZ DECON has occurred!");
				return 255;
			}
			if (detonated && (TeamID == 3 || TeamID == 4 || TeamID == 0 || TeamID == 6))
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
					return (byte)Role.FACILITY_GUARD;
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
					plugin.Error("Tried to select an invalid team!");
					return 255;
			}
		}

		private SortedList<byte, byte> DistinctCount(List<byte> InList)
		{
			SortedList<byte, byte> OutList = new SortedList<byte, byte>();
			var q = from x in InList
					group x by x into g
					let count = g.Count()
					orderby count descending
					select new { Value = g.Key, Count = count };
			foreach (var x in q)
			{
				OutList.Add(x.Value, (byte)x.Count);
			}
			return OutList;
		}
		private SortedList<byte, byte> DistinctCount(byte[] InList)
		{
			SortedList<byte, byte> OutList = new SortedList<byte, byte>();
			var q = from x in InList
					group x by x into g
					let count = g.Count()
					orderby count descending
					select new { Value = g.Key, Count = count };
			foreach (var x in q)
			{
				OutList.Add(x.Value, (byte)x.Count);
			}
			return OutList;
		}

		private byte getfiller()
		{
			byte chosenclass = TeamIDtoClassID(fillerTeam);
			if (spqueue.Length > 0)
			{
				chosenclass = (byte)spqueue[number];
				number = (number + 1) % spqueue.Length;
			}
			return chosenclass;
		}

		private byte ChooseClass(Player player)
		{
			byte chosenclass = 255;
			while (chosenclass == 255)
			{
				if (FilledTeams.Count < queue.Length && hasSmartCPicker == false)
				{
					plugin.Debug("is within index");
					byte chosenteam = queue[FilledTeams.Count];
					chosenclass = TeamIDtoClassID(chosenteam);
					FilledTeams.Add(chosenteam);
				}
				else if (hasSmartCPicker == false)
				{
					plugin.Debug("is outside index, using filler");
					chosenclass = getfiller();
					if (chosenclass == 255)
					{
						break;
					}
				}
				else
				{
					plugin.Debug("Smart Class Picker is enabled! doing special case for that.");
					List<byte> Leftover = new List<byte>();
					SortedList<byte, byte> Qlist = DistinctCount(queue);
					SortedList<byte, byte> Ulist = DistinctCount(FilledTeams);
					foreach (KeyValuePair<byte, byte> kvp in Qlist)
					{
						if (Ulist.Count != 0 && Ulist.ContainsKey(kvp.Key))
						{
							byte differ = (byte)(kvp.Value - Ulist[kvp.Key]);
							if (differ != 0)
							{
								Leftover.Add(kvp.Key);
							}
						}
						else { Leftover.Add(kvp.Key); }
					}
					if (Leftover.Count != 0)
					{
						byte chosenteam = Leftover[getrandom.Next(0, Leftover.Count)];
						FilledTeams.Add(chosenteam);
						chosenclass = TeamIDtoClassID(chosenteam);
					}
					else
					{
						plugin.Debug("no more class slots left, using filler.");
						chosenclass = getfiller();
						if (chosenclass == 255)
						{
							break;
						}
					}
				}
			}
		return chosenclass;
		}

		private byte spawnPlayer(Player player)
		{
			byte chosenclass = ChooseClass(player);
			player.ChangeRole((Role)chosenclass);
			return chosenclass;
		}

		public void OnPlayerJoin(PlayerJoinEvent ev)
		{
			if (roundstarted && ((time >= PluginManager.Manager.Server.Round.Duration) || time == -1) && (!blacklist.Contains(ev.Player.SteamId)) && (ev.Player.TeamRole.Team == Smod2.API.Team.NONE || ev.Player.TeamRole.Team == Smod2.API.Team.SPECTATOR))
			{
				plugin.Info("Player " + RemoveSpecialCharacters(ev.Player.Name) + " joined late!  Setting their class to " + spawnPlayer(ev.Player));
				blacklist.Add(ev.Player.SteamId);
			}
		}

		public void OnPlayerDie(PlayerDeathEvent ev)
		{
			if (infAutoRespawn)
			{
				plugin.Debug("Player " + RemoveSpecialCharacters(ev.Player.Name) + " died!  Respawning them in " + autoRespawnDelay + " seconds!");
				System.Timers.Timer t = new System.Timers.Timer();
				t.Interval = autoRespawnDelay * 1000;
				t.AutoReset = false;
				t.Enabled = true;
				t.Elapsed += delegate
				{
					plugin.Debug("Respawning " + RemoveSpecialCharacters(ev.Player.Name) + " as a class of " + spawnPlayer(ev.Player));

				};
			}
		}
	}
}
