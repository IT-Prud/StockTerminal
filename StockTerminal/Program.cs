using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows.Forms;
using StockTerminal.Forms;
using Utils;
using Logger;

namespace StockTerminal
{
    static class Program
    {
        public static MainForm MainFormInstance
        {
            get { return mainForm; }
        }

        private static MainForm mainForm = null;

        public static readonly LogProcessor TheLogProcessor = new LogProcessor();
        private static Log unhandledLog = null;
        private static EventHandler applicationExitHandler = new EventHandler(OnApplicationExit);

        private static readonly Settings SettingsTradeDB = new Settings("QuoteOnly338", "Language");
        private static string pQuoteOnly;

        private static ResourceManager ResManUI = new ResourceManager(typeof(MainForm).ToString() + "_", Assembly.GetExecutingAssembly());

        public static string QuoteOnly
        {
            get { return pQuoteOnly; }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            SettingsTradeDB.ReadFromFile(BaseForm.INIFile, "TradeDB");
            pQuoteOnly = SettingsTradeDB["QuoteOnly338"];

            if (pQuoteOnly == "1")
            {
                switch (Instance.RegisterInstance())
                {
                    case 1:
                        break;

                    case -1:
                        Instance.ShowInstance(GetResxString("DocumentNameQuote", new CultureInfo("en-US")), FormWindowState.Normal);
                        Instance.ShowInstance(GetResxString("DocumentNameQuote", new CultureInfo("zh-CHT")), FormWindowState.Normal);
                        Instance.ShowInstance(GetResxString("DocumentNameQuote", new CultureInfo("zh-CHS")), FormWindowState.Normal);
                        Application.Exit();
                        return;

                    case -2:
                        MessageBox.Show(string.Format("Another {0} is running. Please close another {0} first.", GetResxString("DocumentNameQuote", new CultureInfo("en-US"))), 
                            GetResxString("DocumentNameQuote", new CultureInfo("en-US")), 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Information);
                        return;
                }
            }

            CultureInfo ci;

            switch ((SettingsTradeDB["Language"] ?? "").ToUpper())
            {
                case "TC": ci = new CultureInfo("zh-CHT"); break;
                case "SC": ci = new CultureInfo("zh-CHS"); break;
                default:
                    switch (CultureInfo.CurrentCulture.ToString())
                    {
                        case "zh-CN": ci = new CultureInfo("zh-CHS"); break;
                        case "zh-CHS": ci = new CultureInfo("zh-CHS"); break;
                        case "zh-CHT": ci = new CultureInfo("zh-CHT"); break;
                        case "zh-HK": ci = new CultureInfo("zh-CHT"); break;
                        case "zh-MO": ci = new CultureInfo("zh-CHT"); break;
                        case "zh-SG": ci = new CultureInfo("zh-CHT"); break;
                        case "zh-TW": ci = new CultureInfo("zh-CHT"); break;
                        default: ci = new CultureInfo("en-US"); break;
                    }
                    break;
            }

            TheLogProcessor.Start();
            unhandledLog = TheLogProcessor.Open(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\" + Application.ProductName + @"\Log\__Unhandled", "", ".log");

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CrashDump);
            Application.ThreadException += new ThreadExceptionEventHandler(OnGuiUnhandedException);

            Application.ApplicationExit += applicationExitHandler;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            mainForm = new MainForm(ci, null);

            Application.AddMessageFilter(mainForm);
            Application.Run(mainForm);
        }

        private static void OnApplicationExit(object sender, EventArgs e)
        {
            Application.ApplicationExit -= applicationExitHandler;
            TheLogProcessor.Stop();

            Instance.UnregisterInstance();
        }

        // Windows Forms unhandled exception
        private static void OnGuiUnhandedException(object send, ThreadExceptionEventArgs e)
        {
            string userId = mainForm != null && mainForm.TradeDB != null ? mainForm.TradeDB.UserId : "";

            StackTrace st = new StackTrace(true);

            unhandledLog.Append(userId + "," + "---------", "--------------------------------------------------------");
            unhandledLog.Append(userId + "," + "Thread Exception", e.Exception.ToString());

            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame sf = st.GetFrame(i);

                unhandledLog.Append(userId + "," + "Method", sf.GetMethod().ToString());
                unhandledLog.Append(userId + "," + "File", sf.GetFileName());
                unhandledLog.Append(userId + "," + "Line Number", sf.GetFileLineNumber().ToString());
                unhandledLog.Append(userId + "," + "---------", "--------------------------------------------------------");
            }

            unhandledLog.Close();
        }

        private static void CrashDump(object sender, UnhandledExceptionEventArgs e)
        {
			StackTrace st = new StackTrace(true);
			Queue<string> qMsg = new Queue<string>();

            DateTime Now = DateTime.Now;

            qMsg.Enqueue("Time: " + Now.ToString("G"));
            qMsg.Enqueue( "Exception: " + e.ExceptionObject.ToString() );
            qMsg.Enqueue("");

            for(int i = 0; i < st.FrameCount; i++ )
            {
                StackFrame sf = st.GetFrame(i);

				qMsg.Enqueue( " Method: {0}" + sf.GetMethod().ToString() );
				qMsg.Enqueue( " File: {0}" + sf.GetFileName() );
				qMsg.Enqueue( " Line Number: {0}" + sf.GetFileLineNumber() );
				qMsg.Enqueue( "" );
				qMsg.Enqueue( " ------------------------------------------" );
				qMsg.Enqueue( "" );
            }

            string dumpFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\" + Application.ProductName + @"\Dump\";
			try
			{
                if (Directory.Exists(dumpFolder) == false)
                {
                    Directory.CreateDirectory(dumpFolder);
                }

                File.WriteAllLines(dumpFolder + DateTime.Now.ToString("yyyyMMddHHmmss") + ".log", qMsg.ToArray());
			}
			catch( Exception )
			{
                if (Directory.Exists(dumpFolder) == false)
                {
                    Directory.CreateDirectory(dumpFolder);
                }

                File.WriteAllLines(dumpFolder + DateTime.Now.ToString("yyyyMMddHHmmss") + ".log", qMsg.ToArray());
            }

/*
            string userId = mainForm != null ? mainForm.TradeDB.UserId : "";

            StackTrace st = new StackTrace(true);

            unhandledLog.Append(userId + "," + "---------", "--------------------------------------------------------");
            unhandledLog.Append(userId + "," + "Exception", e.ExceptionObject.ToString());

            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame sf = st.GetFrame(i);

                unhandledLog.Append(userId + "," + "Method", sf.GetMethod().ToString());
                unhandledLog.Append(userId + "," + "File", sf.GetFileName());
                unhandledLog.Append(userId + "," + "Line Number", sf.GetFileLineNumber().ToString());
                unhandledLog.Append(userId + "," + "---------", "--------------------------------------------------------");
            }

            unhandledLog.Close();
 */
        }

        private static string GetResxString(string Key, CultureInfo TheCulture)
        {
            try
            {
                return ResManUI.GetString(Key, TheCulture);
            }
            catch { }

            return null;
        }
    }
}
