using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace process_manager_test
{
    public partial class MainForm : Form
    {
        private IDictionary<string, Process> processDict = new Dictionary<string, Process>();

        private SynchronizationContext _syncContext;

        /********************************************************************************************************************/
        // Form Methods

        public MainForm()
        {
            _syncContext = SynchronizationContext.Current;
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(() => UpdateGame(txtGameDirectory.Text, txtSteamAppId.Text)));
            thread.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            KillAll();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            KillAll(true, true);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtConsoleOutput.Clear();
        }

        private void btnForceStop_Click(object sender, EventArgs e)
        {
            KillAll(true);
        }

        private void txtRustDirectory_DoubleClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    txtGameDirectory.Text = fbd.SelectedPath;
                }
            }
        }

        private void txtSteamCMDDirectory_DoubleClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    txtSteamCMDDirectory.Text = fbd.SelectedPath;
                }
            }
        }

        /********************************************************************************************************************/
        // Other methods

        private void UpdateGame(string gameDir, string appId)
        {
            if (string.IsNullOrWhiteSpace(gameDir) || string.IsNullOrWhiteSpace(appId) || !Directory.Exists(gameDir))
            {
                return;
            }

            string dictId = $"{gameDir}:{appId}";

            if (processDict.Where(k => k.Key == dictId).SingleOrDefault().Value != null)
            {
                return;
            }

            try
            {
                Process p = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = Path.Combine(txtSteamCMDDirectory.Text, "steamcmd.exe"),
                        Arguments = $"+login anonymous +force_install_dir {gameDir} +app_update {appId} +quit",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true
                    },
                    EnableRaisingEvents = true
                };

                processDict[dictId] = p;

                p.OutputDataReceived += (sender, args) => AppendTextBox(txtConsoleOutput, args.Data);
                p.ErrorDataReceived += (sender, args) => AppendTextBox(txtConsoleOutput, args.Data);

                AppendTextBox(txtConsoleOutput, $"{p.StartInfo.FileName} {p.StartInfo.Arguments}");

                p.Start();

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                p.WaitForExit();

                processDict.Remove(dictId);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " : " + ex.StackTrace);
                return;
            }
        }

        private void AppendTextBox(TextBox textBox, string value)
        {
            if (value != null)
            {
                _syncContext.Post(_ => textBox.AppendText($"{value} {Environment.NewLine}"), null);
            }
        }

        private void KillAll(bool force = false, bool silent = false)
        {
            foreach (KeyValuePair<string, Process> entry in processDict)
            {
                if (entry.Value != null && !entry.Value.HasExited)
                {
                    if (force)
                    {
                        entry.Value.Kill();
                    }
                    else
                    {
                        entry.Value.StandardInput.Close();
                    }
                    if (!silent) {
                        AppendTextBox(txtConsoleOutput, force ? $"Process {entry.Value.Id} : {entry.Key} killed!" : $"Command sent to close process {entry.Value.Id} : {entry.Key}");
                    }
                }
            }
        }
    }
}
