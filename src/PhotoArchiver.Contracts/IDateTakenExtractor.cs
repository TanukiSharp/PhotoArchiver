using System;
using System.IO;

namespace PhotoArchiver.Contracts
{
    public interface IDateTakenExtractor
    {
        bool ExtractDateTaken(Stream stream, out DateTime dateTaken);
    }
}
