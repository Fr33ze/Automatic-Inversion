namespace AutomaticInversionBot
{
    partial class Form1
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.part1btn = new System.Windows.Forms.Button();
            this.part2btn = new System.Windows.Forms.Button();
            this.startIdx = new System.Windows.Forms.NumericUpDown();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.startIdx)).BeginInit();
            this.SuspendLayout();
            // 
            // part1btn
            // 
            this.part1btn.Location = new System.Drawing.Point(12, 12);
            this.part1btn.Margin = new System.Windows.Forms.Padding(4);
            this.part1btn.Name = "part1btn";
            this.part1btn.Size = new System.Drawing.Size(274, 87);
            this.part1btn.TabIndex = 0;
            this.part1btn.Text = "Trim Data";
            this.part1btn.UseVisualStyleBackColor = true;
            this.part1btn.Click += new System.EventHandler(this.part1btn_Click);
            // 
            // part2btn
            // 
            this.part2btn.Location = new System.Drawing.Point(292, 12);
            this.part2btn.Margin = new System.Windows.Forms.Padding(4);
            this.part2btn.Name = "part2btn";
            this.part2btn.Size = new System.Drawing.Size(278, 87);
            this.part2btn.TabIndex = 1;
            this.part2btn.Text = "Create XYZ";
            this.part2btn.UseVisualStyleBackColor = true;
            this.part2btn.Click += new System.EventHandler(this.part2btn_Click);
            // 
            // startIdx
            // 
            this.startIdx.Location = new System.Drawing.Point(210, 107);
            this.startIdx.Margin = new System.Windows.Forms.Padding(4);
            this.startIdx.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.startIdx.Name = "startIdx";
            this.startIdx.Size = new System.Drawing.Size(274, 31);
            this.startIdx.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(82, 109);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(121, 25);
            this.label1.TabIndex = 3;
            this.label1.Text = "Start at file:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(582, 171);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.startIdx);
            this.Controls.Add(this.part2btn);
            this.Controls.Add(this.part1btn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Automatic Inversion Bot";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.startIdx)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button part1btn;
        private System.Windows.Forms.Button part2btn;
        private System.Windows.Forms.NumericUpDown startIdx;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Label label1;
    }
}

