// -----------------------------------------------------------------------------
//  Reconciliation – runtime logic (rev. 2025‑06‑26)
// -----------------------------------------------------------------------------
// -----------------------------------------------------------------------------
//  Reconciliation – runtime logic (rev. 2025‑06‑26)
// -----------------------------------------------------------------------------
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using FormsTimer = System.Windows.Forms.Timer;
using System.Globalization;

namespace Reconciliation
{
    public partial class Form1 : Form
    {
        private static readonly string[] suffixes = { "Bytes", "KB", "MB", "GB" };
        private DataView _microsoftDataView, _sixDotOneDataView, _resultData, _pricematchDataView;
        private string[] _uniqueKeyColumns = new[] { "CustomerDomainName", "ProductId", "SkuId", "ChargeType", "Term", "BillingCycle" };
        private string[] _columnsToBeDeleted = { "CustomerCountry", "MpnId", "AvailabilityId", "Currency", "PriceAdjustmentDescription", "PublisherName", "PublisherId", "ProductQualifiers", "ReferenceId", "MeterDescription", "PCToBCExchangeRateDate", "PCToBCExchangeRate", "PricingCurrency", "BillableQuantity", "AlternateId", "UnitType" };
        private readonly string[] _requiredMicrosoftColumns = new[] { "CustomerDomainName", "ProductId", "SkuId", "ChargeType", "TermAndBillingCycle" };
        private readonly string[] _requiredMspHubColumns = new[] { "InternalReferenceId", "SkuId", "BillingCycle" };
        private int hoveredIndex = -1;
        private bool isSwitchingMode = false;
        private readonly ToolTip _toolTip = new();
        private readonly FormsTimer _flashTimer = new();
        private string _lastSummary = string.Empty;

        #region Form_UX

        public Form1()
        {
            this.AutoScaleMode = AutoScaleMode.Dpi;
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Normal;
            this.Size = new Size(1280, 800);
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
            var assembly = Assembly.GetExecutingAssembly();
            lblVersion.Text = $"Version : {Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            EnableDoubleBuffering(tbcMenu);
            tbcMenu.DrawMode = TabDrawMode.OwnerDrawFixed;
            tbcMenu.DrawItem += new DrawItemEventHandler(tbcMenu_DrawItem);
            tbcMenu.MouseMove += new MouseEventHandler(tbcMenu_MouseMove);
            tbcMenu.MouseLeave += new EventHandler(tbcMenu_MouseLeave);
            tbcMenu.Paint += new PaintEventHandler(tbcMenu_Paint);
            // Ensure all TabPages have a white background
            foreach (TabPage page in tbcMenu.TabPages)
            {
                page.BackColor = Color.White;
            }
            _toolTip.SetToolTip(btnImportMicrosoft, "Select the Microsoft invoice CSV to reconcile");
            _toolTip.SetToolTip(btnImportSixDotOneFile, "Select the MSP Hub invoice CSV to reconcile");
            _toolTip.SetToolTip(btnCompare, "Run reconciliation using the loaded files");
            _toolTip.SetToolTip(btnExportToCsv, "Export reconciliation results to CSV");
            _toolTip.SetToolTip(btnExportLogs, "Export log to CSV");
            _toolTip.SetToolTip(btnResetLogs, "Clear logs");
            _toolTip.SetToolTip(btnToggleFiles, "Hide/Show details");
            _toolTip.SetToolTip(cmbFieldFilter, "Enter part of a field name or a keyword from the explanation to quickly filter discrepancies.");
            _toolTip.SetToolTip(txtExplanationFilter, "Enter part of a field name or a keyword from the explanation to quickly filter discrepancies.");
            this.rbExternal.CheckedChanged += new System.EventHandler(this.RadioButton_CheckedChanged);
            this.rbInternal.CheckedChanged += new System.EventHandler(this.RadioButton_CheckedChanged);
            this.rbExternal.Checked = true;
            btnCompare.Text = "Reconcile";
            textLogs.ReadOnly = true;
            btnImportSixDotOneFile.Text = "Upload MSP Hub Invoice";
            this.BackColor = Color.White;
            tbcMenu.BackColor = Color.White;
            btnExportLogs.Enabled = true;
            btnResetLogs.Enabled = true;
            dgResultdata.Visible = false;
            dgAzurePriceMismatch.Visible = false;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            dgResultdata.RowPrePaint += DataGridView1_RowPrePaint;
            cmbFieldFilter.TextChanged += FilterResults;
            cmbFieldFilter.SelectedIndexChanged += FilterResults;
            txtExplanationFilter.TextChanged += FilterResults;
            chkHighPriorityOnly.CheckedChanged += FilterResults;
            if (tbcMenu.TabPages.Contains(tabPage3))
            {
                tbcMenu.TabPages.Remove(tabPage3);
            }

            DisableFilters();

            _flashTimer.Interval = 5000;
            _flashTimer.Tick += (s, e) =>
            {
                tabPage2.ForeColor = SystemColors.ControlText;
                _flashTimer.Stop();
            };

            ApplyModernGridStyles(dgResultdata);
            ApplyModernGridStyles(dgAzurePriceMismatch);
            ApplyModernGridStyles(dgvLogs);

            dgResultdata.RowPostPaint += DataGridView_RowPostPaint;
            dgAzurePriceMismatch.RowPostPaint += DataGridView_RowPostPaint;
            dgvLogs.RowPostPaint += DataGridView_RowPostPaint;
        }
        private void EnableDoubleBuffering(Control control)
        {
            control.GetType().InvokeMember("DoubleBuffered", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, control, new object[] { true });
        }

        private void tbcMenu_MouseMove(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tbcMenu.TabCount; i++)
            {
                Rectangle tabBounds = tbcMenu.GetTabRect(i);
                if (tabBounds.Contains(e.Location))
                {
                    if (hoveredIndex != i)
                    {
                        hoveredIndex = i;
                        tbcMenu.Invalidate(); // Request a redraw
                    }
                    return;
                }
            }

            // If mouse is not over any tab, reset the hovered index
            if (hoveredIndex != -1)
            {
                hoveredIndex = -1;
                tbcMenu.Invalidate(); // Request a redraw
            }
        }

        private void tbcMenu_MouseLeave(object sender, EventArgs e)
        {
            if (hoveredIndex != -1)
            {
                hoveredIndex = -1;
                tbcMenu.SuspendLayout();
                tbcMenu.Invalidate();
                tbcMenu.ResumeLayout();
            }
        }

        // Paint event to clear the entire background
        private void tbcMenu_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Clear the entire TabControl area with white
            g.Clear(Color.White);

            // Manually fill the tab strip area
            Rectangle tabStripBounds = new Rectangle(0, 0, tbcMenu.Width, tbcMenu.ItemSize.Height);
            using (Brush brush = new SolidBrush(Color.White))
            {
                g.FillRectangle(brush, tabStripBounds);
            }
        }

        // DrawItem event to handle tab rendering
        private void tbcMenu_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            TabPage tabPage = tbcMenu.TabPages[e.Index];
            Rectangle tabBounds = tbcMenu.GetTabRect(e.Index);

            bool hovered = (e.Index == hoveredIndex);

            Color backColor = tabPage.BackColor;
            if (e.Index == tbcMenu.SelectedIndex)
            {
                backColor = Color.FromArgb(173, 216, 230); // light blue for selected tab
            }
            else if (hovered)
            {
                backColor = Color.FromArgb(237, 237, 237); // light gray when hovered
            }

            using (Brush brush = new SolidBrush(backColor))
            {
                g.FillRectangle(brush, tabBounds);
            }
            int bulletDiameter = 10;
            int bulletPadding = 5;

            Rectangle textBounds = new Rectangle(tabBounds.X + 2 * bulletPadding, tabBounds.Y, tabBounds.Width - bulletDiameter - 2 * bulletPadding, tabBounds.Height);

            TextRenderer.DrawText(g, tabPage.Text, tabPage.Font, textBounds, tabPage.ForeColor);
        }

        private void ApplyModernGridStyles(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = Color.White;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowHeadersVisible = false;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 122, 204);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
        }

        private void DataGridView_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            string rowIdx = (e.RowIndex + 1).ToString();
            using var brush = new SolidBrush(grid.RowHeadersDefaultCellStyle.ForeColor);
            e.Graphics.DrawString(rowIdx, grid.Font, brush, e.RowBounds.Location.X + 4, e.RowBounds.Location.Y + 4);
        }

        #endregion Form_UX

        #region Button_Clicks

        private void btnClose_Click(object sender, EventArgs e)
        {
            try
            {
                if (HasDataInTool())
                {
                    // Show confirmation message
                    DialogResult result = MessageBox.Show(
                        "You will lose the discrepancy results. You can export them before proceeding. Do you still want to close?",
                        "Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.No)
                    {
                        return; // Cancel closing
                    }
                }
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void btnImportMicrosoft_Click(object sender, EventArgs e)
        {
            try
            {
                var fileDialog = new OpenFileDialog
                {
                    Filter = "CSV File | *.csv",
                    Title = "Select Microsoft Partner-Center CSV",
                    Multiselect = false
                };

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClearFileInfo(lblMicrosoftFileName, lblMicrosoftFileRowCount);
                    var fileInfo = new FileInfo(fileDialog.FileName);
                    // TODO: Import logic moved to FileImportService
                    var service = new FileImportService();
                    _microsoftDataView = service.ImportMicrosoftInvoice(fileInfo.FullName);

                    // Show file name and row count
                    lblMicrosoftFileRowCount.Text = _microsoftDataView.Table.Rows.Count.ToString();
                    DisplayFileInfo(lblMicrosoftFileName, fileInfo);

                    // Enable Compare button if both files are loaded
                    UpdateCompareButtonState();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Alert", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }







        private void btnMaximized_Click(object sender, EventArgs e)
        {

            this.WindowState = FormWindowState.Minimized;
        }

        /// <summary>
        /// Toggle visibility of the results panel.
        /// </summary>
        private void btnToggleFiles_Click(object sender, EventArgs e)
        {
            splitMain.Panel2Collapsed = !splitMain.Panel2Collapsed;
            btnToggleFiles.Text = splitMain.Panel2Collapsed ? "\uE010" : "\uE014";
        }
        private void btnImportSixDotOneFile_Click(object sender, EventArgs e)
        {
            try
            {
                var fileDialog = new OpenFileDialog
                {
                    Filter = "CSV File | *.csv",
                    Title = "Please select any one CSV file",
                    Multiselect = false
                };

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClearFileInfo(lblSixDotOneFileName, lblSixDotOneFileRowCount);
                    var fileInfo = new FileInfo(fileDialog.FileName);
                    var size = FormatSize(fileInfo.Length);
                    // TODO: Import logic moved to FileImportService
                    var service = new FileImportService();
                    _sixDotOneDataView = service.ImportSixDotOneInvoice(fileInfo.FullName);
                    _sixDotOneDataView = ReorderColumns(_sixDotOneDataView.Table, _uniqueKeyColumns);
                    lblSixDotOneFileRowCount.Text = _sixDotOneDataView.Table.Rows.Count.ToString();
                    DisplayFileInfo(lblSixDotOneFileName, fileInfo);
                    DisplayFileInfo(lblSixdotonefilenameTab4, fileInfo);
                    lblSixdotonefilenameTab4.Visible = true;
                    UpdateCompareButtonState();


                }
            }
            catch (Exception oExp)
            {
                MessageBox.Show(oExp.Message, "Alert", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void btnCompare_Click(object sender, EventArgs e)
        {
            try
            {
                dgResultdata.DataSource = null;
                lblEmptyMessage.Text = "Please wait, processing...";
                lblEmptyMessage.Visible = true;
                lblPricematchingEmptyMessage.Text = "Please wait, processing...";
                lblPricematchingEmptyMessage.Visible = true;
                // Run the CPU-bound operations asynchronously
                if (rbExternal.Checked == true)
                {
                    var svc = new ReconciliationService();
                    _resultData = await Task.Run(() =>
                    {
                        return svc.CompareInvoices(_sixDotOneDataView.Table,
                                                     _microsoftDataView.Table)
                                   .DefaultView;
                    });
                    _lastSummary = svc.LastSummary;

                    Invoke(new Action(() =>
                    {
                        // Bind directly to the DataView so RowFilter updates reflect immediately
                        BindingSource bindingSource = new BindingSource { DataSource = _resultData };
                        dgResultdata.DataSource = bindingSource;
                        AutoFitColumns(dgResultdata);

                        lblMismatchSummary.Text = _lastSummary;
                        lblMismatchSummary.Visible = true;

                        if (dgResultdata.Columns.Contains("Product Code"))
                            dgResultdata.Columns["Product Code"].HeaderCell.ToolTipText = "Product Code: The code identifying the product being invoiced.";
                        if (dgResultdata.Columns.Contains("Invoice Date"))
                            dgResultdata.Columns["Invoice Date"].HeaderCell.ToolTipText = "Invoice Date: The date charges apply.";

                        foreach (DataGridViewColumn column in dgResultdata.Columns)
                        {
                            column.SortMode = DataGridViewColumnSortMode.NotSortable; // Disable column sorting
                        }

                        dgResultdata.ClearSelection();
                        btnExportToCsv.Enabled = true;
                        PopulateFieldFilterOptions();

                        if (dgResultdata.Rows.Count == 0)
                        {
                            lblEmptyMessage.Text = "No Discrepancy Records Found.";
                        }
                        else
                        {
                            lblEmptyMessage.Visible = false;
                        }
                        dgResultdata.Visible = true;
                    }));
                }
                else if (rbAdvanced.Checked)
                {
                    var svc = CreateAdvancedService();
                    var result = await Task.Run(() => svc.Reconcile(_microsoftDataView.Table, _sixDotOneDataView.Table));
                    _resultData = result.Exceptions.DefaultView;
                    _lastSummary = $"Unmatched: {result.Summary.UnmatchedGroups} Over: {result.Summary.OverBill} Under: {result.Summary.UnderBill}";

                    Invoke(new Action(() =>
                    {
                        BindingSource bindingSource = new BindingSource { DataSource = _resultData };
                        dgResultdata.DataSource = bindingSource;
                        AutoFitColumns(dgResultdata);
                        dgResultdata.ClearSelection();
                        btnExportToCsv.Enabled = true;
                        lblMismatchSummary.Text = _lastSummary;
                        lblMismatchSummary.Visible = true;
                        PopulateFieldFilterOptions();
                        lblEmptyMessage.Visible = _resultData.Count == 0;
                        dgResultdata.Visible = true;
                    }));
                }
                else
                {
                    var validation = await Task.Run(() =>
                    {
                        var svc = new InvoiceValidationService();
                        return svc.ValidateInvoice(_sixDotOneDataView.Table);
                    });
                    _resultData = validation.InvalidRowsView;
                    _lastSummary = $"High: {validation.HighPriority}  Low: {validation.LowPriority}";

                    Invoke(new Action(() =>
                    {
                        BindingSource bindingSource = new BindingSource { DataSource = _resultData };
                        dgResultdata.DataSource = bindingSource;
                        AutoFitColumns(dgResultdata);
                        dgResultdata.ClearSelection();
                        btnExportToCsv.Enabled = true;
                        lblMismatchSummary.Text = _lastSummary;
                        lblMismatchSummary.Visible = true;

                        if (dgResultdata.Rows.Count == 0)
                        {
                            lblEmptyMessage.Text = "No Discrepancy Records Found.";
                        }
                        else
                        {
                            lblEmptyMessage.Visible = false;
                        }
                        dgResultdata.Visible = true;
                        PopulateFieldFilterOptions();

                        // Attach the event handler for cell formatting
                        dgResultdata.CellFormatting += DgMismatchData_CellFormatting;
                        if (ErrorLogger.HasErrors)
                        {
                            var errorCount = ErrorLogger.Entries.Count(e => e.ErrorLevel == "Error");
                            var res = MessageBox.Show($"Parsing errors found: {errorCount}. Export log?", "Parsing Errors", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (res == DialogResult.Yes)
                            {
                                using var sfd = new SaveFileDialog { Filter = "CSV File|*.csv", FileName = $"ErrorLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv" };
                                if (sfd.ShowDialog() == DialogResult.OK)
                                    ErrorLogger.Export(sfd.FileName);
                            }
                            ErrorLogger.Clear();
                        }
                    }));
                }

                PopulateLogsGrid();
                FlashLogsTab();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Alert", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnExportToCsv_Click(object sender, EventArgs e)
        {
            try
            {
                // Format current date and time
                string dateTimeNow = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string defaultFileName = $"Export_{dateTimeNow}.xlsx";
                if (rbExternal.Checked == true || rbAdvanced.Checked)
                {
                    defaultFileName = $"FileComparisonsData_{dateTimeNow}.xlsx";
                }
                else
                {
                    defaultFileName = $"MSPHubInvoiceValidationData_{dateTimeNow}.xlsx";
                }
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel File | *.xlsx",
                    Title = "Save Excel File",
                    FileName = defaultFileName // Set default file name with date and time
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    ExportDataTableToExcel(_resultData.Table, filePath);
                    MessageBox.Show("The Excel file has been successfully exported.", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                AppendLog($"Action: {btnExportLogs.Text} click\n\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnClear_Click(object sender, EventArgs e)
        {
            resetData();
            //modeSetFieldInternal();
            AppendLog($"Action: Reset click\n\n");
        }
        private void btnExportLogs_Click(object sender, EventArgs e)
        {
            try
            {
                // Format current date and time
                string dateTimeNow = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string defaultFileName = $"Logs_{dateTimeNow}.csv";
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV File | *.csv",
                    Title = "Save CSV File",
                    FileName = defaultFileName // Set default file name with date and time
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    ErrorLogger.Export(filePath);
                    MessageBox.Show("The Log file has been successfully exported.", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Text: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnResetLogs_Click(object sender, EventArgs e)
        {
            textLogs.Clear();
            UpdateLogSummary();
        }
        private void btnPriceMismatchingExportToCsv_Click(object sender, EventArgs e)
        {
            try
            {
                // Format current date and time
                string dateTimeNow = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string defaultFileName = $"PriceMismatchData_{dateTimeNow}.xlsx";
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel File | *.xlsx",
                    Title = "Save Excel File",
                    FileName = defaultFileName // Set default file name with date and time
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    // TODO: use PriceMismatchService to export file
                    var svc = new PriceMismatchService();
                    svc.ExportPriceMismatchesToExcel(_pricematchDataView.Table, filePath);
                    MessageBox.Show("The Excel file has been successfully exported.", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to CSV: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void UpdateCompareButtonState()
        {
            if (rbExternal.Checked == true)
            {
                if ((_microsoftDataView?.Count > 0) && (_sixDotOneDataView?.Count > 0))
                {
                    btnCompare.Enabled = true;
                    btnReset.Enabled = true;
                }
            }
            else
            {
                if (_sixDotOneDataView?.Count > 0)
                {
                    btnCompare.Enabled = true;
                    btnReset.Enabled = true;
                }
            }
        }
        private DataView ReorderColumns(DataTable table, string[] columnOrder)
        {
            DataTable newTable = new DataTable();

            // Add columns in the specified order
            foreach (string columnName in columnOrder)
            {
                if (table.Columns.Contains(columnName))
                {
                    newTable.Columns.Add(columnName, table.Columns[columnName].DataType);
                }
            }

            // Add remaining columns from original table
            foreach (DataColumn column in table.Columns)
            {
                if (!newTable.Columns.Contains(column.ColumnName))
                {
                    newTable.Columns.Add(column.ColumnName, column.DataType);
                }
            }

            // Copy data from original table to new table
            foreach (DataRow row in table.Rows)
            {
                DataRow newRow = newTable.Rows.Add();
                foreach (DataColumn column in newTable.Columns)
                {
                    newRow[column.ColumnName] = row[column.ColumnName];
                }
            }


            return newTable.DefaultView;
        }
        private void DisplayFileInfo(Label fileNameLabel, FileInfo fileInfo)
        {
            fileNameLabel.Text = fileInfo.Name;
        }
        private static string FormatSize(long bytes)
        {
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }
        // Legacy validation methods retained for backward compatibility
        private void ValidateMicrosoftInvoice(DataTable dataTable, string columnName)
        {
            SchemaValidator.RequireColumns(dataTable, "Microsoft invoice", new[] { columnName });
        }

        private void ValidateSixDotOneInvoice(DataTable dataTable, string columnName)
        {
            SchemaValidator.RequireColumns(dataTable, "MSP Hub invoice", new[] { columnName });
        }
        private void SplitColumn(DataTable dataTable, string sourceColumnName, string termColumnName, string billingCycleColumnName)
        {
            // Add new columns for "Term" and "BillingCycle"
            dataTable.Columns.Add(termColumnName, typeof(string));
            dataTable.Columns.Add(billingCycleColumnName, typeof(string));

            // Split the values in the source column and populate the new columns
            foreach (DataRow row in dataTable.Rows)
            {
                string termAndBillingCycle = row[sourceColumnName]?.ToString();
                string billingFrequency = row["BillingFrequency"]?.ToString();


                // Process the "TermAndBillingCycle" value and determine "Term" and "BillingCycle" accordingly
                DetermineTermAndBillingCycle(termAndBillingCycle, billingFrequency, out string term, out string billingCycle);

                row[termColumnName] = term;
                row[billingCycleColumnName] = billingCycle;

            }
        }
        private void DetermineTermAndBillingCycle(string termAndBillingCycle, string billingFrequency, out string term, out string billingCycle)
        {
            // Default values
            term = "NA";
            billingCycle = "NA";

            // Use switch expression and pattern matching
            switch (termAndBillingCycle)
            {
                case "One-Year commitment for monthly/yearly billing":
                    term = "Annual";
                    billingCycle = string.IsNullOrEmpty(billingFrequency) ? "Annual" : "Monthly";
                    break;
                case "One-Month commitment for monthly billing":
                    term = "Monthly";
                    billingCycle = "Monthly";
                    break;
                case "Three-3 Years commitment for monthly/3 Years/yearly billing":
                    term = "Triennial";
                    billingCycle = "Monthly";
                    break;
                case "1 Cache Instance Hour":
                    term = "Monthly";
                    billingCycle = "Monthly";
                    break;
                case "One-Year commitment for yearly billing":
                    term = "Annual";
                    billingCycle = "Annual";
                    break;
                case null or "":
                    term = "None";
                    billingCycle = "OneTime";
                    break;
            }
        }

        /// <summary>
        /// Auto fit DataGridView columns based on displayed cells.
        /// </summary>
        /// <param name="grid">Target grid.</param>
        private static void AutoFitColumns(DataGridView grid)
        {
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            grid.AutoResizeColumns();
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        }

        private void FilterResults(object? sender, EventArgs e)
        {
            if (_resultData == null) return;
            var filters = new List<string>();
            if (cmbFieldFilter.SelectedIndex > 0)
                filters.Add($"[Field Name] = '{cmbFieldFilter.SelectedItem.ToString().Replace("'", "''")}'");
            if (!string.IsNullOrWhiteSpace(txtExplanationFilter.Text))
                filters.Add($"[Explanation] LIKE '%{txtExplanationFilter.Text.Replace("'", "''")}%'");
            if (chkHighPriorityOnly != null && chkHighPriorityOnly.Checked)
            {
                var cols = new[] { "Eff. Days Validation", "Partner Discount Validation" };
                var checks = cols.Where(c => _resultData.Table.Columns.Contains(c))
                    .Select(c => $"LEN([{c}]) > 0");
                if (checks.Any())
                    filters.Add("(" + string.Join(" OR ", checks) + ")");
            }
            _resultData.RowFilter = string.Join(" AND ", filters);
            lblMismatchSummary.Text = _lastSummary;
            lblMismatchSummary.Visible = !string.IsNullOrEmpty(_lastSummary);
        }

        private void PopulateFieldFilterOptions()
        {
            cmbFieldFilter.Items.Clear();
            txtExplanationFilter.Text = string.Empty;

            if (_resultData == null || !_resultData.Table.Columns.Contains("Field Name") || _resultData.Count == 0)
            {
                cmbFieldFilter.Items.Add("-- No fields available --");
                cmbFieldFilter.SelectedIndex = 0;
                cmbFieldFilter.Enabled = false;
                txtExplanationFilter.Enabled = false;
                return;
            }

            _resultData.Table.CaseSensitive = false;
            cmbFieldFilter.Items.Add("-- Select a Field --");
            var fields = _resultData.Table.AsEnumerable()
                .Select(r => r["Field Name"].ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => s)
                .ToArray();
            cmbFieldFilter.Items.AddRange(fields);
            cmbFieldFilter.SelectedIndex = 0;
            cmbFieldFilter.Enabled = true;
            txtExplanationFilter.Enabled = true;
        }

        private void DisableFilters()
        {
            cmbFieldFilter.Items.Clear();
            cmbFieldFilter.Items.Add("-- No fields available --");
            cmbFieldFilter.SelectedIndex = 0;
            cmbFieldFilter.Enabled = false;
            txtExplanationFilter.Text = string.Empty;
            txtExplanationFilter.Enabled = false;
        }

        #endregion Button_Clicks

        #region RadioButton_Modes
        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (isSwitchingMode)
                    return;

                RadioButton selectedRadioButton = sender as RadioButton;
                if (selectedRadioButton == null || !selectedRadioButton.Checked)
                    return;

                if (HasDataInTool())
                {
                    DialogResult result = MessageBox.Show(
                        "Switching modes will clear the discrepancy results and imported files. You can export the discrepancy records before proceeding. Do you still want to continue?",
                        "Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.No)
                    {
                        isSwitchingMode = true;
                        // Revert radio button selection
                        if (selectedRadioButton == rbExternal)
                        {
                            rbInternal.Checked = true;
                            modeSetFieldInternal();
                        }
                        else if (selectedRadioButton == rbInternal)
                        {
                            rbExternal.Checked = true;
                            modeSetFieldExternal();
                        }
                        isSwitchingMode = false;
                        return;
                    }
                    else
                    {
                        resetData();
                    }
                }
                modeCheckedState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private bool HasDataInTool()
        {
            bool hasData = ((dgResultdata.DataSource != null && dgResultdata.Rows.Count > 0) || (dgAzurePriceMismatch.DataSource != null && dgAzurePriceMismatch.Rows.Count > 0));
            return hasData;
        }
        private void modeCheckedState()
        {
            if (rbExternal.Checked)
            {
                modeSetFieldExternal();
                resetData();
            }
            else if (rbInternal.Checked)
            {
                modeSetFieldInternal();
                resetData();
            }
            else if (rbAdvanced.Checked)
            {
                modeSetFieldAdvanced();
                resetData();
            }
        }
        private void modeSetFieldInternal()
        {
            logoMicrosoft.Visible = false;
            lblMicrosoftFilenameLabel.Visible = false;
            lblMicrosoftFileName.Visible = false;
            lblMicrosoftRowsCountLabel.Visible = false;
            lblMicrosoftFileRowCount.Visible = false;
            btnImportMicrosoft.Visible = false;
            lblExternal1DiscrepancyMsg.Visible = false;
            lblExternal2DiscrepancyMsg.Visible = false;
            lblInternal1DiscrepancyMsg.Visible = true;
            lblInternal2DiscrepancyMsg.Visible = true;
            btnCompare.Text = "Validate";
            rbInternal.Checked = true;
            btnImportSixDotOneFile.Text = "Upload MSP Hub Invoice";
            lblDiscrepancyTitle.Visible = true;
            // Remove tabPage3 if it exists
            //if (tbcMenu.TabPages.Contains(tabPage3))
            //{
            //    tbcMenu.TabPages.Remove(tabPage3);
            //}
        }
        private void modeSetFieldExternal()
        {
            LogoMsbhub.Visible = true;
            lblMsbhubfilenameLabel.Visible = true;
            lblSixDotOneFileName.Visible = true;
            lblMsbhubrowscountLabel.Visible = true;
            lblSixDotOneFileRowCount.Visible = true;
            btnImportSixDotOneFile.Visible = true;

            logoMicrosoft.Visible = true;
            lblMicrosoftFilenameLabel.Visible = true;
            lblMicrosoftFileName.Visible = true;
            lblMicrosoftRowsCountLabel.Visible = true;
            lblMicrosoftFileRowCount.Visible = true;
            btnImportMicrosoft.Visible = true;

            lblExternal1DiscrepancyMsg.Visible = true;
            lblExternal2DiscrepancyMsg.Visible = true;
            lblInternal1DiscrepancyMsg.Visible = false;
            lblInternal2DiscrepancyMsg.Visible = false;
            lblDiscrepancyTitle.Visible = true;
            btnCompare.Text = "Reconcile";
            btnImportSixDotOneFile.Text = "Upload Billing Invoice";

            // Add tabPage3 before the last tab if it's not already present
            //if (!tbcMenu.TabPages.Contains(tabPage3))
            //{
            //    int insertIndex = tbcMenu.TabPages.Count - 1; // Before the last tab
            //    tbcMenu.TabPages.Insert(insertIndex, tabPage3);
            //}
        }

        private void modeSetFieldAdvanced()
        {
            modeSetFieldExternal();
            rbAdvanced.Checked = true;
        }

        #endregion RadioButton_Modes

        #region Export_ToFile
        private void ExportDataTableToExcel(DataTable dataTable, string filePath)
        {

            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Sheet1");

                int startRow = 1;
                if (!string.IsNullOrEmpty(_lastSummary))
                {
                    worksheet.Cells[startRow, 1].Value = _lastSummary;
                    worksheet.Cells[startRow, 1, startRow, dataTable.Columns.Count].Merge = true;
                    worksheet.Cells[startRow, 1].Style.Font.Bold = true;
                    startRow++;
                }

                // Add header row
                AddHeaderRow(dataTable, worksheet, startRow);

                if (rbExternal.Checked || rbAdvanced.Checked)
                {
                    AddDataRows(dataTable, worksheet, startRow + 1);
                    AddDataRowsWithColor(dataTable, worksheet, startRow + 1);
                }
                else
                {
                    AddDataRowsWithColor(dataTable, worksheet, startRow + 1);
                }

                // Save the Excel file
                FileInfo excelFile = new FileInfo(filePath);
                excelPackage.SaveAs(excelFile);
            }
        }

        private void AddDataRowsWithColor(DataTable dataTable, ExcelWorksheet worksheet, int startRow)
        {
            // --- Style the Header Row ---
            for (int col = 0; col < dataTable.Columns.Count; col++)
            {
                var headerCell = worksheet.Cells[startRow, col + 1];
                headerCell.Value = dataTable.Columns[col].ColumnName;

                // Apply light blue background
                headerCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#D9EAF7")); // Light blue

                // Make text bold
                headerCell.Style.Font.Bold = true;

                // Apply border
                headerCell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                headerCell.Style.Border.Top.Color.SetColor(Color.Black);
                headerCell.Style.Border.Bottom.Color.SetColor(Color.Black);
                headerCell.Style.Border.Left.Color.SetColor(Color.Black);
                headerCell.Style.Border.Right.Color.SetColor(Color.Black);
            }

            // --- Add Data Rows with Color Formatting ---
            for (int row = 0; row < dataTable.Rows.Count; row++)
            {
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    var cell = worksheet.Cells[startRow + 1 + row, col + 1];
                    cell.Value = dataTable.Rows[row][col];

                    string columnName = dataTable.Columns[col].ColumnName;

                    // Apply color formatting based on column name
                    if (columnName == "Eff. Days Validation" || columnName == "Partner Discount Validation") // Custom Red (#BB3F3F)
                    {
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#BB3F3F"));
                        cell.Style.Font.Color.SetColor(Color.White);
                    }
                    else if (columnName == "Discount Hierarchy Check" || columnName == "Pricing Consistency Check") // Custom Yellow (#EEDC5B)
                    {
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEDC5B"));
                        cell.Style.Font.Color.SetColor(Color.Black);
                    }

                    // Apply black border to each data cell
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    cell.Style.Border.Top.Color.SetColor(Color.Black);
                    cell.Style.Border.Bottom.Color.SetColor(Color.Black);
                    cell.Style.Border.Left.Color.SetColor(Color.Black);
                    cell.Style.Border.Right.Color.SetColor(Color.Black);
                }
            }
        }

        private void AddHeaderRow(DataTable dataTable, ExcelWorksheet worksheet, int startRow)
        {
            int columnIndex = 1; // Start from first column

            foreach (DataColumn column in dataTable.Columns)
            {
                // Skip the "MissingData" column
                if (column.ColumnName == "MissingData")
                    continue;

                // Add header
                worksheet.Cells[startRow, columnIndex].Value = column.ColumnName;
                columnIndex++;
            }
        }

        //private void AddDataRows(DataTable dataTable, ExcelWorksheet worksheet)
        //{
        //    foreach (DataRow row in dataTable.Rows)
        //    {
        //        ExcelRange rowRange = worksheet.Cells[row.Table.Rows.IndexOf(row) + 2, 1, row.Table.Rows.IndexOf(row) + 2, dataTable.Columns.Count];
        //        SetRowColorAndValues(rowRange, row, dataTable);
        //    }
        //}
        private void AddDataRows(DataTable dataTable, ExcelWorksheet worksheet, int startRow)
        {
            int rowIndex = startRow; // Start from given row



            foreach (DataRow row in dataTable.Rows)
            {
                for (int colIndex = 0; colIndex < dataTable.Columns.Count; colIndex++)
                {
                    object cellValue = row[colIndex];
                    Type dataType = dataTable.Columns[colIndex].DataType;
                    ExcelRange cell = worksheet.Cells[rowIndex, colIndex + 1];

                    // Preserve the correct data type while writing to Excel
                    if (dataType == typeof(DateTime))
                    {
                        if (cellValue != DBNull.Value && DateTime.TryParse(cellValue.ToString(), out DateTime dateValue))
                        {
                            cell.Value = dateValue; // Excel will recognize it as a DateTime
                            cell.Style.Numberformat.Format = "yyyy-MM-dd"; // Ensures date format
                        }
                    }
                    else if (dataType == typeof(int) || dataType == typeof(long))
                    {
                        if (cellValue != DBNull.Value)
                            cell.Value = Convert.ToInt64(cellValue); // Ensures numeric type
                    }
                    else if (dataType == typeof(decimal) || dataType == typeof(double) || dataType == typeof(float))
                    {
                        if (cellValue != DBNull.Value)
                            cell.Value = Convert.ToDouble(cellValue); // Ensures decimal type
                    }
                    else
                    {
                        cell.Value = cellValue.ToString(); // Default to string for text values
                    }

                    // Check if the column is "Reason" and set the background color to yellow
                    if (dataTable.Columns[colIndex].ColumnName.Equals("Reason", StringComparison.OrdinalIgnoreCase))
                    {
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                    }
                }
                rowIndex++;
            }
        }


        private void SetRowColorAndValues(ExcelRange rowRange, DataRow row, DataTable dataTable)
        {
            int columnIndex = 1;

            // --- Apply Header Styling ---
            for (int col = 0; col < dataTable.Columns.Count; col++)
            {
                var headerCell = rowRange.Worksheet.Cells[1, col + 1];
                headerCell.Value = dataTable.Columns[col].ColumnName;

                // Light blue background
                headerCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#D9EAF7"));

                // Bold font
                headerCell.Style.Font.Bold = true;

                // Apply border
                headerCell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                headerCell.Style.Border.Top.Color.SetColor(Color.Black);
                headerCell.Style.Border.Bottom.Color.SetColor(Color.Black);
                headerCell.Style.Border.Left.Color.SetColor(Color.Black);
                headerCell.Style.Border.Right.Color.SetColor(Color.Black);
            }

            // --- Get MissingData flag for the row ---
            bool isMissing = row.Table.Columns.Contains("MissingData") &&
                             bool.TryParse(row["MissingData"].ToString(), out bool missingFlag) && missingFlag;

            // --- Check if the Reason column contains specific values ---
            string reasonValue = row["Reason"]?.ToString();
            bool isOrangeRow = false;
            bool isYellowRow = false;

            // Check if Reason column contains the values for Orange or Yellow row coloring
            if (!string.IsNullOrEmpty(reasonValue))
            {
                if (reasonValue.Contains("This Line Item is missing in Microsoft Invoice") ||
                        reasonValue.Contains("This Line Item is missing in MSP Hub Invoice"))
                {
                    isYellowRow = true; // Set to Yellow
                }
            }

            // --- Iterate through each column to set value and apply coloring ---
            foreach (DataColumn column in dataTable.Columns)
            {
                // Skip "MissingData" column
                if (column.ColumnName == "MissingData")
                    continue;

                string cellValue = row[column].ToString();
                string cleanedValue = cellValue.Replace(MismatchValueIdentifier.MicrosoftMarker, "")
                                               .Replace(MismatchValueIdentifier.SixDotOneMarker, "");

                // Set cell value
                var cell = rowRange[rowRange.Start.Row, columnIndex];
                cell.Value = cleanedValue;

                // --- Apply cell background colors based on specific column and Reason values ---
                if (isOrangeRow)
                {
                    // Set the row to Orange if Reason matches the Orange condition
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.Orange);
                    cell.Style.Font.Color.SetColor(Color.White); // White text for contrast
                }
                else if (isYellowRow)
                {
                    // Set the row to Yellow if Reason matches the Yellow condition
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    cell.Style.Font.Color.SetColor(Color.Black); // Black text for contrast
                }
                else if (cellValue.Contains(MismatchValueIdentifier.MicrosoftMarker) ||
                         cellValue.Contains(MismatchValueIdentifier.SixDotOneMarker))
                {
                    // 🟡 Yellow highlight for mismatches
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEDC5B"));
                }
                else if (column.ColumnName == "Not in this file")
                {
                    if (isMissing)
                    {
                        // 🟡 Yellow for missing data in "Not in this file" column
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#EEDC5B"));
                    }
                    else
                    {
                        // 🔴 Red for non-missing data in "Not in this file" column
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#FF6347"));
                        cell.Style.Font.Color.SetColor(Color.White); // White text for contrast
                    }
                }
                else
                {
                    // Default white background
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.White);
                }

                // --- Apply black border to the cell ---
                cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                cell.Style.Border.Top.Color.SetColor(Color.Black);
                cell.Style.Border.Bottom.Color.SetColor(Color.Black);
                cell.Style.Border.Left.Color.SetColor(Color.Black);
                cell.Style.Border.Right.Color.SetColor(Color.Black);

                columnIndex++;
            }
        }
        public string GetLocalIPAddress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        #endregion Export_ToFile

        protected virtual AdvancedReconciliationService CreateAdvancedService()
        {
            return new AdvancedReconciliationService();
        }

        #region Compare_Logic
        // TODO: External reconciliation logic moved to ReconciliationService
        #endregion Compare_Logic

        // TODO: Validation logic moved to InvoiceValidationService


        #region Helpers

        private void ClearFileInfo(Label fileNameLabel, Label countRowsLabel)
        {
            fileNameLabel.Text = string.Empty;
            countRowsLabel.Text = string.Empty;
        }
        private void AppendLog(string message)
        {
            void Append()
            {
                textLogs.AppendText(message);
                textLogs.SelectionStart = textLogs.TextLength;
                textLogs.ScrollToCaret();
            }

            if (textLogs.InvokeRequired)
            {
                textLogs.Invoke(new Action(Append));
            }
            else
            {
                Append();
            }
        }

        private void PopulateLogsGrid()
        {
            var table = new DataTable();
            table.Columns.Add("Timestamp");
            table.Columns.Add("Level");
            table.Columns.Add("Row");
            table.Columns.Add("Column");
            table.Columns.Add("Description");
            table.Columns.Add("Raw");
            table.Columns.Add("File");
            table.Columns.Add("Context");

            foreach (var e in ErrorLogger.AllEntries)
            {
                var r = table.NewRow();
                r[0] = e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                r[1] = e.ErrorLevel;
                r[2] = e.RowNumber > 0 ? e.RowNumber.ToString() : string.Empty;
                r[3] = e.ColumnName;
                r[4] = FormatNumeric(e.Description, e.ColumnName);
                r[5] = FormatNumeric(e.RawValue, e.ColumnName);
                r[6] = e.FileName;
                r[7] = e.Context;
                table.Rows.Add(r);
            }

            dgvLogs.DataSource = table;
            UpdateLogSummary();
        }

        private void FlashLogsTab()
        {
            tabPage2.ForeColor = Color.Red;
            _flashTimer.Stop();
            _flashTimer.Start();
        }

        private void UpdateLogSummary()
        {
            int errorCount = dgvLogs.Rows.Cast<DataGridViewRow>()
                .Count(r => r.Cells["Level"].Value?.ToString() == "Error");
            int warningCount = dgvLogs.Rows.Cast<DataGridViewRow>()
                .Count(r => r.Cells["Level"].Value?.ToString() == "Warning");
            lblLogsSummary.Text = $"⚠ {warningCount} Warnings,  {errorCount} Errors";
            if (errorCount > 0)
                lblLogsSummary.ForeColor = Color.Red;
            else if (warningCount > 0)
                lblLogsSummary.ForeColor = Color.Orange;
            else
                lblLogsSummary.ForeColor = Color.Green;
        }

        private void dgvLogs_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow row in dgvLogs.Rows)
            {
                if (row.Cells["Level"].Value is string level)
                {
                    if (level == "Error")
                        row.DefaultCellStyle.BackColor = Color.Red;
                    else if (level == "Warning")
                        row.DefaultCellStyle.BackColor = Color.Orange;
                    else if (level == "Info")
                        row.DefaultCellStyle.BackColor = Color.LightGray;
                }
            }
            UpdateLogSummary();
        }
        // Helper method to safely convert to decimal
        private decimal SafeConvertToDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;
            return Convert.ToDecimal(value);
        }

        private static string FormatNumeric(string value, string columnHint)
        {
            if (decimal.TryParse(value.TrimEnd('%'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                bool percent = columnHint.Contains("Percent", StringComparison.OrdinalIgnoreCase) || value.Trim().EndsWith("%");
                string formatted = percent ? NumericFormatter.Percent(d) : NumericFormatter.Money(d);
                return percent && value.Trim().EndsWith("%") ? formatted + "%" : formatted;
            }
            return value;
        }


        private void DgMismatchData_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Get the column name
            string columnName = dgResultdata.Columns[e.ColumnIndex].Name;

            // Apply color for Eff. Days Validation and Partner Discount Validation (High Priority - Red)
            if (columnName == "Eff. Days Validation" || columnName == "Partner Discount Validation")
            {
                if (e.Value != null && !string.IsNullOrEmpty(e.Value.ToString()))
                {
                    // Set background to #BB3F3F and text to white
                    e.CellStyle.BackColor = ColorTranslator.FromHtml("#BB3F3F");
                    e.CellStyle.ForeColor = Color.White; // White text for contrast
                    e.CellStyle.Font = new Font("Arial", 10, FontStyle.Regular);
                }
            }
            // Apply color for Discount Hierarchy Check and Pricing Consistency Check (Medium Priority - Yellow)
            else if (columnName == "Discount Hierarchy Check" || columnName == "Pricing Consistency Check")
            {
                if (e.Value != null && !string.IsNullOrEmpty(e.Value.ToString()))
                {
                    // Set background to #EEDC5B and text to black
                    e.CellStyle.BackColor = ColorTranslator.FromHtml("#EEDC5B");
                    e.CellStyle.ForeColor = Color.Black; // Black text for readability
                    e.CellStyle.Font = new Font("Arial", 10, FontStyle.Regular);
                }
            }

            // Apply color for specific "Reason" column values
            if (columnName == "Reason")
            {
                if (e.Value != null && (
                   e.Value.ToString().Contains("This Line Item is missing in Microsoft Invoice") ||
                   e.Value.ToString().Contains("This Line Item is missing in MSP Hub Invoice")
               ))
                {
                    // Set the entire row to Yellow
                    e.CellStyle.BackColor = Color.Yellow;
                    e.CellStyle.ForeColor = Color.Black; // Black text for contrast
                }
            }
            if (columnName == "PartnerTotalCheck")
            {
                if (e.Value != null && e.Value.ToString().Contains("Partner Total must be greater or equal to Microsoft's line item Total."))
                {
                    // Set the entire row to Orange
                    e.CellStyle.BackColor = Color.Orange;
                    e.CellStyle.ForeColor = Color.White; // White text for contrast
                }
            }
        }
        private void resetData()
        {
            ClearFileInfo(lblMicrosoftFileName, lblMicrosoftFileRowCount);
            ClearFileInfo(lblSixDotOneFileName, lblSixDotOneFileRowCount);
            lblSixdotonefilenameTab4.Text = string.Empty;
            lblMicrosoftfilenameTab4.Text = string.Empty;
            dgResultdata.DataSource = null;
            dgAzurePriceMismatch.DataSource = null;
            dgResultdata.Visible = false;
            lblMismatchSummary.Text = string.Empty;
            lblMismatchSummary.Visible = false;
            _lastSummary = string.Empty;

            _microsoftDataView = null;
            _sixDotOneDataView = null;
            _pricematchDataView = null;
            _resultData = null;

            btnCompare.Enabled = false;
            btnExportToCsv.Enabled = false;
            btnPriceMismatchingExportToCsv.Enabled = false;
            lblEmptyMessage.Visible = false;
            lblPricematchingEmptyMessage.Visible = false;
            if (rbExternal.Checked == true)
                btnCompare.Text = "Reconcile";
            else
                btnCompare.Text = "Validate";

            DisableFilters();
        }

        #endregion Helpers

        #region Unused_Old
        private async void btnCompare_ClickOld(object sender, EventArgs e)
        {
            try
            {
                dgResultdata.DataSource = null;
                dgAzurePriceMismatch.DataSource = null;
                lblEmptyMessage.Text = "Please wait, processing...";
                lblEmptyMessage.Visible = true;
                if (rbExternal.Checked == true)
                {
                    _resultData = await Task.Run(() =>
                    {
                        var mismatchFromMicrosoft = FindMismatchedRowsFromMicrosoft(
                            _microsoftDataView.Table,
                            _sixDotOneDataView.Table,
                            _uniqueKeyColumns,
                            MismatchValueIdentifier.MicrosoftMarker
                        );

                        var mismatchFromSixDotOne = FindMismatchedRowsFromSixDotOne(
                           _sixDotOneDataView.Table,
                           _microsoftDataView.Table,
                           _uniqueKeyColumns,
                           MismatchValueIdentifier.SixDotOneMarker
                        );

                        return CombineMismatchedResults(mismatchFromMicrosoft, mismatchFromSixDotOne);
                    });

                    Invoke(new Action(() =>
                    {
                        var mismatchData = _resultData.Table.Clone();

                        foreach (DataRow row in _resultData.Table.Rows)
                        {
                            DataRow newRow = mismatchData.NewRow();
                            newRow.ItemArray = row.ItemArray; // Copy row data
                            mismatchData.Rows.Add(newRow); // Add row to new DataTable
                        }
                        dgResultdata.DataSource = mismatchData.DefaultView;
                        dgResultdata.ClearSelection();
                        btnExportToCsv.Enabled = true;
                        if (dgResultdata.Rows.Count == 0)
                        {
                            lblEmptyMessage.Text = "No Discrepancy Records Found.";
                        }
                        else
                        {
                            lblEmptyMessage.Visible = false;
                        }
                        dgResultdata.Visible = true;
                    }));
                    DataTable matchedDataTable = await Task.Run(() =>
                    {
                        // TODO: use PriceMismatchService for price comparison
                        var svc = new PriceMismatchService();
                        return svc.GetPriceMismatches(
                            _sixDotOneDataView.Table,
                            _microsoftDataView.Table
                        );
                    });
                    Invoke(new Action(() =>
                    {
                        _pricematchDataView = new DataView(matchedDataTable);
                        dgAzurePriceMismatch.ClearSelection();
                        dgAzurePriceMismatch.DataSource = _pricematchDataView;
                        btnPriceMismatchingExportToCsv.Enabled = true;
                        if (dgAzurePriceMismatch.Rows.Count == 0)
                        {
                            lblPricematchingEmptyMessage.Text = "No Records Found.";
                        }
                        else
                        {
                            lblPricematchingEmptyMessage.Visible = false;
                            lblInfoPricematching.Visible = true;
                        }
                        dgAzurePriceMismatch.Visible = true;
                    }));
                }
                else
                {
                    var invalResult = await Task.Run(() =>
                    {
                        var startTime = DateTime.Now; // Capture start time

                        // Validate records
                        return new InvoiceValidationService().ValidateInvoice(_sixDotOneDataView.Table);
                    });
                    _resultData = invalResult.InvalidRowsView;

                    Invoke(new Action(() =>
                    {
                        dgResultdata.DataSource = invalResult.InvalidRowsView;
                        dgResultdata.ClearSelection();
                        btnExportToCsv.Enabled = true;

                        if (dgResultdata.Rows.Count == 0)
                        {
                            lblEmptyMessage.Text = "No Discrepancy Records Found.";
                        }
                        else
                        {
                            lblEmptyMessage.Visible = false;
                        }
                        dgResultdata.Visible = true;

                        // Attach the event handler for cell formatting
                        dgResultdata.CellFormatting += DgMismatchData_CellFormatting;
                    }));
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataTable FindMismatchedRowsFromMicrosoft(DataTable firstImportDataTable, DataTable secondImportDataTable, string[] uniqueKeyColumns1, string addName)
        {
            if (uniqueKeyColumns1 == null || uniqueKeyColumns1.Length == 0)
            {
                throw new ArgumentException("Unique key columns must be specified.");
            }

            // Create a new DataTable to store the mismatched rows
            DataTable mismatchedRowsTable = firstImportDataTable.Clone();

            int firstTableMissingCount = 0;
            int secondTableMissingCount = 0;

            try
            {
                // Iterate through each row in firstImportDataTable
                foreach (DataRow firstRow in firstImportDataTable.Rows)
                {
                    bool matchFound = false;

                    // Iterate through each row in secondImportDataTable to find a match
                    foreach (DataRow secondRow in secondImportDataTable.Rows)
                    {
                        bool match = true;

                        // Check if all unique key columns match
                        foreach (string col in uniqueKeyColumns1)
                        {
                            if (!firstRow[col].Equals(secondRow[col]))
                            {
                                match = false;
                                break;
                            }
                        }

                        if (match)
                        {
                            matchFound = true;
                            break; // Exit the loop since we found a match
                        }
                    }

                    // If no match is found, add the row to the mismatchedRowsTable
                    if (!matchFound)
                    {
                        DataRow newRow = mismatchedRowsTable.NewRow();
                        newRow.ItemArray = firstRow.ItemArray;
                        mismatchedRowsTable.Rows.Add(newRow);
                        firstTableMissingCount++;
                    }
                }

                // Check for rows in secondImportDataTable not in firstImportDataTable
                foreach (DataRow secondRow in secondImportDataTable.Rows)
                {
                    bool matchFound = false;

                    foreach (DataRow firstRow in firstImportDataTable.Rows)
                    {
                        bool match = true;

                        foreach (string col in uniqueKeyColumns1)
                        {
                            if (!secondRow[col].Equals(firstRow[col]))
                            {
                                match = false;
                                break;
                            }
                        }

                        if (match)
                        {
                            matchFound = true;
                            break;
                        }
                    }

                    if (!matchFound)
                    {
                        secondTableMissingCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"\nError during comparison: {ex.Message}");
                throw;
            }
            return mismatchedRowsTable;
        }
        private DataTable FindMismatchedRowsFromSixDotOne(DataTable firstImportDataTable, DataTable secondImportDataTable, string[] uniqueKeyColumns1, string addName)
        {
            // Start time and stopwatch
            DateTime startTime = DateTime.Now;
            Stopwatch stopwatch = Stopwatch.StartNew();

            string localIP = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString();
            AppendLog($"******************** Comparison of Microsoft and MSP Hub Invoice Run ********************");
            AppendLog($"\nComparison of Microsoft and MSP Hub Invoice started at: {startTime}");
            AppendLog($"\nLocal IP: {localIP}");

            // Validate inputs
            if (uniqueKeyColumns1 == null || uniqueKeyColumns1.Length == 0)
            {
                throw new ArgumentException("Unique key columns must be specified.");
            }

            // Create a new DataTable to store the mismatched rows
            DataTable mismatchedRowsTable = firstImportDataTable.Clone();

            int firstTableMissingCount = 0;
            int secondTableMissingCount = 0;

            try
            {
                // Iterate through each row in firstImportDataTable
                foreach (DataRow firstRow in firstImportDataTable.Rows)
                {
                    bool matchFound = false;

                    // Iterate through each row in secondImportDataTable to find a match
                    foreach (DataRow secondRow in secondImportDataTable.Rows)
                    {
                        bool match = true;

                        // Check if all unique key columns match
                        foreach (string col in uniqueKeyColumns1)
                        {
                            if (!firstRow[col].Equals(secondRow[col]))
                            {
                                match = false;
                                break;
                            }
                        }

                        if (match)
                        {
                            matchFound = true;
                            break; // Exit the loop since we found a match
                        }
                    }

                    // If no match is found, add the row to the mismatchedRowsTable
                    if (!matchFound)
                    {
                        DataRow newRow = mismatchedRowsTable.NewRow();
                        newRow.ItemArray = firstRow.ItemArray;
                        mismatchedRowsTable.Rows.Add(newRow);
                        firstTableMissingCount++;
                    }
                }

                // Check for rows in secondImportDataTable not in firstImportDataTable
                foreach (DataRow secondRow in secondImportDataTable.Rows)
                {
                    bool matchFound = false;

                    foreach (DataRow firstRow in firstImportDataTable.Rows)
                    {
                        bool match = true;

                        foreach (string col in uniqueKeyColumns1)
                        {
                            if (!secondRow[col].Equals(firstRow[col]))
                            {
                                match = false;
                                break;
                            }
                        }

                        if (match)
                        {
                            matchFound = true;
                            break;
                        }
                    }

                    if (!matchFound)
                    {
                        secondTableMissingCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"\nError during comparison: {ex.Message}");
                throw;
            }

            // Log the counts
            AppendLog($"\nTotal rows missing in first file: {firstTableMissingCount}");
            AppendLog($"\nTotal rows missing in second file: {secondTableMissingCount}");

            // Stop stopwatch and log end time
            stopwatch.Stop();
            DateTime endTime = DateTime.Now;
            AppendLog($"\nComparison of Microsoft and MSP Hub Invoice ended at: {endTime}");
            // Format elapsed time
            TimeSpan elapsed = stopwatch.Elapsed;
            string formattedElapsed = elapsed.TotalSeconds < 1
                ? $"{elapsed.TotalMilliseconds:F2} ms"
                : elapsed.ToString(@"hh\:mm\:ss");
            AppendLog($"\nTotal time taken: {formattedElapsed}");
            AppendLog($"\nReconcile is clicked\n\n");
            return mismatchedRowsTable;
        }

        private DataView CombineMismatchedResults(DataTable resultFromMicrosoftTable, DataTable resultFromSixDotOneTable)
        {
            string columnName = "Not in this file";
            string missingDataFlag = "MissingData";

            var combinedResult = new DataTable();

            // Add columns from resultFromSixDotOneTable
            foreach (DataColumn column in resultFromSixDotOneTable.Columns)
            {
                combinedResult.Columns.Add(column.ColumnName, column.DataType);
            }

            // Add columns from resultFromMicrosoftTable that are not already in combinedResult
            foreach (DataColumn column in resultFromMicrosoftTable.Columns)
            {
                if (!combinedResult.Columns.Contains(column.ColumnName))
                {
                    combinedResult.Columns.Add(column.ColumnName, column.DataType);
                }
            }

            combinedResult.Columns.Add(columnName, typeof(string));
            combinedResult.Columns.Add(missingDataFlag, typeof(bool)); // New flag for missing data

            // Add data from resultFromMicrosoftTable
            foreach (DataRow row in resultFromMicrosoftTable.Rows)
            {
                DataRow newRow = combinedResult.NewRow();
                foreach (DataColumn column in resultFromMicrosoftTable.Columns)
                {
                    newRow[column.ColumnName] = row[column.ColumnName];
                }
                newRow[columnName] = lblSixDotOneFileName.Text;
                newRow[missingDataFlag] = true; // Highlight missing in SixDotOne
                combinedResult.Rows.Add(newRow);
            }

            // Add data from resultFromSixDotOneTable without marking as missing
            foreach (DataRow row in resultFromSixDotOneTable.Rows)
            {
                DataRow newRow = combinedResult.NewRow();
                foreach (DataColumn column in resultFromSixDotOneTable.Columns)
                {
                    newRow[column.ColumnName] = row[column.ColumnName];
                }
                newRow[columnName] = lblMicrosoftFileName.Text;
                newRow[missingDataFlag] = false; // No highlight
                combinedResult.Rows.Add(newRow);
            }

            return combinedResult.DefaultView;
        }

        private void DataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (!rbExternal.Checked)
                return;

            var row = dgResultdata.Rows[e.RowIndex];
            if (!row.DataGridView.Columns.Contains("Reason"))
                return;

            // Apply yellow background only to the "Reason" column
            DataGridViewCell reasonCell = row.Cells["Reason"];
            if (reasonCell != null && reasonCell.Value != null)
            {
                string reasonValue = reasonCell.Value.ToString();
                if (!string.IsNullOrEmpty(reasonValue))
                {
                    if (reasonValue.Contains("This Line Item is missing in Microsoft Invoice") ||
                        reasonValue.Contains("This Line Item is missing in MSP Hub Invoice"))
                    {
                        // Set only the Reason column to Yellow
                        reasonCell.Style.BackColor = Color.Yellow;
                        reasonCell.Style.ForeColor = Color.Black;
                    }
                }
            }

            // Existing mismatch highlighting logic
            foreach (DataGridViewCell cell in row.Cells)
            {
                if (cell.Value != null)
                {
                    string cellValue = cell.Value.ToString();
                    if (cellValue.Contains(MismatchValueIdentifier.MicrosoftMarker) ||
                        cellValue.Contains(MismatchValueIdentifier.SixDotOneMarker))
                    {
                        row.DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#EEDC5B"); // Yellow for mismatch
                        return;
                    }
                }
            }
        }


        #endregion Unused_Old




        private string SafeGetString(DataRow row, string columnName)
        {
            if (row == null || string.IsNullOrWhiteSpace(columnName) || !row.Table.Columns.Contains(columnName))
                return string.Empty;
            var val = row[columnName];
            return val == null || val == DBNull.Value ? string.Empty : val.ToString();
        } // <--- This closes the last method in Form1

    }// <--- THIS IS THE ONLY closing brace for Form1
}


            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            pkg.SaveAs(new FileInfo(file));
        }
    }
}
