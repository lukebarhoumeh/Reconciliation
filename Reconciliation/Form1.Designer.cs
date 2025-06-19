using Reconciliation.Properties;
﻿namespace Reconciliation
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            splitMain = new SplitContainer();
            btnToggleFiles = new Button();
            panel1 = new Panel();
            btnMaximized = new Button();
            btnClose = new Button();
            pictureBox2 = new PictureBox();
            panel2 = new Panel();
            lblVersion = new Label();
            label1 = new Label();
            tbcMenu = new TabControl();
            tabPage1 = new TabPage();
            lblInternal2DiscrepancyMsg = new Label();
            lblExternal2DiscrepancyMsg = new Label();
            btnReset = new Button();
            lblInternal1DiscrepancyMsg = new Label();
            label3 = new Label();
            rbInternal = new RadioButton();
            rbExternal = new RadioButton();
            btnCompare = new Button();
            btnExportToCsv = new Button();
            chkFuzzyColumns = new CheckBox();
            lblDiscrepancyTitle = new Label();
            lblExternal1DiscrepancyMsg = new Label();
            lblEmptyMessage = new Label();
            dgResultdata = new DataGridView();
            lblSixDotOneFileRowCount = new Label();
            lblSixDotOneFileName = new Label();
            btnImportSixDotOneFile = new Button();
            LogoMsbhub = new PictureBox();
            lblMsbhubfilenameLabel = new Label();
            lblMsbhubrowscountLabel = new Label();
            lblMicrosoftFileRowCount = new Label();
            lblMicrosoftFileName = new Label();
            btnImportMicrosoft = new Button();
            logoMicrosoft = new PictureBox();
            lblMicrosoftFilenameLabel = new Label();
            lblMicrosoftRowsCountLabel = new Label();
            tabPage3 = new TabPage();
            label5 = new Label();
            btnPriceMismatchingExportToCsv = new Button();
            pictureBox1 = new PictureBox();
            lblPricematchingEmptyMessage = new Label();
            dgAzurePriceMismatch = new DataGridView();
            lblSixdotonefilenameTab4 = new Label();
            lblMicrosoftfilenameTab4 = new Label();
            pictureBox7 = new PictureBox();
            lblInfoPricematching = new Label();
            tabPage2 = new TabPage();
            btnResetLogs = new Button();
            btnExportLogs = new Button();
            dgvLogs = new DataGridView();
            lblLogsSummary = new Label();
            lblMismatchSummary = new Label();
            txtFieldFilter = new TextBox();
            txtExplanationFilter = new TextBox();
            textLogs = new RichTextBox();
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            panel2.SuspendLayout();
            tbcMenu.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgResultdata).BeginInit();
            ((System.ComponentModel.ISupportInitialize)LogoMsbhub).BeginInit();
            ((System.ComponentModel.ISupportInitialize)logoMicrosoft).BeginInit();
            tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgAzurePriceMismatch).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox7).BeginInit();
            tabPage2.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.Transparent;
            panel1.BackgroundImage = (Image)resources.GetObject("panel1.BackgroundImage");
            panel1.BackgroundImageLayout = ImageLayout.Stretch;
            panel1.Controls.Add(btnMaximized);
            panel1.Controls.Add(btnClose);
            panel1.Controls.Add(pictureBox2);
            panel1.Controls.Add(panel2);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(2789, 79);
            panel1.TabIndex = 0;
            // 
            // btnMaximized
            // 
            btnMaximized.BackColor = Color.FromArgb(0, 122, 204);
            btnMaximized.Dock = DockStyle.Right;
            btnMaximized.FlatAppearance.BorderSize = 0;
            btnMaximized.FlatStyle = FlatStyle.Flat;
            btnMaximized.Font = new Font("Segoe MDL2 Assets", 11F);
            btnMaximized.ForeColor = Color.White;
            btnMaximized.Location = new Point(2717, 0);
            btnMaximized.Name = "btnMaximized";
            btnMaximized.Size = new Size(30, 41);
            btnMaximized.TabIndex = 3;
            btnMaximized.Text = "\uE921";
            btnMaximized.TextAlign = ContentAlignment.TopCenter;
            btnMaximized.UseVisualStyleBackColor = false;
            btnMaximized.Click += btnMaximized_Click;
            //
            // btnClose
            //
            btnClose.BackColor = Color.FromArgb(0, 122, 204);
            btnClose.Dock = DockStyle.Right;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Font = new Font("Segoe MDL2 Assets", 11F);
            btnClose.ForeColor = Color.White;
            btnClose.Location = new Point(2747, 0);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(42, 41);
            btnClose.TabIndex = 2;
            btnClose.Text = "\uE8BB";
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Click += btnClose_Click;
            // 
            // pictureBox2
            // 
            pictureBox2.BackColor = Color.Transparent;
            pictureBox2.Image = (Image)resources.GetObject("pictureBox2.Image");
            pictureBox2.Location = new Point(0, 0);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Padding = new Padding(10, 7, 0, 10);
            pictureBox2.Size = new Size(190, 39);
            pictureBox2.TabIndex = 1;
            pictureBox2.TabStop = false;
            // 
            // panel2
            // 
            panel2.BackColor = Color.White;
            panel2.Controls.Add(lblVersion);
            panel2.Controls.Add(label1);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 41);
            panel2.Name = "panel2";
            panel2.Size = new Size(2789, 38);
            panel2.TabIndex = 0;
            // 
            // lblVersion
            // 
            lblVersion.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblVersion.AutoSize = true;
            lblVersion.Font = new Font("Arial Narrow", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblVersion.Location = new Point(2731, 0);
            lblVersion.Name = "lblVersion";
            lblVersion.Padding = new Padding(0, 9, 0, 10);
            lblVersion.Size = new Size(46, 39);
            lblVersion.TabIndex = 1;
            lblVersion.Text = "label3";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Arial Narrow", 13.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(9, 40, 94);
            label1.Location = new Point(7, 11);
            label1.Name = "label1";
            label1.Size = new Size(237, 27);
            label1.TabIndex = 0;
            label1.Text = "RECONCILIATION TOOL";
            // 
            // tbcMenu
            // 
            tbcMenu.AccessibleRole = AccessibleRole.MenuBar;
            tbcMenu.Controls.Add(tabPage1);
            tbcMenu.Controls.Add(tabPage3);
            tbcMenu.Controls.Add(tabPage2);
            tbcMenu.Dock = DockStyle.Fill;
            tbcMenu.Font = new Font("Arial Narrow", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbcMenu.ItemSize = new Size(166, 55);
            tbcMenu.Location = new Point(0, 79);
            tbcMenu.Margin = new Padding(0);
            tbcMenu.Name = "tbcMenu";
            tbcMenu.Padding = new Point(30, 6);
            tbcMenu.SelectedIndex = 0;
            tbcMenu.Size = new Size(2789, 1386);
            tbcMenu.TabIndex = 11;
            // 
            // tabPage1
            // 
            tabPage1.BackColor = Color.White;
            tabPage1.Controls.Add(splitMain);
            splitMain.Panel1.Controls.Add(lblInternal2DiscrepancyMsg);
            splitMain.Panel1.Controls.Add(lblExternal2DiscrepancyMsg);
            splitMain.Panel1.Controls.Add(btnReset);
            splitMain.Panel1.Controls.Add(lblInternal1DiscrepancyMsg);
            splitMain.Panel1.Controls.Add(label3);
            splitMain.Panel1.Controls.Add(rbInternal);
            splitMain.Panel1.Controls.Add(rbExternal);
            splitMain.Panel1.Controls.Add(btnCompare);
            splitMain.Panel1.Controls.Add(btnExportToCsv);
            splitMain.Panel1.Controls.Add(chkFuzzyColumns);
            splitMain.Panel1.Controls.Add(lblDiscrepancyTitle);
            splitMain.Panel1.Controls.Add(lblExternal1DiscrepancyMsg);
            splitMain.Panel1.Controls.Add(lblEmptyMessage);
            splitMain.Panel1.Controls.Add(lblMismatchSummary);
            splitMain.Panel1.Controls.Add(txtFieldFilter);
            splitMain.Panel1.Controls.Add(txtExplanationFilter);
            splitMain.Panel2.Controls.Add(dgResultdata);
            splitMain.Panel1.Controls.Add(lblSixDotOneFileRowCount);
            splitMain.Panel1.Controls.Add(lblSixDotOneFileName);
            splitMain.Panel1.Controls.Add(btnImportSixDotOneFile);
            splitMain.Panel1.Controls.Add(LogoMsbhub);
            splitMain.Panel1.Controls.Add(lblMsbhubfilenameLabel);
            splitMain.Panel1.Controls.Add(lblMsbhubrowscountLabel);
            splitMain.Panel1.Controls.Add(lblMicrosoftFileRowCount);
            splitMain.Panel1.Controls.Add(lblMicrosoftFileName);
            splitMain.Panel1.Controls.Add(btnImportMicrosoft);
            splitMain.Panel1.Controls.Add(logoMicrosoft);
            splitMain.Panel1.Controls.Add(lblMicrosoftFilenameLabel);
            splitMain.Panel1.Controls.Add(lblMicrosoftRowsCountLabel);
            splitMain.Panel1.Controls.Add(btnToggleFiles);
            tabPage1.Location = new Point(4, 59);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(2781, 1323);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Invoice Validation";
            //
            // splitMain
            //
            splitMain.Dock = DockStyle.Fill;
            splitMain.Orientation = Orientation.Horizontal;
            splitMain.Panel1MinSize = 260;
            splitMain.Location = new Point(3, 3);
            splitMain.Name = "splitMain";
            splitMain.Size = new Size(2775, 1317);
            splitMain.TabIndex = 0;
            //
            // btnToggleFiles
            //
            btnToggleFiles.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnToggleFiles.Font = new Font("Segoe MDL2 Assets", 9F);
            btnToggleFiles.Location = new Point(2736, 3);
            btnToggleFiles.Name = "btnToggleFiles";
            btnToggleFiles.Size = new Size(28, 24);
            btnToggleFiles.TabIndex = 51;
            btnToggleFiles.Text = "\uE014";
            btnToggleFiles.UseVisualStyleBackColor = true;
            btnToggleFiles.Click += btnToggleFiles_Click;
            //
            // lblInternal2DiscrepancyMsg
            // 
            lblInternal2DiscrepancyMsg.AutoSize = true;
            lblInternal2DiscrepancyMsg.Font = new Font("Arial Narrow", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblInternal2DiscrepancyMsg.ForeColor = Color.DimGray;
            lblInternal2DiscrepancyMsg.Image = (Image)resources.GetObject("lblInternal2DiscrepancyMsg.Image");
            lblInternal2DiscrepancyMsg.ImageAlign = ContentAlignment.MiddleLeft;
            lblInternal2DiscrepancyMsg.Location = new Point(301, 240);
            lblInternal2DiscrepancyMsg.Name = "lblInternal2DiscrepancyMsg";
            lblInternal2DiscrepancyMsg.Size = new Size(325, 20);
            lblInternal2DiscrepancyMsg.TabIndex = 41;
            lblInternal2DiscrepancyMsg.Text = "       Yellow-highlighted data signifies minor discrepancies.";
            lblInternal2DiscrepancyMsg.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblExternal2DiscrepancyMsg
            // 
            lblExternal2DiscrepancyMsg.Location = new Point(0, 0);
            lblExternal2DiscrepancyMsg.Name = "lblExternal2DiscrepancyMsg";
            lblExternal2DiscrepancyMsg.Size = new Size(100, 23);
            lblExternal2DiscrepancyMsg.TabIndex = 42;
            // 
            // btnReset
            // 
            btnReset.Anchor = AnchorStyles.Right;
            btnReset.BackColor = Color.FromArgb(0, 122, 204);
            btnReset.Cursor = Cursors.Hand;
            btnReset.Enabled = false;
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.FlatStyle = FlatStyle.Flat;
            btnReset.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnReset.ImageAlign = ContentAlignment.MiddleLeft;
            btnReset.Location = new Point(2528, 440);
            btnReset.Name = "btnReset";
            btnReset.Padding = new Padding(12, 5, 12, 5);
            btnReset.Size = new Size(173, 40);
            btnReset.TabIndex = 38;
            btnReset.ForeColor = Color.White;
            btnReset.Text = "Reset";
            btnReset.UseVisualStyleBackColor = false;
            btnReset.Click += btnClear_Click;
            // 
            // lblInternal1DiscrepancyMsg
            // 
            lblInternal1DiscrepancyMsg.AutoSize = true;
            lblInternal1DiscrepancyMsg.Font = new Font("Arial Narrow", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblInternal1DiscrepancyMsg.ForeColor = Color.DimGray;
            lblInternal1DiscrepancyMsg.Image = (Image)resources.GetObject("lblInternal1DiscrepancyMsg.Image");
            lblInternal1DiscrepancyMsg.ImageAlign = ContentAlignment.MiddleLeft;
            lblInternal1DiscrepancyMsg.Location = new Point(19, 240);
            lblInternal1DiscrepancyMsg.Name = "lblInternal1DiscrepancyMsg";
            lblInternal1DiscrepancyMsg.Size = new Size(276, 20);
            lblInternal1DiscrepancyMsg.TabIndex = 37;
            lblInternal1DiscrepancyMsg.Text = "       Red-highlighted data indicates critical errors.";
            lblInternal1DiscrepancyMsg.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            label3.Font = new Font("Arial Narrow", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(8, 12);
            label3.Name = "label3";
            label3.Size = new Size(208, 28);
            label3.TabIndex = 36;
            label3.Text = "Select Mode";
            // 
            // rbInternal
            // 
            rbInternal.AutoSize = true;
            rbInternal.Location = new Point(342, 43);
            rbInternal.Name = "rbInternal";
            rbInternal.Size = new Size(258, 28);
            rbInternal.TabIndex = 35;
            rbInternal.TabStop = true;
            rbInternal.Text = "Validate with MSP Hub Invoice";
            rbInternal.UseVisualStyleBackColor = true;
            // 
            // rbExternal
            // 
            rbExternal.AutoSize = true;
            rbExternal.Location = new Point(61, 43);
            rbExternal.Name = "rbExternal";
            rbExternal.Size = new Size(263, 28);
            rbExternal.TabIndex = 34;
            rbExternal.TabStop = true;
            rbExternal.Text = "Compare with Microsoft Invoice";
            rbExternal.UseVisualStyleBackColor = true;
            // 
            // btnCompare
            // 
            btnCompare.Anchor = AnchorStyles.Right;
            btnCompare.BackColor = Color.FromArgb(0, 122, 204);
            btnCompare.Cursor = Cursors.Hand;
            btnCompare.Enabled = false;
            btnCompare.FlatAppearance.BorderSize = 0;
            btnCompare.FlatStyle = FlatStyle.Flat;
            btnCompare.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCompare.ImageAlign = ContentAlignment.MiddleLeft;
            btnCompare.Location = new Point(2366, 440);
            btnCompare.Name = "btnCompare";
            btnCompare.Padding = new Padding(12, 5, 12, 5);
            btnCompare.Size = new Size(154, 40);
            btnCompare.TabIndex = 33;
            btnCompare.ForeColor = Color.White;
            btnCompare.Text = "Reconcile";
            btnCompare.UseVisualStyleBackColor = false;
            btnCompare.Click += btnCompare_Click;
            // 
            // btnExportToCsv
            // 
            btnExportToCsv.Anchor = AnchorStyles.Right;
            btnExportToCsv.BackColor = Color.FromArgb(0, 122, 204);
            btnExportToCsv.Cursor = Cursors.Hand;
            btnExportToCsv.Enabled = false;
            btnExportToCsv.FlatAppearance.BorderSize = 0;
            btnExportToCsv.FlatStyle = FlatStyle.Flat;
            btnExportToCsv.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnExportToCsv.Image = (Image)resources.GetObject("btnExportToCsv.Image");
            btnExportToCsv.ImageAlign = ContentAlignment.MiddleLeft;
            btnExportToCsv.Location = new Point(2709, 440);
            btnExportToCsv.Name = "btnExportToCsv";
            btnExportToCsv.Padding = new Padding(12, 5, 12, 5);
            btnExportToCsv.Size = new Size(154, 40);
            btnExportToCsv.TabIndex = 32;
            btnExportToCsv.ForeColor = Color.White;
            btnExportToCsv.Text = "Export";
            btnExportToCsv.TextAlign = ContentAlignment.MiddleRight;
            btnExportToCsv.UseVisualStyleBackColor = false;
            btnExportToCsv.Click += btnExportToCsv_Click;
            // chkFuzzyColumns
            //
            chkFuzzyColumns.AutoSize = true;
            chkFuzzyColumns.Location = new Point(2366, 400);
            chkFuzzyColumns.Name = "chkFuzzyColumns";
            chkFuzzyColumns.Size = new Size(187, 24);
            chkFuzzyColumns.TabIndex = 50;
            chkFuzzyColumns.Text = "Allow fuzzy column match";
            chkFuzzyColumns.UseVisualStyleBackColor = true;
            //
            // lblDiscrepancyTitle
            // 
            lblDiscrepancyTitle.AutoSize = true;
            lblDiscrepancyTitle.Font = new Font("Arial Narrow", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblDiscrepancyTitle.ForeColor = Color.FromArgb(9, 40, 94);
            lblDiscrepancyTitle.Location = new Point(10, 218);
            lblDiscrepancyTitle.Name = "lblDiscrepancyTitle";
            lblDiscrepancyTitle.Size = new Size(109, 22);
            lblDiscrepancyTitle.TabIndex = 30;
            lblDiscrepancyTitle.Text = "DISCREPANCY";
            // 
            // lblExternal1DiscrepancyMsg
            // 
            lblExternal1DiscrepancyMsg.AutoSize = true;
            lblExternal1DiscrepancyMsg.Font = new Font("Arial Narrow", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblExternal1DiscrepancyMsg.ForeColor = Color.DimGray;
            lblExternal1DiscrepancyMsg.Image = (Image)resources.GetObject("lblExternal1DiscrepancyMsg.Image");
            lblExternal1DiscrepancyMsg.ImageAlign = ContentAlignment.MiddleLeft;
            lblExternal1DiscrepancyMsg.Location = new Point(19, 240);
            lblExternal1DiscrepancyMsg.Name = "lblExternal1DiscrepancyMsg";
            lblExternal1DiscrepancyMsg.Size = new Size(405, 20);
            lblExternal1DiscrepancyMsg.TabIndex = 31;
            lblExternal1DiscrepancyMsg.Text = "       Yellow-highlighted data represents line items missing in the invoices.";
            lblExternal1DiscrepancyMsg.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblEmptyMessage
            // 
            lblEmptyMessage.AutoSize = true;
            lblEmptyMessage.Location = new Point(835, 476);
            lblEmptyMessage.Name = "lblEmptyMessage";
            lblEmptyMessage.Size = new Size(141, 24);
            lblEmptyMessage.TabIndex = 29;
            lblEmptyMessage.Text = "lblEmptyMessage";
            lblEmptyMessage.Visible = false;
            //
            // lblMismatchSummary
            //
            lblMismatchSummary.AutoSize = true;
            lblMismatchSummary.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            lblMismatchSummary.Location = new Point(3, 260);
            lblMismatchSummary.Name = "lblMismatchSummary";
            lblMismatchSummary.Size = new Size(82, 23);
            lblMismatchSummary.TabIndex = 37;
            lblMismatchSummary.Text = "Summary";
            //
            // txtFieldFilter
            //
            txtFieldFilter.Location = new Point(1200, 260);
            txtFieldFilter.Name = "txtFieldFilter";
            txtFieldFilter.PlaceholderText = "Filter Field";
            txtFieldFilter.Size = new Size(150, 27);
            txtFieldFilter.TabIndex = 38;
            //
            // txtExplanationFilter
            //
            txtExplanationFilter.Location = new Point(1360, 260);
            txtExplanationFilter.Name = "txtExplanationFilter";
            txtExplanationFilter.PlaceholderText = "Filter Explanation";
            txtExplanationFilter.Size = new Size(200, 27);
            txtExplanationFilter.TabIndex = 39;
            //
            // dgResultdata
            // 
            dgResultdata.AllowUserToAddRows = false;
            dgResultdata.AllowUserToDeleteRows = false;
            dgResultdata.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dgResultdata.BackgroundColor = Color.White;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Color.RoyalBlue;
            dataGridViewCellStyle1.Font = new Font("Arial Narrow", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle1.ForeColor = Color.White;
            dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(32, 93, 170);
            dataGridViewCellStyle1.SelectionForeColor = Color.White;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dgResultdata.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgResultdata.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.White;
            dataGridViewCellStyle2.Font = new Font("Arial Narrow", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle2.ForeColor = Color.Black;
            dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(32, 93, 170);
            dataGridViewCellStyle2.SelectionForeColor = Color.White;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dgResultdata.DefaultCellStyle = dataGridViewCellStyle2;
            dgResultdata.Dock = DockStyle.Fill;
            dgResultdata.EnableHeadersVisualStyles = false;
            dgResultdata.Location = new Point(0, 0);
            dgResultdata.Name = "dgResultdata";
            dgResultdata.ReadOnly = true;
            dgResultdata.RowHeadersWidth = 51;
            dgResultdata.RowTemplate.ReadOnly = true;
            dgResultdata.Size = new Size(2775, 607);
            dgResultdata.TabIndex = 28;
            dgResultdata.Visible = false;
            // 
            // lblSixDotOneFileRowCount
            // 
            lblSixDotOneFileRowCount.AutoSize = true;
            lblSixDotOneFileRowCount.Location = new Point(1063, 117);
            lblSixDotOneFileRowCount.Name = "lblSixDotOneFileRowCount";
            lblSixDotOneFileRowCount.Size = new Size(20, 24);
            lblSixDotOneFileRowCount.TabIndex = 27;
            lblSixDotOneFileRowCount.Text = "  ";
            // 
            // lblSixDotOneFileName
            // 
            lblSixDotOneFileName.AutoSize = true;
            lblSixDotOneFileName.Location = new Point(242, 115);
            lblSixDotOneFileName.Name = "lblSixDotOneFileName";
            lblSixDotOneFileName.Size = new Size(20, 24);
            lblSixDotOneFileName.TabIndex = 26;
            lblSixDotOneFileName.Text = "  ";
            // 
            // btnImportSixDotOneFile
            // 
            btnImportSixDotOneFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnImportSixDotOneFile.AutoSize = true;
            btnImportSixDotOneFile.BackColor = Color.FromArgb(0, 122, 204);
            btnImportSixDotOneFile.Cursor = Cursors.Hand;
            btnImportSixDotOneFile.FlatAppearance.BorderSize = 0;
            btnImportSixDotOneFile.FlatStyle = FlatStyle.Flat;
            btnImportSixDotOneFile.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnImportSixDotOneFile.Image = (Image)resources.GetObject("btnImportSixDotOneFile.Image");
            btnImportSixDotOneFile.ImageAlign = ContentAlignment.MiddleLeft;
            btnImportSixDotOneFile.Location = new Point(2529, 89);
            btnImportSixDotOneFile.Name = "btnImportSixDotOneFile";
            btnImportSixDotOneFile.Padding = new Padding(0, 8, 0, 8);
            btnImportSixDotOneFile.Size = new Size(270, 52);
            btnImportSixDotOneFile.TabIndex = 23;
            btnImportSixDotOneFile.ForeColor = Color.White;
            btnImportSixDotOneFile.Text = "Upload MSP Hub Invoice";
            btnImportSixDotOneFile.UseVisualStyleBackColor = false;
            btnImportSixDotOneFile.Click += btnImportSixDotOneFile_Click;
            // 
            // LogoMsbhub
            // 
            LogoMsbhub.Image = Resources.MSPHubLogo;
            LogoMsbhub.Location = new Point(7, 95);
            LogoMsbhub.Name = "LogoMsbhub";
            LogoMsbhub.Size = new Size(150, 48);
            LogoMsbhub.SizeMode = PictureBoxSizeMode.Zoom;
            LogoMsbhub.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            LogoMsbhub.TabIndex = 22;
            LogoMsbhub.TabStop = false;
            // 
            // lblMsbhubfilenameLabel
            // 
            lblMsbhubfilenameLabel.AutoSize = true;
            lblMsbhubfilenameLabel.Font = new Font("Arial Narrow", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMsbhubfilenameLabel.Location = new Point(242, 87);
            lblMsbhubfilenameLabel.Name = "lblMsbhubfilenameLabel";
            lblMsbhubfilenameLabel.Size = new Size(69, 20);
            lblMsbhubfilenameLabel.TabIndex = 24;
            lblMsbhubfilenameLabel.Text = "File Name";
            // 
            // lblMsbhubrowscountLabel
            // 
            lblMsbhubrowscountLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblMsbhubrowscountLabel.AutoSize = true;
            lblMsbhubrowscountLabel.Font = new Font("Arial Narrow", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMsbhubrowscountLabel.Location = new Point(1931, 87);
            lblMsbhubrowscountLabel.Name = "lblMsbhubrowscountLabel";
            lblMsbhubrowscountLabel.Size = new Size(82, 20);
            lblMsbhubrowscountLabel.TabIndex = 25;
            lblMsbhubrowscountLabel.Text = "Rows Count";
            // 
            // lblMicrosoftFileRowCount
            // 
            lblMicrosoftFileRowCount.AutoSize = true;
            lblMicrosoftFileRowCount.Location = new Point(1063, 180);
            lblMicrosoftFileRowCount.Name = "lblMicrosoftFileRowCount";
            lblMicrosoftFileRowCount.Size = new Size(20, 24);
            lblMicrosoftFileRowCount.TabIndex = 14;
            lblMicrosoftFileRowCount.Text = "  ";
            // 
            // lblMicrosoftFileName
            // 
            lblMicrosoftFileName.AutoSize = true;
            lblMicrosoftFileName.Location = new Point(239, 180);
            lblMicrosoftFileName.Name = "lblMicrosoftFileName";
            lblMicrosoftFileName.Size = new Size(20, 24);
            lblMicrosoftFileName.TabIndex = 11;
            lblMicrosoftFileName.Text = "  ";
            // 
            // btnImportMicrosoft
            // 
            btnImportMicrosoft.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnImportMicrosoft.AutoSize = true;
            btnImportMicrosoft.BackColor = Color.FromArgb(0, 122, 204);
            btnImportMicrosoft.Cursor = Cursors.Hand;
            btnImportMicrosoft.FlatAppearance.BorderSize = 0;
            btnImportMicrosoft.FlatStyle = FlatStyle.Flat;
            btnImportMicrosoft.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnImportMicrosoft.Image = (Image)resources.GetObject("btnImportMicrosoft.Image");
            btnImportMicrosoft.ImageAlign = ContentAlignment.MiddleLeft;
            btnImportMicrosoft.Location = new Point(2529, 152);
            btnImportMicrosoft.Name = "btnImportMicrosoft";
            btnImportMicrosoft.Padding = new Padding(0, 8, 0, 8);
            btnImportMicrosoft.Size = new Size(270, 52);
            btnImportMicrosoft.TabIndex = 1;
            btnImportMicrosoft.ForeColor = Color.White;
            btnImportMicrosoft.Text = "Upload Microsoft Invoice";
            btnImportMicrosoft.UseVisualStyleBackColor = false;
            btnImportMicrosoft.Click += btnImportMicrosoft_Click;
            // 
            // logoMicrosoft
            // 
            logoMicrosoft.Image = Resources.MicrosoftLogo;
            logoMicrosoft.Location = new Point(10, 153);
            logoMicrosoft.Name = "logoMicrosoft";
            logoMicrosoft.Size = new Size(150, 48);
            logoMicrosoft.SizeMode = PictureBoxSizeMode.Zoom;
            logoMicrosoft.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            logoMicrosoft.TabIndex = 0;
            logoMicrosoft.TabStop = false;
            // 
            // lblMicrosoftFilenameLabel
            // 
            lblMicrosoftFilenameLabel.AutoSize = true;
            lblMicrosoftFilenameLabel.Font = new Font("Arial Narrow", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMicrosoftFilenameLabel.Location = new Point(242, 153);
            lblMicrosoftFilenameLabel.Name = "lblMicrosoftFilenameLabel";
            lblMicrosoftFilenameLabel.Size = new Size(69, 20);
            lblMicrosoftFilenameLabel.TabIndex = 2;
            lblMicrosoftFilenameLabel.Text = "File Name";
            // 
            // lblMicrosoftRowsCountLabel
            // 
            lblMicrosoftRowsCountLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblMicrosoftRowsCountLabel.AutoSize = true;
            lblMicrosoftRowsCountLabel.Font = new Font("Arial Narrow", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMicrosoftRowsCountLabel.Location = new Point(1931, 153);
            lblMicrosoftRowsCountLabel.Name = "lblMicrosoftRowsCountLabel";
            lblMicrosoftRowsCountLabel.Size = new Size(82, 20);
            lblMicrosoftRowsCountLabel.TabIndex = 5;
            lblMicrosoftRowsCountLabel.Text = "Rows Count";
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(label5);
            tabPage3.Controls.Add(btnPriceMismatchingExportToCsv);
            tabPage3.Controls.Add(pictureBox1);
            tabPage3.Controls.Add(lblPricematchingEmptyMessage);
            tabPage3.Controls.Add(dgAzurePriceMismatch);
            tabPage3.Controls.Add(lblSixdotonefilenameTab4);
            tabPage3.Controls.Add(lblMicrosoftfilenameTab4);
            tabPage3.Controls.Add(pictureBox7);
            tabPage3.Controls.Add(lblInfoPricematching);
            tabPage3.Location = new Point(4, 59);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(1929, 1323);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Price Mismatch";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Arial Narrow", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label5.ForeColor = Color.FromArgb(9, 40, 94);
            label5.Location = new Point(3, 104);
            label5.Name = "label5";
            label5.Size = new Size(109, 22);
            label5.TabIndex = 42;
            label5.Text = "DISCREPANCY";
            // 
            // btnPriceMismatchingExportToCsv
            // 
            btnPriceMismatchingExportToCsv.Anchor = AnchorStyles.Right;
            btnPriceMismatchingExportToCsv.BackColor = Color.FromArgb(0, 122, 204);
            btnPriceMismatchingExportToCsv.Cursor = Cursors.Hand;
            btnPriceMismatchingExportToCsv.Enabled = false;
            btnPriceMismatchingExportToCsv.FlatAppearance.BorderSize = 0;
            btnPriceMismatchingExportToCsv.FlatStyle = FlatStyle.Flat;
            btnPriceMismatchingExportToCsv.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnPriceMismatchingExportToCsv.Image = (Image)resources.GetObject("btnPriceMismatchingExportToCsv.Image");
            btnPriceMismatchingExportToCsv.ImageAlign = ContentAlignment.MiddleLeft;
            btnPriceMismatchingExportToCsv.Location = new Point(1137, 1);
            btnPriceMismatchingExportToCsv.Name = "btnPriceMismatchingExportToCsv";
            btnPriceMismatchingExportToCsv.Padding = new Padding(12, 5, 12, 5);
            btnPriceMismatchingExportToCsv.Size = new Size(124, 40);
            btnPriceMismatchingExportToCsv.TabIndex = 41;
            btnPriceMismatchingExportToCsv.ForeColor = Color.White;
            btnPriceMismatchingExportToCsv.Text = "";
            btnPriceMismatchingExportToCsv.TextAlign = ContentAlignment.MiddleRight;
            btnPriceMismatchingExportToCsv.UseVisualStyleBackColor = false;
            btnPriceMismatchingExportToCsv.Click += btnPriceMismatchingExportToCsv_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(439, 9);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(180, 37);
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.TabIndex = 40;
            pictureBox1.TabStop = false;
            // 
            // lblPricematchingEmptyMessage
            // 
            lblPricematchingEmptyMessage.AutoSize = true;
            lblPricematchingEmptyMessage.Location = new Point(764, 326);
            lblPricematchingEmptyMessage.Name = "lblPricematchingEmptyMessage";
            lblPricematchingEmptyMessage.Size = new Size(244, 24);
            lblPricematchingEmptyMessage.TabIndex = 39;
            lblPricematchingEmptyMessage.Text = "lblPricematchingEmptyMessage";
            lblPricematchingEmptyMessage.Visible = false;
            // 
            // dgAzurePriceMismatch
            // 
            dgAzurePriceMismatch.AllowUserToAddRows = false;
            dgAzurePriceMismatch.AllowUserToDeleteRows = false;
            dgAzurePriceMismatch.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dgAzurePriceMismatch.BackgroundColor = Color.White;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = Color.RoyalBlue;
            dataGridViewCellStyle3.Font = new Font("Arial Narrow", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle3.ForeColor = Color.White;
            dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(32, 93, 170);
            dataGridViewCellStyle3.SelectionForeColor = Color.White;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.True;
            dgAzurePriceMismatch.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgAzurePriceMismatch.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = Color.White;
            dataGridViewCellStyle4.Font = new Font("Arial Narrow", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle4.ForeColor = Color.Black;
            dataGridViewCellStyle4.SelectionBackColor = Color.FromArgb(32, 93, 170);
            dataGridViewCellStyle4.SelectionForeColor = Color.White;
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
            dgAzurePriceMismatch.DefaultCellStyle = dataGridViewCellStyle4;
            dgAzurePriceMismatch.Dock = DockStyle.Bottom;
            dgAzurePriceMismatch.EnableHeadersVisualStyles = false;
            dgAzurePriceMismatch.Location = new Point(3, 606);
            dgAzurePriceMismatch.Name = "dgAzurePriceMismatch";
            dgAzurePriceMismatch.ReadOnly = true;
            dgAzurePriceMismatch.RowHeadersWidth = 51;
            dgAzurePriceMismatch.Size = new Size(1923, 714);
            dgAzurePriceMismatch.TabIndex = 38;
            dgAzurePriceMismatch.Visible = false;
            // 
            // lblSixdotonefilenameTab4
            // 
            lblSixdotonefilenameTab4.AutoSize = true;
            lblSixdotonefilenameTab4.Location = new Point(439, 60);
            lblSixdotonefilenameTab4.Name = "lblSixdotonefilenameTab4";
            lblSixdotonefilenameTab4.Size = new Size(20, 24);
            lblSixdotonefilenameTab4.TabIndex = 37;
            lblSixdotonefilenameTab4.Text = "  ";
            // 
            // lblMicrosoftfilenameTab4
            // 
            lblMicrosoftfilenameTab4.AutoSize = true;
            lblMicrosoftfilenameTab4.Location = new Point(8, 60);
            lblMicrosoftfilenameTab4.Name = "lblMicrosoftfilenameTab4";
            lblMicrosoftfilenameTab4.Size = new Size(20, 24);
            lblMicrosoftfilenameTab4.TabIndex = 36;
            lblMicrosoftfilenameTab4.Text = "  ";
            // 
            // pictureBox7
            // 
            pictureBox7.Image = (Image)resources.GetObject("pictureBox7.Image");
            pictureBox7.Location = new Point(8, 6);
            pictureBox7.Name = "pictureBox7";
            pictureBox7.Size = new Size(188, 40);
            pictureBox7.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox7.TabIndex = 34;
            pictureBox7.TabStop = false;
            // 
            // lblInfoPricematching
            // 
            lblInfoPricematching.AutoSize = true;
            lblInfoPricematching.Font = new Font("Arial Narrow", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblInfoPricematching.ForeColor = Color.DimGray;
            lblInfoPricematching.Image = (Image)resources.GetObject("lblInfoPricematching.Image");
            lblInfoPricematching.ImageAlign = ContentAlignment.MiddleLeft;
            lblInfoPricematching.Location = new Point(2, 126);
            lblInfoPricematching.Name = "lblInfoPricematching";
            lblInfoPricematching.Size = new Size(337, 20);
            lblInfoPricematching.TabIndex = 32;
            lblInfoPricematching.Text = "     The below result are having the discrepancy in the Price .";
            lblInfoPricematching.TextAlign = ContentAlignment.MiddleRight;
            // 
            // tabPage2
            // 
            tabPage2.BackColor = SystemColors.Window;
            tabPage2.Controls.Add(btnResetLogs);
            tabPage2.Controls.Add(btnExportLogs);
            tabPage2.Controls.Add(lblLogsSummary);
            tabPage2.Controls.Add(dgvLogs);
            tabPage2.Controls.Add(textLogs);
            tabPage2.Location = new Point(4, 59);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1929, 1323);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Logs";
            tabPage2.UseVisualStyleBackColor = false;
            // 
            // btnResetLogs
            // 
            btnResetLogs.Anchor = AnchorStyles.Right;
            btnResetLogs.BackColor = Color.FromArgb(0, 122, 204);
            btnResetLogs.Cursor = Cursors.Hand;
            btnResetLogs.Enabled = false;
            btnResetLogs.FlatAppearance.BorderSize = 0;
            btnResetLogs.FlatStyle = FlatStyle.Flat;
            btnResetLogs.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnResetLogs.ImageAlign = ContentAlignment.MiddleLeft;
            btnResetLogs.Location = new Point(959, 8);
            btnResetLogs.Name = "btnResetLogs";
            btnResetLogs.Margin = new Padding(8, 0, 0, 0);
            btnResetLogs.Padding = new Padding(8, 0, 8, 0);
            btnResetLogs.Size = new Size(90, 40);
            btnResetLogs.TabIndex = 34;
            btnResetLogs.ForeColor = Color.White;
            btnResetLogs.Text = "Reset";
            btnResetLogs.TextAlign = ContentAlignment.MiddleCenter;
            btnResetLogs.UseVisualStyleBackColor = false;
            btnResetLogs.Click += btnResetLogs_Click;
            // 
            // btnExportLogs
            // 
            btnExportLogs.Anchor = AnchorStyles.Right;
            btnExportLogs.BackColor = Color.FromArgb(0, 122, 204);
            btnExportLogs.Cursor = Cursors.Hand;
            btnExportLogs.Enabled = false;
            btnExportLogs.FlatAppearance.BorderSize = 0;
            btnExportLogs.FlatStyle = FlatStyle.Flat;
            btnExportLogs.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnExportLogs.Image = (Image)resources.GetObject("btnExportLogs.Image");
            btnExportLogs.ImageAlign = ContentAlignment.MiddleLeft;
            btnExportLogs.Location = new Point(1113, 8);
            btnExportLogs.Name = "btnExportLogs";
            btnExportLogs.Margin = new Padding(8, 0, 0, 0);
            btnExportLogs.Padding = new Padding(8, 0, 8, 0);
            btnExportLogs.Size = new Size(90, 40);
            btnExportLogs.TabIndex = 33;
            btnExportLogs.ForeColor = Color.White;
            btnExportLogs.Text = "Export";
            btnExportLogs.TextAlign = ContentAlignment.MiddleCenter;
            btnExportLogs.UseVisualStyleBackColor = false;
            btnExportLogs.Click += btnExportLogs_Click;
            //
            // lblLogsSummary
            //
            lblLogsSummary.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblLogsSummary.AutoSize = true;
            lblLogsSummary.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            lblLogsSummary.Location = new Point(3, 520);
            lblLogsSummary.Name = "lblLogsSummary";
            lblLogsSummary.Size = new Size(171, 23);
            lblLogsSummary.TabIndex = 36;
            lblLogsSummary.Text = "⚠ 0 Warnings, 0 Errors";
            //
            // dgvLogs
            //
            dgvLogs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvLogs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvLogs.BackgroundColor = SystemColors.Window;
            dgvLogs.BorderStyle = BorderStyle.None;
            dgvLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvLogs.EnableHeadersVisualStyles = false;
            dataGridViewCellStyle5.BackColor = Color.FromArgb(32, 93, 170);
            dataGridViewCellStyle5.ForeColor = Color.White;
            dataGridViewCellStyle5.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            dgvLogs.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            dataGridViewCellStyle6.BackColor = Color.FromArgb(245, 249, 255);
            dgvLogs.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle6;
            dgvLogs.Location = new Point(3, 32);
            dgvLogs.Name = "dgvLogs";
            dgvLogs.ReadOnly = true;
            dgvLogs.RowHeadersVisible = false;
            dgvLogs.DataBindingComplete += dgvLogs_DataBindingComplete;
            dgvLogs.Size = new Size(1923, 530);
            dgvLogs.TabIndex = 35;
            //
            // textLogs
            //
            textLogs.BackColor = Color.White;
            textLogs.Dock = DockStyle.Bottom;
            textLogs.Location = new Point(3, 544);
            textLogs.Name = "textLogs";
            textLogs.Size = new Size(1923, 776);
            textLogs.TabIndex = 2;
            textLogs.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1280, 800);
            Controls.Add(tbcMenu);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.Sizable;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "Reconciliation Tool";
            WindowState = FormWindowState.Normal;
            Load += Form1_Load;
            panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            tbcMenu.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgResultdata).EndInit();
            ((System.ComponentModel.ISupportInitialize)LogoMsbhub).EndInit();
            ((System.ComponentModel.ISupportInitialize)logoMicrosoft).EndInit();
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgAzurePriceMismatch).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox7).EndInit();
            tabPage2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Panel panel2;
        private Label label1;
        private Button btnClose;
        private Button btnMaximized;
        private Button btnImportSixDotOneFile;
        private PictureBox LogoMsbhub;
        private Label lblMsbhubrowscountLabel;
        private Label lblMsbhubfilenameLabel;
        private DataGridView dgSixDotOnedFileData;
        private Label lblVersion;
        private TabControl tbcMenu;
        private DataGridView dgResultdata;
        private Label lblSixDotOneFileName;
        private Label lblSixDotOneFileRowCount;
        private Label lblEmptyMessage;
        private TabPage tabPage1;
        private Label lblMicrosoftFileRowCount;
        private Label lblMicrosoftFileName;
        private DataGridView dgMicrosoftFileData;
        private Button btnImportMicrosoft;
        private PictureBox logoMicrosoft;
        private Label lblMicrosoftFilenameLabel;
        private Label lblMicrosoftRowsCountLabel;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private Label lblDiscrepancyTitle;
        private Label lblExternal1DiscrepancyMsg;
        private Button btnExportToCsv;
        private Button btnCompare;
        private CheckBox chkFuzzyColumns;
        private TabPage tabPage2;
        private Label label3;
        private RadioButton rbInternal;
        private RadioButton rbExternal;
        private Label lblInternal1DiscrepancyMsg;
        private Button btnReset;
        private Label lblExternal2DiscrepancyMsg;
        private Label lblInternal2DiscrepancyMsg;
        private DataGridView dgvLogs;
        private Label lblLogsSummary;
        private Label lblMismatchSummary;
        private TextBox txtFieldFilter;
        private TextBox txtExplanationFilter;
        private RichTextBox textLogs;
        private Button btnExportLogs;
        private Button btnResetLogs;
        private TabPage tabPage3;
        private Label lblSixdotonefilenameTab4;
        private Label lblMicrosoftfilenameTab4;
        private PictureBox pictureBox7;
        private Label lblInfoPricematching;
        private DataGridView dgAzurePriceMismatch;
        private Label lblPricematchingEmptyMessage;
        private PictureBox pictureBox1;
        private Button btnPriceMismatchingExportToCsv;
        private Label label5;
        private PictureBox pictureBox2;
        private SplitContainer splitMain;
        private Button btnToggleFiles;

        private void Form1_Load(object sender, EventArgs e)
        {
            Font = new Font("Segoe UI", 9F);
            dgResultdata.DefaultCellStyle.Font = new Font("Consolas", 9F);
            dgAzurePriceMismatch.DefaultCellStyle.Font = new Font("Consolas", 9F);
            dgvLogs.DefaultCellStyle.Font = new Font("Consolas", 9F);
        }
    }
}
