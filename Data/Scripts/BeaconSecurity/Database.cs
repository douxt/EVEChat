using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;

namespace JimLess
{
    class Database
    {
        private static Database m_instance;
        public static Database Instance { get { if (m_instance == null) m_instance = new Database(); return m_instance; } }

        /// <summary>
        /// TODO: description
        /// </summary>
        private Dictionary<string, string> m_databaseIndexes = new Dictionary<string, string>()
        {
            {"settings", "settings.xml"}
        };

        public void Init()
        {
            Logger.Log.Debug("Database.init()");

        }

        private string ReadDBFile(string index)
        {
            Logger.Log.Debug("Database.ReadDBFile({0})", index);
            try
            {
                TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(m_databaseIndexes[index], typeof(Database));
                string buf = reader.ReadToEnd();
                reader.Close();
                return buf;
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Database.ReadDBFile(): {0}", index, ex);
            }
            return null;
        }

        private void WriteDBFile(string index, string data)
        {
            try
            {
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(m_databaseIndexes[index], typeof(Database));
                writer.Write(data);
                writer.Flush();
                writer.Close();
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Database.WriteDBFile(): {0}", index, ex);
            }
        }

        public T GetData<T>(string index, Type type)
        {
            Logger.Log.Debug("Database.GetData({0}, {1})", index, type.Name);
            try
            {
                return MyAPIGateway.Utilities.SerializeFromXML<T>(ReadDBFile(index));
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Database.GetData(): {0}", index, ex);
            }
            return default(T);
        }

        public bool SetData<T>(string index, Type type, T data)
        {
            Logger.Log.Debug("Database.WriteData({0}, {1})", index, type.Name);
            WriteDBFile(index, MyAPIGateway.Utilities.SerializeToXML<T>(data));
            return true;
        }

        public void Dispose()
        {
            Logger.Log.Debug("Database.Dispose()");
        }

    }
}
