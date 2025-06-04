using System;
using System.Data;
using System.Linq;
using Reconciliation;

namespace Reconciliation.Tests
{
    public class DiscrepancyDetectorTests
    {
        [Fact]
        public void NumericTolerance_AllowsSmallDifference()
        {
            var a = new DataTable();
            a.Columns.Add("Value");
            a.Rows.Add("1.00");
            var b = new DataTable();
            b.Columns.Add("Value");
            b.Rows.Add("1.005");

            var detector = new DiscrepancyDetector { NumericTolerance = 0.01m };
            detector.Compare(a, b);
            Assert.Empty(detector.Discrepancies);
        }

        [Fact]
        public void DateTolerance_AllowsOneDayDifference()
        {
            var a = new DataTable();
            a.Columns.Add("Date");
            a.Rows.Add("2024-01-01");
            var b = new DataTable();
            b.Columns.Add("Date");
            b.Rows.Add("2024-01-02");
            var detector = new DiscrepancyDetector { DateTolerance = TimeSpan.FromDays(1) };
            detector.Compare(a, b);
            Assert.Empty(detector.Discrepancies);
        }

        [Fact]
        public void FuzzyMatch_IgnoresMinorTypos()
        {
            var a = new DataTable();
            a.Columns.Add("Text");
            a.Rows.Add("Widget");
            var b = new DataTable();
            b.Columns.Add("Text");
            b.Rows.Add("Widgte");

            var detector = new DiscrepancyDetector { TextDistance = 2 };
            detector.Compare(a, b);
            Assert.Empty(detector.Discrepancies);
        }

        [Fact]
        public void Handles_EmptyValues_Gracefully()
        {
            var a = new DataTable();
            a.Columns.Add("Col");
            a.Rows.Add(string.Empty);
            var b = new DataTable();
            b.Columns.Add("Col");
            b.Rows.Add(string.Empty);
            var detector = new DiscrepancyDetector();
            detector.Compare(a, b);
            Assert.Empty(detector.Discrepancies);
        }

        [Fact]
        public void Summary_GroupsRepeatedErrors()
        {
            var a = new DataTable();
            a.Columns.Add("Num");
            a.Rows.Add("1");
            a.Rows.Add("1");
            var b = new DataTable();
            b.Columns.Add("Num");
            b.Rows.Add("2");
            b.Rows.Add("2");
            var detector = new DiscrepancyDetector { NumericTolerance = 0m };
            detector.Compare(a, b);
            Assert.Equal(2, detector.Summary.Values.First());
        }
    }
}
