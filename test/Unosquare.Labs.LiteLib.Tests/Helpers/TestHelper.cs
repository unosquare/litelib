using System.IO;

namespace Unosquare.Labs.LiteLib.Tests.Helpers
{
    static class TestHelper
    {
        public static string GetTempDb(string name)
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}-{name}.db");

            if (File.Exists(path))
                File.Delete(path);

            return path;
        }
    }
}
