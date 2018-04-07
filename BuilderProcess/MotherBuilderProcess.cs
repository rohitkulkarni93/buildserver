//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  MotherBuilderProcess Package                                            //
//      This package has is the Main build server.                          //
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
 * This package derives from the communication object. This package creates a 
 * pool of child processes. Then it delegates a build requests to individual
 * child processes whichever are able to handle it.
 * Following are the operations:
 *  -   Create child processes.
 *  -   Stop child processes.
 *  -   Delegate the build request to one of the child process.
 *  
 * 
 * Types and interfaces
 * ====================
 * MotherBuilderProcess -   It Listens for build requests. Creates a pool of child 
 *                          builder processes. Once a build request is received it 
 *                          delegates it to the child process for building.
 * 
 * Required files
 * ==============
 * MotherBuilderProcess.cs
 *
 * 
 * Dependencies
 * ============
 * MessagePassingCommunication.dll - This Class depends on the dll for all the 
 *                                   message passing infrastructure.
 * 
 * Build Command
 * =============
 * csc MotherBuilderProcess.cs /r:MessagePassingCommunication.dll /platform:x64 /out:BuilderProcess.exe
 * 
 * Public Interface
 * ================
 *      StartMessageListener - This method starts the Message Receiver Thread. 
 *                                                                
 * Version
 * =======
 * V1.0 - Added package which supports basic functionality.
 * 
 */

using System;
using System.Collections.Generic;
using MessagePassingCommunuication;
using System.IO;
using System.Threading;

namespace BuilderProcess
{
    /// <summary>
    /// Mother builder process class.
    /// </summary>
    class MotherBuilderProcess : CommunicationBase
    {
        private ProcessPool pool = new ProcessPool();
        string StoragePath = "MotherBuilderStorage";
        private List<string> childBuilderURLs = new List<string>();
        private int childProcPort = 8100;
        private string repoAddr = string.Empty;
        private static BlockingQueue<string> readyQueue = new BlockingQueue<string>();
        private static BlockingQueue<CommMessage> requestQueue = new BlockingQueue<CommMessage>();
        private static Thread requestDispatcher = null;
        public MotherBuilderProcess() : base(Identity.MotherProcess)
        {
            MessageReceiver.FileRequestReceived += OnFileRequestReceived;
            requestDispatcher = new Thread(DispatchBuildRequests);
            requestDispatcher.Start();
        }

        /// <summary>
        /// Set appropriate directory based on file being received.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="args"></param>
        private void OnFileRequestReceived(string filename, FileArgs args)
        {
            this.SetBaseDirectory(this.StoragePath);
        }


        /// <summary>
        /// This method dispatches build requests to child processes whichever is ready.
        /// </summary>
        private void DispatchBuildRequests()
        {
            while (!shutDown)
            {
                CommMessage msg = requestQueue.DeQ();
                string builderUrl = readyQueue.DeQ();
                Console.WriteLine("Forwarding request to builder at " + builderUrl);
                msg.To = builderUrl;
                string fileName = Path.Combine(this.StoragePath, msg.Arguments[CommCommands.BuildReqArguements.PROJECT]);
                if (File.Exists(fileName))
                {
                    this.commObject.PostFile(fileName, builderUrl);
                    this.commObject.PostMessage(msg);
                }
            }
        }


        /// <summary>
        /// Create a child process with the given parameters.
        /// </summary>
        /// <param name="args"></param>
        private void CreateProcesses(Dictionary<string, string> args)
        {
            Console.WriteLine("Creating child process.");
            int numberOfProcs = 0;
            if (args.ContainsKey(CommCommands.ChildProcArguements.NOOFPROCS))
                int.TryParse(args[CommCommands.ChildProcArguements.NOOFPROCS], out numberOfProcs);
            {
                childBuilderURLs.AddRange(this.pool.CreateProcesses(numberOfProcs, "localhost", ref this.childProcPort));
            }
        }

        /// <summary>
        /// Send a quit message to all child processes.
        /// </summary>
        private void QuitChildProcesses()
        {
            foreach (var url in childBuilderURLs)
            {
                CommMessage quitMessage = new CommMessage(CommMessage.MessageType.CloseReceiver)
                {
                    To = url
                };
                quitMessage.Arguments.Add(CommCommands.AUTODISCONNECT, "");
                commObject.PostMessage(quitMessage);
            }
            childBuilderURLs.Clear();
            readyQueue.Clear();
            Console.WriteLine("Attempting to stop childs process.");
        }

        /// <summary>
        /// Handle communication message according to command.
        /// </summary>
        /// <param name="msg"></param>
        protected override void HandleCommunicationMessage(CommMessage msg)
        {
            if (msg.Command == CommCommands.STARTCHILDPROCESS)
                this.CreateProcesses(msg.Arguments);
            else if (msg.Command == CommCommands.STOPCHILDPROCESS)
                this.QuitChildProcesses();
            else if (msg.Command == CommCommands.BUILDREQUEST)
                EnqueueMessageRequest(msg);
            else if (msg.Command == CommCommands.READY)
                readyQueue.EnQ(msg.From);
        }

        /// <summary>
        /// Enqueue request message.
        /// </summary>
        /// <param name="msg"></param>
        private void EnqueueMessageRequest(CommMessage msg)
        {
            requestQueue.EnQ(msg);
        }

        /// <summary>
        /// Main method.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var obj = new MotherBuilderProcess();
            obj.StartMessageListener();
        }

        /// <summary>
        /// Start listener.
        /// </summary>
        public void StartMessageListener()
        {
            this.StartListener();
            string baseDir = this.defaultConfigs[identity].BaseDirectory;
            if (!Directory.Exists(Path.Combine(baseDir, this.StoragePath)))
            {
                Directory.CreateDirectory(Path.Combine(baseDir, this.StoragePath));
            }
            this.StoragePath = Path.GetFullPath(Path.Combine(baseDir, this.StoragePath));
            this.SetBaseDirectory(this.StoragePath);
        }

        /// <summary>
        /// Perform shutdown operations.
        /// </summary>
        /// <param name="msg"></param>
        protected override void OnShutdownInitiated(CommMessage msg)
        {
            QuitChildProcesses();
        }
    }

#if (TEST_MOTHERBUILDERPROCESS)
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Testing Mother builder process.");
            string motherBuilderPort = "8100";
            string motherBuilderBaseAddr = "http://localhost";
            MotherBuilderProcess process = 
                new MotherBuilderProcess(motherBuilderBaseAddr, motherBuilderPort, 
                /*  Repository URL and port.  */
                "http://localhost", "8800");
            process.StartMessageListener();
            Console.WriteLine("Mother builder process hosted.");

            CommMessage msg = new CommMessage(CommMessage.MessageType.Connect);
            msg.Arguments.Add("ARG1", "VAL1");
            CommunicationObject obj = new CommunicationObject("http://localhost", 8020, Identity.Client);
            msg.To = motherBuilderBaseAddr + ":" + motherBuilderPort;
            obj.PostMessage(msg);
            Console.WriteLine("Posting message to mother builder.");
        }
    }
#endif
}
