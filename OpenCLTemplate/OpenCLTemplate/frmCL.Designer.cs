namespace OpenCLTemplate
{
    partial class frmCLInfo
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmCLInfo));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lstDevDetails = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbCurDevice = new System.Windows.Forms.ComboBox();
            this.cmbDevices = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cmbPlat = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnWriteToRegistry = new System.Windows.Forms.Button();
            this.lblRecomTdrDdiDelay = new System.Windows.Forms.Label();
            this.lblTdrDdiDelay = new System.Windows.Forms.Label();
            this.lblRecomTdrDelay = new System.Windows.Forms.Label();
            this.lblTdrDelay = new System.Windows.Forms.Label();
            this.lblRecomHeapSize = new System.Windows.Forms.Label();
            this.lblConfirmModReg = new System.Windows.Forms.Label();
            this.lblReboot = new System.Windows.Forms.Label();
            this.lblNotFound = new System.Windows.Forms.Label();
            this.lblGPUHeap = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.lstDevDetails);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.cmbCurDevice);
            this.groupBox1.Controls.Add(this.cmbDevices);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.cmbPlat);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // lstDevDetails
            // 
            resources.ApplyResources(this.lstDevDetails, "lstDevDetails");
            this.lstDevDetails.FormattingEnabled = true;
            this.lstDevDetails.Name = "lstDevDetails";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // cmbCurDevice
            // 
            resources.ApplyResources(this.cmbCurDevice, "cmbCurDevice");
            this.cmbCurDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCurDevice.FormattingEnabled = true;
            this.cmbCurDevice.Name = "cmbCurDevice";
            this.cmbCurDevice.SelectedIndexChanged += new System.EventHandler(this.cmbCurDevice_SelectedIndexChanged);
            // 
            // cmbDevices
            // 
            resources.ApplyResources(this.cmbDevices, "cmbDevices");
            this.cmbDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDevices.FormattingEnabled = true;
            this.cmbDevices.Name = "cmbDevices";
            this.cmbDevices.SelectedIndexChanged += new System.EventHandler(this.cmbDevices_SelectedIndexChanged);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // cmbPlat
            // 
            resources.ApplyResources(this.cmbPlat, "cmbPlat");
            this.cmbPlat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPlat.FormattingEnabled = true;
            this.cmbPlat.Name = "cmbPlat";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.btnWriteToRegistry);
            this.groupBox2.Controls.Add(this.lblRecomTdrDdiDelay);
            this.groupBox2.Controls.Add(this.lblTdrDdiDelay);
            this.groupBox2.Controls.Add(this.lblRecomTdrDelay);
            this.groupBox2.Controls.Add(this.lblTdrDelay);
            this.groupBox2.Controls.Add(this.lblRecomHeapSize);
            this.groupBox2.Controls.Add(this.lblConfirmModReg);
            this.groupBox2.Controls.Add(this.lblReboot);
            this.groupBox2.Controls.Add(this.lblNotFound);
            this.groupBox2.Controls.Add(this.lblGPUHeap);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // btnWriteToRegistry
            // 
            resources.ApplyResources(this.btnWriteToRegistry, "btnWriteToRegistry");
            this.btnWriteToRegistry.Name = "btnWriteToRegistry";
            this.btnWriteToRegistry.UseVisualStyleBackColor = true;
            this.btnWriteToRegistry.Click += new System.EventHandler(this.btnWriteToRegistry_Click);
            // 
            // lblRecomTdrDdiDelay
            // 
            resources.ApplyResources(this.lblRecomTdrDdiDelay, "lblRecomTdrDdiDelay");
            this.lblRecomTdrDdiDelay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblRecomTdrDdiDelay.Name = "lblRecomTdrDdiDelay";
            // 
            // lblTdrDdiDelay
            // 
            resources.ApplyResources(this.lblTdrDdiDelay, "lblTdrDdiDelay");
            this.lblTdrDdiDelay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTdrDdiDelay.Name = "lblTdrDdiDelay";
            // 
            // lblRecomTdrDelay
            // 
            resources.ApplyResources(this.lblRecomTdrDelay, "lblRecomTdrDelay");
            this.lblRecomTdrDelay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblRecomTdrDelay.Name = "lblRecomTdrDelay";
            // 
            // lblTdrDelay
            // 
            resources.ApplyResources(this.lblTdrDelay, "lblTdrDelay");
            this.lblTdrDelay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTdrDelay.Name = "lblTdrDelay";
            // 
            // lblRecomHeapSize
            // 
            resources.ApplyResources(this.lblRecomHeapSize, "lblRecomHeapSize");
            this.lblRecomHeapSize.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblRecomHeapSize.Name = "lblRecomHeapSize";
            // 
            // lblConfirmModReg
            // 
            this.lblConfirmModReg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.lblConfirmModReg, "lblConfirmModReg");
            this.lblConfirmModReg.Name = "lblConfirmModReg";
            // 
            // lblReboot
            // 
            this.lblReboot.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.lblReboot, "lblReboot");
            this.lblReboot.Name = "lblReboot";
            // 
            // lblNotFound
            // 
            this.lblNotFound.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.lblNotFound, "lblNotFound");
            this.lblNotFound.Name = "lblNotFound";
            // 
            // lblGPUHeap
            // 
            resources.ApplyResources(this.lblGPUHeap, "lblGPUHeap");
            this.lblGPUHeap.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGPUHeap.Name = "lblGPUHeap";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // frmCLInfo
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "frmCLInfo";
            this.Load += new System.EventHandler(this.frmCLInfo_Load);
            this.DoubleClick += new System.EventHandler(this.frmCLInfo_DoubleClick);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox cmbPlat;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbDevices;
        private System.Windows.Forms.ListBox lstDevDetails;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cmbCurDevice;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblTdrDdiDelay;
        private System.Windows.Forms.Label lblTdrDelay;
        private System.Windows.Forms.Label lblGPUHeap;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label lblRecomTdrDdiDelay;
        private System.Windows.Forms.Label lblRecomTdrDelay;
        private System.Windows.Forms.Label lblRecomHeapSize;
        private System.Windows.Forms.Button btnWriteToRegistry;
        private System.Windows.Forms.Label lblNotFound;
        private System.Windows.Forms.Label lblConfirmModReg;
        private System.Windows.Forms.Label lblReboot;
    }
}

