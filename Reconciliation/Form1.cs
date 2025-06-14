﻿using OfficeOpenXml;
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

namespace Reconciliation
{
    public partial class Form1 : Form
    {
        private static readonly string[] suffixes = { "Bytes", "KB", "MB", "GB" };
        private DataView _microsoftDataView, _sixDotOneDataView, _resultData, _pricematchDataView;
        private string[] _uniqueKeyColumns = ["CustomerDomainName", "ProductId", "SkuId", "ChargeType", "Term", "BillingCycle"];
        private string[] _columnsToBeDeleted = { "CustomerCountry", "MpnId", "AvailabilityId", "Currency", "PriceAdjustmentDescription", "PublisherName", "PublisherId", "ProductQualifiers", "ReferenceId", "MeterDescription", "PCToBCExchangeRateDate", "PCToBCExchangeRate", "PricingCurrency", "BillableQuantity", "AlternateId", "UnitType" };
        private readonly string[] _requiredMicrosoftColumns = ["CustomerDomainName", "ProductId", "SkuId", "ChargeType", "TermAndBillingCycle"];
        private readonly string[] _requiredMspHubColumns = ["InternalReferenceId", "SkuId", "BillingCycle"];
        private int hoveredIndex = -1;
        private bool isSwitchingMode = false;
        private bool AllowFuzzyColumns => chkFuzzyColumns.Checked;
        private readonly ToolTip _toolTip = new();

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
            _toolTip.SetToolTip(btnExportLogs, "Export parsing and processing logs");
            _toolTip.SetToolTip(chkFuzzyColumns, "Automatically map similar column headers, e.g. 'SkuName' -> 'SkuId'");
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
            if (tbcMenu.TabPages.Contains(tabPage3))
            {
                tbcMenu.TabPages.Remove(tabPage3);
            }
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
                    var service = new FileImportService(AllowFuzzyColumns);
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
                    var service = new FileImportService(AllowFuzzyColumns);
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
                tbcMenu.SelectedIndex = 2;
                lblEmptyMessage.Text = "Please wait, processing...";
                lblEmptyMessage.Visible = true;
                lblPricematchingEmptyMessage.Text = "Please wait, processing...";
                lblPricematchingEmptyMessage.Visible = true;
                // Run the CPU-bound operations asynchronously
                if (rbExternal.Checked == true)
                {
                    _resultData = await Task.Run(() =>
                    {
                        return new ReconciliationService().CompareInvoices(_sixDotOneDataView.Table, _microsoftDataView.Table).DefaultView;

                    Invoke(new Action(() =>
                    {
                        var mismatchData = _resultData.Table.Clone();

                        foreach (DataRow row in _resultData.Table.Rows)
                        {
                            DataRow newRow = mismatchData.NewRow();
                            newRow.ItemArray = row.ItemArray; // Copy row data
                            mismatchData.Rows.Add(newRow); // Add row to new DataTable
                        }

                        // **Prevent Sorting**
                        BindingSource bindingSource = new BindingSource { DataSource = mismatchData };
                        dgResultdata.DataSource = bindingSource;

                        foreach (DataGridViewColumn column in dgResultdata.Columns)
                        {
                            column.SortMode = DataGridViewColumnSortMode.NotSortable; // Disable column sorting
                        }

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
                }
                else
                {
                    _resultData = await Task.Run(() =>
                    {
                        var startTime = DateTime.Now; // Capture start time

                        // Validate records
                        var invalidRecordsTable = new InvoiceValidationService().ValidateInvoice(_sixDotOneDataView.Table);
                        return invalidRecordsTable.DefaultView; // Convert DataTable to DataView
                    });

                    Invoke(new Action(() =>
                    {
                        var invalidData = _resultData.Table.Clone();

                        foreach (DataRow row in _resultData.Table.Rows)
                        {
                            DataRow newRow = invalidData.NewRow();
                            newRow.ItemArray = row.ItemArray; // Copy row data
                            invalidData.Rows.Add(newRow); // Add row to new DataTable
                        }

                        dgResultdata.DataSource = invalidData.DefaultView;
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
                if (rbExternal.Checked == true)
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
                string defaultFileName = $"Logs_{dateTimeNow}.txt";
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text File | *.txt",
                    Title = "Save Text File",
                    FileName = defaultFileName // Set default file name with date and time
                };

                if (string.IsNullOrEmpty(textLogs.Text)) return;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    ExportLogToText(filePath);
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
            SchemaValidator.RequireColumns(dataTable, "Microsoft invoice", new[] { columnName }, AllowFuzzyColumns);
        }

        private void ValidateSixDotOneInvoice(DataTable dataTable, string columnName)
        {
            SchemaValidator.RequireColumns(dataTable, "MSP Hub invoice", new[] { columnName }, AllowFuzzyColumns);
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

        #endregion RadioButton_Modes

        #region Export_ToFile
        private void ExportDataTableToExcel(DataTable dataTable, string filePath)
        {

            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Sheet1");

                // Add header row
                AddHeaderRow(dataTable, worksheet);

                if (rbExternal.Checked == true)
                {
                    AddDataRows(dataTable, worksheet);
                    AddDataRowsWithColor(dataTable, worksheet);
                }
                else
                {
                    AddDataRowsWithColor(dataTable, worksheet);
                }

                // Save the Excel file
                FileInfo excelFile = new FileInfo(filePath);
                excelPackage.SaveAs(excelFile);
            }
        }

        private void AddDataRowsWithColor(DataTable dataTable, ExcelWorksheet worksheet)
        {
            // --- Style the Header Row ---
            for (int col = 0; col < dataTable.Columns.Count; col++)
            {
                var headerCell = worksheet.Cells[1, col + 1];
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
                    var cell = worksheet.Cells[row + 2, col + 1]; // Data starts from row 2
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

        private void AddHeaderRow(DataTable dataTable, ExcelWorksheet worksheet)
        {
            int columnIndex = 1; // Start from first column

            foreach (DataColumn column in dataTable.Columns)
            {
                // Skip the "MissingData" column
                if (column.ColumnName == "MissingData")
                    continue;

                // Add header
                worksheet.Cells[1, columnIndex].Value = column.ColumnName;
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
        private void AddDataRows(DataTable dataTable, ExcelWorksheet worksheet)
        {
            int rowIndex = 2; // Start from the second row (assuming the first row is the header)



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
        private void ExportLogToText(string filePath)
        {
            try
            {
                File.WriteAllText(filePath, textLogs.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing to file: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion Export_ToFile

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
            if (textLogs.InvokeRequired)
            {
                textLogs.Invoke(new Action(() => textLogs.AppendText(message)));
            }
            else
            {
                textLogs.AppendText(message);
            }
        }
        // Helper method to safely convert to decimal
        private decimal SafeConvertToDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;
            return Convert.ToDecimal(value);
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
                    _resultData = await Task.Run(() =>
                    {
                        var startTime = DateTime.Now; // Capture start time

                        // Validate records
                        var invalidRecordsTable = new InvoiceValidationService().ValidateInvoice(_sixDotOneDataView.Table);
                        return invalidRecordsTable.DefaultView; // Convert DataTable to DataView
                    });

                    Invoke(new Action(() =>
                    {
                        var invalidData = _resultData.Table.Clone();

                        foreach (DataRow row in _resultData.Table.Rows)
                        {
                            DataRow newRow = invalidData.NewRow();
                            newRow.ItemArray = row.ItemArray; // Copy row data
                            invalidData.Rows.Add(newRow); // Add row to new DataTable
                        }

                        dgResultdata.DataSource = invalidData.DefaultView;
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
            if (rbExternal.Checked == true)
            {
                var row = dgResultdata.Rows[e.RowIndex];

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
        }


        #endregion Unused_Old




        private string SafeGetString(DataRow row, string columnName)
        {
            if (row == null || string.IsNullOrWhiteSpace(columnName) || !row.Table.Columns.Contains(columnName))
                return string.Empty;
            var val = row[columnName];
            return val == null || val == DBNull.Value ? string.Empty : val.ToString();
        } // <--- This closes the last method in Form1

    } // <--- THIS IS THE ONLY closing brace for Form1

