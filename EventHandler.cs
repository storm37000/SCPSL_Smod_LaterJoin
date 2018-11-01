using Smod2;
using Smod2.API;
using Smod2.Events;
using Smod2.EventHandlers;
using System.Collections.Generic;
using System.Linq;

namespace Smod.LaterJoin
{

    class EventHandler : IEventHandlerPlayerJoin, IEventHandlerRoundStart, IEventHandlerRoundEnd, IEventHandlerWarheadDetonate, IEventHandlerLCZDecontaminate
    {

        private LaterJoinPlugin plugin;
        private bool allowspawn = false;
        private int number;
        private List<byte> FilledTeams = new List<byte>();
        private List<string> blacklist = new List<string>();
        private List<byte> enabledSCPs = new List<byte>();
        private bool decond = false;
        private static readonly System.Random getrandom = new System.Random();
        private System.Timers.Timer t = new System.Timers.Timer();

        public EventHandler(LaterJoinPlugin plugin)
        {
            this.plugin = plugin;
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

        private void cleanup()
        {
            t.Enabled = false;
            if (allowspawn) { allowspawn = false; }
            FilledTeams.Clear();
            blacklist.Clear();
            enabledSCPs.Clear();
        }

        private void InitTimer(int time)
        {
            t.Interval = time * 1000;
            t.AutoReset = false;
            t.Enabled = true;
            t.Elapsed += delegate
            {
                plugin.Debug("Time Over");
                cleanup();
            };
        }

        public void OnDetonate()
        {
            cleanup();
        }

        public void OnDecontaminate()
        {
            decond = true;
        }

        public void OnRoundEnd(RoundEndEvent ev)
        {
            if (ev.Round.Duration >= 3)
            {
                cleanup();
            }
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            if (ConfigManager.Manager.Config.GetBoolValue("smart_class_picker", false))
            {
                plugin.Info("smart_class_picker is enabled! the addon will behave unexpectedly with it enabled, it is recommended to turn it off.");
            }
            allowspawn = true;
            decond = false;
            if (ConfigManager.Manager.Config.GetBoolValue("scp049_disable", false) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp049_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_049); } }
            if (ConfigManager.Manager.Config.GetBoolValue("scp096_disable", false) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp096_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_096); } }
            if (ConfigManager.Manager.Config.GetBoolValue("scp106_disable", false) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp106_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_106); } }
            if (ConfigManager.Manager.Config.GetBoolValue("scp173_disable", false) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp173_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_173); } }
            if (ConfigManager.Manager.Config.GetBoolValue("scp939_53_disable", false) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp939_53_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_939_53); } }
            if (ConfigManager.Manager.Config.GetBoolValue("scp939_89_disable", false) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp939_89_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_939_89); } }
			if (ConfigManager.Manager.Config.GetBoolValue("scp079_disable", true) == false) { for (byte a = 0; a < (byte)ConfigManager.Manager.Config.GetIntValue("scp079_amount", 1); a++) { enabledSCPs.Add((byte)Role.SCP_079); } }
			if (plugin.GetConfigIntList("lj_queue").Count() != 0)
            {
                number = 0;
            }
            foreach (Player player in ev.Server.GetPlayers())
            {
                blacklist.Add(player.SteamId);
                FilledTeams.Add((byte)player.TeamRole.Team);
                if (enabledSCPs.Contains((byte)player.TeamRole.Role)) { enabledSCPs.Remove((byte)player.TeamRole.Role); }
            }
            int time = plugin.GetConfigInt("lj_time");
            if (time != -1)
            {
                InitTimer(time);
            } else if (time == -1)
            {
            } else
            {
                plugin.Error("Config for lj_time of " + time + " is not a valid value! Using default instead.");
                InitTimer(30);
            }
        }

        private byte TeamIDtoClassID(byte TeamID)
        {
            if (decond && (TeamID == 3 || TeamID == 4 || TeamID == 0))
            {
                TeamID = 255;
            }
            switch (TeamID)
            {
                case (byte)Smod2.API.Team.SCP:
                    return enabledSCPs[getrandom.Next(0, enabledSCPs.Count)];
                case (byte)Smod2.API.Team.NINETAILFOX:
                    return (byte)Role.FACILITY_GUARD;
                case (byte)Smod2.API.Team.CHAOS_INSURGENCY:
                    return (byte)Role.CHAOS_INSUGENCY;
                case (byte)Smod2.API.Team.SCIENTISTS:
                    return (byte)Role.SCIENTIST;
                case (byte)Smod2.API.Team.CLASSD:
                    return (byte)Role.CLASSD;
                case (byte)Smod2.API.Team.SPECTATOR:
                    return (byte)Role.SPECTATOR;
                case (byte)Smod2.API.Team.TUTORIAL:
                    return (byte)Role.TUTORIAL;
                default:
                    plugin.Error("Tried to select an invalid team! Attempting using filler!");
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

        private byte ChooseClass(Player player)
        {
            byte chosenclass = (byte)Role.CLASSD;
            if (plugin.GetConfigIntList("lj_queue").Length == 0)
            {
                string[] queuestr = ConfigFile.GetList("team_respawn_queue");
                char[] queuechar = queuestr[0].ToCharArray();
                byte[] queue = System.Array.ConvertAll(queuechar, c => (byte)System.Char.GetNumericValue(c));
                plugin.Debug("no special queue, using vanilla queue.");
                if (FilledTeams.Count < queue.Length && ConfigManager.Manager.Config.GetBoolValue("smart_class_picker", false) == false)
                {
                    plugin.Debug("is within index");
                    byte chosenteam = queue[FilledTeams.Count];
                    chosenclass = TeamIDtoClassID(chosenteam);
                    if (chosenclass == 255)
                    {
                        plugin.Error("Your filler_team_id is set to an invalid value! Using default!");
                        chosenclass = (byte)Role.FACILITY_GUARD;
                    }
                    else
                    {
                        FilledTeams.Add(chosenteam);
                    }
                }
                else if(ConfigManager.Manager.Config.GetBoolValue("smart_class_picker", false) == false)
                {
                    plugin.Debug("is outside index, using filler");
                    chosenclass = TeamIDtoClassID((byte)ConfigManager.Manager.Config.GetIntValue("filler_team_id", chosenclass));
                    if (chosenclass == 255)
                    {
                        plugin.Error("Your filler_team_id is set to an invalid value! Using default!");
                        chosenclass = (byte)Role.FACILITY_GUARD;
                    }
                } else
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
                        } else { Leftover.Add(kvp.Key); }
                    }
                    byte chosenteam;
                    if (Leftover.Count != 0)
                    {
                        chosenteam = Leftover[getrandom.Next(0, Leftover.Count)];
                        FilledTeams.Add(chosenteam);
                        chosenclass = TeamIDtoClassID(chosenteam);
                    } else
                    {
                        plugin.Debug("no more class slots left, using filler.");
                        chosenclass = TeamIDtoClassID((byte)ConfigManager.Manager.Config.GetIntValue("filler_team_id", chosenclass));
                        if (chosenclass == 255)
                        {
                            plugin.Error("Your filler_team_id is set to an invalid value! Using default!");
                            chosenclass = (byte)Role.FACILITY_GUARD;
                        }
                    }
                }
            }
            else
            {
                int[] queue = plugin.GetConfigIntList("lj_queue");
                plugin.Debug("queue[number]:" + queue[number]);
                chosenclass = (byte)queue[number];
                if (!System.Enum.IsDefined(typeof(Role), (int)chosenclass))
                {
                    plugin.Error(chosenclass + " is not a valid class ID!  Setting ID to filler.");
                    chosenclass = TeamIDtoClassID((byte)ConfigManager.Manager.Config.GetIntValue("filler_team_id", chosenclass));
                    if (chosenclass == 255)
                    {
                        plugin.Error("Your filler_team_id is set to an invalid value! Using default!");
                        chosenclass = (byte)Role.FACILITY_GUARD;
                    }
                }
                number = (number + 1) % queue.Length;
            }
            return chosenclass;
        }

        public void OnPlayerJoin(PlayerJoinEvent ev)
        {
            Player player = ev.Player;
            if (allowspawn && (player.TeamRole.Team == Smod2.API.Team.NONE || player.TeamRole.Team == Smod2.API.Team.SPECTATOR))
            {
                bool result = false;
                foreach (string name in blacklist)
                {
                    if (name == player.SteamId)
                    {
                        result = true;
                        break;
                    }
                }
                if (!result)
                {
                    byte chosenclass = ChooseClass(player);
                    plugin.Info("Player " + RemoveSpecialCharacters(player.Name) + " joined late!  Setting their class to " + chosenclass);
                    player.ChangeRole((Role)chosenclass);
                    blacklist.Add(player.SteamId);
                }
            }
        }
    }
}
