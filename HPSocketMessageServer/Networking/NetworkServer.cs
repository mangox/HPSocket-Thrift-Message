﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using HPSocketCS;
using MessageProtocol;

namespace Networking
{
    [StructLayout(LayoutKind.Sequential)]
    public class ClientInfo
    {
        public IntPtr ConnId { get; set; }
        public string IpAddress { get; set; }
        public ushort Port { get; set; }
    }

    public class NetworkServer
    {
        private static NetworkServer _instance = null;
        private HPSocketCS.TcpPackServer server = null;
        public HPSocketCS.ServiceState ServerState { get { return server.State; } }

        #region 包头校验标志 | 包最大长度

        private ushort PackHeaderFlag = 0xF2; //65282
        private uint MaxPackSize = 0x1000; //65536

        #endregion

        #region 收发控制
        public static object recvLock = new object();
        public static Queue<Package> recvQueue = new Queue<Package>();

        public static object sendLock = new object();
        #endregion

        #region 服务器初始化
        private NetworkServer()
        {
            server = new TcpPackServer();

            server.OnPrepareListen += new TcpServerEvent.OnPrepareListenEventHandler(this.OnPrepareListen);
            server.OnAccept += new TcpServerEvent.OnAcceptEventHandler(this.OnAccept);
            server.OnSend += new TcpServerEvent.OnSendEventHandler(this.OnSend);
            server.OnReceive += new TcpServerEvent.OnReceiveEventHandler(this.OnReceive);
            server.OnClose += new TcpServerEvent.OnCloseEventHandler(this.OnClose);
            server.OnShutdown += new TcpServerEvent.OnShutdownEventHandler(this.OnShutdown);
            
            // 设置包头标识,与对端设置保证一致性
            server.PackHeaderFlag = this.PackHeaderFlag;
            // 设置最大封包大小
            server.MaxPackSize = this.MaxPackSize;
        }
        
        public static NetworkServer Instance()
        {
            if (_instance == null)
            {
                _instance = new NetworkServer();
            }
            return _instance;
        }

        public bool Start(string IpAddress, ushort Port)
        {
            server.IpAddress = IpAddress;
            server.Port = Port;

            try
            {
                if (server.Start())
                {
                    Console.WriteLine(string.Format("服务启动成功->({0}:{1})", server.IpAddress, server.Port));
                    return true;
                }
                else
                {
                    throw new Exception(string.Format("服务启动失败->{0}({1})", server.ErrorMessage, server.ErrorCode));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("服务启动失败：" + ex.Message);
                return false;
            }
        }

        public bool Stop()
        {
            server.Stop();
            return true;
        }

        #endregion

        #region 接收线程

        protected HandleResult OnPrepareListen(IntPtr soListen)
        {
            Console.WriteLine("启动监听...");
            return HandleResult.Ok;
        }

        protected HandleResult OnAccept(IntPtr connId, IntPtr pClient)
        {
            // 获取客户端ip和端口
            string ip = string.Empty;
            ushort port = 0;
            if (server.GetRemoteAddress(connId, ref ip, ref port))
            {
                Console.WriteLine(string.Format(" > [{0},OnAccept] -> PASS({1}:{2})", connId, ip.ToString(), port));
            }
            else
            {
                Console.WriteLine(string.Format(" > [{0},OnAccept] -> Server_GetClientAddress() Error", connId));
            }
            
            // 设置附加数据，目前没什么用
            ClientInfo clientInfo = new ClientInfo();
            clientInfo.ConnId = connId;
            clientInfo.IpAddress = ip;
            clientInfo.Port = port;
            if (server.SetExtra(connId, clientInfo) == false)
            {
                Console.WriteLine(string.Format(" > [{0},OnAccept] -> SetConnectionExtra fail", connId));
            }
            return HandleResult.Ok;
        }

        protected HandleResult OnSend(IntPtr connId, byte[] bytes)
        {
            return HandleResult.Ok;
        }

        protected HandleResult OnReceive(IntPtr connId, byte[] bytes)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream(bytes);
            Thrift.Transport.TTransport tp = new Thrift.Transport.TStreamTransport(ms, null);
            Thrift.Protocol.TCompactProtocol cp = new Thrift.Protocol.TCompactProtocol(tp);
            try
            {
                Package pack = null;
                short packType = cp.ReadI16();
                switch (packType)
                {
                    case PackType.Command:
                        {
                            try
                            {
                                string test = cp.ReadString();
                                pack = new Package() { ConnId = connId, PackType = packType, Content = test };
                            }
                            catch (Thrift.TException ex)
                            {
                                // 包解析错误
                                Console.WriteLine(ex.Message);
                                pack = null;
                            }
                        }
                        break;
                    case PackType.ResultModel:
                        {
                            try
                            {
                                ResultModel rm = new ResultModel();
                                rm.Read(cp);
                                pack = new Package() { ConnId = connId, PackType = packType, Content = rm };
                            }
                            catch(Thrift.TException ex)
                            {
                                // 包解析错误
                                Console.WriteLine(ex.Message);
                                pack = null;
                            }
                        }
                        break;
                    case PackType.RequestTask:
                        {
                            try
                            {
                                RequestTask task = new RequestTask();
                                task.Read(cp);
                                pack = new Package() { ConnId = connId, PackType = packType, Content = task };
                            }
                            catch(Thrift.TException ex)
                            {
                                // 包解析错误
                                Console.WriteLine(ex.Message);
                                pack = null;
                            }
                        }
                        break;
                    default:
                        pack = null;
                        break;
                }

                if (pack != null)
                {
                    lock (recvLock)
                    {
                        recvQueue.Enqueue(pack);
                    }
                }

                return HandleResult.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine("接收函数异常: " + ex.Message);
                return HandleResult.Ignore;
            }
        }

        protected HandleResult OnClose(IntPtr connId, SocketOperation enOperation, int errorCode)
        {
            HandleResult result = errorCode == 0 ? HandleResult.Ok : HandleResult.Error;
            
            if (server.RemoveExtra(connId) == false)
            {
                Console.WriteLine(string.Format(" > [{0},OnClose] -> SetConnectionExtra({0}, null) fail", connId));
            }
            //Util.Logger.Instance().InfoFormat("> [{0}, OnClose] -> 断开连接", connId);
            return result;
        }

        protected HandleResult OnShutdown()
        {
            // Todo
            // 检查是否有东西需要保存到磁盘
            return HandleResult.Ok;
        }

        HandleResult OnHandShake(IntPtr connId)
        {
            // 握手了
            return HandleResult.Ok;
        }
        #endregion

        #region 发送函数

        public void Send(Package pkg)
        {
            lock (sendLock)
            {
                if (pkg == null) return;
                try
                {


                    System.IO.MemoryStream msWBuffer = new System.IO.MemoryStream();
                    Thrift.Transport.TTransport tp = new Thrift.Transport.TStreamTransport(null, msWBuffer);
                    Thrift.Protocol.TCompactProtocol cp = new Thrift.Protocol.TCompactProtocol(tp);

                    switch (pkg.PackType)
                    {
                        case PackType.Command:
                            {
                                cp.WriteI16(PackType.Command);
                                cp.WriteString((string)pkg.Content);
                            }
                            break;
                        case PackType.TaskModel:
                            {
                                cp.WriteI16(PackType.TaskModel);
                                TaskModel task = pkg.Content as TaskModel;
                                task.Write(cp);
                            }
                            break;
                        default:
                            break;
                    }

                    tp.Flush();
                    byte[] buffer = msWBuffer.ToArray();
                    server.Send(pkg.ConnId, buffer, buffer.Length);
                    msWBuffer.Flush();
                }
                catch(Exception ex)
                {
                    Console.WriteLine("发送消息错误："+ex.Message);
                }
            }
        }
        
        #endregion
    }
}
