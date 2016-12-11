using System;
using System.IO;
using PhotoArchiver.Contracts;
//using System.ComponentModel.Composition;

namespace DateTakenExtractors.Extractors
{
	// [Export(typeof(IDateTakenExtractor))]
	public class LastModificationDateTakenExtractor : IDateTakenExtractor
	{
		public bool ExtractDateTaken(Stream stream, out DateTime dateTaken)
		{
            dateTaken = DateTime.MinValue;

            var fs = stream as FileStream;
            if (fs == null)
                return false;

			try
			{
				dateTaken = File.GetLastWriteTime(fs.Name);
				return true;
			}
			catch (Exception)
			{
				dateTaken = DateTime.MinValue;
				return false;
			}
		}
	}
}
