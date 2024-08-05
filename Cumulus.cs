using System.Globalization;

namespace ImportWC
{
	class Cumulus
	{
		public decimal Latitude;
		public decimal Longitude;
		public double Altitude;
		public bool AltitudeInFeet;

		public int RolloverHour;
		public bool Use10amInSummer;

		public string TempFormat;
		public string WindFormat;
		public string WindAvgFormat;
		public string RainFormat;
		public string PressFormat;
		public string UVFormat;
		public string SunFormat;
		public string ETFormat;
		public string WindRunFormat;
		public string TempTrendFormat;

		public double NOAAheatingthreshold;
		public double NOAAcoolingthreshold;

		public int ChillHourSeasonStart;
		public double ChillHourThreshold;

		public double CalibRainMult;

		public DateTime RecordsBeganDateTime;

		public SolarOptions SolarOptions = new();

		private readonly StationOptions StationOptions = new();
		internal StationUnits Units = new();
		private readonly int[] WindDPlaceDefaults = [1, 0, 0, 0]; // m/s, mph, km/h, knots
		private readonly int[] TempDPlaceDefaults = [1, 1];
		private readonly int[] PressDPlaceDefaults = [1, 1, 2];
		private readonly int[] RainDPlaceDefaults = [1, 2];

		public Cumulus()
		{
			// Get all the stuff we need from Cumulus.ini
			ReadIniFile();

			TempFormat = "F" + Units.TempDPlaces;
			WindFormat = "F" + Units.WindDPlaces;
			WindAvgFormat = "F" + Units.WindAvgDPlaces;
			RainFormat = "F" + Units.RainDPlaces;
			PressFormat = "F" + Units.PressDPlaces;
			UVFormat = "F" + Units.UVDPlaces;
			SunFormat = "F" + Units.SunshineDPlaces;
			ETFormat = "F" + (Units.RainDPlaces + 1);
			WindRunFormat = "F" + Units.WindRunDPlaces;
			TempTrendFormat = "+0.0;-0.0;0";

		}

		private void ReadIniFile()
		{
			if (!System.IO.File.Exists(Program.Location + "Cumulus.ini"))
			{
				Program.LogMessage("Failed to find Cumulus.ini file!");
				Console.WriteLine("Failed to find Cumulus.ini file!");
				Environment.Exit(1);
			}

			Program.LogMessage("Reading Cumulus.ini file");

			IniFile ini = new IniFile("Cumulus.ini");

			Latitude = ini.GetValue("Station", "Latitude", (decimal) 0.0);
			if (Latitude > 90 || Latitude < -90)
			{
				Latitude = 0;
				Program.LogMessage($"Error, invalid latitude value in Cumulus.ini [{Latitude}], defaulting to zero.");
				Program.LogConsole($"Error, invalid latitude value in Cumulus.ini [{Latitude}], defaulting to zero.", ConsoleColor.Red);
			}

			Longitude = ini.GetValue("Station", "Longitude", (decimal) 0.0);
			if (Longitude > 180 || Longitude < -180)
			{
				Longitude = 0;
				Program.LogMessage($"Error, invalid longitude value in Cumulus.ini [{Longitude}], defaulting to zero.");
				Program.LogConsole($"Error, invalid longitude value in Cumulus.ini [{Longitude}], defaulting to zero.", ConsoleColor.Red);
			}

			Altitude = ini.GetValue("Station", "Altitude", 0.0);
			AltitudeInFeet = ini.GetValue("Station", "AltitudeInFeet", false);

			var StationType = ini.GetValue("Station", "Type", -1);

			var IncrementPressureDP = ini.GetValue("Station", "DavisIncrementPressureDP", false);

			var RecordsBeganDate = ini.GetValue("Station", "StartDateIso", DateTime.Now.ToString("yyyy-MM-dd"));
			RecordsBeganDateTime = DateTime.ParseExact(RecordsBeganDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

			Program.LogMessage($"Cumulus start date Parsed: {RecordsBeganDateTime:yyyy-MM-dd}");


			if ((StationType == 0) || (StationType == 1))
			{
				Units.UVDPlaces = 1;
			}
			else
			{
				Units.UVDPlaces = 0;
			}

			RolloverHour = ini.GetValue("Station", "RolloverHour", 0);
			Use10amInSummer = ini.GetValue("Station", "Use10amInSummer", true);

			Units.Wind = ini.GetValue("Station", "WindUnit", 0);
			Units.Press = ini.GetValue("Station", "PressureUnit", 0);

			Units.Rain = ini.GetValue("Station", "RainUnit", 0);
			Units.Temp = ini.GetValue("Station", "TempUnit", 0);

			var RoundWindSpeed = ini.GetValue("Station", "RoundWindSpeed", false);

			// Unit decimals
			Units.RainDPlaces = RainDPlaceDefaults[Units.Rain];
			Units.TempDPlaces = TempDPlaceDefaults[Units.Temp];
			Units.PressDPlaces = PressDPlaceDefaults[Units.Press];
			Units.WindDPlaces = RoundWindSpeed ? 0 : WindDPlaceDefaults[Units.Wind];
			Units.WindAvgDPlaces = Units.WindDPlaces;

			// Unit decimal overrides
			Units.WindDPlaces = ini.GetValue("Station", "WindSpeedDecimals", Units.WindDPlaces);
			Units.WindAvgDPlaces = ini.GetValue("Station", "WindSpeedAvgDecimals", Units.WindAvgDPlaces);
			Units.WindRunDPlaces = ini.GetValue("Station", "WindRunDecimals", Units.WindRunDPlaces);
			Units.SunshineDPlaces = ini.GetValue("Station", "SunshineHrsDecimals", 1);

			if ((StationType == 0 || StationType == 1) && IncrementPressureDP)
			{
				// Use one more DP for Davis stations
				++Units.PressDPlaces;
			}
			Units.PressDPlaces = ini.GetValue("Station", "PressDecimals", Units.PressDPlaces);
			Units.RainDPlaces = ini.GetValue("Station", "RainDecimals", Units.RainDPlaces);
			Units.TempDPlaces = ini.GetValue("Station", "TempDecimals", Units.TempDPlaces);
			Units.UVDPlaces = ini.GetValue("Station", "UVDecimals", Units.UVDPlaces);

			StationOptions.UseZeroBearing = ini.GetValue("Station", "UseZeroBearing", false);

			NOAAheatingthreshold = ini.GetValue("NOAA", "HeatingThreshold", -1000.0);
			if (NOAAheatingthreshold < -99 || NOAAheatingthreshold > 150)
			{
				NOAAheatingthreshold = Units.Temp == 0 ? 18.3 : 65;
			}
			NOAAcoolingthreshold = ini.GetValue("NOAA", "CoolingThreshold", -1000.0);
			if (NOAAcoolingthreshold < -99 || NOAAcoolingthreshold > 150)
			{
				NOAAcoolingthreshold = Units.Temp == 0 ? 18.3 : 65;
			}

			CalibRainMult = ini.GetValue("Offsets", "RainMult", 1.0);

			ChillHourSeasonStart = ini.GetValue("Station", "ChillHourSeasonStart", 10);
			if (ChillHourSeasonStart < 1 || ChillHourSeasonStart > 12)
				ChillHourSeasonStart = 1;
			ChillHourThreshold = ini.GetValue("Station", "ChillHourThreshold", -999.0);
			if (ChillHourThreshold < -998)
			{
				ChillHourThreshold = Units.Temp == 0 ? 7 : 45;
			}

			SolarOptions.SunThreshold = ini.GetValue("Solar", "SunThreshold", 75, 1, 200);
			SolarOptions.SolarMinimum = ini.GetValue("Solar", "SolarMinimum", 30, 0);
			SolarOptions.LuxToWM2 = ini.GetValue("Solar", "LuxToWM2", 0.0079);
			SolarOptions.UseBlakeLarsen = ini.GetValue("Solar", "UseBlakeLarsen", false);
			SolarOptions.SolarCalc = ini.GetValue("Solar", "SolarCalc", 0, 0, 1);

			// Migrate old single solar factors to the new dual scheme
			if (ini.ValueExists("Solar", "RStransfactor"))
			{
				SolarOptions.RStransfactorJun = ini.GetValue("Solar", "RStransfactor", 0.8, 0.1);
				SolarOptions.RStransfactorDec = SolarOptions.RStransfactorJun;
			}
			else
			{
				if (ini.ValueExists("Solar", "RStransfactorJul"))
				{
					SolarOptions.RStransfactorJun = ini.GetValue("Solar", "RStransfactorJul", 0.8, 0.1);
				}
				else
				{
					SolarOptions.RStransfactorJun = ini.GetValue("Solar", "RStransfactorJun", 0.8, 0.1);
				}
				SolarOptions.RStransfactorDec = ini.GetValue("Solar", "RStransfactorDec", 0.8, 0.1);
			}
			if (ini.ValueExists("Solar", "BrasTurbidity"))
			{
				SolarOptions.BrasTurbidityJun = ini.GetValue("Solar", "BrasTurbidity", 2.0);
				SolarOptions.BrasTurbidityDec = SolarOptions.BrasTurbidityJun;
			}
			else
			{
				if (ini.ValueExists("Solar", "BrasTurbidityJul"))
				{
					SolarOptions.BrasTurbidityJun = ini.GetValue("Solar", "BrasTurbidityJul", 2.0);
				}
				else
				{
					SolarOptions.BrasTurbidityJun = ini.GetValue("Solar", "BrasTurbidityJun", 2.0);
				}
				SolarOptions.BrasTurbidityDec = ini.GetValue("Solar", "BrasTurbidityDec", 2.0);
			}
		}

		public int GetHourInc(DateTime timestamp)
		{
			if (RolloverHour == 0)
			{
				return 0;
			}
			else
			{
				try
				{
					if (Use10amInSummer && TimeZoneInfo.Local.IsDaylightSavingTime(timestamp))
					{
						// Locale is currently on Daylight time
						return -10;
					}
					else
					{
						// Locale is currently on Standard time or unknown
						return -9;
					}
				}
				catch (Exception)
				{
					return -9;
				}
			}
		}

	}

	internal class StationUnits
	{
		public int Wind { get; set; }
		public int Press { get; set; }
		public int Rain { get; set; }
		public int Temp { get; set; }
		public int WindDPlaces { get; set; }
		public int PressDPlaces { get; set; }
		public int RainDPlaces { get; set; }
		public int TempDPlaces { get; set; }
		public int WindAvgDPlaces { get; set; }
		public int WindRunDPlaces { get; set; }
		public int SunshineDPlaces { get; set; }
		public int UVDPlaces { get; set; }
	}

	public class StationOptions
	{
		public bool UseZeroBearing { get; set; }
		public bool UseWind10MinAve { get; set; }
		public bool UseSpeedForAvgCalc { get; set; }
		public bool UseSpeedForLatest { get; set; }
		public bool Humidity98Fix { get; set; }
		public bool CalculatedDP { get; set; }
		public bool CalculatedWC { get; set; }
		public bool SyncTime { get; set; }
		public int ClockSettingHour { get; set; }
		public bool UseCumulusPresstrendstr { get; set; }
		public bool LogExtraSensors { get; set; }
		public bool WS2300IgnoreStationClock { get; set; }
		public bool RoundWindSpeed { get; set; }
		public int PrimaryAqSensor { get; set; }
		public bool NoSensorCheck { get; set; }
		public int AvgBearingMinutes { get; set; }
		public int AvgSpeedMinutes { get; set; }
		public int PeakGustMinutes { get; set; }
	}

	public class SolarOptions
	{
		public int SunThreshold { get; set; }
		public int SolarMinimum { get; set; }
		public double LuxToWM2 { get; set; }
		public bool UseBlakeLarsen { get; set; }
		public int SolarCalc { get; set; }
		public double RStransfactorJun { get; set; }
		public double RStransfactorDec { get; set; }
		public double BrasTurbidityJun { get; set; }
		public double BrasTurbidityDec { get; set; }
	}
}
