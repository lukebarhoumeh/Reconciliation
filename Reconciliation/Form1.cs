// -----------------------------------------------------------------------------
//  Reconciliation – runtime logic (rev. 2025‑06‑26)
// -----------------------------------------------------------------------------
#nullable enable
using OfficeOpenXml;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;
using WinFormsTimer = System.Windows.Forms.Timer;   // ← disambiguate

namespace Reconciliation
{
    public partial class Form1 : Form
    {
        // ────────────────────────────────────────────────────────── STATE
        private DataView? _msView;      // Microsoft invoice
        private DataView? _hubView;     // MSP Hub invoice
        private DataView? _resultView;  // Reconciliation / validation result
        private readonly string[] _keys =
            { "CustomerDomainName","ProductId","SkuId","ChargeType",
              "Term","BillingCycle","ReferenceId" };

        // flashing log tab
        private readonly WinFormsTimer _flash = new() { Interval = 4000 };

        // factory method so tests can inject a stub
        protected virtual AdvancedReconciliationService CreateAdvancedService()
            => new();

        // ────────────────────────────────────────────────────────── CTOR
        public Form1()
        {
            InitializeComponent();

            // If the form is being instantiated inside the WinForms designer
            // skip all run-time-only initialisation to avoid the IUIService error.
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            // ── run-time initialisation ─────────────────────────────────────────────
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            lblVersion.Text = $"v{Assembly.GetExecutingAssembly().GetName().Version}";
            ApplyGridStyle(dgvResults);
            ApplyGridStyle(dgvPrices);
            ApplyGridStyle(dgvLogs);

            // nice row numbers
            dgvResults.RowPostPaint += dgv_RowPostPaint;
            dgvPrices.RowPostPaint += dgv_RowPostPaint;
            dgvLogs.RowPostPaint += dgv_RowPostPaint;

            _flash.Tick += (_, _) =>
            {
                tabLogs.ForeColor = SystemColors.ControlText;
                _flash.Stop();
            };

            // default mode = compare two invoices
            rbCompare.Checked = true;
            rbCompare.CheckedChanged += rbMode_CheckedChanged;
            rbValidate.CheckedChanged += rbMode_CheckedChanged;
            UpdateButtons();
        }



        // ────────────────────────────────────────────────────────── IMPORT
        private void btnImportMicrosoft_Click(object? sender, EventArgs e)
        {
            if (OpenCsv(out string file))
            {
                _msView = new FileImportService().ImportMicrosoftInvoice(file);
                lblMsInfo.Text =
                    $@"Microsoft: {Path.GetFileName(file)}   rows: {_msView.Count:N0}";
                UpdateButtons();
            }
        }
        private void btnImportHub_Click(object? sender, EventArgs e)
        {
            if (OpenCsv(out string file))
            {
                _hubView = new FileImportService().ImportSixDotOneInvoice(file);
                lblHubInfo.Text =
                    $@"MSP‑Hub:  {Path.GetFileName(file)}   rows: {_hubView.Count:N0}";
                UpdateButtons();
            }
        }
        private static bool OpenCsv(out string file)
        {
            file = string.Empty;
            using var ofd = new OpenFileDialog { Filter = "CSV|*.csv" };
            return ofd.ShowDialog() == DialogResult.OK && (file = ofd.FileName) != null;
        }

        // ────────────────────────────────────────────────────────── RUN
        private async void btnRun_Click(object? sender, EventArgs e)
        {
            lblEmpty.Visible = false;
            dgvResults.Visible = false;
            btnExport.Enabled = false;
            lblSummary.Text = @"Working…";

            try
            {
                if (rbCompare.Checked)           // ── two‑file reconciliation
                {
                    if (_msView == null || _hubView == null)
                    {
                        MessageBox.Show(@"Import both invoices first."); return;
                    }

                    var svc = CreateAdvancedService();
                    var res = await Task.Run(() =>
                        svc.Reconcile(_msView.Table, _hubView.Table));

                    _resultView = res.Exceptions.DefaultView;
                    lblSummary.Text =
                        $@"Unmatched groups: {res.Summary.UnmatchedGroups}   " +
                        $@"Over‑bill: {NumericFormatter.Money(res.Summary.OverBill)}   " +
                        $@"Under‑bill: {NumericFormatter.Money(res.Summary.UnderBill)}";
                }
                else                                // ── single‑file validation
                {
                    if (_hubView == null)
                    {
                        MessageBox.Show(@"Import an MSP‑Hub invoice first."); return;
                    }

                    var svc = new InvoiceValidationService();
                    var val = await Task.Run(() =>
                        svc.ValidateInvoice(_hubView.Table));

                    _resultView = val.InvalidRowsView;
                    lblSummary.Text =
                        $@"High: {val.HighPriority}   Low: {val.LowPriority}";
                }

                BindResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Reconciliation",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindResult()
        {
            dgvResults.DataSource = _resultView;
            dgvResults.Visible = _resultView?.Count > 0;
            lblEmpty.Text = _resultView?.Count == 0
                                    ? @"No discrepancies found." : string.Empty;
            lblEmpty.Visible = _resultView?.Count == 0;
            btnExport.Enabled = _resultView?.Count > 0;

            UpdateLogGrid();
        }

        // ────────────────────────────────────────────────────────── EXPORT
        private void btnExport_Click(object? sender, EventArgs e)
        {
            if (_resultView == null) return;

            string def = rbCompare.Checked
                ? $"Reconcile_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                : $"Validation_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            using var sfd = new SaveFileDialog
            {
                Filter = "Excel|*.xlsx",
                FileName = def
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            ExportHelper.ToExcel(_resultView.Table, sfd.FileName, lblSummary.Text);
            MessageBox.Show(@"Export complete.");
        }

        private void btnExportPrices_Click(object? sender, EventArgs e)
            => MessageBox.Show(@"Price‑mismatch export is not implemented in this build.");

        // ────────────────────────────────────────────────────────── RESET
        private void btnReset_Click(object? sender, EventArgs e) => HardReset();
        private void HardReset()
        {
            _msView = _hubView = _resultView = null;

            dgvResults.DataSource = dgvPrices.DataSource = null;
            lblMsInfo.Text = lblHubInfo.Text = lblSummary.Text = string.Empty;
            lblEmpty.Visible = false;
            btnExport.Enabled = false;
            lblPriceInfo.Text = @"No price mismatches.";

            UpdateButtons();
            ErrorLogger.Clear();
            UpdateLogGrid();
        }

        // ────────────────────────────────────────────────────────── MODE
        private void rbMode_CheckedChanged(object? s, EventArgs e) => UpdateButtons();
        private void UpdateButtons()
        {
            btnRun.Enabled = rbCompare.Checked
                ? _msView?.Count > 0 && _hubView?.Count > 0
                : _hubView?.Count > 0;

            btnReset.Enabled = _msView?.Count > 0 || _hubView?.Count > 0;
            btnImportMicrosoft.Enabled = rbCompare.Checked; // not needed in validate‑only mode
        }

        // ────────────────────────────────────────────────────────── FILTER
        private void FilterResults(object? sender, EventArgs e)
        {
            if (_resultView == null) return;

            string field = cmbFieldFilter.Text.Trim();
            string kw = txtExplanationFilter.Text.Trim();
            string filter = string.Empty;

            if (!string.IsNullOrEmpty(field) && field != "-- Select --")
                filter = $"[Field Name] = '{field.Replace("'", "''")}'";

            if (!string.IsNullOrEmpty(kw))
            {
                if (filter.Length > 0) filter += " AND ";
                filter += $"[Explanation] LIKE '%{kw.Replace("'", "''")}%'";
            }

            if (chkHPOnly.Checked &&
                _resultView.Table.Columns.Contains("IsHighPriority"))
            {
                if (filter.Length > 0) filter += " AND ";
                filter += "[IsHighPriority] = true";
            }

            _resultView.RowFilter = filter;
            dgvResults.Refresh();
        }

        // ────────────────────────────────────────────────────────── LOG GRID
        private void UpdateLogGrid()
        {
            var dt = new DataTable();
            dt.Columns.Add("Time"); dt.Columns.Add("Level");
            dt.Columns.Add("Row"); dt.Columns.Add("Col"); dt.Columns.Add("Msg");

            foreach (var e in ErrorLogger.AllEntries)
                dt.Rows.Add(e.Timestamp.ToString("HH:mm:ss"), e.ErrorLevel,
                            e.RowNumber == 0 ? "" : e.RowNumber.ToString(),
                            e.ColumnName, e.Description);

            dgvLogs.DataSource = dt;

            int warns = ErrorLogger.AllEntries.Count(x => x.ErrorLevel == "Warning");
            int errs = ErrorLogger.AllEntries.Count(x => x.ErrorLevel == "Error");
            lblLogsSummary.Text = $"⚠ {warns} warnings, {errs} errors";

            btnExportLogs.Enabled = dt.Rows.Count > 0;
            btnResetLogs.Enabled = dt.Rows.Count > 0;

            // flash the tab
            tabLogs.ForeColor = Color.Red;
            _flash.Stop(); _flash.Start();
        }
        private void dgvLogs_DataBindingComplete(object? s,
                                                 DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow r in dgvLogs.Rows)
            {
                string? lvl = r.Cells[1].Value?.ToString();
                if (lvl == "Error") r.DefaultCellStyle.BackColor = Color.LightCoral;
                if (lvl == "Warning") r.DefaultCellStyle.BackColor = Color.Khaki;
            }
        }

        private void btnExportLogs_Click(object? s, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV|*.csv",
                FileName = $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            ErrorLogger.Export(sfd.FileName);
            MessageBox.Show(@"Log exported.");
        }
        private void btnResetLogs_Click(object? s, EventArgs e)
        {
            ErrorLogger.Clear();
            UpdateLogGrid();
        }

        // ────────────────────────────────────────────────────────── GRID STYLE
        private static void ApplyGridStyle(DataGridView g)
        {
            g.EnableHeadersVisualStyles = false;
            g.BackgroundColor = Color.White;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 122, 204);
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
        }
        private void dgv_RowPostPaint(object? s, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = (DataGridView)s!;
            string idx = (e.RowIndex + 1).ToString();
            e.Graphics.DrawString(idx, grid.Font, Brushes.Gray,
                                  e.RowBounds.Left + 4, e.RowBounds.Top + 4);
        }

        // ────────────────────────────────────────────────────────── TITLE BAR
        private void btnClose_Click(object? s, EventArgs e) => Close();
        private void btnMinimise_Click(object? s, EventArgs e) => WindowState = FormWindowState.Minimized;

        // ────────────────────────────────────────────────────────── FORM LOAD
        private void Form1_Load(object? s, EventArgs e)
        {
            Font = new Font("Segoe UI", 9F);
        }
    }

    // helper for workbook export
    internal static class ExportHelper
    {
        public static void ToExcel(DataTable t, string file, string summary)
        {
            using var pkg = new OfficeOpenXml.ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("Data");
            int r = 1;

            if (!string.IsNullOrWhiteSpace(summary))
            {
                ws.Cells[r, 1].Value = summary;
                ws.Cells[r, 1, r, t.Columns.Count].Merge = true;
                ws.Cells[r, 1].Style.Font.Bold = true;
                r++;
            }

            for (int c = 0; c < t.Columns.Count; c++)
            {
                ws.Cells[r, c + 1].Value = t.Columns[c].ColumnName;
                ws.Cells[r, c + 1].Style.Font.Bold = true;
            }
            r++;

            foreach (DataRow row in t.Rows)
            {
                for (int c = 0; c < t.Columns.Count; c++)
                    ws.Cells[r, c + 1].Value = row[c];
                r++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            pkg.SaveAs(new FileInfo(file));
        }
    }
}