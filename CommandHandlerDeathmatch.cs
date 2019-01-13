using Smod2.Commands;

namespace LaterJoin
{
	class CommandHandlerDeathmatch : ICommandHandler
	{
		private Main plugin;

		public CommandHandlerDeathmatch(Main plugin)
		{
			this.plugin = plugin;
		}

		public string GetCommandDescription()
		{
			return "toggles lj_InfAutoRespawn for the round";
		}

		public string GetUsage()
		{
			return "(NO ARGUMENTS)";
		}

		public string[] OnCall(ICommandSender sender, string[] args)
		{
			if (plugin.infAutoRespawn == false)
			{
				plugin.infAutoRespawn = true;
				return new string[] { "Deathmatch Mode Toggled On!" };
			}
			else
			{
				plugin.infAutoRespawn = false;
				return new string[] { "Deathmatch Mode Toggled Off!" };
			}
		}
	}
}
