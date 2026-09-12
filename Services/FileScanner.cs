using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace R10CSharp.Services
{
    public static class FileScanner
    {
        public static IEnumerable<string> GetImageFiles(string rootPath)
        {
            if (!Directory.Exists(rootPath)) return Enumerable.Empty<string>();
            return Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".cr2", System.StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".nef", System.StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".arw", System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
