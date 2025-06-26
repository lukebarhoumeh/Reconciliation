//  Form1.Designer.cs – revised 2025‑06‑26
//  (all event‑handler names match the runtime file above)
// -----------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Reconciliation
{
    partial class Form1
    {
        // Designer‑managed fields
        private IContainer components = null!;
        private Panel titleBar;
        private Label lblTitle;
        private Label lblVersion;      //  ← NEW (fixes missing symbol)
        private Button btnClose;
        private Button btnMinimise;
        private ToolTip toolTip1;

        private TableLayoutPanel layoutValidation;
        private TableLayoutPanel layoutControls;
        private FlowLayoutPanel flowModes;
        private FlowLayoutPanel flowRunButtons;
        private FlowLayoutPanel flowImports;
        private FlowLayoutPanel flowFilters;
        private TabControl tabs;
        private TabPage tabValidation;
        private TabPage tabPrices;
        private TabPage tabLogs;

        // ── Validation tab
        private RadioButton rbCompare;
        private RadioButton rbValidate;
        private RadioButton rbAdvanced;
        private Button btnImportHub;
        private Button btnImportMicrosoft;
        private Button btnRun;
        private Button btnReset;
        private Button btnExport;
        private Label lblHubInfo;
        private Label lblMsInfo;
        private DataGridView dgvResults;
        private Label lblSummary;
        private ComboBox cmbFieldFilter;
        private TextBox txtExplanationFilter;
        private CheckBox chkHPOnly;
        private Label lblEmpty;

        private TableLayoutPanel layoutPrices;
        private FlowLayoutPanel flowPriceTop;

        // ── Price‑mismatch tab
        private DataGridView dgvPrices;
        private Button btnExportPrices;
        private Label lblPriceInfo;

        // ── Logs tab
        private TableLayoutPanel layoutLogs;
        private FlowLayoutPanel flowLogTop;
        private DataGridView dgvLogs;
        private Button btnExportLogs;
        private Button btnResetLogs;
        private Label lblLogsSummary;
        private RichTextBox txtRawLog;

        // ---------------------------------------------------------------------
        private void InitializeComponent()
        {
            components = new Container();
            toolTip1 = new ToolTip(components);

            // ===== TITLE BAR ==================================================
            titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.FromArgb(0, 122, 204)
            };
            lblTitle = new Label
            {
                Text = "Reconciliation Tool",
                ForeColor = Color.White,
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(10, 8)
            };
            lblVersion = new Label
            {               // ← fixes lblVersion error
                ForeColor = Color.White,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Regular)
            };
            // stick it to the right
            lblVersion.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblVersion.Location = new Point(1000, 10);

            btnClose = new Button
            {
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe MDL2 Assets", 9),
                Text = "\uE8BB",
                Dock = DockStyle.Right,
                Width = 40
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += btnClose_Click;

            btnMinimise = new Button
            {
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe MDL2 Assets", 9),
                Text = "\uE921",
                Dock = DockStyle.Right,
                Width = 30
            };
            btnMinimise.FlatAppearance.BorderSize = 0;
            btnMinimise.Click += btnMinimise_Click;

            titleBar.Controls.AddRange(new Control[]
                { btnMinimise, btnClose, lblVersion, lblTitle });

            // ===== LAYOUT CONTAINERS ========================================
            layoutValidation = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layoutValidation.RowStyles.Add(new RowStyle(SizeType.Percent, 26F));
            layoutValidation.RowStyles.Add(new RowStyle(SizeType.Percent, 74F));

            layoutControls = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(12)
            };
            layoutControls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            layoutControls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            for (int i = 0; i < 4; i++)
                layoutControls.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            flowModes = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            flowRunButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            flowImports = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            flowFilters = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };

            // ===== TABS =======================================================
            tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9)
            };
            tabValidation = new TabPage("Invoice Validation");
            tabPrices = new TabPage("Price Mismatch");
            tabLogs = new TabPage("Logs");
            tabs.TabPages.AddRange(new[] { tabValidation, tabPrices, tabLogs });

            // === Validation controls (Panel1) =================================
            rbCompare = new RadioButton
            {
                Text = "Compare Microsoft vs MSP Hub",
                Location = new Point(10, 10)
            };
            rbValidate = new RadioButton
            {
                Text = "Validate MSP‑Hub",
                Location = new Point(10, 40)
            };
            rbAdvanced = new RadioButton
            {
                Text = "Advanced compare",
                Location = new Point(10, 70)
            };

            btnImportHub = new Button
            {
                Text = "Import MSP Hub invoice",
                Location = new Point(230, 10)
            };
            toolTip1.SetToolTip(btnImportHub, "Load MSP Hub invoice (CSV)");
            btnImportMicrosoft = new Button
            {
                Text = "Import Microsoft invoice",
                Location = new Point(230, 50)
            };
            toolTip1.SetToolTip(btnImportMicrosoft, "Load Microsoft invoice (CSV)");

            btnRun = new Button
            {
                Text = "Run",
                Enabled = false,
                Location = new Point(500, 10)
            };
            toolTip1.SetToolTip(btnRun, "Start reconciliation");
            btnReset = new Button
            {
                Text = "Reset",
                Enabled = false,
                Location = new Point(500, 50)
            };
            toolTip1.SetToolTip(btnReset, "Clear loaded data");
            btnExport = new Button { Text = "Export Results", Enabled = false };
            toolTip1.SetToolTip(btnExport, "Save results to Excel");

            lblHubInfo = new Label { AutoSize = true };
            lblMsInfo = new Label { AutoSize = true };

            lblSummary = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            cmbFieldFilter = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDown };
            txtExplanationFilter = new TextBox { Width = 180 };
            chkHPOnly = new CheckBox { Text = "High Priority Only" };

            lblEmpty = new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Visible = false
            };

            // wire up filtering
            cmbFieldFilter.TextChanged += FilterResults;
            cmbFieldFilter.SelectedIndexChanged += FilterResults;
            txtExplanationFilter.TextChanged += FilterResults;
            chkHPOnly.CheckedChanged += FilterResults;

            // wire up import/run/reset/export
            btnImportHub.Click += btnImportHub_Click;
            btnImportMicrosoft.Click += btnImportMicrosoft_Click;
            btnRun.Click += btnRun_Click;
            btnReset.Click += btnReset_Click;
            btnExport.Click += btnExport_Click;

flowModes.Controls.AddRange(new Control[] { rbCompare, rbValidate, rbAdvanced });
flowRunButtons.Controls.AddRange(new Control[] { btnRun, btnReset, btnExport });
flowImports.Controls.AddRange(new Control[] { btnImportMicrosoft, lblMsInfo, btnImportHub, lblHubInfo });
flowFilters.Controls.AddRange(new Control[] { cmbFieldFilter, txtExplanationFilter, chkHPOnly });

layoutControls.Controls.Add(flowModes, 0, 0);
layoutControls.Controls.Add(flowRunButtons, 1, 0);
layoutControls.Controls.Add(flowImports, 0, 1);
layoutControls.SetColumnSpan(flowImports, 2);
layoutControls.Controls.Add(lblSummary, 0, 2);
layoutControls.SetColumnSpan(lblSummary, 2);
layoutControls.Controls.Add(flowFilters, 0, 3);
layoutControls.SetColumnSpan(flowFilters, 2);

// === Validation results grid ===================================
dgvResults = new DataGridView
{
    Dock = DockStyle.Fill,
    ReadOnly = true,
    AllowUserToAddRows = false,
    AllowUserToDeleteRows = false,
    RowHeadersVisible = false,
    Visible = false
};
var pnlResults = new Panel { Dock = DockStyle.Fill };
pnlResults.Controls.AddRange(new Control[] { dgvResults, lblEmpty });

layoutValidation.Controls.Add(layoutControls, 0, 0);
layoutValidation.Controls.Add(pnlResults, 0, 1);

tabValidation.Controls.Add(layoutValidation);

// ===== Price‑mismatch TAB ========================================
layoutPrices = new TableLayoutPanel
{
    Dock = DockStyle.Fill,
    ColumnCount = 1,
    RowCount = 2
};
layoutPrices.RowStyles.Add(new RowStyle(SizeType.AutoSize));
layoutPrices.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

flowPriceTop = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };

dgvPrices = new DataGridView
{
    Dock = DockStyle.Fill,
    ReadOnly = true,
    AllowUserToAddRows = false,
    AllowUserToDeleteRows = false,
    RowHeadersVisible = false
};
btnExportPrices = new Button { Text = "Export …", Enabled = false };
btnExportPrices.Click += btnExportPrices_Click;
toolTip1.SetToolTip(btnExportPrices, "Export price mismatches");
lblPriceInfo = new Label { Text = "No price mismatches.", AutoSize = true };

flowPriceTop.Controls.AddRange(new Control[] { btnExportPrices, lblPriceInfo });
layoutPrices.Controls.Add(flowPriceTop, 0, 0);
layoutPrices.Controls.Add(dgvPrices, 0, 1);

tabPrices.Controls.Add(layoutPrices);

// ===== Logs TAB ================================================
layoutLogs = new TableLayoutPanel
{
    Dock = DockStyle.Fill,
    ColumnCount = 1,
    RowCount = 3
};
layoutLogs.RowStyles.Add(new RowStyle(SizeType.AutoSize));
layoutLogs.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
layoutLogs.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

flowLogTop = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };

dgvLogs = new DataGridView
{
    Dock = DockStyle.Fill,
    ReadOnly = true,
    AllowUserToAddRows = false,
    AllowUserToDeleteRows = false,
    RowHeadersVisible = false
};
dgvLogs.DataBindingComplete += dgvLogs_DataBindingComplete;
btnExportLogs = new Button { Text = "Export log", Enabled = false };
btnResetLogs = new Button { Text = "Clear log", Enabled = false };
btnExportLogs.Click += btnExportLogs_Click;
btnResetLogs.Click += btnResetLogs_Click;
toolTip1.SetToolTip(btnExportLogs, "Export logs to CSV");
toolTip1.SetToolTip(btnResetLogs, "Clear all log entries");
lblLogsSummary = new Label { Text = "⚠ 0 warnings, 0 errors", AutoSize = true };
txtRawLog = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true };

flowLogTop.Controls.AddRange(new Control[] { btnExportLogs, btnResetLogs, lblLogsSummary });
layoutLogs.Controls.Add(flowLogTop, 0, 0);
layoutLogs.Controls.Add(dgvLogs, 0, 1);
layoutLogs.Controls.Add(txtRawLog, 0, 2);

tabLogs.Controls.Add(layoutLogs);
            // ===== MAIN FORM ===================================================
            ClientSize = new Size(1280, 800);
            Controls.AddRange(new Control[] { tabs, titleBar });
            Text = "Reconciliation Tool";
            MinimumSize = new Size(1100, 700);
            Load += Form1_Load;
        }
    }
}