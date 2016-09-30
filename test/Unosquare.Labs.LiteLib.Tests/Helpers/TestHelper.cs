using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosquare.Labs.LiteLib.Tests.Helpers
{
    static class TestHelper
    {
        public static string GetTempDb(string name)
        {
            var path = Path.Combine(Path.GetTempPath(), $"{name}.db");

            if (File.Exists(path))
                File.Delete(path);

            return path;
        }
    }
}
