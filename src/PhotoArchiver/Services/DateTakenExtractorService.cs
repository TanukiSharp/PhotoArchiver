using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoArchiver.Services
{
    public class DateTakenExtractorService : ObjectPool<RootDateTakenExtractor>
    {
        public DateTakenExtractorService()
            : base(() => new RootDateTakenExtractor())
        {
        }
    }
}
