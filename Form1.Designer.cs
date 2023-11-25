namespace RTD
{
    partial class Form1
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
            this.stripChart1 = new RTD.StripChart();
            this.SuspendLayout();
            // 
            // stripChart1
            // 
            this.stripChart1.BackColor = System.Drawing.SystemColors.Control;
            this.stripChart1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.stripChart1.Location = new System.Drawing.Point(0, 0);
            this.stripChart1.Name = "stripChart1";
            this.stripChart1.Size = new System.Drawing.Size(801, 266);
            this.stripChart1.TabIndex = 0;
            this.stripChart1.YAxisLabel = null;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(892, 595);
            this.Controls.Add(this.stripChart1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private StripChart stripChart1;
    }
}

