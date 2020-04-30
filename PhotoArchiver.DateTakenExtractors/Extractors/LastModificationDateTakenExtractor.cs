using System;
using System.IO;
using PhotoArchiver.Contracts;

namespace DateTakenExtractors.Extractors
{
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
            catch
            {
                dateTaken = DateTime.MinValue;
                return false;
            }
        }
    }
}
