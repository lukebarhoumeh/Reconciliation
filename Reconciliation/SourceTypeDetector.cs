using System;
using System.IO;

namespace Reconciliation;

public static class SourceTypeDetector
{
    public static SourceType FromFilename(string path)
    {
        string file = Path.GetFileName(path).ToUpperInvariant();
        if (file.StartsWith("G0")) return SourceType.Microsoft;
        if (file.StartsWith("IND")) return SourceType.Partner;
        if (file.StartsWith("BILLINGTRANSACTIONS_")) return SourceType.Distributor;
        throw new ArgumentException($"Unable to detect source type for {file}");
    }
}
