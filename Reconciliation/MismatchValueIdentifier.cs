using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reconciliation
{
    /// <summary>
    /// Unique markers appended to cell values to indicate the origin table during comparison.
    /// </summary>
    public static class MismatchValueIdentifier
    {
        /// <summary>Marker for values coming from the Microsoft invoice.</summary>
        public const string MicrosoftMarker = "[MICROSOFT]";

        /// <summary>Marker for values coming from the MSP Hub invoice.</summary>
        public const string SixDotOneMarker = "[MSPHUB]";
    }
}
