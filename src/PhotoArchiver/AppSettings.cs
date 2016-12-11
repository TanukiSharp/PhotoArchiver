using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoArchiver
{
    public class AppSettings
    {
        public string TargetAbsolutePath { get; set; }
        public string TempAbsolutePath { get; set; }
        public string[] AllowedExtensions { get; set; }
    }
}
