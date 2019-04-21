using Smod2.Commands;

namespace LaterJoin
{
	class CommandHandlerToggle : ICommandHandler
	{
		private Main plugin;

		public CommandHandlerToggle(Main plugin)
		{
			this.plugin = plugin;
		}

		public string GetCommandDescription()
		{
			return "toggles the late join feature.";
		}

		public string GetUsage()
		{
			return "(NO ARGUMENTS)";
		}

		public string[] OnCall(ICommandSender sender, string[] args)
		{
			if (plugin.LJenabled == false)
			{
				plugin.LJenabled = true;
				return new string[] { "Late Joins Toggled On!" };
			}
			else
			{
				plugin.LJenabled = false;
				return new string[] { "Late Joins Toggled Off!" };
			}
		}
	}
}
