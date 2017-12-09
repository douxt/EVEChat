using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace JimLess
{
    public class DamageHandler
    {
        public static void BeforeDamageHandler(object target, ref MyDamageInformation info)
        {
            try
            {
                Logger.Log.Debug("BeaconSecurity.BeforeDamageHandler {0} - {1}, {2}, {3}", target, info.Amount, info.Type, info.AttackerId);

                IMySlimBlock targetBlock = target as IMySlimBlock;
                if (targetBlock == null)
                    return;

                MyCubeGrid targetGrid = targetBlock.CubeGrid as MyCubeGrid;
                if (targetGrid == null)
                    return;

                if (!targetGrid.DestructibleBlocks)
                {
                    Logger.Log.Debug(" * DestructibleBlocks {0}, so DAMAGE IGNORED...", targetGrid.DestructibleBlocks);
                    info.Amount = 0f;
                }

                bool owner = false;
                IMyFunctionalBlock targetFunctionalBlock = targetBlock.FatBlock as IMyFunctionalBlock;
                IMyEntity attackerEntity;
                Logger.Log.Debug(" targetFunctionalBlock '{0}'", targetFunctionalBlock);
                if (targetFunctionalBlock != null && MyAPIGateway.Entities.TryGetEntityById(info.AttackerId, out attackerEntity))
                {
                    if (info.Type == MyDamageType.Grind)
                    {
                        IMyPlayer player = null;
                        if (attackerEntity is IMyShipGrinder)
                            player = MyAPIGateway.Players.GetPlayerControllingEntity(attackerEntity.GetTopMostParent());

                        if (player == null)
                        {
                            List<IMyPlayer> players = new List<IMyPlayer>();
                            MyAPIGateway.Players.GetPlayers(players);

                            double nearestDistance = 5.0f;
                            foreach (var pl in players)
                            {
                                IMyEntity character = pl.Controller.ControlledEntity as IMyEntity;
                                if (character != null)
                                {
                                    var distance = (character.GetPosition() - attackerEntity.GetPosition()).LengthSquared();
                                    if (distance > nearestDistance)
                                        continue;
                                    nearestDistance = distance;
                                    player = pl;
                                }
                            }
                        }

                        if (player != null)
                        {
                            MyRelationsBetweenPlayerAndBlock relation = targetFunctionalBlock.GetUserRelationToOwner(player.IdentityId);
                            Logger.Log.Debug(" relation '{0}' is {1} == {2}", relation, player.IdentityId, targetFunctionalBlock.OwnerId);
                            owner = (relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.NoOwnership) ? true : false;
                        }
                    }
                }

                // check admins grids
                if (Core.Settings != null && Core.Settings.Indestructible.Contains(targetGrid.EntityId))
                {
                    if (owner && Core.Settings.IndestructibleGrindOwner || owner && Core.Settings.IndestructibleOverrideGrindOwner.Contains(targetGrid.EntityId))
                    {
                        // пилить можно
                        Logger.Log.Debug(" * Target '{0}' has own property, so DAMAGE permit...", targetGrid.DisplayName);
                    }
                    else
                    {
                        // пилить нельзя
                        Logger.Log.Debug(" * Target '{0}' in indestructible list, so DAMAGE IGNORED...", targetGrid.DisplayName);
                        info.Amount = 0f;
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Log.Error("Exception BeforeDamageHandler in {0}", ex.Message);
            }
        }
    }
}
