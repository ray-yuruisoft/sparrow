﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MixLibrary;
using System.Runtime.InteropServices;

namespace GameServer
{
    public class Program
    {
        public static int noTableWorkerCount = 32;
        public static TimerService timerSvc = new TimerService();
        public static WorkerManager workerMgr = new WorkerManager();
        public static DatabaseService dbSvc = new DatabaseService();
        public static DBHelper dbHelper = new DBHelper();
        public static GameServer server = new GameServer();
        public static ModuleManager moduleManager = new ModuleManager();

        public static void Main(string[] args)
        {
            //挂载全局异常处理
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            if (GCSettings.IsServerGC)
            {
                Console.WriteLine("GC优化已开启");
            }

            var config = Configure.Inst;

            if (!config.Load())
                return;

            int workerCount = config.workerCount + noTableWorkerCount;

            dbSvc.Start(config.dbConnectStr, workerCount);
            dbHelper.Start();
            workerMgr.Start(workerCount);
            if(!server.Start(config.serverPort, 10000))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("端口：{0}被占用，请按任意键退出", config.serverPort);
                Console.ResetColor();
                Console.ReadKey();
                return;
            }
            moduleManager.Start();
            timerSvc.Start();

            Console.WriteLine("游戏服务器启动完毕，端口：{0}", config.serverPort);

            //Thread thread = new Thread(ClearMemoryThreadProc);
            //thread.IsBackground = true;
            //thread.Start();

            while (true)
            {
                var key = Console.ReadKey();

                if (key.Key == ConsoleKey.S)
                {
                    Console.WriteLine("");
                    Console.WriteLine("当前连接数：{0} 连接池存量：{1}",
                        server.connectNum, server.GetSessionPoolCount());
                    Console.WriteLine("支持游戏：{0}", Configure.Inst.supportGames);
                }
                if (key.Key == ConsoleKey.T)
                {
                    Configure.Inst.isShowStat = !Configure.Inst.isShowStat;

                    Console.WriteLine("");
                    if (Configure.Inst.isShowStat)
                        Console.WriteLine("已打开统计信息显示");
                    else
                        Console.WriteLine("已关闭统计信息显示");
                }
                if (key.Key == ConsoleKey.C)
                {
                    Console.Clear();
                }
                if (key.Key == ConsoleKey.Q)
                {
                    break;
                }
                if(key.Key == ConsoleKey.R)
                {
                    Console.WriteLine("");
                    moduleManager.gameModule.LoadAllConfigs();
                }
                if (key.Key == ConsoleKey.D1)
                {
                    Console.WriteLine("");
                    moduleManager.gameModule.SetAllGameIsShowInfo(true);
                    Console.WriteLine("已打开游戏内信息显示");
                }
                if (key.Key == ConsoleKey.D2)
                {
                    Console.WriteLine("");
                    moduleManager.gameModule.SetAllGameIsShowInfo(false);
                    Console.WriteLine("已关闭游戏内信息显示");
                }
                //if(key.Key == ConsoleKey.W)
                //{
                //    dbHelper.LogPlayGame(@"D:\Work\ChessServers\bin\GameServer\Games\Br1", "test", "12312414", "阿拉丁", 0, 100, new JObject());
                //    dbHelper.LogGame(@"D:\Work\ChessServers\bin\GameServer\Games\Br1", "test", "阿拉丁", 0, 100, new JObject());
                //}

                Thread.Sleep(100);
            }

            timerSvc.Stop();
            moduleManager.Stop();
            server.Stop();
            workerMgr.Stop();
            dbHelper.Stop();
            dbSvc.Stop();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogUtil.Log("捕获到全局异常：{0}", e.ExceptionObject);
        }

        private static void ClearMemoryThreadProc()
        {
            while (true)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Thread.Sleep(5000);
            }
        }
    }
}
