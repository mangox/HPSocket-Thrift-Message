﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using MessageProtocol;

namespace Networking
{
    public class NetworkActionProcessor
    {
        private Queue<Package> pkgQueue;
        private bool threadRunFlag = false;
        
        private int NETWORK_THREADS = 1;

        public NetworkActionProcessor(ref Queue<Package> queue)
        {
            this.pkgQueue = queue;
            // 处理网络IO的自定义线程池：目前先创建一个线程试试看，之后线程池可以尝试增加
            CreateThreadPool(NETWORK_THREADS);
        }

        private void CreateThreadPool(int count)
        {
            for(int i = 0; i < count; i++)
            {
                Thread th = new Thread(new ThreadStart(BusinessThreadFunc)) { IsBackground = true, Priority= ThreadPriority.Highest, Name = string.Format("Thread# Business{0}", i + 1)};
                threadRunFlag = true;
                th.Start();
            }
        }

        private void BusinessThreadFunc()
        {
            while(threadRunFlag)
            {
                List<Package> cachedQ = new List<Package>();
                lock (Networking.NetworkServer.recvLock)
                {
                    while (this.pkgQueue.Count > 0) { cachedQ.Add(this.pkgQueue.Dequeue()); }
                }

                foreach(var pkgReq in cachedQ)
                {
                    switch (pkgReq.PackType)
                    {
                        case PackType.Command:
                            ProcessCmdString(pkgReq);
                            break;
                        case PackType.RequestTask:
                            ProcessRequestTask(pkgReq);
                            break;
                        case PackType.ResultModel:
                            ProcessResult(pkgReq);
                            break;
                        case PackType.Rollback:
                            ProcessRollback(pkgReq);
                            break;
                        default:
                            ProcessUnknowPkg(pkgReq);
                            break;
                    }
                }

                BeforeSleeping(); 
            }
        }
        
        protected void BeforeSleeping()
        {
           Thread.Sleep(50);
        }

        protected void ProcessCmdString(Package pkg)
        {
            Package response = null;
            string strCmd = (string)pkg.Content;

            switch (strCmd)
            {
                case "loadversion":  // web访问时间太长，没有放在这里
                    break;
                case "loadblocks":
                    break;
                default:
                    // 未知命令
                    string unknowCmd = string.Format("UNKNOW COMMAND: {0}", strCmd);
                    response = new Package() { ConnId = pkg.ConnId, Content = unknowCmd, PackType = MessageProtocol.PackType.Command };
                    break;
            }

            Networking.NetworkServer.Instance().Send(response);
        }

        private void ProcessRollback(Package pkgReq)
        {
        }

        protected void ProcessResult(Package resultPkg)
        {
            try
            {
                ResultModel result = resultPkg.Content as ResultModel;
                Console.WriteLine("收到结果{0}", result.Result);

                if(!result.Status)
                {
                    // 从缓存队列中得到任务，案例没有实现这个队列，仅做参考
                    var task = new object();
                    ProcessRollback(new Package() { Content = task, ConnId = resultPkg.ConnId });
                    return;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("结果包处理错误："+ex.Message);
            }
        }
        

        protected void ProcessRequestTask(Package taskPkg)
        {
            RequestTask request = taskPkg.Content as RequestTask;
            
            try
            {
               Console.WriteLine("收到{0}的任务包, Version:{1}", request.ClientId, request.Version);
               if(request.Version == "1.0.1")
                {
                    TaskModel tmodel = new TaskModel();
                    tmodel.ProjectId = "project1";
                    tmodel.TaskId = "task1";

                    Package response = new Package() { ConnId = taskPkg.ConnId, PackType = PackType.TaskModel, Content = tmodel };
                    Networking.NetworkServer.Instance().Send(response);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("请求任务错误："+ex.Message);
            }
        }

        protected void ProcessUnknowPkg(Package pkgReq)
        {
            //返回错误
            string respStr = string.Format("UNKNOWN PKG:{0}", pkgReq.PackType);
            Package response = new Package() { ConnId = pkgReq.ConnId, PackType = PackType.Command, Content = respStr };
            Networking.NetworkServer.Instance().Send(response);
        }
    }
}
