using PhotoArchiver.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using DateTakenExtractors.Extractors;

namespace PhotoArchiver
{
    public class RootDateTakenExtractor : IDateTakenExtractor
    {
        private readonly IDateTakenExtractor[] extractors = new IDateTakenExtractor[]
        {
            new ExifDateTakenExtractor(),
            new RiffDateTakenExtractor(),
            new QuickTimeDateTakenExtractor(),
            //new FilenameDateTakenExtractor(),
            //new LastModificationDateTakenExtractor(),
        };

        public bool ExtractDateTaken(Stream stream, out DateTime dateTaken)
        {
            dateTaken = DateTime.MinValue;

            foreach (IDateTakenExtractor extractor in extractors)
            {
                bool result = extractor.ExtractDateTaken(stream, out dateTaken);
                stream.Position = 0;
                if (result)
                    return true;
            }

            return false;
        }
    }
}
