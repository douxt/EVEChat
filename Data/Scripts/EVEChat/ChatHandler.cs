using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Douxt
{
    public class ChatHandler
    {
        #region TextConstants
        private static readonly string EVEHELP = @"欢迎!请加QQ群：116381172  以便和大家一起玩！

本服务器的基本玩法：
1.获取资源：只要在线玩家背包就会发放少量小凡币。资源点信标处会产生大量小凡币，而且在线人数越多，产生速度越快。

2.建造船只：用小凡币在黑店购买组件，即可建造船只，推荐在隐蔽的小行星建造自己的基地!

3.搞事情：直接用日不完重生船或者自己造的船出去搞事情或者约战，请自行发挥！


PS：南北据点有公共黑店，黑店出售小凡组件，可以用小凡组件建造自己的黑店。

输入/eve help获取更多信息。
";

        private static readonly string EVEHELP2 = @"命令：
/eve help               显示本页信息。
/eve config             显示服务器配置信息。
/eve set num value      将num号配置值设置为value
/eve redeem code        使用兑换码code兑换奖励
";
        private static readonly string BSHELP_CHAT = @"/bs help [1-10], /bs config, /bs set number/name value,  /bs on/off, /bs debug, /bs list/find/add/rem/clear/buildon";
        private static readonly List<string> BSHELP_VARIABLES = new List<string>() {
"1) 打开前的延迟时间(0-3600) - 开启保护之前的延迟时间,以秒为单位,此时所有者不应该在操作区Beacon Security.默认值:120",
"2) 打开前的距离(0-10000) - 没有找到所有者的距离,或者玩家可以简单地离开游戏.默认值:400",
"3) 仅限于站点(on/off) - 仅对站点开启/关闭信标安全的限制.默认值:关闭",
"4) 只有零速(on/off) -  如果启用此选项,信标安全只能在零速的电网上工作,默认值:开",
"5) 建筑不允许(on/off) - 打开/关闭与信标安全网格的能力.默认值:打开",
"6) 坚不可摧建造(on/off) - 打开/关闭建立在坚不可摧网格上的能力默认值:开启",
"7) 坚不可摧的研磨所有者(on/off) - 打开/关闭在坚不可摧的网格上研磨自己的属性的能力.默认值:开",
"8) 限制网格大小(0-1000) - 以米为单位的网格大小限制,如果超出大小,则信标安全将不起作用,默认为150.0 - 禁用",
"9) 限制每个阵营(1-100) - 每个信标安全数量的限制,默认值:30",
"10) 每人限制(1-100) - Beacon安全信标数量限制,默认值:3",
"11) 清洗频率(0-3600) - 清洗的频率,以秒为单位.默认值:5.0 - 禁用"
        };
        #endregion TextConstants

        private static List<string> m_lastFound = new List<string>();
        public static void ChatMessageEntered(string messageText, ref bool sendToOthers)
        {
            Logger.Log.Debug("ChatEvents.ChatMessageEntered: {0}", messageText);
            if (messageText.StartsWith("/eve", StringComparison.OrdinalIgnoreCase))
            {
                if (messageText.Length == 4)
                {
                    MyAPIGateway.Utilities.ShowMissionScreen("帮助", "EVE-PVP", "服务器", EVEHELP);
                    //MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, String.Format("欢迎来到EVE服务器！输入/eve help获取帮助。"));
                }
                string[] commands = (messageText.Remove(0, 4)).Split(null as string[], 2, StringSplitOptions.RemoveEmptyEntries);
                if (commands.Length > 0)
                {
                    string internalCommand = commands[0];
                    string arguments = (commands.Length > 1) ? commands[1] : "";
                    Logger.Log.Debug("internalCommand: {0} arguments {1}", internalCommand, arguments);

                    if (internalCommand.Equals("help", StringComparison.OrdinalIgnoreCase))
                    {
                        #region help
                        var index = 0;
                        try { index = Int32.Parse(arguments); }
                        catch { }
                        if (index < 1 || index > 10)
                        {
                            MyAPIGateway.Utilities.ShowMissionScreen("帮助", "EVE-PVP", "服务器", EVEHELP2);
                            //MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, BSHELP_CHAT);
                        }
                        else
                        {
                            MyAPIGateway.Utilities.ShowMissionScreen("Help: explanation of variables", "Beacon ", "Security", BSHELP_VARIABLES[index - 1]);
                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, BSHELP_VARIABLES[index - 1]);
                        }
                        #endregion help
                    }
                    else if (Core.Settings != null)
                    {
                        if (internalCommand.Equals("redeem", StringComparison.OrdinalIgnoreCase))
                        {
                            //兑换码，应该发消息给服务器端，让服务器端处理。

                            sendToOthers = false;

                            if (arguments.Length == 0)
                            {
                                return;
                            }

                            Core.SendCodeToServer(arguments, MyAPIGateway.Session.Player.SteamUserId);


                            //SyncPacket newpacket = new SyncPacket();
                            //newpacket.request = false;
                            //newpacket.proto = SyncPacket.Version;
                            //newpacket.command = (ushort)Command.Redeem;
                            //newpacket.message = arguments;
                            //newpacket.steamId = MyAPIGateway.Session.Player.SteamUserId;
                            ////newpacket.steamId = MyAPIGateway.Session.Player.SteamUserId;
                            //Core.SendMessage(newpacket); // send to others
                            return;

                            //if (Core.Settings.RedeemedCodes.Contains(arguments)) {
                            //    MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, String.Format("已经被兑换！"));
                            //    return;
                            //}

                            //string t = arguments.Replace("-", "");
                            //byte[] data2 = Base24Encoding.Default.GetBytes(t);
                            //if (data2 == null)
                            //{
                            //    return;
                            //}
                            //String str = System.Text.Encoding.ASCII.GetString(data2);
                            //str = str.TrimStart('\0');
                            //string[] sep = {":" };
                            //string[] msgs = str.Split(sep, 4,StringSplitOptions.RemoveEmptyEntries);
                            //if (msgs.Length != 4)
                            //{
                            //    return;
                            //}

                

                            //string type = msgs[0];
                            //if (type.Equals("Coin", StringComparison.OrdinalIgnoreCase))
                            //{
                            //    var num = 0;
                            //    try { num = Int32.Parse(msgs[1]); } catch { }

                            //    if (num == 0)
                            //    {
                            //        return;
                            //    }
                            //    var sum = 0;
                            //    try { sum = Int32.Parse(msgs[3]); } catch { }

                            //    string content = msgs[0] + ":" + msgs[1] + ":" + msgs[2];

                            //    var sum2 = 0;
                            //    foreach (char c in content)
                            //    {
                            //        sum2 += (int)c;
                            //    }
                            //    sum2 = sum2 % 100;
                            //    if (sum2 != sum)
                            //    {
                            //        MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, String.Format("兑换码错误！"));
                            //        return;
                            //    }


                            //    MyEntity entity = MyAPIGateway.Session.Player.Character.Entity as MyEntity;
                            //    if (entity.HasInventory)
                            //    {
                            //        MyInventory inventory = entity.GetInventoryBase() as MyInventory;

                            //        inventory.AddItems(num, new MyObjectBuilder_Ingot { SubtypeName = "Coin" });

                            //        Core.Settings.RedeemedCodes.Add(arguments);

                            //        Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);

                            //        MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, String.Format("兑换成功！"));

                            //        //SyncPacket newpacket = new SyncPacket();
                            //        //newpacket.proto = SyncPacket.Version;
                            //        //newpacket.command = (ushort)Command.MessageToChat;
                            //        //newpacket.message = "Code Redeemed.";
                            //        //Core.SendMessage(newpacket); // send to others
                            //        //if (!inventory.ContainItems(100000, new MyObjectBuilder_Ingot { SubtypeName = "Iron" }))
                            //        //{
                            //        //    inventory.AddItems(index, new MyObjectBuilder_Ingot { SubtypeName = "Iron" });
                            //        //    //terminalBlock.RefreshCustomInfo();
                            //        //}
                            //    }
                            //}

                            //if (index > 0)
                            //{
                            //    MyEntity entity = MyAPIGateway.Session.Player.Character.Entity as MyEntity;
                            //    if (entity.HasInventory)
                            //    {
                            //        MyInventory inventory = entity.GetInventoryBase() as MyInventory;

                            //        if (!inventory.ContainItems(100000, new MyObjectBuilder_Ingot { SubtypeName = "Iron" }))
                            //        {
                            //            inventory.AddItems(index, new MyObjectBuilder_Ingot { SubtypeName = "Iron" });
                            //            //terminalBlock.RefreshCustomInfo();
                            //        }
                            //    }


                            //}

                        }

                        if (internalCommand.Equals("config", StringComparison.OrdinalIgnoreCase))
                        {
                            #region config
                            MyAPIGateway.Utilities.ShowMissionScreen("配置", "EVE-PVP", "服务器", String.Format(@"1) 每分钟工资(0-10000,D:60) = {0}
2) 工资累积上限(0-50000,D:10000) = {1}
3) 每分钟基础资源(0-50000,D:300) = {2}
4) 资源累积上限(0-50000,D:20000) = {3}
5) 每在线玩家每分钟资源加成(0-50000,D:150) = {4}
",
    Core.Settings.SalaryPerMinute,
    Core.Settings.SalaryMax,
    Core.Settings.ResourcePerMinute,
    Core.Settings.ResourceMax,
    Core.Settings.ResourceIncreasePerPlayerMinute
    ));
                            #endregion config
                        }

                        if (Core.IsServer || Core.IsAdmin(MyAPIGateway.Session.Player))
                        {
                            #region alladminsettings
                            if (internalCommand.Equals("on", StringComparison.OrdinalIgnoreCase))
                            {
                                Core.Settings.Enabled = true;
                                Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);

                                SyncPacket newpacket = new SyncPacket();
                                newpacket.proto = SyncPacket.Version;
                                newpacket.command = (ushort)Command.MessageToChat;
                                newpacket.message = "Beacon Security is ON.";
                                Core.SendMessage(newpacket); // send to others
                            }
                            else if (internalCommand.Equals("off", StringComparison.OrdinalIgnoreCase))
                            {
                                Core.Settings.Enabled = false;
                                Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);

                                SyncPacket newpacket = new SyncPacket();
                                newpacket.proto = SyncPacket.Version;
                                newpacket.command = (ushort)Command.MessageToChat;
                                newpacket.message = "Beacon Security is OFF.";
                                Core.SendMessage(newpacket); // send to others
                            }
                            else if (internalCommand.Equals("debug", StringComparison.OrdinalIgnoreCase))
                            {
                                Core.Settings.Debug = !Core.Settings.Debug;
                                Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);

                                MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("调试日志记录是 {0}", (Core.Settings.Debug) ? "ON" : "OFF"));
                            }
                            else if (internalCommand.Equals("list", StringComparison.OrdinalIgnoreCase))
                            {
                                List<string> GridNames = new List<string>();
                                List<string> GridNamesNotFound = new List<string>();
                                foreach (long entId in Core.Settings.Indestructible)
                                {
                                    string gridName = GetGridName(entId);
                                    if (gridName != null)
                                        GridNames.Add(string.Format("'{0}'{1}{2}", gridName, Core.Settings.IndestructibleOverrideBuilds.Contains(entId) ? "[BO]" : "", Core.Settings.IndestructibleOverrideGrindOwner.Contains(entId) ? "[GO]" : ""));
                                    else
                                        GridNamesNotFound.Add(string.Format("id[{0}]{1}{2}", entId, Core.Settings.IndestructibleOverrideBuilds.Contains(entId) ? "[BO]" : "", Core.Settings.IndestructibleOverrideGrindOwner.Contains(entId) ? "[GO]" : ""));
                                }
                                string list = String.Join(", ", GridNames.ToArray());
                                string nflist = String.Join(", ", GridNamesNotFound.ToArray());

                                MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("坚不可摧的列表: {0}", (GridNames.Count > 0) ? list : "[EMPTY]"));
                                if (GridNamesNotFound.Count > 0)
                                    MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("目前没有找到: {0}", nflist));
                            }
                            else if (internalCommand.Equals("add", StringComparison.OrdinalIgnoreCase))
                            {
                                if (arguments.Length > 0)
                                {
                                    long entId = 0;
                                    if (!long.TryParse(arguments, out entId))
                                        entId = GetGridEntityId(arguments);
                                    if (entId > 0)
                                    {

                                        if (Core.Settings.Indestructible.Contains(entId))
                                        {
                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("无法添加网格名称 '{0}', 已经在列表中...", arguments));
                                        }
                                        else
                                        {
                                            Core.Settings.Indestructible.Add(entId);
                                            Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);

                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("网格名称 '{0}' 已添加.", arguments));
                                        }
                                    }
                                    else
                                    {
                                        MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("网格名称 '{0}' 未找到...", arguments));
                                    }
                                }
                                else
                                {
                                    List<string> added = new List<string>();
                                    foreach (string gridname in m_lastFound)
                                    {
                                        long entId = GetGridEntityId(gridname);
                                        if (entId <= 0) continue;
                                        if (!Core.Settings.Indestructible.Contains(entId))
                                        {
                                            added.Add(gridname);
                                            Core.Settings.Indestructible.Add(entId);
                                        }
                                    }

                                    string list = String.Join(", ", added.ToArray());
                                    Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);
                                    MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("添加 {0} 网格名称: {1}", added.Count, list));
                                }
                            }
                            else if (internalCommand.Equals("rem", StringComparison.OrdinalIgnoreCase) || internalCommand.Equals("remove", StringComparison.OrdinalIgnoreCase) || internalCommand.Equals("del", StringComparison.OrdinalIgnoreCase))
                            {
                                if (arguments.Length > 0)
                                {
                                    long entId = 0;
                                    if (!long.TryParse(arguments, out entId))
                                        entId = GetGridEntityId(arguments);
                                    if (entId > 0)
                                    {
                                        if (!Core.Settings.Indestructible.Contains(entId))
                                        {
                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("网格名称 '{0}' 没有在列表中找到...", arguments));
                                        }
                                        else
                                        {
                                            if (Core.Settings.IndestructibleOverrideBuilds.Contains(entId))
                                                Core.Settings.IndestructibleOverrideBuilds.Remove(entId);
                                            Core.Settings.Indestructible.Remove(entId);
                                            Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);

                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("网格名称 '{0}' 去除.", arguments));
                                        }
                                    }
                                    else
                                    {
                                        MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("网格名称 '{0}' 未找到...", arguments));
                                    }
                                }
                                else
                                {
                                    List<string> removed = new List<string>();
                                    foreach (string gridname in m_lastFound)
                                    {
                                        long entId = GetGridEntityId(gridname);
                                        if (entId <= 0) continue;
                                        if (Core.Settings.Indestructible.Contains(entId))
                                        {
                                            removed.Add(gridname);
                                            Core.Settings.Indestructible.Remove(entId);
                                        }
                                    }

                                    string list = String.Join(", ", removed.ToArray());
                                    Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);
                                    MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("删除 {0} 网格名称: {1}", removed.Count, list));
                                }
                            }
                            else if (internalCommand.Equals("bo", StringComparison.OrdinalIgnoreCase) || internalCommand.Equals("buildon", StringComparison.OrdinalIgnoreCase))
                            {
                                if (arguments.Length > 0)
                                {
                                    long entId = 0;
                                    if (!long.TryParse(arguments, out entId))
                                        entId = GetGridEntityId(arguments);
                                    if (entId > 0)
                                    {
                                        if (!Core.Settings.Indestructible.Contains(entId))
                                        {
                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("网格名称 '{0}' 没有在列表中找到...", arguments));
                                        }
                                        else
                                        {
                                            if (Core.Settings.IndestructibleOverrideBuilds.Contains(entId))
                                                Core.Settings.IndestructibleOverrideBuilds.Remove(entId);
                                            else
                                                Core.Settings.IndestructibleOverrideBuilds.Add(entId);
                                            Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);

                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("对于网格名称 '{0}' 建筑覆盖 {1}.", arguments, Core.Settings.IndestructibleOverrideBuilds.Contains(entId)));
                                        }
                                    }
                                    else
                                    {
                                        MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("网格名称 '{0}' 未找到...", arguments));
                                    }
                                }
                                else
                                {
                                    List<string> marked = new List<string>();
                                    foreach (string gridname in m_lastFound)
                                    {
                                        long entId = GetGridEntityId(gridname);
                                        if (entId <= 0) continue;
                                        if (Core.Settings.Indestructible.Contains(entId))
                                        {
                                            if (Core.Settings.IndestructibleOverrideBuilds.Contains(entId))
                                                Core.Settings.IndestructibleOverrideBuilds.Remove(entId);
                                            else
                                                Core.Settings.IndestructibleOverrideBuilds.Add(entId);
                                            marked.Add(gridname);
                                        }
                                    }

                                    string list = String.Join(", ", marked.ToArray());
                                    Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);
                                    MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("建筑覆盖 {0} 网格名称: {1}", marked.Count, list));
                                }
                            }
                            else if (internalCommand.Equals("go", StringComparison.OrdinalIgnoreCase) || internalCommand.Equals("grindon", StringComparison.OrdinalIgnoreCase))
                            {
                                if (arguments.Length > 0)
                                {
                                    long entId = 0;
                                    if (!long.TryParse(arguments, out entId))
                                        entId = GetGridEntityId(arguments);
                                    if (entId > 0)
                                    {
                                        if (!Core.Settings.Indestructible.Contains(entId))
                                        {
                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("网格名称 '{0}' 没有在列表中找到...", arguments));
                                        }
                                        else
                                        {
                                            if (Core.Settings.IndestructibleOverrideGrindOwner.Contains(entId))
                                                Core.Settings.IndestructibleOverrideGrindOwner.Remove(entId);
                                            else
                                                Core.Settings.IndestructibleOverrideGrindOwner.Add(entId);
                                            Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);

                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("对于网格名称 '{0}' 研磨自己的财产是重写 {1}.", arguments, Core.Settings.IndestructibleOverrideGrindOwner.Contains(entId)));
                                        }
                                    }
                                    else
                                    {
                                        MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("网格名称 '{0}' 未找到...", arguments));
                                    }
                                }
                                else
                                {
                                    List<string> marked = new List<string>();
                                    foreach (string gridname in m_lastFound)
                                    {
                                        long entId = GetGridEntityId(gridname);
                                        if (entId <= 0) continue;
                                        if (Core.Settings.Indestructible.Contains(entId))
                                        {
                                            if (Core.Settings.IndestructibleOverrideGrindOwner.Contains(entId))
                                                Core.Settings.IndestructibleOverrideGrindOwner.Remove(entId);
                                            else
                                                Core.Settings.IndestructibleOverrideGrindOwner.Add(entId);
                                            marked.Add(gridname);
                                        }
                                    }

                                    string list = String.Join(", ", marked.ToArray());
                                    Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);
                                    MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("研磨自己的财产是重写 {0} 网格名称: {1}", marked.Count, list));
                                }
                            }
                            else if (internalCommand.Equals("clear", StringComparison.OrdinalIgnoreCase))
                            {
                                Core.Settings.Indestructible.Clear();
                                Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);

                                MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, "Indestructible list cleared.");
                            }
                            else if (internalCommand.Equals("find", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    uint value = (arguments.Length > 0) ? UInt32.Parse(arguments) : 10;
                                    if (value < 1)
                                        value = 1;
                                    if (value > 100000)
                                        value = 100000;

                                    Vector3D pos = MyAPIGateway.Session.Player.GetPosition();
                                    BoundingSphereD sphere = new BoundingSphereD(pos, value);
                                    List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

                                    string[] found = entities.FindAll(x => x is IMyCubeGrid).Select(x => (x as IMyCubeGrid).DisplayName).ToArray();
                                    string foundedList = String.Join(", ", found);
                                    m_lastFound.Clear();
                                    m_lastFound.AddRange(found);
                                    MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("In radius {0}m found: {1}", value, foundedList));
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log.Error("Exception in command find: {0}", ex.Message);
                                    MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("Exception: {0}", ex.Message));
                                }
                            }
                            else if (internalCommand.Equals("set", StringComparison.OrdinalIgnoreCase))
                            {
                                #region setcommand
                                string ResultMessage = "";
                                string[] argument = (arguments).Split(null as string[], 2, StringSplitOptions.RemoveEmptyEntries);

                                if (argument.Length >= 2)
                                {
                                    bool changed = false;

                                    if (argument[0].Equals("1") || argument[0].Equals("SalaryPerMinute", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            ushort value = UInt16.Parse(argument[1]);
                                            if (value < 0 || value > 10000)
                                            {
                                                MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("非法值. [ 0 - 10000 ]", value));
                                            }
                                            else
                                            {
                                                Core.Settings.SalaryPerMinute = value;
                                                changed = true;
                                                ResultMessage = string.Format("SalaryPerMinute changed to {0}", value);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("Incorrect number. {0}", ex.Message));
                                        }
                                    }
                                    else if (argument[0].Equals("2") || argument[0].Equals("SalaryMax", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            ushort value = UInt16.Parse(argument[1]);
                                            if (value < 0 || value > 50000)
                                            {
                                                MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("非法值. [ 0 - 50000 ]", value));
                                            }
                                            else
                                            {
                                                Core.Settings.SalaryMax = value;
                                                changed = true;
                                                ResultMessage = string.Format("SalaryMax changed to {0}", value);

                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("Incorrect number. {0}", ex.Message));
                                        }
                                    }
                                    else if (argument[0].Equals("3") || argument[0].Equals("ResourcePerMinute", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            ushort value = UInt16.Parse(argument[1]);
                                            if (value < 0 || value > 50000)
                                            {
                                                MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("非法值. [ 0 - 50000 ]", value));
                                            }
                                            else
                                            {
                                                Core.Settings.ResourcePerMinute = value;
                                                changed = true;
                                                ResultMessage = string.Format("ResourcePerMinute changed to {0}", value);

                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("Incorrect number. {0}", ex.Message));
                                        }
                                    }
                                    else if (argument[0].Equals("4") || argument[0].Equals("ResourceMax", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            ushort value = UInt16.Parse(argument[1]);
                                            if (value < 0 || value > 50000)
                                            {
                                                MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("非法值. [ 0 - 50000 ]", value));
                                            }
                                            else
                                            {
                                                Core.Settings.ResourceMax = value;
                                                changed = true;
                                                ResultMessage = string.Format("ResourceMax changed to {0}", value);

                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("Incorrect number. {0}", ex.Message));
                                        }
                                    }
                                    else if (argument[0].Equals("5") || argument[0].Equals("ResourceIncreasePerPlayerMinute", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            ushort value = UInt16.Parse(argument[1]);
                                            if (value < 0 || value > 50000)
                                            {
                                                MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("非法值. [ 0 - 50000 ]", value));
                                            }
                                            else
                                            {
                                                Core.Settings.ResourceIncreasePerPlayerMinute = value;
                                                changed = true;
                                                ResultMessage = string.Format("ResourceIncreasePerPlayerMinute changed to {0}", value);

                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("Incorrect number. {0}", ex.Message));
                                        }
                                    }

                                    //if (argument[0].Equals("1") || argument[0].Equals("DelayBeforeTurningOn", StringComparison.OrdinalIgnoreCase))
                                    //{
                                    //    try
                                    //    {
                                    //        ushort value = UInt16.Parse(argument[1]);
                                    //        if (value < 0 || value > 3600)
                                    //        {
                                    //            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("The value is not within the allowed limits. [ 0 - 3600 ]", value));
                                    //        }
                                    //        else
                                    //        {
                                    //            Core.Settings.DelayBeforeTurningOn = value;
                                    //            changed = true;
                                    //            ResultMessage = string.Format("DelayBeforeTurningOn changed to {0}", value);
                                    //        }
                                    //    }
                                    //    catch (Exception ex)
                                    //    {
                                    //        MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("Incorrect number. {0}", ex.Message));
                                    //    }
                                    //}
                                    //else if (argument[0].Equals("2") || argument[0].Equals("DistanceBeforeTurningOn", StringComparison.OrdinalIgnoreCase))
                                    //{
                                    //    try
                                    //    {
                                    //        ushort value = UInt16.Parse(argument[1]);
                                    //        if (value < 0 || value > 10000)
                                    //        {
                                    //            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("The value is not within the allowed limits. [ 0 - 10000 ]", value));
                                    //        }
                                    //        else
                                    //        {
                                    //            Core.Settings.DistanceBeforeTurningOn = value;
                                    //            changed = true;
                                    //            ResultMessage = string.Format("DistanceBeforeTurningOn changed to {0}", value);

                                    //        }
                                    //    }
                                    //    catch (Exception ex)
                                    //    {
                                    //        MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("Incorrect number. {0}", ex.Message));
                                    //    }
                                    //}
                                    //else if (argument[0].Equals("3") || argument[0].Equals("OnlyForStations", StringComparison.OrdinalIgnoreCase))
                                    //{
                                    //    if (argument[1].Equals("on", StringComparison.OrdinalIgnoreCase) || argument[1].Equals("true", StringComparison.OrdinalIgnoreCase))
                                    //    {
                                    //        Core.Settings.OnlyForStations = true;
                                    //        changed = true;
                                    //    }
                                    //    else if (argument[1].Equals("off", StringComparison.OrdinalIgnoreCase) || argument[1].Equals("false", StringComparison.OrdinalIgnoreCase))
                                    //    {
                                    //        Core.Settings.OnlyForStations = false;
                                    //        changed = true;
                                    //    }
                                    //    ResultMessage = string.Format("OnlyForStations changed to {0}", (Core.Settings.OnlyForStations) ? "On" : "Off");
                                    //}
                                    //else if (argument[0].Equals("4") || argument[0].Equals("OnlyWithZeroSpeed", StringComparison.OrdinalIgnoreCase))
                                    //{
                                    //    if (argument[1].Equals("on", StringComparison.OrdinalIgnoreCase) || argument[1].Equals("true", StringComparison.OrdinalIgnoreCase))
                                    //    {
                                    //        Core.Settings.OnlyWithZeroSpeed = true;
                                    //        changed = true;
                                    //    }
                                    //    else if (argument[1].Equals("off", StringComparison.OrdinalIgnoreCase) || argument[1].Equals("false", StringComparison.OrdinalIgnoreCase))
                                    //    {
                                    //        Core.Settings.OnlyWithZeroSpeed = false;
                                    //        changed = true;
                                    //    }
                                    //    ResultMessage = string.Format("OnlyWithZeroSpeed changed to {0}", (Core.Settings.OnlyWithZeroSpeed) ? "On" : "Off");
                                    //}
                                    //else if (argument[0].Equals("5") || argument[0].Equals("BuildingNotAllowed", StringComparison.OrdinalIgnoreCase))
                                    //{
                                    //    if (argument[1].Equals("on", StringComparison.OrdinalIgnoreCase) || argument[1].Equals("true", StringComparison.OrdinalIgnoreCase))
                                    //    {
                                    //        Core.Settings.BuildingNotAllowed = true;
                                    //        changed = true;
                                    //    }
                                    //    else if (argument[1].Equals("off", StringComparison.OrdinalIgnoreCase) || argument[1].Equals("false", StringComparison.OrdinalIgnoreCase))
                                    //    {
                                    //        Core.Settings.BuildingNotAllowed = false;
                                    //        changed = true;
                                    //    }
                                    //    ResultMessage = string.Format("BuildingNotAllowed changed to {0}", (Core.Settings.BuildingNotAllowed) ? "On" : "Off");
                                    //}
                                    else if (argument[0].Equals("6") || argument[0].Equals("IndestructibleNoBuilds", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (argument[1].Equals("on", StringComparison.OrdinalIgnoreCase) || argument[1].Equals("true", StringComparison.OrdinalIgnoreCase))
                                        {
                                            Core.Settings.IndestructibleNoBuilds = true;
                                            changed = true;
                                        }
                                        else if (argument[1].Equals("off", StringComparison.OrdinalIgnoreCase) || argument[1].Equals("false", StringComparison.OrdinalIgnoreCase))
                                        {
                                            Core.Settings.IndestructibleNoBuilds = false;
                                            changed = true;
                                        }
                                        ResultMessage = string.Format("IndestructibleNoBuilds changed to {0}", (Core.Settings.IndestructibleNoBuilds) ? "On" : "Off");
                                    }
                                    else if (argument[0].Equals("7") || argument[0].Equals("IndestructibleGrindOwner", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (argument[1].Equals("on", StringComparison.OrdinalIgnoreCase) || argument[1].Equals("true", StringComparison.OrdinalIgnoreCase))
                                        {
                                            Core.Settings.IndestructibleGrindOwner = true;
                                            changed = true;
                                        }
                                        else if (argument[1].Equals("off", StringComparison.OrdinalIgnoreCase) || argument[1].Equals("false", StringComparison.OrdinalIgnoreCase))
                                        {
                                            Core.Settings.IndestructibleGrindOwner = false;
                                            changed = true;
                                        }
                                        ResultMessage = string.Format("IndestructibleGrindOwner changed to {0}", (Core.Settings.IndestructibleGrindOwner) ? "On" : "Off");
                                    }
                                    else if (argument[0].Equals("8") || argument[0].Equals("LimitGridSizes", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            ushort value = UInt16.Parse(argument[1]);
                                            if (value < 0 || value > 1000)
                                            {
                                                MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("The value is not within the allowed limits. [ 0 - 1000 ]", value));
                                            }
                                            else
                                            {
                                                Core.Settings.LimitGridSizes = value;
                                                changed = true;
                                                ResultMessage = string.Format("LimitGridSizes changed to {0}", value);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("Incorrect number. {0}", ex.Message));
                                        }
                                    }
                                    else if (argument[0].Equals("9") || argument[0].Equals("LimitPerFaction", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            ushort value = UInt16.Parse(argument[1]);
                                            if (value < 1 || value > 100)
                                            {
                                                MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("The value is not within the allowed limits. [ 1 - 100 ]", value));
                                            }
                                            else
                                            {
                                                Core.Settings.LimitPerFaction = value;
                                                changed = true;
                                                ResultMessage = string.Format("LimitPerFaction changed to {0}", value);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("Incorrect number. {0}", ex.Message));
                                        }
                                    }
                                    else if (argument[0].Equals("10") || argument[0].Equals("LimitPerPlayer", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            ushort value = UInt16.Parse(argument[1]);
                                            if (value < 1 || value > 100)
                                            {
                                                MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("The value is not within the allowed limits. [ 1 - 100 ]", value));
                                            }
                                            else
                                            {
                                                Core.Settings.LimitPerPlayer = value;
                                                changed = true;
                                                ResultMessage = string.Format("每玩家限制改为 {0}", value);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("Incorrect number. {0}", ex.Message));
                                        }
                                    }
                                    else if (argument[0].Equals("11") || argument[0].Equals("CleaningFrequency", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            ushort value = UInt16.Parse(argument[1]);
                                            if (value < 0 || value > 3600)
                                            {
                                                MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("The value is not within the allowed limits. [ 0 - 3600 ]", value));
                                            }
                                            else
                                            {
                                                Core.Settings.CleaningFrequency = value;
                                                changed = true;
                                                ResultMessage = string.Format("CleaningFrequency changed to {0}", value);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, string.Format("Incorrect number. {0}", ex.Message));
                                        }
                                    }

                                    if (changed)
                                    {
                                        Core.SendSettingsToServer(Core.Settings, MyAPIGateway.Session.Player.SteamUserId);

                                        SyncPacket newpacket = new SyncPacket();
                                        newpacket.proto = SyncPacket.Version;
                                        newpacket.command = (ushort)Command.MessageToChat;
                                        newpacket.message = ResultMessage;
                                        Core.SendMessage(newpacket); // send to others
                                    }
                                    if (!Core.IsServer)
                                        MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, ResultMessage);
                                }
                                else
                                {
                                    MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, "Incorrect syntax for command set. Use: /bs set var value");
                                }
                                #endregion setcommand
                            }
                            #endregion alladminsettings
                        }
                        else
                        {
                            MyAPIGateway.Utilities.ShowMessage(Core.MODSAY, "Only an administrator can change the settings...");
                        }
                    }
                }
                sendToOthers = false;
            }
        }

        private static long GetGridEntityId(string name)
        {
            Logger.Log.Debug("GetGridEntityId() - {0}", name);
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
            foreach (IMyCubeGrid grid in entities)
            {
                if (grid.DisplayName == name)
                    return grid.EntityId;
            }
            return 0;
        }

        private static string GetGridName(long entityId)
        {
            Logger.Log.Debug("GetGridName() - {0}", entityId);
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
            foreach (IMyCubeGrid entity in entities)
            {
                if (entity.EntityId == entityId)
                    return entity.DisplayName;
            }
            return null;
        }
    }
}
