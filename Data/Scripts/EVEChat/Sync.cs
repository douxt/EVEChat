using Sandbox.ModAPI;
using System;
using VRage.Game;
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
                                        MyAPIGateway.Utilities.ShowNotification("安全信标设置已更新!", 2000, MyFontEnum.Green);
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
