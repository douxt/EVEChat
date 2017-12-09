using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace JimLess
{

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Core : MySessionComponentBase
    {
        public static readonly string MODSAY = "BS";

        public static Settings Settings { get; private set; }
        internal static void setSettings(Settings settings) { Settings = settings; Flush(); }

        public static bool Inited { get; private set; }
        public static bool IsServer { get; private set; }

        private int Frame;
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

            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageHandler.BeforeDamageHandler);

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

        private static void Flush()
        {
            if (!IsServer || Settings == null)
                return;

            Database.Instance.SetData<Settings>("settings", typeof(Settings), Settings);
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

                if (Frame++ % 10 == 0)
                {
                    HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
                    MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
                    foreach (IMyEntity entity in entities)
                    {
                        IMyCubeGrid grid = entity as IMyCubeGrid;
                        if (grid == null)
                            continue;

                        // if this grid have an active BS, then turn off destructible for grid
                        gridDestructible(grid, !isActiveBeaconSecurity(grid));

                        if (!eventedGrids.Contains(grid))
                        { // skip if already added actions
                            grid.OnBlockAdded += BuildHandler.grid_OnBlockAdded;
                            eventedGrids.Add(grid);
                            Logger.Log.Debug("OnBlockAdded event added to {0} grids.", grid.EntityId);
                        }
                    }

                    // Remove block from queues
                    foreach (var block in queueRemoveBlocks)
                    {
                        IMyCubeGrid grid = block.CubeGrid as IMyCubeGrid;
                        if (grid == null)
                            continue;
                        grid.RemoveBlock(block, true);
                        if (block.FatBlock != null)
                            block.FatBlock.Close();
                    }
                    queueRemoveBlocks.Clear();
                }

                if (Settings.CleaningFrequency == 0)
                    return;
                #region Cleanup
                if ((DateTime.Now - m_cleaning).TotalSeconds > Settings.CleaningFrequency) // Cleaning by frequency
                {
                    Logger.Log.Debug("Time for cleaning, from last {0} secs elapsed.", (DateTime.Now - m_cleaning).TotalSeconds);
                    m_cleaning = DateTime.Now;
                    HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
                    MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);

                    int removed = 0;
                    List<IMySlimBlock> ToRemove = new List<IMySlimBlock>();

                    HashSet<long> AlreadyCounted = new HashSet<long>();
                    Dictionary<long, long[]> PerFactionCount = new Dictionary<long, long[]>();
                    Dictionary<long, long[]> PerPlayerCount = new Dictionary<long, long[]>();

                    foreach (IMyCubeGrid grid in entities)
                    {
                        var blocks = new List<IMySlimBlock>();
                        grid.GetBlocks(blocks, x => x.FatBlock != null);
                        foreach (IMySlimBlock block in blocks)
                        {
                            //Logger.Log.Debug("Check block {0} {1}", block.FatBlock.DisplayNameText, block.FatBlock.ToString());
                            BeaconSecurity bs = block.FatBlock.GameLogic as BeaconSecurity;
                            if (bs == null || !bs.IsBeaconSecurity || bs.OwnerId == 0)
                                continue;

                            if (!AlreadyCounted.Contains(block.FatBlock.EntityId))
                                AlreadyCounted.Add(block.FatBlock.EntityId);
                            else
                                continue;

                            // Priority for players limit.

                            if (!PerPlayerCount.ContainsKey(bs.OwnerId)) // if there no counts, add it
                                PerPlayerCount[bs.OwnerId] = new long[] { 0, 0 };
                            PerPlayerCount[bs.OwnerId][0]++;
                            if (bs.OwnerId != 0 && PerPlayerCount[bs.OwnerId][0] > Settings.LimitPerPlayer)
                            {
                                PerPlayerCount[bs.OwnerId][1]++;
                                ToRemove.Add(block);
                                continue;
                            }

                            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(bs.OwnerId);
                            if (faction != null)
                            {
                                if (!PerFactionCount.ContainsKey(faction.FactionId))
                                    PerFactionCount[faction.FactionId] = new long[] { 0, 0 };
                                PerFactionCount[faction.FactionId][0]++;

                                if (PerFactionCount[faction.FactionId][0] > Settings.LimitPerFaction)
                                {
                                    PerFactionCount[faction.FactionId][1]++;
                                    ToRemove.Add(block);
                                }
                            }
                        }
                        foreach (IMySlimBlock block in ToRemove)
                            grid.RemoveBlock(block, true);
                        removed += ToRemove.Count;
                        ToRemove.Clear();
                    }

                    List<IMyPlayer> players = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(players);

                    // Send warn message about cleaning and restrictions!
                    foreach (long ownerId in PerPlayerCount.Keys)
                    {
                        if (ownerId == 0 || PerPlayerCount[ownerId][1] == 0)
                            continue;
                        IMyPlayer player = players.FirstOrDefault(x => x.IdentityId == ownerId);
                        if (player == null)
                            continue;
                        SyncPacket pckOut = new SyncPacket();
                        pckOut.proto = SyncPacket.Version;
                        pckOut.command = (ushort)Command.MessageToChat;
                        pckOut.message = string.Format("Removed {0} entity as a result of limitations on the amount {1} per player.", PerPlayerCount[ownerId][1], Settings.LimitPerPlayer);
                        Core.SendMessage(pckOut, player.SteamUserId);
                    }

                    foreach (long factionId in PerFactionCount.Keys)
                    {
                        if (PerFactionCount[factionId][1] == 0)
                            continue;
                        IMyFaction faction = MyAPIGateway.Session.Factions.TryGetFactionById(factionId);
                        if (faction == null)
                            continue;

                        foreach (var member in faction.Members)
                        {
                            IMyPlayer player = players.FirstOrDefault(x => x.IdentityId == member.Key);
                            if (player == null)
                                continue;
                            SyncPacket pckOut = new SyncPacket();
                            pckOut.proto = SyncPacket.Version;
                            pckOut.command = (ushort)Command.MessageToChat;
                            pckOut.message = string.Format("Removed {0} entity as a result of limitations on the amount {1} per faction.", PerFactionCount[factionId][1], Settings.LimitPerFaction);
                            Core.SendMessage(pckOut, player.SteamUserId);
                        }
                    }

                    if (removed > 0)
                        Logger.Log.Info("Cleaning, totaly removed {0} entities.", removed);
                }
                #endregion Cleanup
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

        public static bool isActiveBeaconSecurity(IMyCubeGrid grid)
        {
            if (grid == null)
                return false;

            IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
            if (gridTerminal == null)
                return false;
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            gridTerminal.GetBlocks(blocks);
            foreach (var block in blocks)
            {
                BeaconSecurity bs = block.GameLogic as BeaconSecurity;
                if (bs != null && bs.IsBeaconSecurity && bs.OwnerId != 0 && bs.IsWorking)
                    return true; // one beacon removed, one more stay - nothing to do...
            }
            return false;
        }

        public static void gridDestructible(IMyCubeGrid grid, bool enable)
        {
            IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
            if (gridTerminal == null)
                return;
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            gridTerminal.GetBlocks(blocks);
            HashSet<VRage.Game.ModAPI.Ingame.IMyCubeGrid> applied = new HashSet<VRage.Game.ModAPI.Ingame.IMyCubeGrid>();
            foreach (var block in blocks)
            {
                if (applied.Contains(block.CubeGrid))
                    continue;

                applied.Add(block.CubeGrid);
                (block.CubeGrid as MyCubeGrid).DestructibleBlocks = enable;
            }
            if (!applied.Contains(grid))
                (grid as MyCubeGrid).DestructibleBlocks = enable;
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
