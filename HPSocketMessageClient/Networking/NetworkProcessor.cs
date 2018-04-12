using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageProtocol;

namespace Networking
{
    public class NetworkProcessor
    {
        private string IPAddress = "127.0.0.1";
        private ushort Port = 8355;

        private Semaphore SempRequest = new Semaphore(1, 1);
        private Dictionary<string, Thread> DictThreads = new Dictionary<string, Thread>();

        private Networking.NetworkClient client = Networking.NetworkClient.Instance();

        public NetworkProcessor()
        {
        }

        public void Run()
        {
            Thread HandleThread = new Thread(new ThreadStart(HandlingThreadFunc)) { IsBackground = true, Name = "Thread# Handling Thread", Priority = ThreadPriority.AboveNormal };
            Thread RequestThread = new Thread(new ThreadStart(RequestTaskFunc)) { IsBackground = true, Name = "Thread# Request Task" };
            HandleThread.Start();
            DictThreads.Add("RequestThread", RequestThread);
            DictThreads.Add("HandleThread", HandleThread);

            if (this.client.Connect(IPAddress, Port))
            {
                Console.WriteLine("初次连接成功!");
                DictThreads["RequestThread"].Start();
            }
        }

        #region 网络事件定义
        /// <summary>
        /// 在没有任务再跑的情况下，5s发一条Request
        /// 有任务的情况下，要等任务跑完了才能发下一条Request
        /// </summary>
        private void RequestTaskFunc()
        {
            while (true)
            {
                SempRequest.WaitOne();

                MessageProtocol.RequestTask rt = new RequestTask();
                rt.ClientId = "client#1";
                rt.Version = "1.0.1";
                rt.Message = "";
                Console.WriteLine("-------请求任务---------");
                Package rtPkg = new Package() { PackType = PackType.RequestTask, Content = rt };
                this.client.Send(rtPkg);

                SempRequest.Release();

                Thread.Sleep(5000);
            }
        }

        private void HandlingThreadFunc()
        {
            while (true)
            {
                List<Package> cacheQ = new List<Package>();

                lock (Networking.NetworkClient.recvLock)
                {
                    while (NetworkClient.recvQueue.Count > 0)
                    {
                        cacheQ.Add(NetworkClient.recvQueue.Dequeue());
                    }
                }

                foreach (var pkg in cacheQ)
                {
                    switch (pkg.PackType)
                    {
                        case PackType.Command:
                            {
                                string strCmd = (string)pkg.Content;
                                ProcessCommand(strCmd);
                            }
                            break;
                        case PackType.TaskModel:
                            {
                                TaskModel tm = pkg.Content as TaskModel;
                                ProcessTask(tm);
                            }
                            break;
                        default:
                            Console.WriteLine("未知包类型:" + pkg.PackType.ToString());
                            break;
                    } // end of switch
                }
                BeforeSleeping();
            } // end of while
        }

        private void ProcessCommand(string cmd)
        {
            switch (cmd)
            {
                case "reconnect": // 重连接
                    ProcessReconnect();
                    break;
                default:
                    break;
            }
        }

        private void ProcessReconnect()
        {
            try
            {
                if (client.Connect(IPAddress, Port))
                {
                    Console.WriteLine("重连成功!");
                    Thread RequestThread = new Thread(new ThreadStart(RequestTaskFunc)) { IsBackground = true, Name = "Thread# Request Task" };

                    try
                    {
                        DictThreads["RequestThread"].Abort();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("终止RequestTask时发生异常：" + ex.Message);
                    }
                    finally
                    {
                        DictThreads["RequestThread"] = RequestThread;
                    }

                    RequestThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("重新连接出现异常: " + ex.Message);
            }
        }

        private void ProcessTask(TaskModel tmodel)
        {
            Thread computingThread = new Thread(new ParameterizedThreadStart(StartTask)) { IsBackground = true, Priority = ThreadPriority.Highest, Name = "Task" };
            computingThread.Start(tmodel);
        }

        private object taskLock = new object();
        private void StartTask(object taskobj)
        {
            lock(taskLock)
            {
                SempRequest.WaitOne();

                TaskModel tmodel = taskobj as TaskModel;
                ResultModel rm = new ResultModel();

                try
                {
                    #region 处理任务
                    Thread.Sleep(5000);
                    Console.WriteLine(string.Format("处理工程{0}中的任务Id:{1}", tmodel.ProjectId, tmodel.TaskId));
                    Thread.Sleep(5000);
                    #endregion

                    rm.ClientId = "client#1";
                    rm.ProjectId = tmodel.ProjectId;
                    rm.Result = "right result";
                    rm.Status = true;
                }
                catch(Exception ex)
                {
                    rm.ClientId = "client#1";
                    rm.ProjectId = tmodel.ProjectId;
                    rm.Result = ex.Message;
                    rm.Status = false;
                }
                finally
                {
                    Package pkgResponse = new Package() { PackType = PackType.ResultModel, Content = rm };
                    this.client.Send(pkgResponse);
                }
                
                SempRequest.Release();
            }
        }

        private int counter = 0;
        protected void BeforeSleeping()
        {
            #region 重连
            if (client.Status == ClientStatus.Stopped && counter % 60 == 0) // 如果掉线，1分钟重新连接一次
            {
                Console.Write("连接中...");
                // 重连包
                Package pkgReconnect = new Package() { PackType = PackType.Command, Content = "reconnect" };
                lock (Networking.NetworkClient.recvLock) { Networking.NetworkClient.recvQueue.Enqueue(pkgReconnect); }
            }
            #endregion
            counter++;
            Thread.Sleep(1000);
        }
        #endregion
    }
}
