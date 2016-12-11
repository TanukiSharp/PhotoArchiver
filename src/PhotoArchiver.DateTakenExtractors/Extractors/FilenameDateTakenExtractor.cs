using System;
using System.IO;
using PhotoArchiver.Contracts;
//using System.ComponentModel.Composition;

namespace DateTakenExtractors.Extractors
{
	// [Export(typeof(IDateTakenExtractor))]
	public class FilenameDateTakenExtractor : IDateTakenExtractor
	{
        private static readonly char[] UnderscoreCharacter = new char[] { '_' };
        private static readonly char[] DotCharacter = new char[] { '.' };

        public bool ExtractDateTaken(Stream stream, out DateTime dateTaken)
		{
            // parse filename like 'any/path/yyyy.MM.dd_HH.mm.ss.ext'

			dateTaken = DateTime.MinValue;

            var fs = stream as FileStream;
            if (fs == null)
                return false;

			string name = Path.GetFileNameWithoutExtension(fs.Name); // remains 'yyyy.MM.dd_HH.mm.ss'

            string[] dateTimeParts = name.Split(UnderscoreCharacter);

            if (dateTimeParts.Length != 2)
                return false;

			string[] dateParts = dateTimeParts[0].Split(DotCharacter);
            if (dateParts.Length != 3)
                return false;

			string[] timeParts = dateTimeParts[1].Split(DotCharacter);
            if (timeParts.Length != 3)
                return false;

            int year;
			int month;
			int day;

            int hour;
            int min;
            int sec;

            if (int.TryParse(dateParts[0], out year) == false ||
                int.TryParse(dateParts[1], out month) == false ||
                int.TryParse(dateParts[2], out day) == false ||
                int.TryParse(timeParts[0], out hour) == false ||
                int.TryParse(timeParts[1], out min) == false ||
                int.TryParse(timeParts[2], out sec) == false)
                return false;

			if (year < 0)
				return false;

			if (month < 1 || month > 12)
				return false;

			if (day < 1 || day > 31)
				return false;

			if (hour < 0 || hour > 23)
				return false;

			if (min < 0 || min > 59)
				return false;

			if (sec < 0 || sec > 59)
				return false;

			dateTaken = new DateTime(year, month, day, hour, min, sec);

			return true;
		}
	}
}
