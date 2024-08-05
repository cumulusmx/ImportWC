using System.Globalization;
using System.Text;


namespace ImportWC
{
	static partial class LogFile
	{
		private static readonly SortedList<DateTime, LogFileRec> records = [];

		public static int RecordsCount { get => records.Count; }


		public static DateTime LastTimeStamp { get; set; }

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
				value = new LogFileRec() { LogTime = rec.Timestamp};
				records.Add(rec.Timestamp, value);
			}

			value.Temperature = rec.OutsideTemp ?? 0;

			value.Humidity = rec.OutsideHumidity ?? 0;

			value.Dewpoint = rec.Dewpoint ?? 0;

			value.WindSpeed = rec.WindSpeed ?? 0;
			value.WindGust = rec.WindGust ?? 0;
			value.WindBearing = rec.WindDir;

			value.RainfallRate = rec.RainRate ?? 0;
			value.RainfallToday = rec.RainDay ?? 0;

			value.Baro = rec.Baro ?? 0;

			value.RainfallCounter = rec.RainYear ?? 0;

			value.InsideTemp = rec.InsideTemp ?? 0;

			value.InsideHum = rec.InsideHumidity ?? 0;

			value.CurrentGust = rec.WindGust ?? 0;

			value.WindChill = rec.WindChill ?? 0;

			value.UVI = rec.UV ?? 0;
			value.SolarRad = rec.Solar ?? 0;

			value.ET = rec.ET ?? 0;
			value.ETyear = rec.ETYear ?? 0;

			value.CurrentBearing = rec.WindDir;

			value.RainSinceMidnight = rec.RainDay ?? 0;

			if (rec.OutsideTemp.HasValue && rec.OutsideHumidity.HasValue)
			{
				var val = MeteoLib.HeatIndex(ConvertUnits.UserTempToC(value.Temperature), value.Humidity);
				value.HeatIndex = ConvertUnits.TempCToUser(val);

				val = MeteoLib.Humidex(ConvertUnits.UserTempToC(value.Temperature), value.Humidity);
				value.Humidex = ConvertUnits.TempCToUser(val);

				if (!rec.Dewpoint.HasValue)
				{
					val = MeteoLib.DewPoint(ConvertUnits.UserTempToC(value.Temperature), value.Humidity);
					value.Dewpoint = ConvertUnits.TempCToUser(val);
				}

				if (rec.WindSpeed.HasValue)
				{
					val = MeteoLib.ApparentTemperature(ConvertUnits.UserTempToC(value.Temperature), ConvertUnits.UserWindToMS(value.WindSpeed), value.Humidity);
					value.ApparentTemp = ConvertUnits.TempCToUser(val);

					val = MeteoLib.FeelsLike(ConvertUnits.UserTempToC(value.Temperature), ConvertUnits.UserWindToKPH(value.WindSpeed), value.Humidity);
					value.FeelsLike = ConvertUnits.TempCToUser(val);
				}
			}


			value.SolarMax = AstroLib.SolarMax(
					rec.Timestamp,
					(double) Program.Cumulus.Longitude,
					(double) Program.Cumulus.Latitude,
					Utils.AltitudeM(Program.Cumulus.Altitude),
					out _,
					Program.Cumulus.SolarOptions
				);
		}


		public static void WriteLogFile()
		{
			var logfilename = "data" + Path.DirectorySeparatorChar + GetLogFileName(records.First().Key);

			if (records.Count == 0)
			{
				Program.LogMessage($"No records to write to {logfilename}!");
				Program.LogConsole($"  No records to write to {logfilename}!", ConsoleColor.Red);
				return;
			}

			Program.LogMessage($"Writing {records.Count} to {logfilename}");
			Program.LogConsole($"  Writing to {logfilename}", ConsoleColor.Gray);

			// backup old log file
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

		public static string RecToCsv(KeyValuePair<DateTime, LogFileRec> keyval)
		{
			// Writes an entry to the n-minute log file. Fields are comma-separated:
			// 0  Date in the form dd/mm/yy (the slash may be replaced by a dash in some cases)
			// 1  Current time - hh:mm
			// 2  Current temperature
			// 3  Current humidity
			// 4  Current dewpoint
			// 5  Current wind speed
			// 6  Recent (10-minute) high gust
			// 7  Average wind bearing
			// 8  Current rainfall rate
			// 9  Total rainfall today so far
			// 10  Current sea level pressure
			// 11  Total rainfall counter as held by the station
			// 12  Inside temperature
			// 13  Inside humidity
			// 14  Current gust (i.e. 'Latest')
			// 15  Wind chill
			// 16  Heat Index
			// 17  UV Index
			// 18  Solar Radiation
			// 19  Evapotranspiration
			// 20  Annual Evapotranspiration
			// 21  Apparent temperature
			// 22  Current theoretical max solar radiation
			// 23  Hours of sunshine so far today
			// 24  Current wind bearing
			// 25  RG-11 rain total
			// 26  Rain since midnight
			// 27  Feels like
			// 28  Humidex

			var rec = keyval.Value;

			// make sure solar max is calculated for those stations without a solar sensor
			Program.LogDebugMessage("DoLogFile: Writing log entry for " + rec.LogTime);
			var inv = CultureInfo.InvariantCulture;
			var sep = ",";

			var sb = new StringBuilder(256);
			sb.Append(rec.LogTime.ToString("dd/MM/yy", inv) + sep);
			sb.Append(rec.LogTime.ToString("HH:mm", inv) + sep);
			sb.Append(rec.Temperature.ToString(Program.Cumulus.TempFormat, inv) + sep);
			sb.Append(rec.Humidity.ToString() + sep);
			sb.Append(rec.Dewpoint.ToString(Program.Cumulus.TempFormat, inv) + sep);
			sb.Append(rec.WindSpeed.ToString(Program.Cumulus.WindAvgFormat, inv) + sep);
			sb.Append(rec.WindGust.ToString(Program.Cumulus.WindFormat, inv) + sep);
			sb.Append(rec.WindBearing.ToString() + sep);
			sb.Append(rec.RainfallRate.ToString(Program.Cumulus.RainFormat, inv) + sep);
			sb.Append(rec.RainfallToday.ToString(Program.Cumulus.RainFormat, inv) + sep);
			sb.Append(rec.Baro.ToString(Program.Cumulus.PressFormat, inv) + sep);
			sb.Append(rec.RainfallCounter.ToString(Program.Cumulus.RainFormat, inv) + sep);
			sb.Append(rec.InsideTemp.ToString(Program.Cumulus.TempFormat, inv) + sep);
			sb.Append(rec.InsideHum.ToString() + sep);
			sb.Append(rec.CurrentGust.ToString(Program.Cumulus.WindFormat, inv) + sep);
			sb.Append(rec.WindChill.ToString(Program.Cumulus.TempFormat, inv) + sep);
			sb.Append(rec.HeatIndex.ToString(Program.Cumulus.TempFormat, inv) + sep);
			sb.Append(rec.UVI.ToString(Program.Cumulus.UVFormat, inv) + sep);
			sb.Append(rec.SolarRad.ToString() + sep);
			sb.Append(rec.ET.ToString(Program.Cumulus.ETFormat, inv) + sep);
			sb.Append(rec.ETyear.ToString(Program.Cumulus.ETFormat, inv) + sep); // annual ET
			sb.Append(rec.ApparentTemp.ToString(Program.Cumulus.TempFormat, inv) + sep);
			sb.Append(rec.SolarMax.ToString() + sep);
			sb.Append(rec.SunshineHours.ToString(Program.Cumulus.SunFormat, inv) + sep);
			sb.Append(rec.WindBearing.ToString() + sep);
			sb.Append(rec.RG11Rain.ToString(Program.Cumulus.RainFormat, inv) + sep);
			sb.Append(rec.RainSinceMidnight.ToString(Program.Cumulus.RainFormat, inv) + sep);
			sb.Append(rec.FeelsLike.ToString(Program.Cumulus.TempFormat, inv) + sep);
			sb.Append(rec.Humidex.ToString(Program.Cumulus.TempFormat, inv));

			return sb.ToString();
		}

		private static string GetLogFileName(DateTime thedate)
		{
			return  thedate.ToString("yyyyMM") + "log.txt";
		}

	}

	internal class LogFileRec
	{
		public DateTime LogTime;
		public double Temperature;
		public int Humidity;
		public double Dewpoint;
		public double WindSpeed;
		public double WindGust;
		public int WindBearing;
		public double RainfallRate;
		public double RainfallToday;
		public double Baro;
		public double RainfallCounter;
		public double InsideTemp;
		public double InsideHum;
		public double CurrentGust;
		public double WindChill;
		public double HeatIndex;
		public double UVI;
		public int SolarRad;
		public double ET;
		public double ETyear;
		public double ApparentTemp;
		public int SolarMax;
		public double SunshineHours = 0;
		public int CurrentBearing;
		public double RG11Rain = 0;
		public double RainSinceMidnight;
		public double FeelsLike;
		public double Humidex;
	}
}
