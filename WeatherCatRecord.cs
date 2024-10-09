using System.Globalization;

namespace ImportWC
{
	internal class WeatherCatRecord
	{
		private readonly int _year;
		private readonly int _month;

		public DateTime Timestamp { get; private set; }
		public double? OutsideTemp { get; private set; }
		public double? InsideTemp { get; private set; }
		public double? Dewpoint { get; private set; }
		public double? Baro { get; private set; }
		public double? WindSpeed { get; private set; }
		public int WindDir { get; private set; } = 0;
		public double? WindChill { get; private set; }
		public double? WindGust { get; private set; }
		public double? RainHour { get; private set; }
		public double? RainDay { get; private set; }
		public double? RainMonth { get; private set; }
		public double? RainYear { get; set; }
		public double? RainRate { get; private set; }
		public int? OutsideHumidity { get; private set; }
		public int? InsideHumidity { get; private set; }
		public int? Solar { get; private set; }
		public double? UV { get; private set; }
		//public double? Sunshine { get; private set; }
		//public double? WindRun { get; private set; }
		//public double? WindAvg { get; private set; }
		//public double? TempTrend { get; private set; }
		//public double? WindTrend { get; private set; }
		public double? ET { get; private set; }
		public double? ETMonth { get; private set; }
		public double? ETYear { get; private set; }

		public double?[] ExtraTemp { get; private set; } = new double?[8];
		public int?[] ExtraHum { get; private set; } = new int?[8];
		public int?[] SoilMoist { get; private set; } = new int?[4];
		public double?[] SoilTemp { get; private set; } = new double?[4];
		public int?[] LeafWet { get; private set; } = new int?[4];
		public int?[] CO2 { get; private set; } = new int?[4];
		public double?[] Synth { get; private set; } = new double?[10];


		public bool HasExtraData { get; private set; }
		public bool HasSynthData { get; private set; }

		public WeatherCatRecord(int year, int month, string entry)
		{
			_year = year;
			_month = month;

			// replace conditions string
			if (entry.Contains("C:"))
			{
				var start = entry.IndexOf("C:") + 3;
				var end = entry.IndexOf("\" ", start);
				entry = string.Concat(entry.AsSpan(0, start), "x", entry.AsSpan(end));
			}

			var arr = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			// ignore the first entry = record number
			for (var i = 1; i < arr.Length; i++)
			{
				try
				{
					ProcessPair(arr[i].Split(':'));
				}
				catch(Exception ex)
				{
					Program.LogConsole($"Error at entry={arr[0]} field={i} year={year} month={month}: {ex.Message}", ConsoleColor.Red);
					Program.LogMessage($"Error at entry={arr[0]} field={i} year={year} month={month}: {ex.Message}");
				}
			}
		}


		/*
		 * t and V are not optional, all other fields are.
		 *
		 * t is the day, hour and minute (2 digits each)
		 * T is outside temperature
		 * Ti is internal temperature
		 * D is dew point
		 * Pr is barometric pressure
		 * W is wind speed
		 * Wd is wind direction
		 * Wc is wind chill
		 * Wg is wind gust
		 * Ph is hourly precipitation
		 * P is total precipitation
		 * Pm is monthly precipitation
		 * Py is annual precipitation
		 * H is outside humidity
		 * Hi is internal humidity
		 * S is solar
		 * U is UV
		 * T1 to T8 is auxiliary temperatures
		 * H1 to H8 are auxiliary humidity sensors
		 * Sm1 to Sm4 is soil moisture
		 * St1 to St4 is soil temperature
		 * Lw1 to Lw4 is leaf wetness
		 * Lt1 to Lt4 is leaf temperature
		 * Sy1 to Sy? is ????
		 * CO21 to CO24 is CO2
		 * Ed is daily ET
		 * Em is monthly ET
		 * Ey is yearly ET
		 * C is current conditions (delimited by double quotes)
		 * V is validation.
		 */

		private void ProcessPair(string[] entry)
		{
			if (entry.Length != 2)
			{
				Program.LogConsole("Invalid entry = " + entry[0] + " " + entry[1], ConsoleColor.Red);
				Program.LogMessage("Invalid entry = " + entry[0] + " " + entry[1]);
				return;
			}

			var key = entry[0];
			var val = entry[1];
			switch (key)
			{
				case "t":
					// t is the day, hour and minute (2 digits each)
					if (val.Length != 6)
					{
						Program.LogMessage("Invalid 't' value = " + val);
						break;
					}
					Timestamp = new DateTime(_year, _month, int.Parse(val[0..2]), int.Parse(val[2..4]), int.Parse(val[4..6]), 0, DateTimeKind.Local);
					break;

				case "T":
					// T is outside temperature
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double temp))
					{
						OutsideTemp = DoTemp(temp);
					}
					break;

				case "Ti":
					// Ti is internal temperature
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double intemp))
					{
						InsideTemp = DoTemp(intemp);
					}
					break;

				case "D":
					// D is dew point
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double dp))
					{
						Dewpoint = DoDewPt(dp);
					}
					break;

				case "Pr":
					// Pr is barometric pressure
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double pr))
					{
						Baro = DoPress(pr);
					}
					break;

				case "W":
					// W is wind speed
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double ws))
					{
						WindSpeed = DoWind(ws);
					}
					break;

				case "Wd":
					// Wd is wind direction
					if (int.TryParse(val, out int wd))
					{
						WindDir = wd;
					}
					break;

				case "Wc":
					// Wc is wind chill
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double wc))
					{
						WindChill = DoTemp(wc);
					}
					break;

				case "Wg":
					// Wg is wind gust
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double wg))
					{
						WindGust = DoWind(wg);
					}
					break;

				case "Ph":
					// Ph is hourly precipitation
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double ph))
					{
						RainHour = DoRain(ph);
						// There is no rainfall rate in the WC files, so use the hourly value
						RainRate = RainHour;
					}
					break;

				case "P":
					// P is total precipitation
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double p))
					{
						RainDay = DoRain(p);
					}
					break;

				case "Pm":
					// Pm is monthly precipitation
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double pm))
					{
						RainMonth = DoRain(pm);
					}
					break;

				case "Py":
					// Py is annual precipitation
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double py))
					{
						RainYear = DoRain(py);
					}
					break;

				case "H":
					// H is outside humidity
					if (int.TryParse(val, out int hum))
					{
						OutsideHumidity = hum;
					}
					break;

				case "Hi":
					// Hi is internal humidity
					if (int.TryParse(val, out int hi))
					{
						InsideHumidity = hi;
					}
					break;

				case "S":
					// S is solar
					if (int.TryParse(val, out int sol))
					{
						Solar = sol;
					}
					break;

				case "U":
					// U is uv
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double uv))
					{
						UV = uv;
					}
					break;

				case "Ed":
					// Ed is daily evapotranspiration
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double ed))
					{
						ET = ed;
					}
					break;

				case "Em":
					// Em is monthly evapotranspiration
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double em))
					{
						ETMonth = em;
					}
					break;

				case "Ey":
					// Ey is annual evapotranspiration
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double ey))
					{
						ETYear = ey;
					}
					break;

				case "T1":
				case "T2":
				case "T3":
				case "T4":
				case "T5":
				case "T6":
				case "T7":
				case "T8":
					// Tn is temperature
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double tempn))
					{
						var ind = int.Parse(key[1..]);
						ExtraTemp[ind - 1] = DoTemp(tempn);
						HasExtraData = true;
					}
					break;

				case "H1":
				case "H2":
				case "H3":
				case "H4":
				case "H5":
				case "H6":
				case "H7":
				case "H8":
					// Hn is humidity
					if (int.TryParse(val, out int humn))
					{
						var ind = int.Parse(key[1..]);
						ExtraHum[ind - 1] = humn;
						HasExtraData = true;
					}
					break;

				case "Sm1":
				case "Sm2":
				case "Sm3":
				case "Sm4":
					// Smn is Soil moisture
					if (int.TryParse(val, out int sm))
					{
						var ind = int.Parse(key[2..]);
						SoilMoist[ind - 1] = sm;
						HasExtraData = true;
					}
					break;

				case "St1":
				case "St2":
				case "St3":
				case "St4":
					// Stn is soil temperature
					if (double.TryParse(val, CultureInfo.InvariantCulture, out double st))
					{
						var ind = int.Parse(key[2..]);
						SoilTemp[ind - 1] = DoTemp(st);
						HasExtraData = true;
					}
					break;

				case "Lw1":
				case "Lw2":
				case "Lw3":
				case "Lw4":
					// Lwn is leaf wetness
					if (int.TryParse(val, out int lw))
					{
						var ind = int.Parse(key[2..]);
						LeafWet[ind - 1] = lw;
						HasExtraData = true;
					}
					break;

				case "CO21":
				case "CO22":
				case "CO23":
				case "CO24":
					// CO2n is CO2
					if (int.TryParse(val, out int co2))
					{
						var ind = int.Parse(key[3..]);
						CO2[ind - 1] = co2;
						HasExtraData = true;
					}
					break;

				case "Sy1":
				case "Sy2":
				case "Sy3":
				case "Sy4":
				case "Sy5":
				case "Sy6":
				case "Sy7":
				case "Sy8":
				case "Sy9":
				case "Sy10":
					if (double.TryParse(val, out double sy))
					{
						var ind = int.Parse(key[2..]);
						Synth[ind - 1] = sy;
						HasSynthData = true;
					}
					break;
			}
		}

		private static double DoTemp(double inp)
		{
			if (Program.WcConfigTemp == "c")
			{
				return ConvertUnits.TempCToUser(inp);
			}
			else
			{
				return ConvertUnits.TempFToUser(inp);
			}
		}

		private static double DoDewPt(double inp)
		{
			if (Program.WcConfigDew == "c")
			{
				return ConvertUnits.TempCToUser(inp);
			}
			else
			{
				return ConvertUnits.TempFToUser(inp);
			}
		}

		private static double DoPress(double inp)
		{
			return Program.WcConfigPress switch
			{
				"inhg" => ConvertUnits.PressINHGToUser(inp),
				"hpa" or "mb" => ConvertUnits.PressMBToUser(inp),
				"kpa" => ConvertUnits.PressKPAToUser(inp),
				_ => inp,
			};
		}

		private static double DoWind(double inp)
		{
			return Program.WcConfigWind switch
			{
				"mph" => ConvertUnits.WindMPHToUser(inp),
				"kts" => ConvertUnits.WindKnotsToUser(inp),
				_ => inp,
			};
		}

		private static double DoRain(double inp)
		{
			return Program.WcConfigRain switch
			{
				"mm" => ConvertUnits.RainMMToUser(inp),
				"in" => ConvertUnits.RainINToUser(inp),
				_ => inp,
			};
		}
	}
}
