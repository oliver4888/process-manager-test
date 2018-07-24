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
        TextBox consoleOutput;
        DirectoryInfo rustDir = new DirectoryInfo(@"C:\Users\Oliver\Desktop\process-manager-test-stuff\rust");
        DirectoryInfo fivemDir = new DirectoryInfo(@"C:\Users\Oliver\Desktop\process-manager-test-stuff\fivem");
        DirectoryInfo steamCmdDir = new DirectoryInfo(@"G:\steamcmd");
        IDictionary<string, Process> processDict = new Dictionary<string, Process>();

        public MainForm()
        {
            processDict.Add("rustProcess", null);
            processDict.Add("steamCmdProcess", null);
            InitializeComponent();
            consoleOutput = Controls.OfType<TextBox>().FirstOrDefault();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(() => UpdateGame(rustDir.FullName, "258550")));
            thread.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            KillAll();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            KillAll(true);
        }

        private bool? UpdateGame(string gameDir, string appId)
        {
            if (processDict["steamCmdProcess"] != null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(gameDir) || string.IsNullOrWhiteSpace(appId) || !Directory.Exists(gameDir))
            {
                return null;
            }
            try
            {
                processDict["steamCmdProcess"] = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = Path.Combine(steamCmdDir.FullName, "steamcmd.exe"),
                        Arguments = string.Format(
                            @"+login anonymous +force_install_dir {0} +app_update {1} +quit",
                            gameDir,
                            appId
                            ),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                processDict["steamCmdProcess"].Start();
                AppendTextBox(consoleOutput, processDict["steamCmdProcess"].StartInfo.FileName + " " + processDict["steamCmdProcess"].StartInfo.Arguments + Environment.NewLine);
                while (!processDict["steamCmdProcess"].StandardOutput.EndOfStream)
                {
                    AppendTextBox(consoleOutput, processDict["steamCmdProcess"].StandardOutput.ReadLine() + Environment.NewLine);
                }

                if (!processDict["steamCmdProcess"].HasExited)
                {
                    processDict["steamCmdProcess"].Kill();
                }
                processDict["steamCmdProcess"] = null;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " : " + ex.StackTrace);
                return null;
            }
        }

        private void AppendTextBox(TextBox textBox, string value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<TextBox, string>(AppendTextBox), new object[] { textBox, value });
                return;
            }
            textBox?.AppendText(value);
        }

        private void KillAll(bool silent = false)
        {
            bool killedAProcess = false;
            foreach (KeyValuePair<string, Process> entry in processDict)
            {
                if (entry.Value != null && !entry.Value.HasExited)
                {
                    entry.Value.Kill();
                    killedAProcess = true;
                }
            }

            if (!silent)
            {
                string output = string.Empty;
                if (killedAProcess)
                {
                    output = "Killed all running processes!";
                }
                else
                {
                    output = "There are no running processes!";
                }
                consoleOutput.AppendText(output + Environment.NewLine);
            }
        }
    }
}
