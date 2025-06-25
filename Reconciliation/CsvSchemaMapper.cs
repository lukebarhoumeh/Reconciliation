using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Reconciliation;

/// <summary>
/// Maps arbitrary CSV headers into the canonical Microsoft schema
/// defined in column‑map.json, adds any missing columns, and
/// evaluates simple arithmetic expressions where needed.
/// </summary>
public static class CsvSchemaMapper
{
    // ───────────────────────────────────────────────────────────────
    // 1.  LAZY‑LOADED MAP (THREAD‑SAFE)
    // ───────────────────────────────────────────────────────────────
    private static readonly Lazy<Dictionary<string, JsonElement>> _maps =
        new(() => LoadMap(), isThreadSafe: true);

    private static Dictionary<string, JsonElement> LoadMap()
    {
        // Ordered list of locations to probe
        var candidatePaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                         AppConfig.Reconciliation.ColumnMapPath ?? "column-map.json"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "column-map.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "column-map.json")
        }.Distinct().ToArray();

        string? found = candidatePaths.FirstOrDefault(File.Exists);
        if (found == null)
            throw new FileNotFoundException(
                $"column-map.json could not be located. Paths checked:\n• " +
                string.Join("\n• ", candidatePaths));

        using var doc = JsonDocument.Parse(File.ReadAllText(found));
        return doc.RootElement.EnumerateObject()
                              .ToDictionary(p => p.Name, p => p.Value.Clone(),
                                            StringComparer.OrdinalIgnoreCase);
    }

    // ───────────────────────────────────────────────────────────────
    // 2.  CANONICAL MICROSOFT COLUMN LIST  (must match FileImport)
    // ───────────────────────────────────────────────────────────────
    private static readonly string[] _msColumns =
    {
        // Identity
        "PartnerId","CustomerId","CustomerName","CustomerDomainName","CustomerCountry",
        // Commercial identifiers
        "InvoiceNumber","MpnId","Tier2MpnId","OrderId","OrderDate","ReferenceId",
        // Product identifiers
        "ProductId","SkuId","AvailabilityId","SkuName","ProductName",
        // Financials
        "ChargeType","UnitPrice","Quantity","Subtotal","TaxTotal","Total","Currency",
        "PriceAdjustmentDescription",
        // Metadata
        "PublisherName","PublisherId","SubscriptionDescription","SubscriptionId",
        "ChargeStartDate","ChargeEndDate","TermAndBillingCycle","EffectiveUnitPrice",
        "UnitType","AlternateId","BillableQuantity","BillingFrequency","PricingCurrency",
        "PCToBCExchangeRate","PCToBCExchangeRateDate","MeterDescription","ReservationOrderId",
        "CreditReasonCode","SubscriptionStartDate","SubscriptionEndDate",
        "ProductQualifiers","PromotionId","ProductCategory",
        // Split columns used by match engine
        "Term","BillingCycle"
    };

    // ───────────────────────────────────────────────────────────────
    // 3.  PUBLIC API
    // ───────────────────────────────────────────────────────────────
    public static DataTable Normalize(DataTable raw, SourceType type)
    {
        if (raw == null) throw new ArgumentNullException(nameof(raw));

        var map = _maps.Value[TypeName(type)];
        DataTable table = new();

        // Ensure the canonical columns exist in order
        foreach (var col in _msColumns)
            table.Columns.Add(col, typeof(string));

        // Map each incoming row
        foreach (DataRow src in raw.Rows)
        {
            DataRow dest = table.NewRow();
            foreach (var canonical in _msColumns)
            {
                dest[canonical] = ResolveValue(map, canonical, src, dest);
            }
            table.Rows.Add(dest);
        }

        // Optional post‑normalisation (e.g. trim, upper‑case, etc.)
        DataNormaliser.Normalise(table, AppConfig.Reconciliation.CompositeKeys);
        return table;
    }

    // ───────────────────────────────────────────────────────────────
    // 4.  VALUE RESOLUTION  (string | array | expression)
    // ───────────────────────────────────────────────────────────────
    private static string ResolveValue(
        JsonElement map, string canonical, DataRow src, DataRow dest)
    {
        if (!map.TryGetProperty(canonical, out JsonElement def))
            return string.Empty;

        return def.ValueKind switch
        {
            JsonValueKind.String => ResolveScalar(def.GetString()!, src, dest),
            JsonValueKind.Array => ResolveArray(def, src),
            _ => string.Empty
        };
    }

    private static string ResolveScalar(string token, DataRow src, DataRow dest)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;

        // Expression like "{UnitPrice} * {Quantity}"
        if (token.Contains('{'))
        {
            decimal val = ExpressionColumnBuilder.Evaluate(token, dest, src);
            return val.ToString(CultureInfo.InvariantCulture);
        }

        // Plain column alias
        return src.Table.Columns.Contains(token)
             ? Convert.ToString(src[token]) ?? string.Empty
             : string.Empty;
    }

    private static string ResolveArray(JsonElement arr, DataRow src)
    {
        var items = arr.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToArray();

        // Pattern: ["Col1", "Col2"] → first non‑empty
        if (items.Length == 2 && items.All(s => s != "*" && s != "+"))
        {
            foreach (var col in items)
                if (!string.IsNullOrEmpty(Safe(src, col))) return Safe(src, col);
            return string.Empty;
        }

        // Pattern: ["A","B","*"] → A * B
        if (items.Length == 3 && items[2] == "*")
        {
            decimal a = ParseDecimal(src, items[0]);
            decimal b = ParseDecimal(src, items[1]);
            return (a * b).ToString(CultureInfo.InvariantCulture);
        }

        // Pattern: ["A","B","+","C"] → (A * B) + C
        if (items.Length == 4 && items[2] == "+")
        {
            decimal a = ParseDecimal(src, items[0]);
            decimal b = ParseDecimal(src, items[1]);
            decimal c = ParseDecimal(src, items[3]);
            return (a * b + c).ToString(CultureInfo.InvariantCulture);
        }

        return string.Empty;
    }

    // ───────────────────────────────────────────────────────────────
    // 5.  UTILITY
    // ───────────────────────────────────────────────────────────────
    private static decimal ParseDecimal(DataRow row, string col)
    {
        var val = Safe(row, col);
        decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d);
        return d;
    }

    private static string Safe(DataRow row, string col) =>
        row.Table.Columns.Contains(col) && row[col] != DBNull.Value
            ? Convert.ToString(row[col]) ?? string.Empty
            : string.Empty;

    private static string TypeName(SourceType t) => t switch
    {
        SourceType.Microsoft => "Microsoft",
        SourceType.Partner => "Partner",
        SourceType.Distributor => "Distributor",
        _ => "Microsoft"
    };
}
