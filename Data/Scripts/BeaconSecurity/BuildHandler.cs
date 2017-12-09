using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Threading;
using VRage.Game.ModAPI;
using VRage.Library.Utils;
using VRageMath;

namespace JimLess
{
    class BuildHandler
    {

        public static void grid_OnBlockAdded(IMySlimBlock obj)
        {
            try
            {
                Logger.Log.Debug("BeaconSecurity.grid_OnBlockAdded {0}", obj);

                MyCubeGrid grid = obj.CubeGrid as MyCubeGrid;
                if (grid == null)
                    return;

                bool removing = false;
                if (!grid.DestructibleBlocks && Core.Settings != null && Core.Settings.BuildingNotAllowed)
                {
                    Logger.Log.Debug(" * DestructibleBlocks {0}, so block removed...", grid.DestructibleBlocks);
                    removing = true;
                }

                // check admins grids
                if (Core.Settings != null && Core.Settings.IndestructibleNoBuilds && Core.Settings.Indestructible.Contains(grid.EntityId) && !Core.Settings.IndestructibleOverrideBuilds.Contains(grid.EntityId))
                {
                    Logger.Log.Debug(" * Target '{0}' in indestructible list, so block removed...", grid.DisplayName);
                    removing = true;
                }

                if (removing)
                {
                    Logger.Log.Debug("RTY Grid: {0} {1} OBJ: {2} {3} POS: {4} {5}", grid, grid is IMyCubeGrid, obj, obj is IMySlimBlock, obj.Position, obj.CubeGrid.PositionComp);
                    Core.EnqueueRemoveBlock(obj);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Exception grid_OnBlockAdded in {0}", ex.Message);
            }

        }

        //private static void TryToRemoveBlockFromGrid(IMyCubeGrid grid, IMySlimBlock obj)
        //{
        //    Logger.Log.Debug("RTY TryToRemoveBlockFromGrid {0} {1}", grid, obj);
        //    if (grid.GetCubeBlock(obj.Position) != obj)
        //        return;

        //    Logger.Log.Debug("RTY repeat");
        //    grid.RemoveBlock(obj, true);
        //    if (obj.FatBlock != null)
        //        obj.FatBlock.Close();

        //    if (grid.GetCubeBlock(obj.Position) == obj)
        //    {
        //        Logger.Log.Debug("RTY repeat SET ANOTHER HANDLER");
        //        m_handler2 = new MyTimer.TimerEventHandler((a, b, c, d, e) =>
        //        {
        //            TryToRemoveBlockFromGrid(grid, obj);
        //        });

        //        MyTimer.StartOneShot(10, m_handler2);
        //    }
        //}


    }
}
