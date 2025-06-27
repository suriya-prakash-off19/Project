namespace ObjectRemoverProject
{
    partial class PDFViewerForm
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
            this.ContainerPanel = new System.Windows.Forms.Panel();
            this.HeaderPanel = new System.Windows.Forms.Panel();
            this.ResetBtn = new System.Windows.Forms.Button();
            this.RemoveObjectCheck = new System.Windows.Forms.CheckBox();
            this.LoadPdfBtn = new System.Windows.Forms.Button();
            this.HeaderPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // ContainerPanel
            // 
            this.ContainerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ContainerPanel.Location = new System.Drawing.Point(0, 58);
            this.ContainerPanel.Name = "ContainerPanel";
            this.ContainerPanel.Size = new System.Drawing.Size(800, 392);
            this.ContainerPanel.TabIndex = 3;
            // 
            // HeaderPanel
            // 
            this.HeaderPanel.Controls.Add(this.ResetBtn);
            this.HeaderPanel.Controls.Add(this.RemoveObjectCheck);
            this.HeaderPanel.Controls.Add(this.LoadPdfBtn);
            this.HeaderPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.HeaderPanel.Location = new System.Drawing.Point(0, 0);
            this.HeaderPanel.Name = "HeaderPanel";
            this.HeaderPanel.Size = new System.Drawing.Size(800, 58);
            this.HeaderPanel.TabIndex = 2;
            // 
            // ResetBtn
            // 
            this.ResetBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ResetBtn.Location = new System.Drawing.Point(575, 14);
            this.ResetBtn.Name = "ResetBtn";
            this.ResetBtn.Size = new System.Drawing.Size(105, 29);
            this.ResetBtn.TabIndex = 2;
            this.ResetBtn.Text = "Reset";
            this.ResetBtn.UseVisualStyleBackColor = true;
            this.ResetBtn.Click += new System.EventHandler(this.ResetBtnClicked);
            // 
            // RemoveObjectCheck
            // 
            this.RemoveObjectCheck.AutoSize = true;
            this.RemoveObjectCheck.Location = new System.Drawing.Point(26, 22);
            this.RemoveObjectCheck.Name = "RemoveObjectCheck";
            this.RemoveObjectCheck.Size = new System.Drawing.Size(100, 17);
            this.RemoveObjectCheck.TabIndex = 1;
            this.RemoveObjectCheck.Text = "Remove Object";
            this.RemoveObjectCheck.UseVisualStyleBackColor = true;
            // 
            // LoadPdfBtn
            // 
            this.LoadPdfBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LoadPdfBtn.Location = new System.Drawing.Point(689, 14);
            this.LoadPdfBtn.Name = "LoadPdfBtn";
            this.LoadPdfBtn.Size = new System.Drawing.Size(101, 30);
            this.LoadPdfBtn.TabIndex = 0;
            this.LoadPdfBtn.Text = "Load Pdf";
            this.LoadPdfBtn.UseVisualStyleBackColor = true;
            this.LoadPdfBtn.Click += new System.EventHandler(this.LoadPdfBtnClicked);
            // 
            // PDFViewerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.ContainerPanel);
            this.Controls.Add(this.HeaderPanel);
            this.Name = "PDFViewerForm";
            this.Text = "PDFViewerForm";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.HeaderPanel.ResumeLayout(false);
            this.HeaderPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel ContainerPanel;
        private System.Windows.Forms.Panel HeaderPanel;
        private System.Windows.Forms.CheckBox RemoveObjectCheck;
        private System.Windows.Forms.Button LoadPdfBtn;
        private System.Windows.Forms.Button ResetBtn;
    }
}