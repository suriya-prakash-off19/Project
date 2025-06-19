namespace ObjectRemoverProject
{
    partial class ObjectRemovalForm
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.button1 = new System.Windows.Forms.Button();
            this.RemoveAllObject = new System.Windows.Forms.Button();
            this.RemoveObj = new System.Windows.Forms.CheckBox();
            this.PdfuploadBtn = new System.Windows.Forms.Button();
            this.ControlContainer = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.RemoveAllObject);
            this.panel1.Controls.Add(this.RemoveObj);
            this.panel1.Controls.Add(this.PdfuploadBtn);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1060, 44);
            this.panel1.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Dock = System.Windows.Forms.DockStyle.Right;
            this.button1.Location = new System.Drawing.Point(757, 0);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(101, 44);
            this.button1.TabIndex = 4;
            this.button1.Text = "Show All Regions";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // RemoveAllObject
            // 
            this.RemoveAllObject.Dock = System.Windows.Forms.DockStyle.Right;
            this.RemoveAllObject.Location = new System.Drawing.Point(858, 0);
            this.RemoveAllObject.Name = "RemoveAllObject";
            this.RemoveAllObject.Size = new System.Drawing.Size(101, 44);
            this.RemoveAllObject.TabIndex = 3;
            this.RemoveAllObject.Text = "Remove All Objects";
            this.RemoveAllObject.UseVisualStyleBackColor = true;
            this.RemoveAllObject.Click += new System.EventHandler(this.ControlContainer_Paint);
            // 
            // RemoveObj
            // 
            this.RemoveObj.AutoSize = true;
            this.RemoveObj.Location = new System.Drawing.Point(24, 15);
            this.RemoveObj.Name = "RemoveObj";
            this.RemoveObj.Size = new System.Drawing.Size(100, 17);
            this.RemoveObj.TabIndex = 1;
            this.RemoveObj.Text = "Remove Object";
            this.RemoveObj.UseVisualStyleBackColor = true;
            // 
            // PdfuploadBtn
            // 
            this.PdfuploadBtn.Dock = System.Windows.Forms.DockStyle.Right;
            this.PdfuploadBtn.Location = new System.Drawing.Point(959, 0);
            this.PdfuploadBtn.Name = "PdfuploadBtn";
            this.PdfuploadBtn.Size = new System.Drawing.Size(101, 44);
            this.PdfuploadBtn.TabIndex = 0;
            this.PdfuploadBtn.Text = "Select PDF";
            this.PdfuploadBtn.UseVisualStyleBackColor = true;
            this.PdfuploadBtn.Click += new System.EventHandler(this.selectbtn_Click);
            // 
            // ControlContainer
            // 
            this.ControlContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ControlContainer.Location = new System.Drawing.Point(0, 44);
            this.ControlContainer.Name = "ControlContainer";
            this.ControlContainer.Size = new System.Drawing.Size(1060, 576);
            this.ControlContainer.TabIndex = 1;
            // 
            // ObjectRemovalForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1060, 620);
            this.Controls.Add(this.ControlContainer);
            this.Controls.Add(this.panel1);
            this.Name = "ObjectRemovalForm";
            this.Text = "Object Removal Form";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button PdfuploadBtn;
        private System.Windows.Forms.Panel ControlContainer;
        private System.Windows.Forms.CheckBox RemoveObj;
        private System.Windows.Forms.Button RemoveAllObject;
        private System.Windows.Forms.Button button1;
    }
}

