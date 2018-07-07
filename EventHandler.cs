using Smod2;
using Smod2.API;
using Smod2.Events;
using System.Collections.Generic;
using System.Linq;

namespace Smod.TestPlugin
{

    class EventHandler : IEventRoundStart, IEventRoundEnd, IEventPlayerJoin
    {

        private Plugin plugin;
        private bool allowspawn = false;
        private int number = 0;
        private byte plycount = 0;
        private List<byte> FilledTeams = new List<byte>();
        private List<string> blacklist = new List<string>();
        private static readonly System.Random getrandom = new System.Random();

        List<byte> enabledSCPs = new List<byte>();

        public EventHandler(Plugin plugin)
        {
            this.plugin = plugin;
        }

        System.Timers.Timer t = new System.Timers.Timer();

        private void InitTimer(int time)
        {
            t.Interval = time * 1000;
            t.AutoReset = false;
            t.Enabled = true;
            t.Elapsed += delegate
            {
                allowspawn = false;
                t.Enabled = false;
                plugin.Debug("Time Over");
            };
        }

        public void OnRoundEnd(Server server, Round round)
        {
            t.Enabled = false;
            if (allowspawn) { allowspawn = false; }
        }

        public void OnRoundStart(Server server)
        {
            if (ConfigManager.Manager.Config.GetBoolValue("SCP049_DISABLE", false) == false) { enabledSCPs.Add((byte)Classes.SCP_049); }
//            if (ConfigManager.Manager.Config.GetBoolValue("SCP079_DISABLE", true) == false) { enabledSCPs.Add((byte)Classes.SCP_079); }
            if (ConfigManager.Manager.Config.GetBoolValue("SCP096_DISABLE", false) == false) { enabledSCPs.Add((byte)Classes.SCP_096); }
            if (ConfigManager.Manager.Config.GetBoolValue("SCP106_DISABLE", false) == false) { enabledSCPs.Add((byte)Classes.SCP_106); }
            if (ConfigManager.Manager.Config.GetBoolValue("SCP173_DISABLE", false) == false) { enabledSCPs.Add((byte)Classes.SCP_173); }
//            if (ConfigManager.Manager.Config.GetBoolValue("SCP457_DISABLE", true) == false) { enabledSCPs.Add((byte)Classes.SCP_457); }
            allowspawn = true;
            number = 0;
            plycount = 0;
            List<byte> FilledTeams = new List<byte>();
            List<string> blacklist = new List<string>();
            foreach (Player player in server.GetPlayers())
            {
                blacklist.Add(player.SteamId);
                FilledTeams.Add((byte)player.Class.Team);
                if (enabledSCPs.Contains((byte)player.Class.ClassType)) { enabledSCPs.Remove((byte)player.Class.ClassType); }
                plycount++;
            }
            int time = plugin.GetConfigInt("lj_time");
            if (time != -1)
            {
                InitTimer(time);
            } else if (time == -1)
            {
                allowspawn = true;
            } else
            {
                plugin.Error("Config for lj_time of " + time + " is not a valid value! Using default instead.");
                InitTimer(30);
            }
        }

        private byte TeamIDtoClassID(byte TeamID)
        {
            switch (TeamID)
            {
                case (byte)Teams.SCP:
                    if (enabledSCPs.Count != 0)
                    {
                        return enabledSCPs[getrandom.Next(0, enabledSCPs.Count - 1)];
                    } else
                    {
                        plugin.Debug("Tried to select an SCP but all SCP slots are filled! Choosing another class...");
                        return 255;
                    }

                case (byte)Teams.NINETAILFOX:
                    return (byte)Classes.FACILITY_GUARD;
                case (byte)Teams.CHAOS_INSURGENCY:
                    return (byte)Classes.CHAOS_INSUGENCY;
                case (byte)Teams.SCIENTISTS:
                    return (byte)Classes.SCIENTIST;
                case (byte)Teams.CLASSD:
                    return (byte)Classes.CLASSD;
                case (byte)Teams.SPECTATOR:
                    return (byte)Classes.SPECTATOR;
                case (byte)Teams.TUTORIAL:
                    return (byte)Classes.TUTORIAL;
                default:
                    plugin.Error("Tried to select an invalid team! Choosing another one...");
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
            byte chosenclass = (byte)Classes.CLASSD;
            if (plugin.GetConfigIntList("lj_queue").Length == 0)
            {
                string[] queuestr = ConfigFile.GetList("team_respawn_queue");
                char[] queuechar = queuestr[0].ToCharArray();
                byte[] queue = System.Array.ConvertAll(queuechar, c => (byte)System.Char.GetNumericValue(c));
                plugin.Debug("no special queue, using vanilla queue.");
                if (plycount < queue.Length && ConfigManager.Manager.Config.GetBoolValue("smart_class_picker", false) == false)
                {
                    plugin.Debug("is within index");
                    plugin.Debug("queue[plycount]:" + queue[plycount]);
                    byte chosenteam = queue[plycount];
                    chosenclass = TeamIDtoClassID(chosenteam);
                    FilledTeams.Add(chosenteam);
                    plycount++;
                }
                else if(ConfigManager.Manager.Config.GetBoolValue("smart_class_picker", false) == false)
                {
                    plugin.Debug("is outside index, using filler");
                    chosenclass = TeamIDtoClassID((byte)ConfigManager.Manager.Config.GetIntValue("filler_team_id", chosenclass));
                    if (chosenclass == 255)
                    {
                        plugin.Error("Your filler_team_id is set to an invalid value! Using default!");
                        chosenclass = (byte)Classes.CLASSD;
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
                        chosenteam = Leftover[getrandom.Next(0, Leftover.Count - 1)];
                        FilledTeams.Add(chosenteam);
                        chosenclass = TeamIDtoClassID(chosenteam);
                    } else
                    {
                        plugin.Debug("no more class slots left, using filler.");
                        chosenclass = TeamIDtoClassID((byte)ConfigManager.Manager.Config.GetIntValue("filler_team_id", chosenclass));
                        if (chosenclass == 255)
                        {
                            plugin.Error("Your filler_team_id is set to an invalid value! Using default!");
                            chosenclass = (byte)Classes.CLASSD;
                        }
                    }
                }
            }
            else
            {
                int[] queue = plugin.GetConfigIntList("lj_queue");
                plugin.Debug("queue[number]:" + queue[number]);
                chosenclass = (byte)queue[number];
                if (!System.Enum.IsDefined(typeof(Classes), (int)chosenclass))
                {
                    plugin.Error(chosenclass + " is not a valid class ID!  Setting ID to filler.");
                    chosenclass = TeamIDtoClassID((byte)ConfigManager.Manager.Config.GetIntValue("filler_team_id", chosenclass));
                    if (chosenclass == 255)
                    {
                        plugin.Error("Your filler_team_id is set to an invalid value! Using default!");
                        chosenclass = (byte)Classes.CLASSD;
                    }
                }
                number = (number + 1) % queue.Length;
            }
            return chosenclass;
        }

        public void OnPlayerJoin(Player player)
        {
            if (allowspawn && (player.Class.Team == Teams.SPECTATOR || player.Class.Team == Teams.NONE))
            {
                bool result = false;
                foreach (string name in blacklist)
                {
                    if (name.Equals(player.SteamId))
                    {
                        result = true;
                        break;
                    }
                }
                if (!result)
                {
                    byte chosenclass = ChooseClass(player);
                    while (chosenclass == 255)
                    {
                        chosenclass = ChooseClass(player);
                    }
                    plugin.Info("Player " + player.Name + " joined late!  Setting their class to " + chosenclass);
                    player.ChangeClass((Classes)chosenclass, true, true);
                    blacklist.Add(player.SteamId);
                }
            }
        }
    }
}
