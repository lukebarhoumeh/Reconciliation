using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace Reconciliation;

/// <summary>
/// Lightweight logger that records entries in memory and can export to CSV.
/// </summary>
public static class SimpleLogger
{
    private static readonly List<(DateTime Timestamp,string Level,string Message)> _entries = new();

    public static IReadOnlyList<(DateTime Timestamp,string Level,string Message)> Entries => _entries;

    public static void Info(string message) => Log("INFO", message);
    public static void Warn(string message) => Log("WARN", message);
    public static void Error(string message) => Log("ERROR", message);

    public static void Log(string level, string message)
    {
        _entries.Add((DateTime.Now, level, message));
    }

    public static void Clear() => _entries.Clear();

    public static void Export(string filePath)
    {
        var lines = new List<string> {"Timestamp,Level,Message"};
        foreach (var e in _entries)
        {
            string msg = e.Message.Replace("\"", "\"\"");
            lines.Add($"{e.Timestamp:yyyy-MM-dd HH:mm:ss},{e.Level},\"{msg}\"");
        }
        File.WriteAllLines(filePath, lines);
    }

    public static DataTable ToTable()
    {
        var t = new DataTable();
        t.Columns.Add("Timestamp");
        t.Columns.Add("Level");
        t.Columns.Add("Message");
        foreach (var e in _entries)
        {
            var r = t.NewRow();
            r[0] = e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            r[1] = e.Level;
            r[2] = e.Message;
            t.Rows.Add(r);
        }
        return t;
    }
}
