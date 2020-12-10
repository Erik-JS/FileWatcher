using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileWatcher
{
    public partial class Form1 : Form
    {

        private static Form1 mainForm;

        private static bool stopFlag = true;

        private static List<Thread> lstThreads = new List<Thread>();

        public Form1()
        {
            InitializeComponent();
            mainForm = this;
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK)
                return;
            listBox1.Items.Add(ofd.FileName);
        }


        public static void LogText(string text, Color color)
        {
            mainForm.Invoke(new Action(() =>
            {
                mainForm.rtb.SelectionStart = mainForm.rtb.TextLength;
                mainForm.rtb.SelectionLength = 0;
                mainForm.rtb.SelectionColor = color;
                mainForm.rtb.AppendText(text + Environment.NewLine);
                mainForm.rtb.SelectionBackColor = mainForm.rtb.BackColor;
                mainForm.rtb.SelectionColor = mainForm.rtb.ForeColor;
                mainForm.rtb.SelectionStart = mainForm.rtb.TextLength;
                mainForm.rtb.SelectionLength = 0;
                mainForm.rtb.ScrollToCaret();
            }));
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnSelect.Enabled = false;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            stopFlag = false;

            lstThreads.Clear();
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                var t = new Thread(FileWatchThread);
                t.Start(listBox1.Items[i]);
                lstThreads.Add(t);
            }
        }

        public static void FileWatchThread(object param)
        {
            string fullfilename = (string)param;
            FileSystemWatcher2 fsw = null;

            try
            {
                var dir = Path.GetDirectoryName(fullfilename);
                var backupdir = Path.Combine(dir, "FileWatcherBackups");
                Directory.CreateDirectory(backupdir);

                fsw = new FileSystemWatcher2(dir);
                fsw.Filter = Path.GetFileName(fullfilename);
                fsw.NotifyFilter = NotifyFilters.LastWrite;
                fsw.Changed += FileSystemWatcher_Changed;

                fsw.EnableRaisingEvents = true;

                while (!stopFlag)
                {
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                LogText("***FileWatchThread EXCEPTION***\n" + fullfilename + " >> " + (fsw != null).ToString() + "\n" + GetExceptionMessage(ex), Color.LightCoral);
            }
            finally
            {
                if (fsw != null)
                {
                    fsw.EnableRaisingEvents = false;
                    fsw.Dispose();
                }
            }

        }

        private static void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcher2 fsw = (FileSystemWatcher2)sender;
            fsw.EnableRaisingEvents = false;
            try
            {
                //LogText(e.Name + " >> " + fsw.ControlNumber.ToString("D4"), Color.White);
                string newfilename = String.Format("{0:D4}_{1}", fsw.ControlNumber, e.Name);
                string source = Path.Combine(fsw.Path, e.Name);
                string destination = Path.Combine(fsw.Path, "FileWatcherBackups", newfilename);
                File.Copy(source, destination);
                LogText(e.Name + " >> " + fsw.ControlNumber.ToString("D4") + " copy OK", Color.Cyan);
                fsw.ControlNumber++;
            }
            catch(Exception ex)
            {
                LogText("***FileSystemWatcher_Changed EXCEPTION***\n" + e.Name + " >> " + fsw.ControlNumber.ToString("D4") + "\n" + GetExceptionMessage(ex), Color.LightCoral);
            }
            finally
            {
                fsw.EnableRaisingEvents = true;
            }
           
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnSelect.Enabled = true;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            stopFlag = true;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            stopFlag = true;
        }


        public class FileSystemWatcher2 : FileSystemWatcher
        {
            public int ControlNumber = 0;
            public object Tag = null;

            public FileSystemWatcher2(string path) : base(path)
            {
                
            }

        }

        public static string GetExceptionMessage(Exception exception, bool includeStack = false)
        {
            string message;
            message = exception.GetType().FullName + ": " + exception.Message;
            if (exception.InnerException != null)
            {
                message += "\n[InnerException] " + exception.InnerException.GetType().FullName + ": " + exception.InnerException.Message;
            }
            if (includeStack)
                message += "\n" + exception.StackTrace;
            return message;
        }


    }
}
