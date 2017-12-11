using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Douxt
{

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Core : MySessionComponentBase
    {
        public static readonly string MODSAY = "EVE";

        public static Settings Settings { get; private set; }
        internal static void setSettings(Settings settings) { Settings = settings; Flush(); }

        public static Codes Codes { get; private set; }
        internal static void setCodes(Codes codes) { Codes = codes; FlushCodes(); }

        public static bool Inited { get; private set; }
        public static bool IsServer { get; private set; }

        private static ulong frameShift = 0;
        private ulong Frame;
        private DateTime m_cleaning = DateTime.MinValue;
        private DateTime m_settingRequested = DateTime.MinValue;

        static List<IMySlimBlock> queueRemoveBlocks = new List<IMySlimBlock>();

        List<IMyCubeGrid> eventedGrids = new List<IMyCubeGrid>();

        void Init()
        {
            Logger.Log.Info("BeaconSecurity.Init()");

            IsServer = MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;

            MyAPIGateway.Utilities.MessageEntered += ChatHandler.ChatMessageEntered;
            MyAPIGateway.Multiplayer.RegisterMessageHandler(SyncPacket.SyncPacketID, Sync.OnSyncRequest);

            Database.Instance.Init();

            //MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageHandler.BeforeDamageHandler);

            // if custom blocks exists, overriding...
            try
            {
                MyCubeBlockDefinition custom = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Beacon), "LargeBlockBeaconSecurityCustom"));
                if (custom != null)
                {
                    custom = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Beacon), "LargeBlockBeaconSecurity"));
                    custom.Public = false;
                }
                custom = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Beacon), "SmallBlockBeaconSecurityCustom"));
                if (custom != null)
                {
                    custom = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Beacon), "SmallBlockBeaconSecurity"));
                    custom.Public = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Exception while trying to check Custom block definition: {0}", ex.Message);
            }

            Inited = true;
        }

        void GetSettings()
        {
            if (IsServer)
            {   // Server side, get from local drive
                Logger.Log.Info("Server get settings.");
                if ((Settings = Database.Instance.GetData<Settings>("settings", typeof(Settings))) == null)
                    Settings = new Settings();
            }
            else
            {   // request settings from server.
                TimeSpan ts = DateTime.Now - m_settingRequested;
                if (MyAPIGateway.Session.Player != null && ts.TotalSeconds > 10) // request settings every 10 secs if there no response
                {
                    Logger.Log.Info("Request settings from server!");
                    SyncPacket packet = new SyncPacket();
                    packet.proto = SyncPacket.Version;
                    packet.request = true;
                    packet.command = (ushort)Command.SettingsSync;
                    packet.steamId = MyAPIGateway.Session.Player.SteamUserId;
                    SendMessageToServer(packet); // send request only to server
                    m_settingRequested = DateTime.Now;
                }
            }
        }

        void GetCodes() {
            if (IsServer)
            {   // Server side, get from local drive
                Logger.Log.Info("Server redeemed codes");
                if ((Codes = Database.Instance.GetData<Codes>("codes", typeof(Codes))) == null)
                    Codes = new Codes();
            }
        }

        private static void Flush()
        {
            if (!IsServer || Settings == null)
                return;

            Database.Instance.SetData<Settings>("settings", typeof(Settings), Settings);
        }

        private static void FlushCodes()
        {
            if (!IsServer || Codes == null)
                return;

            Database.Instance.SetData<Codes>("codes", typeof(Codes), Codes);
        }

        protected override void UnloadData()
        {
            Logger.Log.Info("BeaconSecurity.Unload()");
            Flush();
            if (Inited)
            {
                MyAPIGateway.Utilities.MessageEntered -= ChatHandler.ChatMessageEntered;
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(SyncPacket.SyncPacketID, Sync.OnSyncRequest);
                Database.Instance.Dispose();
            }
            Logger.Dispose();
        }

        //
        // == SessionComponent Hooks
        //
        public override void UpdateBeforeSimulation()
        {
            try
            {
                if (MyAPIGateway.Session == null || MyAPIGateway.Utilities == null || MyAPIGateway.Multiplayer == null) // exit if api is not ready
                    return;

                if (!Inited) // init and set handlers
                    Init();

                if (Settings == null) // request or load setting
                    GetSettings();    // if server, settings will be set a this call, so it safe 

                if (!IsServer)  // if client exit at this point. Cleaning works only on server side.
                    return;

                if (Codes == null)
                    GetCodes();

                Frame = frameShift++;
                if (Frame % 3600 != 0)
                {
                    return;
                }

                List<IMyPlayer> players = new List<IMyPlayer>();

                //MyAPIGateway.Players.GetPlayers(players, x => x.Controller != null && x.Controller.ControlledEntity != null);
                MyAPIGateway.Players.GetPlayers(players, x => x.Character != null && !x.IsBot);
                foreach (IMyPlayer player in players)
                {
                    if (player.Character is IMyCharacter)
                    {

                        MyEntity entity = player.Character.Entity as MyEntity;
                        if (entity.HasInventory)
                        {
                            IMyInventory inventory = entity.GetInventoryBase() as MyInventory;
                            if (!inventory.ContainItems(Settings.SalaryMax, new MyObjectBuilder_Ingot { SubtypeName = "Coin" }))
                            {
                                inventory.AddItems(Settings.SalaryPerMinute, new MyObjectBuilder_Ingot { SubtypeName = "Coin" });
                                //terminalBlock.RefreshCustomInfo();
                            }
                        }
                    }

                }


            }
            catch (Exception ex)
            {
                Logger.Log.Error("EXCEPTION at BeaconSecurity.UpdateBeforeSimulation(): {0} {1}", ex.Message, ex.StackTrace);
            }
        }

        public static void EnqueueRemoveBlock(IMySlimBlock block)
        {
            queueRemoveBlocks.Add(block);
        }


        public static bool IsAdmin(IMyPlayer Player = null, ulong SteamUserId = 0)
        {
            // Credits go to Midspace AKA Screaming Angels for this one:
            // Attempts to determine if a player is an administrator of a dedicated server.
            var clients = MyAPIGateway.Session.GetCheckpoint("null").Clients;
            if (clients == null)
                return false;
            var client = clients.FirstOrDefault(c => c.SteamId == ((SteamUserId > 0) ? SteamUserId : ((Player != null) ? Player.SteamUserId : 0)));
            return (client != null && client.IsAdmin);
        }

        public static bool IsAdmin(ulong SteamUserId)
        {
            var clients = MyAPIGateway.Session.GetCheckpoint("null").Clients;
            if (clients == null)
                return false;
            var client = clients.FirstOrDefault(c => c.SteamId == SteamUserId);
            return (client != null && client.IsAdmin);
        }

        public static IMyPlayer GetPlayer(ulong SteamUserId)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, x => x.SteamUserId == SteamUserId);
            if (players.Count > 0) {
                return players[0];
            }
            return null;
        }


        public static void SendSettingsToServer(Settings settings, ulong steamId)
        {
            SyncPacket newpacket = new SyncPacket();
            newpacket.proto = SyncPacket.Version;
            newpacket.request = false;
            newpacket.command = (ushort)Command.SettingsChange;
            newpacket.steamId = steamId;
            newpacket.settings = settings;
            SendMessageToServer(newpacket); // send only to server
        }

        public static void SendCodeToServer(string code, ulong steamId)
        {
            SyncPacket newpacket = new SyncPacket();
            newpacket.proto = SyncPacket.Version;
            newpacket.request = false;
            newpacket.command = (ushort)Command.Redeem;
            newpacket.steamId = steamId;
            newpacket.message = code;
            //newpacket.settings = settings;
            SendMessageToServer(newpacket); // send only to server
        }

        public static void SendMessage(SyncPacket package, ulong steamId = 0)
        {
            try
            {
                string text = MyAPIGateway.Utilities.SerializeToXML<SyncPacket>(package);
                Byte[] message = System.Text.Encoding.Unicode.GetBytes(text);
                if (steamId != 0)
                {
                    MyAPIGateway.Multiplayer.SendMessageTo(SyncPacket.SyncPacketID, message, steamId);
                }
                else
                {
                    MyAPIGateway.Multiplayer.SendMessageToOthers(SyncPacket.SyncPacketID, message);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Exception in BeaconSecurity.SendMessage(): {0}", ex.Message);
            }
        }

        public static void SendMessageToServer(SyncPacket package)
        {
            try
            {
                string text = MyAPIGateway.Utilities.SerializeToXML<SyncPacket>(package);
                MyAPIGateway.Multiplayer.SendMessageToServer(SyncPacket.SyncPacketID, System.Text.Encoding.Unicode.GetBytes(text));
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Exception in BeaconSecurity.SendMessageToServer(): {0}", ex.Message);
            }
        }
    }
}
