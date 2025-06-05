using System;
using System.Data;
using System.IO;

namespace Reconciliation.Cli
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                PrintUsage();
                return 0;
            }

            string? left = null;
            string? right = null;
            string? config = null;
            string? diffOut = null;
            string? logOut = null;
            string? summaryOut = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--left":
                        left = args[++i];
                        break;
                    case "--right":
                        right = args[++i];
                        break;
                    case "--config":
                        config = args[++i];
                        break;
                    case "--output-diff":
                        diffOut = args[++i];
                        break;
                    case "--output-log":
                        logOut = args[++i];
                        break;
                    case "--summary-file":
                        summaryOut = args[++i];
                        break;
                }
            }

            if (left == null || right == null)
            {
                Console.Error.WriteLine("Error: --left and --right are required.");
                PrintUsage();
                return 1;
            }

            if (config != null)
            {
                Environment.SetEnvironmentVariable("RECONCILIATION_CONFIG_PATH", Path.GetFullPath(config));
            }

            try
            {
                Run(left, right, diffOut, logOut, summaryOut);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            return 0;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: Reconciliation.Cli --left A.csv --right B.csv [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --config PATH        Path to appsettings.json");
            Console.WriteLine("  --output-diff PATH   Write discrepancy CSV to PATH");
            Console.WriteLine("  --output-log PATH    Write error log CSV to PATH");
            Console.WriteLine("  --summary-file PATH  Write summary text to PATH");
        }

        private static void Run(string leftPath, string rightPath, string? diffOut, string? logOut, string? summaryOut)
        {
            ErrorLogger.Clear();

            DataTable left = CsvNormalizer.NormalizeCsv(leftPath).Table;
            DataTable right = CsvNormalizer.NormalizeCsv(rightPath).Table;

            string[] requiredMicrosoft = ["CustomerDomainName", "ProductId", "SkuId", "ChargeType", "TermAndBillingCycle"];
            string[] requiredMspHub = ["InternalReferenceId", "SkuId", "BillingCycle"];
            SchemaValidator.RequireColumns(left, Path.GetFileName(leftPath), requiredMicrosoft, true);
            SchemaValidator.RequireColumns(right, Path.GetFileName(rightPath), requiredMspHub, true);

            DataQualityValidator.Run(left, Path.GetFileName(leftPath));
            DataQualityValidator.Run(right, Path.GetFileName(rightPath));

            var detector = new DiscrepancyDetector();
            detector.Compare(left, right);
            string summary = detector.GetSummary();

            if (diffOut != null)
            {
                detector.ExportCsv(diffOut);
            }
            if (logOut != null)
            {
                ErrorLogger.Export(logOut);
            }
            if (summaryOut != null)
            {
                File.WriteAllText(summaryOut, summary);
            }

            Console.WriteLine(summary);
        }
    }
}
