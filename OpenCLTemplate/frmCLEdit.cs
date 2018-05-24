using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OpenCLTemplate
{
    /// <summary>OpenCL Helper Editor</summary>
    public partial class frmCLEdit : Form
    {
        /// <summary>Constructor.</summary>
        public frmCLEdit()
        {
            InitializeComponent();
        }

        private void frmCLEdit_Load(object sender, EventArgs e)
        {
            OpenCLRTBController CLCtrl = new OpenCLRTBController(rTBCLCode);
            foreach (OpenCLRTBController.StringsToMark stm in CLCtrl.OpenCLStrings)
            {
                TreeNode t = new TreeNode(stm.Description);
                foreach (string s in stm.Strings)
                {
                    TreeNode t2 = new TreeNode(s);
                    t2.ForeColor = stm.StringsColor;
                    t2.NodeFont = stm.StringsFont;
                    t.Nodes.Add(t2);
                }
                treeCLRef.Nodes.Add(t);
                t.ForeColor = stm.StringsColor;
                t.NodeFont = stm.StringsFont;
            }
            positionUpdateTimer = new Timer();
            positionUpdateTimer.Tick += new EventHandler(updateCursorPositionInfo);
            positionUpdateTimer.Enabled = true;
            positionUpdateTimer.Interval = 200;
        }

        #region Test the code
        List<string> BuildLogs;
        Form logsWindow;
        RichTextBox logsBox;
        Timer positionUpdateTimer;

        private void rTBCLCode_KeyDown(object sender, KeyEventArgs e)
        {
            //F5 tests the code
            if (e.KeyCode == Keys.F5)
            {
                btnCompileTest_Click(sender, new EventArgs());
            }
        }

        /// <summary>Button to test code</summary>
        private void btnCompileTest_Click(object sender, EventArgs e)
        {
            this.TopMost = true;
            if (CLCalc.CLAcceleration == CLCalc.CLAccelerationType.Unknown)
            {
                CLCalc.InitCL();
            }
            
            try
            {
                CLCalc.Program.Compile(rTBCLCode.Text, out BuildLogs);
                btnCompileTest.BackColor = Color.Green;
            }
            catch
            {
                btnCompileTest.BackColor = Color.Red;
            }

            this.TopMost = false;
        }

        private void btnLogs_Click(object sender, EventArgs e)
        {
            if (BuildLogs != null)
            {
                #region Creating the logs window
                if (logsWindow == null || logsWindow.IsDisposed)
                {
                    logsWindow = new Form();
                    Panel logsGroupBox;
                    logsWindow.FormClosing += frmCLEdit_LogsFormClosing;

                    logsBox = new System.Windows.Forms.RichTextBox();
                    logsGroupBox = new System.Windows.Forms.Panel();
                    // 
                    // logsGroupBox
                    // 
                    logsGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                | System.Windows.Forms.AnchorStyles.Left)
                                | System.Windows.Forms.AnchorStyles.Right)));
                    logsGroupBox.Controls.Add(logsBox);
                    logsGroupBox.Location = new System.Drawing.Point(12, 12);
                    logsGroupBox.Name = "logsGroupBox";
                    logsGroupBox.Size = new System.Drawing.Size(524, 533);
                    logsGroupBox.TabIndex = 0;
                    // 
                    // logsBox
                    // 
                    logsBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                | System.Windows.Forms.AnchorStyles.Left)
                                | System.Windows.Forms.AnchorStyles.Right)));
                    logsBox.BackColor = System.Drawing.Color.White;
                    logsBox.ReadOnly = true;
                    logsBox.Font = new System.Drawing.Font("Courier New", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    logsBox.ForeColor = System.Drawing.Color.Black;
                    logsBox.Font = new System.Drawing.Font("Courier New", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    logsBox.Location = new System.Drawing.Point(3, 3);
                    logsBox.Name = "logsBox";
                    logsBox.Size = new System.Drawing.Size(518, 527);
                    logsBox.TabIndex = 0;
                    logsBox.Text = "";
                    // 
                    // logsWindow
                    // 
                    logsWindow.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
                    logsWindow.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
                    logsWindow.ClientSize = new System.Drawing.Size(548, 557);
                    logsWindow.Controls.Add(logsGroupBox);
                    logsWindow.Name = "logsWindow";
                    logsWindow.Text = "Build Logs";
                }
                #endregion
                logsBox.Clear();
                for (int i = 0; i < BuildLogs.Count; i++)
                {
                    logsBox.AppendText(BuildLogs[i]);
                    logsWindow.Text = "LOG: Device " + i.ToString();
                }
                logsWindow.Show();
            }
        }

        private void frmCLEdit_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (logsWindow != null)
            {
                logsWindow.Hide();
                logsWindow.Dispose();
            }
        }

        private void frmCLEdit_LogsFormClosing(object sender, FormClosingEventArgs e)
        {
            if (logsWindow == null) return;
            logsWindow.Hide();
        }
        #endregion

        private void updateCursorPositionInfo(Object myObject, EventArgs myEventArgs)
        {
            // Get the line.
            int index = rTBCLCode.SelectionStart;
            int line = rTBCLCode.GetLineFromCharIndex(index);

            // Get the column.
            int firstChar = rTBCLCode.GetFirstCharIndexFromLine(line);
            int column = index - firstChar;

            toolStripStatusLabel.Text = "Ln " + line + "   Col " + column;
        }
    }
}
