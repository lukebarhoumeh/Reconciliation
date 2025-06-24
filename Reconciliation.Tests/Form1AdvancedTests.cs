#if HAS_UI
using System;
using System.Data;
using System.Reflection;
using System.Windows.Forms;

namespace Reconciliation.Tests;

public class Form1AdvancedTests
{
    private class StubService : AdvancedReconciliationService
    {
        public bool Called;
        public override ReconciliationResult Reconcile(DataTable msft, DataTable other)
        {
            Called = true;
            return new ReconciliationResult(new DataTable(), new ReconciliationSummary(0,0,0,0));
        }
    }

    private class TestForm : Form1
    {
        public readonly StubService Service = new();
        protected override AdvancedReconciliationService CreateAdvancedService() => Service;
        public RadioButton Advanced => (RadioButton)typeof(Form1)
            .GetField("rbAdvanced", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(this)!;
        public void SetData(DataView view)
        {
            typeof(Form1).GetField("_microsoftDataView", BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(this, view);
            typeof(Form1).GetField("_sixDotOneDataView", BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(this, view);
        }
        public void TriggerCompare() =>
            typeof(Form1).GetMethod("btnCompare_Click", BindingFlags.NonPublic|BindingFlags.Instance)!
                .Invoke(this, new object?[] { this, EventArgs.Empty });
    }

    [Fact]
    public void AdvancedMode_UsesService()
    {
        var dt = new DataTable();
        dt.Columns.Add("A");
        dt.Rows.Add("1");
        var form = new TestForm();
        form.SetData(dt.DefaultView);
        form.Advanced.Checked = true;
        form.TriggerCompare();
        System.Threading.Thread.Sleep(50);
        Assert.True(form.Service.Called);
    }
}
#endif
