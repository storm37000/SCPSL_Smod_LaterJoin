using Smod2;
using Smod2.API;
using Smod2.Events;
using Smod2.EventHandlers;
using System.Collections.Generic;

namespace LaterJoin
{

    class EventHandler : IEventHandlerRoundStart, IEventHandlerRoundEnd, IEventHandlerPlayerJoin, IEventHandlerWarheadDetonate, IEventHandlerLCZDecontaminate, IEventHandlerWaitingForPlayers, IEventHandlerPlayerDie
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
        private byte fillerTeam;
        private int[] spqueue;
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
                number = 0;
                roundstarted = false;
                decond = false;
                detonated = false;
                plugin.infAutoRespawn = false;
            }
        }

        public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
        {
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

            fillerTeam = (byte)ConfigManager.Manager.Config.GetIntValue("filler_team_id", (byte)Smod2.API.Team.NINETAILFOX);
            if (!System.Enum.IsDefined(typeof(Smod2.API.Team), (int)fillerTeam))
            {
                plugin.Error("your filler_team_id contains an invalid value!  The default will be used.");
                fillerTeam = (byte)Smod2.API.Team.NINETAILFOX;
            }

            spqueue = plugin.GetConfigIntList("lj_FillerTeamQueue");
            foreach (int v in spqueue)
            {
                if (!System.Enum.IsDefined(typeof(Smod2.API.Team), v))
                {
                    plugin.Error("your lj_FillerTeamQueue contains an invalid value!  It will not be used.");
                    spqueue = new int[] { };
                    break;
                }
            }

            string[] queuestr = ConfigManager.Manager.Config.GetListValue("team_respawn_queue");
            char[] queuechar = queuestr[0].ToCharArray();
            queue = System.Array.ConvertAll(queuechar, c => (byte)System.Char.GetNumericValue(c));
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            plugin.Debug("round start!");
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
                        plugin.Debug("Attempted to choose a facility guard but the warhead has already detonated!  Spawning NTF leutenant instead.");
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
            if (spqueue.Length > 0)
            {
                chosenclass = TeamIDtoClassID((byte)spqueue[number]);
                number = (number + 1) % spqueue.Length;
            }
            return chosenclass;
        }

        private byte ChooseClass()
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
            plugin.Debug("Choosing class finished on attempt #" + attempt);
            return chosenclass;
        }

        private string spawnPlayer(Player player)
        {
            if (player.OverwatchMode) { return "Error, player in overwatch mode!"; }

            byte chosenclass = ChooseClass();
            if (chosenclass != 255)
            {
                player.ChangeRole((Role)chosenclass);
                if (enabledSCPs.Contains(chosenclass)) { enabledSCPs.Remove(chosenclass); }
                return "" + (Smod2.API.Role)chosenclass;
            } else
            {
                return "Error, could not select a spawnable class!";
            }
            
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
            if (plugin.infAutoRespawn)
            {
                if (ev.Player.OverwatchMode) { return; }
                plugin.Info("Player " + RemoveSpecialCharacters(ev.Player.Name) + " died!  Respawning them in " + autoRespawnDelay + " seconds!");
                System.Timers.Timer t = new System.Timers.Timer();
                t.Interval = autoRespawnDelay * 1000;
                t.AutoReset = false;
                t.Enabled = true;
                t.Elapsed += delegate
                {
                    plugin.Info("Respawning " + RemoveSpecialCharacters(ev.Player.Name) + " as a class of " + spawnPlayer(ev.Player));

                };
            }
        }
    }
}
