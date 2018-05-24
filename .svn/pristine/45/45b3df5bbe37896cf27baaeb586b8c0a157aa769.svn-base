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

            

        }

        #region Test the code
        List<string> BuildLogs;

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
                for (int i = 0; i < BuildLogs.Count; i++)
                {
                    MessageBox.Show(BuildLogs[i], "LOG: Device " + i.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        #endregion




    }
}
