using System.Globalization;

namespace ImportWC
{
	static internal class Utils
	{
		public static DateTime DdmmyyStrToDate(string d)
		{
			if (DateTime.TryParseExact(d, "dd/MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var result))
			{
				return result;
			}
			return DateTime.MinValue;
		}

		public static DateTime GetDateTime(DateTime date, string time, int rolloverHr)
		{
			var tim = time.Split(':');
			var timOnly = new TimeSpan(int.Parse(tim[0]), int.Parse(tim[1]), 0);
			var dat = date.Add(timOnly);
			if (rolloverHr != 0 && timOnly.Hours < rolloverHr)
			{
				dat.AddDays(1);
			}
			return dat;
		}

		public static bool TryDetectNewLine(string path, out string newLine)
		{
			using var fs = File.OpenRead(path);
			char prevChar = '\0';

			// read the first 1000 characters to try and find a newLine
			for (var i = 0; i < 1000; i++)
			{
				int b;
				if ((b = fs.ReadByte()) == -1)
					break;

				char curChar = (char) b;

				if (curChar == '\n')
				{
					newLine = prevChar == '\r' ? "\r\n" : "\n";
					return true;
				}

				prevChar = curChar;
			}

			// Returning false means could not determine linefeed convention
			newLine = Environment.NewLine;
			return false;
		}

		/// <summary>
		/// Convert altitude from user units to metres
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static double AltitudeM(double altitude)
		{
			if (Program.Cumulus.AltitudeInFeet)
			{
				return altitude * 0.3048;
			}
			else
			{
				return altitude;
			}
		}
	}
}
