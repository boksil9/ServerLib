using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ServerLib
{
	public abstract class ConfigManager
	{
		public abstract void LoadConfig(string path = "./config.json");
	}
}
