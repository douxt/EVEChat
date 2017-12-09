using Sandbox.ModAPI;
using System;
using System.Text;

namespace JimLess
{
    public class Logger
    {
        private static Log m_log;
        public static Log Log { get { if (m_log == null) m_log = new Log("BeaconSecurity.log");  return m_log; } }

        public static void Dispose() { if (m_log == null) return; m_log.Close(); m_log = null; }
    }

    public class Log
    {
        private System.IO.TextWriter m_writer;
        private string m_filename;
        private StringBuilder m_cache;

        public Log(string filename)
        {
            m_filename = filename;
            m_cache = new StringBuilder();
        }

        internal void Flush(bool force = false)
        {
            if (m_writer == null)
                return;
            if (force || m_cache.Length > 1024)
            {
                m_writer.Write(m_cache.ToString());
                m_writer.Flush();
                m_cache.Clear();
            }
        }

        internal void Close()
        {
            Flush(true);
            m_cache.Clear();
            m_cache = null;
            if (m_writer == null)
                return;
            m_writer.Flush();
            m_writer.Close();
            m_writer = null;
        }

        void WriteLine(string level, string text)
        {
            if (m_writer == null && MyAPIGateway.Utilities != null)
                m_writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(m_filename, typeof(Log));

            m_cache.AppendLine(string.Format("{0}\t {1}\t {2}", DateTime.Now.ToString("[HH:mm:ss]"), level, text));
            Flush();
        }

        public void Info(string format, params object[] list)
        {
            WriteLine("Info", string.Format(format, list));
        }

        public void Debug(string format, params object[] list)
        {
            if (Core.Settings == null || !Core.Settings.Debug)
                return;
            WriteLine("Debug", string.Format(format, list));
        }

        public void Error(string format, params object[] list)
        {
            WriteLine("Error", string.Format(format, list));
        }

    }
}
