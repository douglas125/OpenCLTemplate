using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Cloo;

namespace OpenCLTemplate
{
    /// <summary>Displays OpenCL related information</summary>
    public partial class frmCLInfo : Form
    {
        private void frmCLInfo_Load(object sender, EventArgs e)
        {
            CLCalc.InitCL(ComputeDeviceTypes.All);

            if (CLCalc.CLAcceleration != CLCalc.CLAccelerationType.UsingCL)
            {
                cmbPlat.Items.Add("OpenCL ERROR");
                if (cmbPlat.Items.Count > 0) cmbPlat.SelectedIndex = 0;
            }
            else
            {
                foreach(ComputePlatform p in CLCalc.CLPlatforms)
                {
                    cmbPlat.Items.Add(p.Name + " " + p.Profile + " " + p.Vendor + " " + p.Version);
                }
                if (cmbPlat.Items.Count > 0) cmbPlat.SelectedIndex = 0;

                int i=0;
                foreach (ComputeDevice d in CLCalc.CLDevices)
                {
                    //if (d.CLDeviceAvailable)
                    //{
                        cmbDevices.Items.Add(d.Name + " " + d.Type + " " + d.Vendor + " " + d.Version);
                        cmbCurDevice.Items.Add(d.Name + " " + d.Type + " " + d.Vendor + " " + d.Version);
                    //}
                    //else
                    //{
                    //    cmbDevices.Items.Add("NOT AVAILABLE: " + d.CLDeviceName + " " + d.CLDeviceType + " " + d.CLDeviceVendor + " " + d.CLDeviceVersion);
                    //    cmbCurDevice.Items.Add("NOT AVAILABLE: " + d.CLDeviceName + " " + d.CLDeviceType + " " + d.CLDeviceVendor + " " + d.CLDeviceVersion);
                    //}

                    i++;
                }

                if (cmbDevices.Items.Count > 0)
                {
                    cmbDevices.SelectedIndex = 0;
                    cmbCurDevice.SelectedIndex = CLCalc.Program.DefaultCQ;
                }
            }

            ReadImportantRegistryEntries();



            //int[] n = new int[3] {1,1,1};
            //int[] nn = new int[3];
            //CLCalc.Program.Variable v = new CLCalc.Program.Variable(n);

            //v.WriteToDevice(n);

            //v.ReadFromDeviceTo(nn);

            string s = @" kernel void teste() {}";

            CLCalc.Program.Compile(s);
            try
            {
                CLCalc.Program.Kernel k = new CLCalc.Program.Kernel("teste");
            }
            catch
            {
                MessageBox.Show("");
            }
        }

        private void ReadImportantRegistryEntries()
        {
            //Reads registry keys
            Utility.ModifyRegistry.ModifyRegistry reg = new Utility.ModifyRegistry.ModifyRegistry();
            reg.SubKey = "SYSTEM\\CURRENTCONTROLSET\\CONTROL\\SESSION MANAGER\\ENVIRONMENT";
            try
            {
                string s = (string)reg.Read("GPU_MAX_HEAP_SIZE");
                lblGPUHeap.Text = s == null ? lblNotFound.Text : s;
            }
            catch
            {
                lblGPUHeap.Text = lblNotFound.Text;
            }

            reg.SubKey = "SYSTEM\\CURRENTCONTROLSET\\CONTROL\\GraphicsDrivers";
            int val;
            try
            {
                val = (int)reg.Read("TdrDelay");
                lblTdrDelay.Text = val.ToString();
            }
            catch
            {
                lblTdrDelay.Text = lblNotFound.Text;
            }

            try
            {
                val = (int)reg.Read("TdrDdiDelay");
                lblTdrDdiDelay.Text = val.ToString();
            }
            catch
            {
                lblTdrDdiDelay.Text = lblNotFound.Text;
            }

            long size = 32;
            for (int i = 0; i < CLCalc.CLDevices.Count; i++)
            {
                if (CLCalc.CLDevices[i].Type == ComputeDeviceTypes.Gpu || CLCalc.CLDevices[i].Type == ComputeDeviceTypes.Accelerator )
                {
                    if (CLCalc.CLDevices[i].GlobalMemorySize > size)
                        size = CLCalc.CLDevices[i].GlobalMemorySize / (1024 * 1024);
                }
            }

            lblRecomHeapSize.Text = "90";// size.ToString();
            lblRecomTdrDdiDelay.Text = "256";
            lblRecomTdrDelay.Text = "128";

        }

        private void btnWriteToRegistry_Click(object sender, EventArgs e)
        {
            string msg = lblConfirmModReg.Text + "\n";
            msg += "HKEY_LOCAL_MACHINE\\SYSTEM\\CURRENTCONTROLSET\\CONTROL\\SESSION MANAGER\\ENVIRONMENT - GPU_MAX_HEAP_SIZE = " + lblRecomHeapSize.Text + "\n";
            msg += "HKEY_LOCAL_MACHINE\\SYSTEM\\CURRENTCONTROLSET\\CONTROL\\GraphicsDrivers - TdrDelay = " + lblTdrDelay.Text + "\n";
            msg += "HKEY_LOCAL_MACHINE\\SYSTEM\\CURRENTCONTROLSET\\CONTROL\\GraphicsDrivers - TdrDdiDelay = " + lblTdrDdiDelay.Text + "\n";

            if (MessageBox.Show(msg, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Utility.ModifyRegistry.ModifyRegistry reg = new Utility.ModifyRegistry.ModifyRegistry();
                reg.SubKey = "SYSTEM\\CURRENTCONTROLSET\\CONTROL\\SESSION MANAGER\\ENVIRONMENT";
                try
                {
                    reg.Write("GPU_MAX_HEAP_SIZE", lblRecomHeapSize.Text);
                }
                catch { }

                reg.SubKey = "SYSTEM\\CURRENTCONTROLSET\\CONTROL\\GraphicsDrivers";
                int val;
                try
                {
                    int.TryParse(lblRecomTdrDelay.Text, out val);
                    reg.Write("TdrDelay", val);
                }
                catch { }

                try
                {
                    int.TryParse(lblRecomTdrDdiDelay.Text, out val);
                    reg.Write("TdrDdiDelay", val);
                }
                catch { }

                ReadImportantRegistryEntries();

                MessageBox.Show(lblReboot.Text, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void cmbDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ind = cmbDevices.SelectedIndex;
            lstDevDetails.Items.Clear();
            ComputeDevice d = CLCalc.CLDevices[ind];
            lstDevDetails.Items.Add("Name: " + d.Name);
            lstDevDetails.Items.Add("Type: " + d.Type);
            lstDevDetails.Items.Add("Vendor: " + d.Vendor);
            lstDevDetails.Items.Add("Version: " + d.Version);
            lstDevDetails.Items.Add("Memory size (Mb): " + d.GlobalMemorySize/(1024*1024));
            lstDevDetails.Items.Add("Maximum allocation size (Mb):" + d.MaxMemoryAllocationSize / (1024 * 1024));
            lstDevDetails.Items.Add("Compiler available? " + d.CompilerAvailable);
            lstDevDetails.Items.Add("Device available? " + d.Available);

            lstDevDetails.Items.Add("  ");
            lstDevDetails.Items.Add("Extensions: ");
            foreach (string s in d.Extensions)
            {
                lstDevDetails.Items.Add(s);
            }
            //lstDevDetails.Items.Add("Device available? " + d.CLDeviceAvailable);
        }


        /// <summary>Constructor.</summary>
        public frmCLInfo()
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(System.Globalization.CultureInfo.CurrentCulture.LCID);
            InitializeComponent();
        }

        private void frmCLInfo_DoubleClick(object sender, EventArgs e)
        {
            //frmCLEdit frmEdit = new frmCLEdit();
            //frmEdit.ShowDialog();
        }

        private void cmbCurDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            CLCalc.Program.DefaultCQ = cmbCurDevice.SelectedIndex;
        }



    }
}
