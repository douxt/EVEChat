using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Douxt
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false)]
    public class BeaconSecurity : MyGameLogicComponent
    {
        private MyObjectBuilder_EntityBase m_objectBuilder;
        private IMyFunctionalBlock m_block;

        public bool IsBeaconSecurity { get; private set; }
        public bool IsMoving { get; set; }
        public long OwnerId { get { return m_block.OwnerId; } }
        public string DisplayName { get { return m_block.DisplayNameText; } }
        public bool IsEnabled { get { return m_block.Enabled; } }
        public bool IsWorking { get { return m_block.IsWorking; } }
        public void RequestEnable(bool enable)
        {
            Logger.Log.Debug("BeaconSecurity.RequestEnable() enable={0}", enable);
            // change status on server side block
            //m_block.RequestEnable(enable);
            m_block.Enabled = enable;

            // send packet...
            SyncPacket packet = new SyncPacket();
            packet.proto = SyncPacket.Version;
            packet.command = (ushort)((enable) ? Command.SyncOn : Command.SyncOff);
            packet.ownerId = m_block.OwnerId;
            packet.entityId = Entity.EntityId;
            Core.SendMessage(packet);

        }
        public bool IsPowered
        {
            get
            {
                IMyCubeGrid grid = (IMyCubeGrid)Entity.GetTopMostParent();
                if (grid == null)
                    return false;

                IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                gridTerminal.GetBlocks(blocks);
                foreach (var block in blocks)
                {
                    if (block is IMyReactor)
                    {
                        if (block.IsWorking)
                            return true;
                    }

                    var battery = block as IMyBatteryBlock;
                    if (battery != null)
                    {
                        if (battery.CurrentStoredPower > 0f && battery.IsWorking)
                            return true;
                    }

                    var solar = block as IMySolarPanel;
                    if (solar != null)
                    {
                        if (solar.IsWorking)
                            return true;
                    }
                }
                return false;
            }
        }

        public Vector3D GetPosition()
        {
            return Entity.GetPosition();
        }

        private static ulong frameShift = 0;
        private static ulong updateCount = 0;
        private ulong Frame;
        private DateTime m_lastOwnerSeen = DateTime.MinValue;

        private DateTime m_lastNotInMotion = DateTime.Now;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                base.Init(objectBuilder);
                m_objectBuilder = objectBuilder;

                m_block = Entity as IMyFunctionalBlock;

                if (m_block.BlockDefinition.SubtypeId == "LargeBlockBeaconSecurity" || m_block.BlockDefinition.SubtypeId == "SmallBlockBeaconSecurity"
                    || m_block.BlockDefinition.SubtypeId == "LargeBlockBeaconSecurityCustom" || m_block.BlockDefinition.SubtypeId == "SmallBlockBeaconSecurityCustom")
                {
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                    IsBeaconSecurity = true;
                    IsMoving = false;
                    Frame = frameShift++; //each other BS uses shifted frame, for balancing
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Debug(ex.Message);
            }
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? (MyObjectBuilder_EntityBase)m_objectBuilder.Clone() : m_objectBuilder;
        }

        public override void UpdateBeforeSimulation10()
        {
            if (!IsBeaconSecurity)
                return;

            base.UpdateBeforeSimulation10();

            //给玩家增加物品
            ulong count = updateCount++;

            if (count % 30 == 0)
            {
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, x => x.Controller != null && x.Controller.ControlledEntity != null);
                foreach (IMyPlayer player in players)
                {
                    if (player.Controller.ControlledEntity is IMyCharacter)
                    {

                        MyEntity entity = player.Controller.ControlledEntity.Entity as MyEntity;
                        if (entity.HasInventory)
                        {
                            MyInventory inventory = entity.GetInventoryBase() as MyInventory;

                            if (!inventory.ContainItems(10000, new MyObjectBuilder_Ingot { SubtypeName = "Coin" }))
                            {
                                inventory.AddItems(10, new MyObjectBuilder_Ingot { SubtypeName = "Coin" });
                                //terminalBlock.RefreshCustomInfo();
                            }
                        }
                    }

                }
            }


            if (Core.Settings == null || MyAPIGateway.Session == null || MyAPIGateway.Utilities == null || MyAPIGateway.Multiplayer == null)
            {
                Logger.Log.Debug("UpdateBeforeSimulation10() - Exit early");
                return;
            }

            if (!Core.Settings.Enabled) // if some on just turn off the switch, try to turn off all BS
            {
                if (Core.IsServer && m_block.Enabled)
                {
                    Logger.Log.Debug("Beacon Security is EMERGENCY deactivated {0} ownerid {1}...", m_block.EntityId, m_block.OwnerId);
                    RequestEnable(false);
                }
                return;
            }

            // skip noowner BeaconSecurity
            if (m_block.OwnerId == 0)
            {
                if (m_block.Enabled)
                    RequestEnable(false);
                return;
            }

            if (!Core.IsServer)
            {
                Logger.Log.Debug("UpdateBeforeSimulation10() - Exit !Core.IsServer");
                return;
            }

            try
            {
                DateTime DTNow = DateTime.Now;
                MyCubeGrid grid = Entity.GetTopMostParent() as MyCubeGrid;

                // calculations only server side, that way i'm sync it with clients...
                if (Frame++ % 3 != 0) // every 3*10 frame check
                    return;

                // First of all, check the share mode...
                if ((Entity as MyCubeBlock).IDModule.Owner != 0 && (Entity as MyCubeBlock).IDModule.ShareMode != MyOwnershipShareModeEnum.Faction)
                {  // share it to faction, this request auto sync by game
                    (Entity as MyCubeBlock).ChangeBlockOwnerRequest(m_block.OwnerId, MyOwnershipShareModeEnum.Faction);
                    Logger.Log.Debug("BeaconSecurity changed share mode {0} {1}", m_block.EntityId, m_block.OwnerId);
                }

                // the next step, check whether the the ship is moving, if the appropriate flag
                if (Core.Settings.OnlyWithZeroSpeed)
                {
                    if (grid != null)
                    {
                        bool movingNow = (grid.Physics.LinearVelocity.Length() > 0.2f || grid.Physics.AngularVelocity.Length() > 0.01f) ? true : false;

                        Logger.Log.Debug("BeaconSecurity: grid with BS {0} MOVING CHECKS: LV:{1} LA:{2} AV:{3} AA:{4}", Entity.EntityId,
                            grid.Physics.LinearVelocity.Length(), grid.Physics.LinearAcceleration.Length(), grid.Physics.AngularVelocity.Length(), grid.Physics.AngularAcceleration.Length()
                            );

                        if (movingNow == true)
                        {   // ship is moving
                            TimeSpan ts = DTNow - m_lastNotInMotion;
                            if (ts.TotalSeconds > Core.Settings.MotionShutdownDelay)
                            {   // if still moving, after N secs, just shutdown beacon
                                IsMoving = true;
                                m_lastOwnerSeen = DateTime.Now;
                            }
                        }
                        else
                        { // if ship not moving, reset timer
                            m_lastNotInMotion = DateTime.Now;
                            IsMoving = false;
                            Logger.Log.Debug(" * set last not moving state at NOW");
                        }
                    }
                    else
                    {
                        Logger.Log.Error("BeaconSecurity: BS {0} no GRID FOUND!!!", Entity.EntityId);
                    }
                } // if flag is off, check that IsMoving must be false all time
                else
                    IsMoving = false;

                // Check the owner's presence near the beacon.
                // Check the search of all the players in relation to the current beacon.
                // one BS - all players.
                // preset owner sphere
                BoundingSphereD SphereOwner = new BoundingSphereD(GetPosition(), Core.Settings.DistanceBeforeTurningOn);

                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, x => x.Controller != null && x.Controller.ControlledEntity != null);
                foreach (IMyPlayer player in players)
                {
                    // get relations between player and beacon
                    MyRelationsBetweenPlayerAndBlock relation = m_block.GetUserRelationToOwner(player.IdentityId);
                    if (relation != MyRelationsBetweenPlayerAndBlock.Owner && relation != MyRelationsBetweenPlayerAndBlock.FactionShare)
                        continue;
                    // if player has rights to this beacon, check in radius

                    if (Core.Settings.DistanceBeforeTurningOn <= 0)
                    { // we need just check if player online, any of group
                        m_lastOwnerSeen = DTNow;
                        break;
                    }

                    // spaceman or ship, get world boundingbox
                    BoundingBoxD playerObject = (player.Controller.ControlledEntity is IMyCharacter) ? player.Controller.ControlledEntity.Entity.WorldAABB : player.Controller.ControlledEntity.Entity.GetTopMostParent().WorldAABB;
                    if (SphereOwner.Contains(playerObject) != ContainmentType.Disjoint)
                    {   // user is in sphere, set last seen date
                        m_lastOwnerSeen = DTNow;
                        break;
                    }

                }

                // Next part - Check the switching conditions 
                // 1 - owner away by DistanceBeforeTurningOn meters
                // 2 - delay before turning on DelayBeforeTurningOn seconds
                // 3 - IsMoving must be false
                // 4 - if set OnlyForStations, check grid isStatic
                // 5 - if not exceed sizes

                bool chkOnlyForStations = true;
                bool chkSizes = true;
                if (Core.Settings.OnlyForStations && grid != null) // check grid istatic property
                    chkOnlyForStations = grid.IsStatic;
                IMyEntity entGrid = grid as IMyEntity;
                if (entGrid != null)
                {
                    Vector3 size = entGrid.LocalAABB.Size;
                    long limit = Core.Settings.LimitGridSizes;
                    if (limit > 0)
                    {
                        Logger.Log.Debug("Limitation for sizes: {0}   {1}x{2}x{3}", limit, size.X, size.Y, size.Z);
                        if (size.X > limit || size.Y > limit || size.Z > limit)
                        {
                            chkSizes = false;
                        }
                    }
                }

                TimeSpan diff = DTNow - m_lastOwnerSeen;
                if (diff.TotalSeconds > Core.Settings.DelayBeforeTurningOn && !IsMoving && chkOnlyForStations && chkSizes && m_block.IsFunctional && IsPowered)
                {   // BeaconSecurity must be ON
                    if (!m_block.Enabled)
                    {
                        Logger.Log.Debug("BeaconSecurity is activated {0} ownerid {1}, sync date info to others...", m_block.EntityId, m_block.OwnerId);
                        RequestEnable(true);
                    }
                    else if (!m_block.IsWorking)
                    { // if enabled, but still don't working...
                        Logger.Log.Debug("BeaconSecurity is deactivated NOPOWER {0} ownerid {1}, sync date info to others...", m_block.EntityId, m_block.OwnerId);
                        RequestEnable(false);
                        m_lastOwnerSeen = DTNow.AddSeconds(10); // shift a power on by time;
                    }
                }
                else
                {
                    // must be off
                    if (m_block.Enabled)
                    {
                        Logger.Log.Debug("BeaconSecurity is deactivated {0} ownerid {1}, sync date info to others...", m_block.EntityId, m_block.OwnerId);
                        RequestEnable(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Exception in BeaconSecurity: {0}", ex.Message);
            }
        }
    }
}
