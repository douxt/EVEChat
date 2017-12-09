
namespace JimLess
{
    public enum Command
    {
        SettingsSync = 0x100,
        SettingsChange,
        SyncOn,
        SyncOff,
        MessageToChat
    }

    public struct SyncPacket
    {
        public static readonly ushort SyncPacketID = (1 << 13) + 224;
        public static readonly int Version = SyncPacketID + 0x0001;

        /// <summary>
        /// request if true, response if false
        /// </summary>
        public bool request; 
        public int proto;
        public int command;
        public ulong steamId;
        public long ownerId;
        public long entityId;
        public string message;
        public Settings settings;
    }
}
