using log4net;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace DetentionLetterFunctionApp
{
    /// <summary>
    /// Logger class to log exception in file for debugging purpose
    /// categorizes logging into levels: DEBUG, INFO, WARNING, ERROR and FATAL
    /// </summary>
    public static class Logger
    {
        static ILog m_Log;

        // Initialize logger configuration as soon as the logger class is referenced first time
        static Logger()
        {
            m_Log = LogManager.GetLogger("DetentionLetterFunctionApp");
            log4net.Config.XmlConfigurator.Configure();
        }

        public static void Debug(object obj)
        {
            m_Log.Debug(obj);
        }

        public static void Debug(string arg, params object[] args)
        {
            string logString = string.Format(arg, args);
            m_Log.Debug(logString);
        }

        public static void DumpMethodStack()
        {
            //we do not want to do processing in non-debug mode
            if (m_Log.IsDebugEnabled)
            {
                StringBuilder traceBuffer = new StringBuilder(100);
                StackTrace st = new StackTrace();
                int max = st.FrameCount > 7 ? 7 : st.FrameCount;
                for (int i = 1; i < max; i++)
                {
                    StackFrame sf = st.GetFrame(i);
                    MethodBase mthd = sf.GetMethod();
                    traceBuffer.Append(mthd.Name);
                    traceBuffer.Append("\n");
                }
                Logger.Debug(traceBuffer);
            }
        }

        public static void Info(object obj)
        {
            m_Log.Info(obj);
        }

        public static void Warning(object obj)
        {
            m_Log.Warn(obj);
        }

        public static void Error(object obj)
        {
            m_Log.Error(obj);
        }

        public static void Error(string arg, params object[] args)
        {
            if (args.Length > 0)
            {
                string logString = string.Format(arg, args);
                m_Log.Error(logString);
            }
            else
            {
                m_Log.Error(arg);
            }
        }

        public static void Fatal(object obj)
        {
            m_Log.Fatal(obj);
        }
    }
}
