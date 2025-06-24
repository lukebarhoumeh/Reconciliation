using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Reconciliation;

public static class CsvSchemaMapper
{
    private static readonly Lazy<Dictionary<string, JsonElement>> _maps = new(LoadMap);

    private static Dictionary<string, JsonElement> LoadMap()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConfig.Reconciliation.ColumnMapPath);
        if (!File.Exists(path)) throw new FileNotFoundException("Column map not found", path);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var dict = new Dictionary<string, JsonElement>();
        foreach (var p in doc.RootElement.EnumerateObject())
        {
            dict[p.Name] = p.Value.Clone();
        }
        return dict;
    }

    private static readonly string[] _msColumns =
    {
        "PartnerId","CustomerId","CustomerName","CustomerDomainName","CustomerCountry",
        "InvoiceNumber","MpnId","Tier2MpnId","OrderId","OrderDate","ProductId","SkuId",
        "AvailabilityId","SkuName","ProductName","ChargeType","UnitPrice","Quantity",
        "Subtotal","TaxTotal","Total","Currency","PriceAdjustmentDescription",
        "PublisherName","PublisherId","SubscriptionDescription","SubscriptionId",
        "ChargeStartDate","ChargeEndDate","TermAndBillingCycle","EffectiveUnitPrice",
        "UnitType","AlternateId","BillableQuantity","BillingFrequency","PricingCurrency",
        "PCToBCExchangeRate","PCToBCExchangeRateDate","MeterDescription","ReservationOrderId",
        "CreditReasonCode","SubscriptionStartDate","SubscriptionEndDate","ReferenceId",
        "ProductQualifiers","PromotionId","ProductCategory"
    };

    public static DataTable Normalize(DataTable raw, SourceType type)
    {
        if (raw == null) throw new ArgumentNullException(nameof(raw));
        var map = _maps.Value[TypeName(type)];
        DataTable table = new();
        foreach (var col in _msColumns) table.Columns.Add(col);
        foreach (DataRow row in raw.Rows)
        {
            DataRow newRow = table.NewRow();
            foreach (var col in _msColumns)
            {
                if (!map.TryGetProperty(col, out var def)) { newRow[col] = string.Empty; continue; }
                newRow[col] = GetValue(def, row, raw);
            }
            table.Rows.Add(newRow);
        }
        DataNormaliser.Normalise(table, AppConfig.Reconciliation.CompositeKeys);
        return table;
    }

    private static string GetValue(JsonElement def, DataRow row, DataTable raw)
    {
        if (def.ValueKind == JsonValueKind.String)
        {
            var src = def.GetString();
            if (string.IsNullOrEmpty(src)) return string.Empty;
            return raw.Columns.Contains(src) ? Convert.ToString(row[src]) ?? string.Empty : string.Empty;
        }
        if (def.ValueKind == JsonValueKind.Array)
        {
            var list = def.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
            if (list.Count == 2 && !new[] {"*", "+"}.Contains(list[1]))
            {
                var first = raw.Columns.Contains(list[0]) ? Convert.ToString(row[list[0]]) ?? string.Empty : string.Empty;
                if (!string.IsNullOrEmpty(first)) return first;
                return raw.Columns.Contains(list[1]) ? Convert.ToString(row[list[1]]) ?? string.Empty : string.Empty;
            }
            if (list.Count == 3 && list[2] == "*")
            {
                decimal a = ParseDecimal(row, list[0]);
                decimal b = ParseDecimal(row, list[1]);
                return (a * b).ToString(CultureInfo.InvariantCulture);
            }
            if (list.Count == 4 && list[2] == "+")
            {
                decimal a = ParseDecimal(row, list[0]);
                decimal b = ParseDecimal(row, list[1]);
                decimal c = ParseDecimal(row, list[3]);
                return (a * b + c).ToString(CultureInfo.InvariantCulture);
            }
        }
        return string.Empty;
    }

    private static decimal ParseDecimal(DataRow row, string col)
    {
        string val = row.Table.Columns.Contains(col) ? Convert.ToString(row[col]) ?? string.Empty : string.Empty;
        decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d);
        return d;
    }

    private static string TypeName(SourceType t) => t switch
    {
        SourceType.Microsoft => "Microsoft",
        SourceType.Partner => "Partner",
        SourceType.Distributor => "Distributor",
        _ => "Microsoft"
    };
}
