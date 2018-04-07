//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  CoreBuilderHost Package                                                 //
//      This package has functionality for handling build requests.         //
//                                                                          //
//  Language:     C#, VS 2017                                               //
//  Platform:     Macbook Pro 2017, Bootcamp Windows 10                     //
//  Application:  Federation Server                                         //
//  Author:       Rohit Kulkarni, Syracuse University                       //
//                rvkulkar@syr.edu                                          //
//////////////////////////////////////////////////////////////////////////////
/*
 * Package operations
 * ==================
 * This package derives from the communication object. This package basically hosts
 * the RemoteBuildServer. It creates a compiler instance and delegates build reqs
 * to it.
 * 
 * Types and interfaces
 * ====================
 * CoreBuilderHost -    This class contains not much logic. It just does configuration
 *                      and delegates the build requests to the compiler instance.
 * 
 * Required files
 * ==============
 *  CoreBuilderHost.cs
 * 
 * Public Interface
 * ================
 * SetMotherBuilderURL - This method is used to configure mother builder
 *                       URL for the communication between child and mother
 *                       processes.
 * Start               - This method is used to start the child builder process
 *                       message handler. The message handler will start receiving 
 *                       messages / build requests. It also sends the first ready
 *                       message to the mother builder process. It also creates 
 *                       an instance of CSharpCompiler class which will actually 
 *                       process build requests from mother builder.
 * 
 * Version
 * =======
 * V1.0 - Added package which supports basic functionality.
 * 
 */


using System;
using MessagePassingCommunuication;
using System.Threading;
using RemoteBuildServer.Interfaces;
using System.IO;
using RemoteBuildServer.CompilerServices;

namespace RemoteBuildServer.BuildServer
{
    class CoreBuilderHost : CommunicationBase
    {
        private ICompilerService compilerService = null;
        private object wakeMeUp = new object();
        private string baseStorageFolder = string.Empty;
        private CommConfig config = null;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseAddress"></param>
        /// <param name="port"></param>
        public CoreBuilderHost(string baseAddress, string port) : base(Identity.ChildProcess)
        {
            this.baseStorageFolder = base.defaultConfigs[Identity.MotherProcess].BaseDirectory + "\\" + port;
            this.config = new CommConfig(Identity.ChildProcess, baseAddress, port, this.baseStorageFolder);
        }

        /// <summary>
        /// Create compiler. Start Message handler thread. Send ready message to
        /// the mother builder process. Put main thread to sleep.
        /// </summary>
        public void Start()
        {
            this.StartListener(this.config);
            if (!Directory.Exists(this.baseStorageFolder))
                this.baseStorageFolder = Directory.CreateDirectory(this.baseStorageFolder).FullName;
            else
                this.baseStorageFolder = Path.GetFullPath(this.baseStorageFolder);

            //CompilerServices.CompilerFactory.host = this.commObject;
            this.compilerService = new CSharpCompiler(this.commObject, base.defaultConfigs);
            Console.WriteLine("\n\nChild builder process started at : " + this.config.ServiceURL);
            var com = new CommMessage(CommMessage.MessageType.Request)
            {
                Command = CommCommands.READY,
                To = base.defaultConfigs[Identity.MotherProcess].ServiceURL
            };
            this.compilerService.ExecuteCommCommand(com);
            while (!shutDown)
            {
                lock (wakeMeUp)
                {
                    //  Main thread sleeping till child process shutdown is called.
                    //  Use the main thread to process any other events. For now
                    //  all the processing is with Compiler so main thread is not
                    //  doing anything.
                    Monitor.Wait(wakeMeUp);
                }
            }
        }

        /// <summary>
        /// Handle all the messages received by the receiver.
        /// </summary>
        /// <param name="msg"></param>
        protected override void HandleCommunicationMessage(CommMessage msg)
        {
            this.compilerService.ExecuteCommCommand(msg);
        }

        /// <summary>
        /// Gracefully close the child process.
        /// </summary>
        /// <param name="msg"></param>
        protected override void OnShutdownInitiated(CommMessage msg)
        {
            lock (wakeMeUp)
            {
                Monitor.Pulse(wakeMeUp);
            }
            base.OnShutdownInitiated(msg);
        }

        /// <summary>
        /// Configure repository. This message will be received from build server.
        /// The repository config will be used for requesting files.
        /// </summary>
        /// <param name="commMessage"></param>
        private void ConfigureRepository(CommMessage commMessage)
        {
            //  <TODO>
            //ConfigurationManager.Instance.AddConfig(
            //    CommCommands.ConfigurationArgs.REPOEPURL,
            //    commMessage.Arguments[CommCommands.ConfigurationArgs.REPOEPURL], true);

            this.baseStorageFolder = commMessage.Arguments[CommCommands.ConfigurationArgs.BUILDSERVERSTORAGEPATH];
            if (Directory.Exists(this.baseStorageFolder))
            {
                this.baseStorageFolder += "\\BuilderAt_" + this.config.Port;
                if (!Directory.Exists(this.baseStorageFolder))
                    Directory.CreateDirectory(this.baseStorageFolder);
                this.baseStorageFolder = Path.GetFullPath(this.baseStorageFolder);
                this.commObject.SetBaseDirectory(this.baseStorageFolder);
            }
        }

        /// <summary>
        /// Main method.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(String[] args)
        {
            //System.Diagnostics.Debugger.Launch();
            CoreBuilderHost host = new CoreBuilderHost(args[0], args[1]);
            host.Start();
        }
    }


#if (TEST_COREBUILDERHOST)
    class Program
    {
        public static void Main(String[] args)
        {
            Console.WriteLine("Starting CoreBuildServer");
            if (args.Length == 4)
            {
                //  Provide host address and port.
                CoreBuilderHost host = new CoreBuilderHost("http://localhost","8200");
                //  Provide mother build address and port.
                host.ConfigureServer(args[2], args[3]);
                //  Start to start handling build requests.
                host.Start();
            }
        }   //  Gracefully exiting the remote build server.

    }
#endif
}
