﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DicomStrictCompare;

namespace DSC
{
    public partial class Form1 : Form
    {
        public bool tested;
        public float TightTol { get; private set; } = 1;
        public float MainTol { get; private set; } = 2;
        public float Threshold { get; private set; } = 10;
        public string SourceDirectory { get; private set; }
        public string TargetDirectory { get; private set; }
        public string SaveDirectory { get; private set; }
        public string SaveNamePrefix { get; private set; }
        public string SourceAliasName { get; private set; } = "Reference";
        public string TargetAliasName { get; private set; } = "New Model";

        public BindingList<DicomStrictCompare.Model.Dta> Dtas { get; private set; }

        private readonly DscDataHandler _dataHandler;

        private readonly BackgroundWorker worker;
        private bool _isRunning;
        /// <inheritdoc />
        public Form1()
        {
            InitializeComponent();
            Dtas = new BindingList<DicomStrictCompare.Model.Dta>();
            this.dtaListPairs.DataSource = Dtas;
            tbxSourceLabel.Text = SourceAliasName.ToString();
            tbxTargetLabel.Text = TargetAliasName.ToString();
            _dataHandler = new DscDataHandler();
            tested = false;
            worker = new BackgroundWorker
            {
                WorkerReportsProgress = true, WorkerSupportsCancellation = true
            };
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += WorkerRunWorkerCompleted;
            _isRunning = false;

        }


        private void Form1_Load(object sender, EventArgs e)
        {
            tested = false;
        }



        #region textBoxesChanged

        /// <summary>
        /// Allows user to enter a tolerance value and converts it to a class parameter 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TbxTightTol_TextChanged(object sender, EventArgs e)
        {
            tested = false;
        }

        private void TbxMainTol_TextChanged(object sender, EventArgs e)
        {
            tested = false;
        }

        private void ThreshBox_TextChanged(object sender, EventArgs e)
        {
            tested = false;
        }

        private void TbxSource_TextChanged(object sender, EventArgs e)
        {
            tested = false;
        }

        private void TbxTarget_TextChanged(object sender, EventArgs e)
        {
            tested = false;
        }


        private void TbxSaveDir_TextChanged(object sender, EventArgs e)
        {
            tested = false;
        }

        private void TbxSourceLabel_TextChanged(object sender, EventArgs e)
        {
            tested = false;
            string temp = tbxSourceLabel.Text;
            SourceAliasName = Path.GetInvalidFileNameChars().Aggregate(temp, (current, c) => current.Replace(c.ToString(), string.Empty));
            tbxSourceLabel.Text = SourceAliasName;
        }

        private void TbxTargetLabel_TextChanged(object sender, EventArgs e)
        {
            string temp = tbxTargetLabel.Text;
            TargetAliasName = Path.GetInvalidFileNameChars().Aggregate(temp, (current, c) => current.Replace(c.ToString(), string.Empty));
            tbxTargetLabel.Text = TargetAliasName;
            tested = false;
        }

        private void TbxSaveName_TextChanged(object sender, EventArgs e)
        {
            tested = false;
            string temp = tbxSaveName.Text;
            SaveNamePrefix = Path.GetInvalidFileNameChars().Aggregate(temp, (current, c) => current.Replace(c.ToString(), string.Empty));
            tbxSaveName.Text = SaveNamePrefix;
        }
        #endregion


        #region ButtonClicks

        private void BtnSourceDir_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = @"Select Source Dose Folder Location";
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    SourceDirectory = fbd.SelectedPath;
                    tbxSource.Text = SourceDirectory;
                    lblSourceFilesFound.Text = _dataHandler.CreateSourceList(SourceDirectory).ToString();
                }

            }

        }

        private void BtnTargetDir_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = @"Select Source Dose Folder Location";
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    TargetDirectory = fbd.SelectedPath;
                    tbxTarget.Text = TargetDirectory;
                    lblTargetFilesFound.Text = _dataHandler.CreateTargetList(TargetDirectory).ToString();
                }
            }
        }

        private void BtnSaveDir_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = @"Select Source Dose Folder Location";
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    SaveDirectory = fbd.SelectedPath;
                    tbxSaveDir.Text = SaveDirectory;
                }

            }
        }


        #endregion










        /// <summary>
        /// TODO add error checking here!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnExecute_Click(object sender, EventArgs e)
        {
            DicomStrictCompare.Controller.Settings settings = new DicomStrictCompare.Controller.Settings(
                chkBoxFuzzy.Checked, (float)0.25,  false, false, 1, false, Dtas.ToArray(), Threshold, chkDoseCompare.Checked, chkPDDCompare.Checked, true );
                /// TODO make Fuzzy res width gui configurable  
                /// TODO Impliment gamma
            

            if (!_isRunning)
            {
                _dataHandler.Settings = settings;
                _dataHandler.SourceAliasName = SourceAliasName;
                _dataHandler.TargetAliasName = TargetAliasName;
                


                if (tested == false)
                {
                    TestDirectories_Click(sender, e);
                }
                if (tested == false)
                {
                    lblRunStatus.Text = "Tested text Fields, Please Rerun";
                    return;
                }
                if (!chkPDDCompare.Checked && !chkDoseCompare.Checked)
                {
                    lblRunStatus.Text = "Nothing to do";
                    return;
                }



                worker.RunWorkerAsync();
                return;
            }
            else
                worker.CancelAsync();
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            _isRunning = true;
            _dataHandler.Run(chkDoseCompare.Checked, chkPDDCompare.Checked, SaveDirectory, sender);

            if (chkDoseCompare.Checked == true)
            {
                SaveFile saveFile = new SaveFile(SaveNamePrefix, SaveDirectory);
                saveFile.Save(_dataHandler.ResultMessage);
            }
        }

        void Worker_ProgressChanged (object sender, ProgressChangedEventArgs e)
        {
            doseProgressBar.Value = e.ProgressPercentage;
            lblRunStatus.Text = e.UserState.ToString();
        }

        void WorkerRunWorkerCompleted (object sender, RunWorkerCompletedEventArgs e)
        {
            _isRunning = false;
            lblRunStatus.Text = "Finished";
            doseProgressBar.Value = doseProgressBar.Maximum;

            if(e.Error != null)
            {
                _ = System.Windows.Forms.MessageBox.Show(e.Error.ToString());
            }

        }










        private void TestDirectories_Click(object sender, EventArgs e)
        {
            try
            {
                SaveDirectory = tbxSaveDir.Text;
                if (!Directory.Exists(SaveDirectory))
                    SaveDirectory = null;
                tbxSaveDir.Text = SaveDirectory;

                TargetDirectory = tbxTarget.Text;
                if (!Directory.Exists(TargetDirectory))
                    TargetDirectory = null;
                tbxTarget.Text = TargetDirectory;
                lblTargetFilesFound.Text = _dataHandler.CreateTargetList(TargetDirectory).ToString();

                SourceDirectory = tbxSource.Text;
                if (!Directory.Exists(SourceDirectory))
                    SourceDirectory = null;
                tbxSource.Text = SourceDirectory;
                lblSourceFilesFound.Text = _dataHandler.CreateSourceList(SourceDirectory).ToString();
            }
            catch (ArgumentNullException )
            {
                _ = System.Windows.Forms.MessageBox.Show("One of the directory fields is either empty or invalid");
                return;
            }
            tested = true;
        }

        private void dtaListPairs_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnDAadd_Click(object sender, EventArgs e)
        {
            double distance = 0;
            if (txtBoxDAdta.TextLength > 0)
            {
                distance = Convert.ToDouble(txtBoxDAdta.Text);
            }

            bool isMM;

            if(units.SelectedItem == null)
            {
                isMM = false;
            }
            else
            {
                isMM = units.SelectedItem.ToString() == "mm" ? true : false;
            }


            var temp = new DicomStrictCompare.Model.Dta( isMM
                , Convert.ToDouble(txtBoxDAthres.Text)/100
                , Convert.ToDouble(txtBoxDAtol.Text)/100
                , distance, chkBoxDArel.Checked, Convert.ToInt32(txtBxTrim.Text));
            Dtas.Add(temp);

            txtBoxDAdta.Clear();
            txtBoxDAthres.Clear();
            txtBoxDAtol.Clear();
            units.ClearSelected();
            chkBoxDArel.Checked = false ;

        }
    }
}
