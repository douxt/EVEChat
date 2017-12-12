using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Douxt
{
    public class Sync
    {
        public static void OnSyncRequest(byte[] bytes)
        {
            try
            {
                Logger.Log.Debug("BeaconSecurity.OnSyncRequest() - starts");

                SyncPacket pckIn = new SyncPacket();
                string data = System.Text.Encoding.Unicode.GetString(bytes);
                //Logger.Log.Debug(@"*******************************\n{0}\n*******************************\n", data);
                pckIn = MyAPIGateway.Utilities.SerializeFromXML<SyncPacket>(data);
                Logger.Log.Info("OnSyncRequest COMMAND:{0}, id:{1}, entity:'{2}', steamid: {3}, isserver: {4}", Enum.GetName(typeof(Command), pckIn.command), pckIn.ownerId, pckIn.entityId, pckIn.steamId, Core.IsServer);

                if (pckIn.proto != SyncPacket.Version)
                {
                    Logger.Log.Error("Wrong version of sync protocol client [{0}] <> [{1}] server", SyncPacket.Version, pckIn.proto);
                    MyAPIGateway.Utilities.ShowNotification("同步协议版本不匹配!尝试重新启动游戏或服务器!", 5000, MyFontEnum.Red);
                    return;
                }

                switch ((Command)pckIn.command)
                {

                    case Command.Redeem:
                        {
                            //收到命令，给这个玩家加点东西？
                            if (Core.IsServer) {
                                Logger.Log.Info("Some one with steamid={0} trying to redeem", pckIn.steamId);
                                //IMyPlayer player = Core.GetPlayer(pckIn.steamId) as IMyPlayer;
                                string msg = "返回信息";
                                MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, String.Format(msg));
                                if (Core.Codes.RedeemedCodes.Contains(pckIn.message))
                                {
                                    MyAPIGateway.Utilities.ShowNotification("已经兑换过了！", 2000, MyFontEnum.Green);
                                    SyncPacket newpacket = new SyncPacket();
                                    newpacket.proto = SyncPacket.Version;
                                    newpacket.command = (ushort)Command.MessageToChat;
                                    newpacket.message = "已经兑换过了！";
                                    Core.SendMessage(newpacket); // send to others
                                    return;
                                    //MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, String.Format("已经被兑换！"));
                                    //msg = "已经被兑换！";
                                }
                                else {
                                    string t = pckIn.message.Replace("-", "");
                                    byte[] data2 = Base24Encoding.Default.GetBytes(t);
                                    if (data2 == null)
                                    {
                                        return;
                                    }
                                    String str = System.Text.Encoding.ASCII.GetString(data2);
                                    str = str.TrimStart('\0');
                                    string[] sep = { ":" };
                                    string[] msgs = str.Split(sep, 4, StringSplitOptions.RemoveEmptyEntries);
                                    if (msgs.Length != 4)
                                    {
                                        return;
                                    }



                                    string type = msgs[0];
                                    if (type.Equals("Coin", StringComparison.OrdinalIgnoreCase))
                                    {
                                        var num = 0;
                                        try { num = Int32.Parse(msgs[1]); } catch { }

                                        if (num == 0)
                                        {
                                            return;
                                        }
                                        var sum = 0;
                                        try { sum = Int32.Parse(msgs[3]); } catch { }

                                        string content = msgs[0] + ":" + msgs[1] + ":" + msgs[2];

                                        var sum2 = 0;
                                        foreach (char c in content)
                                        {
                                            sum2 += (int)c;
                                        }
                                        sum2++;
                                        sum2 = sum2 % 100;
                                        if (sum2 != sum)
                                        {
                                           //MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, String.Format("兑换码错误！"));
                                           MyAPIGateway.Utilities.ShowNotification("兑换码错误！", 2000, MyFontEnum.Green);
                                            SyncPacket newpacket = new SyncPacket();
                                            newpacket.proto = SyncPacket.Version;
                                            newpacket.command = (ushort)Command.MessageToChat;
                                            newpacket.message = "兑换码错误！";
                                            Core.SendMessage(newpacket); // send to others
                                            return;
                                        }

                                        //IMyPlayer player = MyAPIGateway.Session.Player as IMyPlayer;
                                        IMyPlayer player = Core.GetPlayer(pckIn.steamId) as IMyPlayer;

                                        if (player == null)
                                        {
                                            Logger.Log.Info("redeem player null");
                                            return;
                                        }

                                        MyEntity entity = player.Character.Entity as MyEntity;
                                        if (entity!=null && entity.HasInventory)
                                        {
                                            MyInventory inventory = entity.GetInventoryBase() as MyInventory;

                                            inventory.AddItems(num, new MyObjectBuilder_Ingot { SubtypeName = "Coin" });

                                            Core.Codes.RedeemedCodes.Add(pckIn.message);

                                            Core.setCodes(Core.Codes);
                                           // Core.SendSettingsToServer(Core.Settings, pckIn.steamId);

                                            //MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, String.Format("兑换成功！"));
                                            MyAPIGateway.Utilities.ShowNotification("兑换成功！", 2000, MyFontEnum.Green);
                                            SyncPacket newpacket = new SyncPacket();
                                            newpacket.proto = SyncPacket.Version;
                                            newpacket.command = (ushort)Command.MessageToChat;
                                            newpacket.message = "兑换成功！";
                                            Core.SendMessage(newpacket); // send to others
                                            //Core.setSettings(pckIn.settings);

                                            //// resend for all clients a new settings
                                            //SyncPacket newpacket = new SyncPacket();
                                            //newpacket.proto = SyncPacket.Version;
                                            //newpacket.request = false;
                                            //newpacket.command = (ushort)Command.SettingsSync;
                                            //newpacket.steamId = 0; // for all
                                            //newpacket.settings = Core.Settings;
                                            //Core.SendMessage(newpacket);
                                        }
                                    }
                                }





                                //IMyPlayer player = MyAPIGateway.Session.Player as IMyPlayer;
                                //if (player != null)
                                //{
                                //    MyEntity entity = player.Character.Entity as MyEntity;
                                //    if (entity.HasInventory)
                                //    {
                                //        IMyInventory inventory = entity.GetInventoryBase() as MyInventory;
                                //        if (!inventory.ContainItems(1000, new MyObjectBuilder_Ingot { SubtypeName = "Iron" }))
                                //        {
                                //            inventory.AddItems(1, new MyObjectBuilder_Ingot { SubtypeName = "Iron" });
                                //            Logger.Log.Info("Some one with steamid={0} trying to redeemed", pckIn.steamId);
                                //            //terminalBlock.RefreshCustomInfo();
                                //        }
                                //    }

                                //    // resend for all clients a new settings
                                //    SyncPacket newpacket = new SyncPacket();
                                //    newpacket.proto = SyncPacket.Version;
                                //    newpacket.request = false;
                                //    newpacket.command = (ushort)Command.Redeem;
                                //    newpacket.steamId = 0; // for all
                                //    newpacket.message = "兑换成功";
                                //    Core.SendMessage(newpacket);
                                //}
                                //else
                                //{
                                //    Logger.Log.Info("Some one with steamid={0} trying to redeem, no player", pckIn.steamId);
                                //}
                            }

                            break;
                        }
                    case Command.MessageToChat:
                        {
                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, pckIn.message);
                            break;
                        }
                    case Command.SettingsSync:
                        {
                            if (pckIn.request) // Settings sync request
                            {
                                if (Core.IsServer && Core.Settings != null)
                                { // server send settings to client
                                    Logger.Log.Info("Send sync packet with settings to user steamId {0}", pckIn.steamId);
                                    SyncPacket pckOut = new SyncPacket();
                                    pckOut.proto = SyncPacket.Version;
                                    pckOut.request = false;
                                    pckOut.command = (ushort)Command.SettingsSync;
                                    pckOut.steamId = pckIn.steamId;
                                    pckOut.settings = Core.Settings;
                                    Core.SendMessage(pckOut, pckIn.steamId);
                                }
                            }
                            else
                            {
                                if (!Core.IsServer)
                                { // setting sync only for clients
                                    Logger.Log.Info("User config synced...");
                                    // if settings changes or syncs...
                                    Core.setSettings(pckIn.settings);
                                    if (pckIn.steamId == 0) // if steamid is zero, so we updating for all clients and notify this message
                                        MyAPIGateway.Utilities.ShowNotification("设置已更新!", 2000, MyFontEnum.Green);
                                }
                            }
                            break;
                        }
                    case Command.SettingsChange:
                        {
                            if (Core.IsServer) // Only server can acccept this message
                            {
                                Logger.Log.Info("Some one with steamid={0} trying to change server settings", pckIn.steamId);
                                if (Core.IsAdmin(pckIn.steamId) || pckIn.steamId == MyAPIGateway.Session.Player.SteamUserId)
                                {
                                    Logger.Log.Info("Server config changed by steamId {0}", pckIn.steamId);
                                    Core.setSettings(pckIn.settings);

                                    // resend for all clients a new settings
                                    SyncPacket newpacket = new SyncPacket();
                                    newpacket.proto = SyncPacket.Version;
                                    newpacket.request = false;
                                    newpacket.command = (ushort)Command.SettingsSync;
                                    newpacket.steamId = 0; // for all
                                    newpacket.settings = Core.Settings;
                                    Core.SendMessage(newpacket);
                                }
                            }
                            break;
                        }
                    case Command.SyncOff:
                        {

                            break;
                        }
                    case Command.SyncOn:
                        {

                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

            }
            catch (Exception ex)
            {
                Logger.Log.Error("Exception at BeaconSecurity.OnSyncRequest(): {0}", ex.Message);
                return;
            }
        }
    }
}
