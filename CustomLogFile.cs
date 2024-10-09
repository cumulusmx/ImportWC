using System.Globalization;
using System.Text;


namespace ImportWC
{
	static partial class CustomLogFile
	{
		private static readonly SortedList<DateTime, CustomLogFileRec> records = [];

		public static DateTime LastTimeStamp { get; set; }

		public static int RecordsCount { get => records.Count; }

		internal static void Initialise()
		{
			records.Clear();
			LastTimeStamp = DateTime.MinValue;
		}

		internal static void AddRecord(WeatherCatRecord rec)
		{
			LastTimeStamp = rec.Timestamp;

			if (!records.TryGetValue(rec.Timestamp, out var value))
			{
				value = new CustomLogFileRec() { LogTime = rec.Timestamp};
				records.Add(rec.Timestamp, value);
			}

			// Synthetic Channel 1-10
			for (var i = 0; i < 10; i++)
			{
				value.Synth[i] = rec.Synth[i];
			}
		}


		public static void WriteLogFile()
		{

			if (records.Count == 0)
			{
				Program.LogMessage("No records to write to Extra Log file!");
				return;
			}

			var logfilename = "data" + Path.DirectorySeparatorChar + GetCustomLogFileName(records.First().Key);
			Program.LogMessage($"Writing {records.Count} to {logfilename}");
			Program.LogConsole($"  Writing to {logfilename}", ConsoleColor.Gray);

			// backup old logfile
			if (File.Exists(logfilename))
			{
				if (!File.Exists(logfilename + ".sav"))
				{
					File.Move(logfilename, logfilename + ".sav");
				}
				else
				{
					var i = 1;
					do
					{
						if (!File.Exists(logfilename + ".sav" + i))
						{
							File.Move(logfilename, logfilename + ".sav" + i);
							break;
						}
						else
						{
							i++;
						}
					} while (true);
				}
			}


			try
			{
				using FileStream fs = new FileStream(logfilename, FileMode.Append, FileAccess.Write, FileShare.Read);
				using StreamWriter file = new StreamWriter(fs);
				Program.LogMessage($"{logfilename} opened for writing {records.Count} records");

				foreach (var rec in records)
				{
					var line = RecToCsv(rec);
					if (null != line)
						file.WriteLine(line);
				}

				file.Close();
				Program.LogMessage($"{logfilename} write complete");
			}
			catch (Exception ex)
			{
				Program.LogMessage($"Error writing to {logfilename}: {ex.Message}");
			}

		}

		public static string RecToCsv(KeyValuePair<DateTime, CustomLogFileRec> keyval)
		{
			// Writes an entry to the n-minute extralogfile. Fields are comma-separated:
			// 0  Date in the form dd/mm/yy (the slash may be replaced by a dash in some cases)
			// 1  Current time - hh:mm
			// 2-11  Synthetic Channel 1-10

			var rec = keyval.Value;

			// make sure solar max is calculated for those stations without a solar sensor
			Program.LogDebugMessage("DoCustomLogFile: Writing log entry for " + rec.LogTime);
			var inv = CultureInfo.InvariantCulture;
			var sep = ',';

			var sb = new StringBuilder(256);
			sb.Append(rec.LogTime.ToString("dd/MM/yy", inv) + sep);
			sb.Append(rec.LogTime.ToString("HH:mm", inv) + sep);
			// Sythetic channel 1-10
			for (int i = 0; i < 10; i++)
			{
				var v = rec.Synth[i] ?? -99999;
				sb.Append((v > -99999 ? v.ToString("F1", inv) : string.Empty) + sep);
			}

			return sb.ToString();
		}

		private static string GetCustomLogFileName(DateTime thedate)
		{
			return "SythLog" + thedate.ToString("yyyyMM") + ".txt";
		}

	}

	internal class CustomLogFileRec
	{
		public DateTime LogTime;
		public double?[] Synth { get; set; } = new double?[10];
	}
}
