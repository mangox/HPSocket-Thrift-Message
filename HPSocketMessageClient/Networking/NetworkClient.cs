using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MessageProtocol;
using HPSocketCS;

namespace Networking
{
    public enum ClientStatus
    {
        Stopped = 0,
        Running = 1,
        StopSending = 2
    }

    public class NetworkClient
    {
        private static NetworkClient _instance = null;
        HPSocketCS.TcpPackClient client = null;

        // 客户端状态
        private ClientStatus _status;
        private object _statusLock = new object();
        public ClientStatus Status
        {
            get { lock (_statusLock) { return _status; } }
            set { lock (_statusLock) { _status = value; } }
        }

        #region 包头校验标志 | 包最大长度 
        private ushort PackHeaderFlag = 0xF2; //65282
        private uint MaxPackSize = 0x1000; //65536
        #endregion

        #region 收发控制
        public static object recvLock = new object();
        public static Queue<Package> recvQueue = new Queue<Package>();
        private object sendLock = new object();
        #endregion

        #region 初始化客户端及连接函数

        private NetworkClient()
        {
            client = new HPSocketCS.TcpPackClient();

            client.OnPrepareConnect += new TcpClientEvent.OnPrepareConnectEventHandler(OnPrepareConnect);
            client.OnConnect += new TcpClientEvent.OnConnectEventHandler(OnConnect);
            client.OnSend += new TcpClientEvent.OnSendEventHandler(OnSend);
            client.OnReceive += new TcpClientEvent.OnReceiveEventHandler(OnReceive);
            client.OnClose += new TcpClientEvent.OnCloseEventHandler(OnClose);

            client.PackHeaderFlag = this.PackHeaderFlag;
            client.MaxPackSize = this.MaxPackSize;
            Status = ClientStatus.Stopped;
        }

        public static NetworkClient Instance()
        {
            if(_instance == null)
            {
                _instance = new NetworkClient();
            }
            return _instance;
        }

        public bool Connect(string ServerIPAddress, ushort Port)
        {
            try
            { 
                if(client.Connect(ServerIPAddress, Port))
                {
                    Status = ClientStatus.Running;
                    return true;
                }
                else
                {
                    Status = ClientStatus.Stopped;
                    throw new Exception(string.Format("客户端启动失败 -> {0}({1})", client.ErrorMessage, client.ErrorCode));
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("[ERROR]连接失败："+ex.Message);
                Status = ClientStatus.Stopped;
                return false;
            }
        }
        #endregion

        #region 接收函数

        HandleResult OnHandShake(TcpClient client)
        {
            // 握手了
            return HandleResult.Ok;
        }

        protected HandleResult OnPrepareConnect(TcpClient sender, IntPtr socket)
        {
            return HandleResult.Ok;
        }

        protected HandleResult OnConnect(TcpClient sender)
        {
            // 已连接 到达一次

            // 如果是异步联接,更新界面状态
            //this.Invoke(new ConnectUpdateUiDelegate(ConnectUpdateUi));

            //AddMsg(string.Format(" > [{0},OnConnect]", sender.ConnectionId));

            return HandleResult.Ok;
        }

        protected HandleResult OnSend(TcpClient sender, byte[] bytes)
        {
            // 客户端发数据了
            //AddMsg(string.Format(" > [{0},OnSend] -> ({1} bytes)", sender.ConnectionId, bytes.Length));

            return HandleResult.Ok;
        }

        protected HandleResult OnReceive(TcpClient sender, byte[] bytes)
        {
            try
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream(bytes);
                Thrift.Transport.TTransport tp = new Thrift.Transport.TStreamTransport(ms, null);
                Thrift.Protocol.TCompactProtocol cp = new Thrift.Protocol.TCompactProtocol(tp);

                Package pack = null;
                short packType = cp.ReadI16();
                switch (packType)
                {
                    case PackType.Command:
                        {
                            try
                            {
                                string test = cp.ReadString();
                                pack = new Package() { PackType = packType, Content = test };
                            }
                            catch(Thrift.TException ex)
                            {
                                // 包解析错误
                                Console.WriteLine(ex.Message);
                                pack = null;
                            }
                        }
                        break;
                    case PackType.TaskModel:
                        {
                            try
                            {
                                TaskModel tm = new TaskModel();
                                tm.Read(cp);
                                pack = new Package() { PackType = packType, Content = tm };
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

        protected HandleResult OnClose(TcpClient sender, SocketOperation enOperation, int errorCode)
        {
            HandleResult result;
            if (errorCode == 0)
                // 连接关闭了
                //AddMsg(string.Format(" > [{0},OnClose]", sender.ConnectionId));
                result = HandleResult.Ok;
            else
                result = HandleResult.Error;

            Status = ClientStatus.Stopped;
            Console.WriteLine("掉线了");
            return result;
        }
        #endregion

        #region 发送函数

        public void Send(Package pkg)
        {
            if (Status == ClientStatus.Stopped)
            {
                return;
            }
            
            if (pkg != null)
            {
                lock (sendLock)
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
                        case PackType.ResultModel:
                            {
                                cp.WriteI16(PackType.ResultModel);
                                ResultModel rm = (ResultModel)pkg.Content;
                                rm.Write(cp);
                            }
                            break;
                        case PackType.RequestTask:
                            {
                                cp.WriteI16(PackType.RequestTask);
                                RequestTask rm = (RequestTask)pkg.Content;
                                rm.Write(cp);
                            }
                            break;
                        default:
                            break;
                    }

                    tp.Flush();
                    byte[] buffer = msWBuffer.ToArray();
                    client.Send(buffer, buffer.Length);
                    msWBuffer.Flush();
                }
            }
        }
        #endregion
    }
}
