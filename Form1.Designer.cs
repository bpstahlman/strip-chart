namespace RTD
{
    partial class stripChartDemoForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.stripChart = new RTD.StripChart();
            this.pauseButton = new System.Windows.Forms.CheckBox();
            this.stripChartPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // stripChart
            // 
            this.stripChart.BackColor = System.Drawing.SystemColors.Control;
            this.stripChart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.stripChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stripChart.InchesPerSecond = 0.1D;
            this.stripChart.LabelFont = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
            this.stripChart.LargeStepsPerScreen = 2;
            this.stripChart.Location = new System.Drawing.Point(0, 0);
            this.stripChart.Name = "stripChart";
            this.stripChart.RangeMax = 100D;
            this.stripChart.RangeMin = 0D;
            this.stripChart.SecondsPerSmallChange = 0.1D;
            this.stripChart.ShiftAtFraction = 0.75D;
            this.stripChart.Size = new System.Drawing.Size(957, 285);
            this.stripChart.TabIndex = 0;
            // 
            // pauseButton
            // 
            this.pauseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pauseButton.Appearance = System.Windows.Forms.Appearance.Button;
            this.pauseButton.AutoSize = true;
            this.pauseButton.Location = new System.Drawing.Point(913, 595);
            this.pauseButton.Name = "pauseButton";
            this.pauseButton.Size = new System.Drawing.Size(56, 26);
            this.pauseButton.TabIndex = 1;
            this.pauseButton.Text = "Pause";
            this.pauseButton.UseVisualStyleBackColor = true;
            this.pauseButton.CheckedChanged += new System.EventHandler(this.pauseButton_CheckedChanged);
            // 
            // stripChartPropertyGrid
            // 
            this.stripChartPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stripChartPropertyGrid.Location = new System.Drawing.Point(0, 0);
            this.stripChartPropertyGrid.Name = "stripChartPropertyGrid";
            this.stripChartPropertyGrid.Size = new System.Drawing.Size(957, 288);
            this.stripChartPropertyGrid.TabIndex = 2;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(12, 12);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.stripChartPropertyGrid);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.stripChart);
            this.splitContainer1.Size = new System.Drawing.Size(957, 577);
            this.splitContainer1.SplitterDistance = 288;
            this.splitContainer1.TabIndex = 3;
            // 
            // stripChartDemoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(981, 633);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.pauseButton);
            this.Name = "stripChartDemoForm";
            this.Text = "Strip Chart Demo";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private StripChart stripChart;
        private System.Windows.Forms.CheckBox pauseButton;
        private System.Windows.Forms.PropertyGrid stripChartPropertyGrid;
        private System.Windows.Forms.SplitContainer splitContainer1;
    }
}

