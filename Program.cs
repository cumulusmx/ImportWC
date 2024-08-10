using System.Diagnostics;


namespace ImportWC
{
	static class Program
	{
		public static Cumulus Cumulus { get; set; }
		public static string Location { get; set; }

		private static ConsoleColor defConsoleColour;

		public static string WcDataPath { get; set; }
		public static string WcConfigTemp { get; set; }
		public static string WcConfigDew { get; set; }
		public static string WcConfigWind { get; set; }
		public static string WcConfigPress { get; set; }
		public static string WcConfigRain { get; set; }


		static void Main()
		{
			// Tell the user what is happening

			TextWriterTraceListener myTextListener = new TextWriterTraceListener($"MXdiags{Path.DirectorySeparatorChar}ImportWC-{DateTime.Now:yyyyMMdd-HHmmss}.txt", "WClog");
			Trace.Listeners.Add(myTextListener);
			Trace.AutoFlush = true;

			defConsoleColour = Console.ForegroundColor;

			var fullVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
			var version = $"{fullVer.Major}.{fullVer.Minor}.{fullVer.Build}";
			LogMessage("ImportWC v." + version);
			Console.WriteLine("ImportWC v." + version);

			// Read the config file
			ReadWeatherCatConfig();

			LogMessage("Processing started");
			Console.WriteLine();
			Console.WriteLine($"Processing started: {DateTime.Now:U}");
			Console.WriteLine();

			// get the location of the exe - we will assume this is in the Cumulus root folder
			Location = AppDomain.CurrentDomain.BaseDirectory;

			// Read the Cumulus.ini file
			Cumulus = new Cumulus();

			// Check meteo day
			if (Cumulus.RolloverHour != 0)
			{
				LogMessage("Cumulus is not configured for a midnight rollover, so Import cannot create any day file entries");
				LogConsole("Cumulus is not configured for a midnight rollover, so no day file entries will be created", ConsoleColor.DarkYellow);
				LogConsole("You must run CreateMissing after this Import to create the day file entries", ConsoleColor.DarkYellow);
			}
			else
			{
				LogMessage("Cumulus is configured for a midnight rollover, Import will create day file entries");
				LogConsole("Cumulus is configured for a midnight rollover, so day file entries will be created", ConsoleColor.Cyan);
				LogConsole("You must still run CreateMissing after this Import to add missing details to those day file entries", ConsoleColor.Cyan);
			}
			Console.WriteLine();

			// Find all the wlk files
			// naming convention YYYY-MM.wlk, eg 2024-05.wlk
			LogMessage("Searching for cat files");
			Console.WriteLine("Searching for cat log files...");

			if (!Directory.Exists(WcDataPath))
			{
				LogMessage($"The source directory '{WcDataPath}' does not exist, aborting");
				LogConsole($"The source directory '{WcDataPath}' does not exist, aborting", ConsoleColor.Red);
				Environment.Exit(1);
			}

			var dirInfo = new DirectoryInfo(WcDataPath);
			var wcFiles = dirInfo.GetFiles("*_WeatherCatData.cat", SearchOption.AllDirectories);

			LogMessage($"Found {wcFiles.Length} cat log files");
			LogConsole($"Found {wcFiles.Length} cat log files", defConsoleColour);

			// sort the file list
			var wcList = wcFiles.OrderBy(f => f.FullName).ToList();

			var year = 0;
			var month = 0;

			foreach (var cat in wcList)
			{
				string[] lines;

				try
				{
					lines = File.ReadAllLines(cat.FullName);
				}
				catch (Exception ex)
				{
					LogMessage($"Error opening file {cat.FullName} - {ex.Message}");
					LogConsole($"Error opening file {cat.FullName} - {ex.Message}", ConsoleColor.Red);
					LogConsole("Skipping to next file", defConsoleColour);
					// abort this file
					continue;
				}

				// get the year/month from the filename
				// year is folder name containg the file
				// month is the first 1 or 2 characters of the filename followed by an underscore
				// eg /wc/2024/5_WeatherCatData.cat

				if (!int.TryParse(new DirectoryInfo(cat.FullName).Parent.Name, out year))
				{
					LogMessage($"Error parsing year from {cat.FullName}");
					LogConsole($"Error parsing year from {cat.FullName}", ConsoleColor.Red);
					LogConsole("Skipping to next file", defConsoleColour);
					// abort this file
					continue;
				}

				if (!int.TryParse(cat.Name.Split('_')[0], out month))
				{
					LogMessage($"Error parsing month from {cat.FullName}");
					LogConsole($"Error parsing month from {cat.FullName}", ConsoleColor.Red);
					LogConsole("Skipping to next file", defConsoleColour);
					// abort this file
					continue;
				}

				LogConsole($"Processing {cat.Name}...", ConsoleColor.Gray);
				LogMessage($"Processing {cat.FullName}...");

				LogMessage($"  {cat.Name} contains {lines.Length} lines");

				var started = false;

				foreach (var line in lines)
				{
					// we want to start reading data after the line containing "VERS:"

					if (!started && !line.StartsWith("VERS:"))
					{
						continue;
					}
					else
					{
						if (!started)
						{
							started = true;
							continue;
						}
						else if (line.Length == 0)
						{
							// skip blank lines afer VERS:
							continue;
						}
					}


					var rec = new WeatherCatRecord(year, month, line);

					LogFile.AddRecord(rec);

					if (rec.HasExtraData)
					{
						ExtraLogFile.AddRecord(rec);
					}

					if (rec.HasSynthData)
					{
						CustomLogFile.AddRecord(rec);
					}
				}

				// Write out the log file
				if (LogFile.RecordsCount > 0)
				{
					LogFile.WriteLogFile();
					LogFile.Initialise();
				}

				if (ExtraLogFile.RecordsCount > 0)
				{
					ExtraLogFile.WriteLogFile();
					ExtraLogFile.Initialise();
				}

				if (CustomLogFile.RecordsCount > 0)
				{
					CustomLogFile.WriteLogFile();
					CustomLogFile.Initialise();
				}
			}
		}

		public static void LogMessage(string message)
		{
			Trace.TraceInformation(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + message);
		}

		public static void LogDebugMessage(string message)
		{
#if DEBUG
			//Trace.TraceInformation(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + message);
#endif
		}

		public static void LogConsole(string msg, ConsoleColor colour, bool newLine = true)
		{
			Console.ForegroundColor = colour;

			if (newLine)
			{
				Console.WriteLine(msg);
			}
			else
			{
				Console.Write(msg);
			}

			Console.ForegroundColor = defConsoleColour;
		}

		private static void ReadWeatherCatConfig()
		{
			if (!System.IO.File.Exists(Program.Location + "wc_config.ini"))
			{
				Program.LogMessage("Failed to find wc_config.ini file!");
				Console.WriteLine("Failed to find wc_config.ini file!");
				Environment.Exit(1);
			}

			Program.LogMessage("Reading wc_config.ini file");

			IniFile ini = new IniFile("wc_config.ini");

			WcDataPath = ini.GetValue("data", "path", "");
			if (WcDataPath == "")
			{
				Program.LogMessage("Failed to find data path in wc_config.ini");
				Console.WriteLine("Failed to find data path in wc_config.ini");
				Environment.Exit(1);
			}

			WcConfigTemp = ini.GetValue("units", "temperature", "").ToLower();
			if (WcConfigTemp == "" || (WcConfigTemp != "c" && WcConfigTemp != "f"))
			{
				Program.LogMessage("Failed to find temperature units in wc_config.ini");
				Console.WriteLine("Failed to find temperature units in wc_config.ini");
				Environment.Exit(1);
			}

			WcConfigDew = ini.GetValue("units", "dewpoint", "").ToLower();
			if (WcConfigDew == "" || (WcConfigDew != "c" && WcConfigDew != "f"))
			{
				Program.LogMessage("Failed to find dewpoint units in wc_config.ini");
				Console.WriteLine("Failed to find dewpoint units in wc_config.ini");
				Environment.Exit(1);
			}

			WcConfigPress = ini.GetValue("units", "pressure", "").ToLower();
			if (WcConfigPress == "" || (WcConfigPress != "inhg" && WcConfigPress != "mb" && WcConfigPress != "hpa"))
			{
				Program.LogMessage("Failed to find pressure units in wc_config.ini");
				Console.WriteLine("Failed to find pressure units in wc_config.ini");
				Environment.Exit(1);
			}

			WcConfigWind = ini.GetValue("units", "wind", "");
			if (WcConfigWind == "" || (WcConfigWind != "kph" && WcConfigWind != "mps" && WcConfigWind != "mph" && WcConfigWind != "knots"))
			{
				Program.LogMessage("Failed to find wind units in wc_config.ini");
				Console.WriteLine("Failed to find wind units in wc_config.ini");
				Environment.Exit(1);
			}

			WcConfigRain = ini.GetValue("units", "rain", "");
			if (WcConfigRain == "" || (WcConfigRain != "mm" && WcConfigRain != "in"))
			{
				Program.LogMessage("Failed to find rain units in wc_config.ini");
				Console.WriteLine("Failed to find rain units in wc_config.ini");
				Environment.Exit(1);
			}
		}
	}
}
