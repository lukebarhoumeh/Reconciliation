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

        private SplitContainer splitMain;
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

        // ── Price‑mismatch tab
        private DataGridView dgvPrices;
        private Button btnExportPrices;
        private Label lblPriceInfo;

        // ── Logs tab
        private DataGridView dgvLogs;
        private Button btnExportLogs;
        private Button btnResetLogs;
        private Label lblLogsSummary;
        private RichTextBox txtRawLog;

        // ---------------------------------------------------------------------
        private void InitializeComponent()
        {
            components = new Container();

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

            // ===== MAIN SPLITTER =============================================
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };
            splitMain.Panel1.Padding = new Padding(12);
            splitMain.SplitterDistance = 260;

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
                Text = "Compare invoices",
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
                Text = "Import MSP‑Hub CSV",
                Location = new Point(230, 10)
            };
            btnImportMicrosoft = new Button
            {
                Text = "Import Microsoft CSV",
                Location = new Point(230, 50)
            };

            btnRun = new Button
            {
                Text = "Run",
                Enabled = false,
                Location = new Point(500, 10)
            };
            btnReset = new Button
            {
                Text = "Reset",
                Enabled = false,
                Location = new Point(500, 50)
            };
            btnExport = new Button
            {
                Text = "Export",
                Enabled = false,
                Location = new Point(500, 90)
            };

            lblHubInfo = new Label { AutoSize = true, Location = new Point(10, 110) };
            lblMsInfo = new Label { AutoSize = true, Location = new Point(10, 130) };

            lblSummary = new Label
            {
                AutoSize = true,
                Location = new Point(10, 160),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            cmbFieldFilter = new ComboBox
            {
                Width = 180,
                Location = new Point(700, 10),
                DropDownStyle = ComboBoxStyle.DropDown
            };
            txtExplanationFilter = new TextBox
            {
                Width = 180,
                Location = new Point(700, 45)
            };
            chkHPOnly = new CheckBox
            {
                Text = "High‑priority only",
                Location = new Point(700, 75)
            };

            lblEmpty = new Label
            {
                AutoSize = true,
                Location = new Point(500, 200),
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

            splitMain.Panel1.Controls.AddRange(new Control[] {
                rbCompare, rbValidate, rbAdvanced,
                btnImportHub, btnImportMicrosoft,
                btnRun, btnReset, btnExport,
                lblHubInfo, lblMsInfo, lblSummary, lblEmpty,
                cmbFieldFilter, txtExplanationFilter, chkHPOnly
            });

            // === Validation results grid (Panel2) =============================
            dgvResults = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                Visible = false
            };
            splitMain.Panel2.Controls.Add(dgvResults);

            tabValidation.Controls.Add(splitMain);

            // ===== Price‑mismatch TAB =========================================
            dgvPrices = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false
            };
            btnExportPrices = new Button
            {
                Text = "Export …",
                Enabled = false,
                Dock = DockStyle.Top
            };
            btnExportPrices.Click += btnExportPrices_Click;
            lblPriceInfo = new Label
            {
                Text = "No price mismatches.",
                AutoSize = true,
                Dock = DockStyle.Top
            };
            tabPrices.Controls.AddRange(
                new Control[] { dgvPrices, btnExportPrices, lblPriceInfo });

            // ===== Logs TAB ====================================================
            dgvLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false
            };
            dgvLogs.DataBindingComplete += dgvLogs_DataBindingComplete;
            btnExportLogs = new Button
            {
                Text = "Export log",
                Enabled = false,
                Dock = DockStyle.Top
            };
            btnResetLogs = new Button
            {
                Text = "Clear log",
                Enabled = false,
                Dock = DockStyle.Top
            };
            btnExportLogs.Click += btnExportLogs_Click;
            btnResetLogs.Click += btnResetLogs_Click;
            lblLogsSummary = new Label
            {
                Text = "⚠ 0 warnings, 0 errors",
                AutoSize = true,
                Dock = DockStyle.Top
            };
            txtRawLog = new RichTextBox
            {
                Dock = DockStyle.Bottom,
                Height = 200,
                ReadOnly = true
            };

            tabLogs.Controls.AddRange(new Control[]
            { txtRawLog, dgvLogs, lblLogsSummary, btnResetLogs, btnExportLogs });

            // ===== MAIN FORM ===================================================
            ClientSize = new Size(1280, 800);
            Controls.AddRange(new Control[] { tabs, titleBar });
            Text = "Reconciliation Tool";
            MinimumSize = new Size(1100, 700);
            Load += Form1_Load;
        }
    }
}