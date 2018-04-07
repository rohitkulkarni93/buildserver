//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  TestHarnessService Package                                              //
//      This package loads the test DLLs and executes test cases.           //
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
 * This package is loads all the test DLLs into a different app domain. It
 * creates a new app domain within the process. It also creates an instance 
 * of the DLLLoaderWrapper using reflection.
 * 
 * Types and interfaces
 * ====================
 * TestHarnessService - Type which has the logic to create app domain
 *                      and execute calls into the other app domain 
 *                      via proxy. This type also writes the log data 
 *                      received from the remote call to the DllLoaderWrapper
 *                      to a log file.
 * Required files
 * ==============
 *  TestHarnessService.cs
 * 
 * Dependencies
 * ============
 * DLLLoader and    -   Depends on these classes for execution
 * DllLoaderWrapper     of the test cases.
 * 
 * Public Interface
 * ================
 * TestHarnessService service = new TestHarnessService();
 * string path = servicve.RelativePath;
 * service.LoadAndExecuteTestCases(Guid.NewGuid().ToString());
 * 
 * Version
 * =======
 * V1.0 - Added package which supports basic functionality.
 * V2.0 - Changed implementation and split the logic between
 *        this type and DllLoader.
 * 
 * v3.0 - adding communication functionality to the CommunicationBase.
 */

using MessagePassingCommunuication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace MockTestHarness
{
    public class TestHarnessService : CommunicationBase
    {
                    
        private static BlockingQueue<CommMessage> requestQueue = new BlockingQueue<CommMessage>();

        /// <summary>
        /// The base directory for which test harness is configured.
        /// </summary>
        public TestHarnessService() : base(Identity.TestHarness)
        {
            MessageReceiver.FileRequestReceived += MessageReceiver_FileRequestReceived;
            Thread requestHandler = new Thread(HandleRequests);
            requestHandler.Start();
        }

        /// <summary>
        /// File received event handler. Used for setting directory so file is written to
        /// correct directory.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="args"></param>
        private void MessageReceiver_FileRequestReceived(string fileName, FileArgs args)
        {
            if (args != null)
            {
                string baseDir = Path.Combine(this.defaultConfigs[identity].BaseDirectory, args.SessionId);
                commObject.SetBaseDirectory(baseDir);
            }
        }
        
        /// <summary>
        /// Execute test cases.
        /// </summary>
        /// <param name="message"></param>
        private void ExecuteTestCases(CommMessage message)
        {
            string sessionId = message.Arguments[CommCommands.RepositoryArgs.SESSIONID];
            string baseDir = Path.Combine(defaultConfigs[identity].BaseDirectory, sessionId);
            string logFileName = "TestLog_" + sessionId + ".txt";
            logFileName = Path.Combine(baseDir, logFileName);
            string clientId = message.Arguments[CommCommands.CLIENTDETAILS];
            try
            {
                SendNotification(clientId, "[" + sessionId + "] Request received from ChildBuilder.");
                string log = LoadAndExecuteFile(message);
                StreamWriter sw = new StreamWriter(File.Create(logFileName));
                sw.Write(log);
                sw.Close();
                if(File.Exists(logFileName))
                {
                    this.commObject.PostFile(logFileName, defaultConfigs[Identity.Repository].ServiceURL);
                    SendNotification(clientId, "[" + sessionId + "] Log file saved to repository. Log file Name = " + Path.GetFileName(logFileName));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                SendNotification(clientId, "[" + sessionId + "] Exception occured.");
            }
        }

        /// <summary>
        /// Send notification to client.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="msg"></param>
        void SendNotification(string address, string msg)
        {
            CommMessage notif = new CommMessage(CommMessage.MessageType.Reply)
            {
                To = address,
                Command = CommCommands.NOTIFICATION
            };
            notif.Arguments[CommCommands.NotificationArgs.NOTIFICATIONMSG] = msg;
            base.commObject.PostMessage(notif);
        }

        /// <summary>
        /// Make call to proxy method.
        /// </summary>
        /// <param name="dllFile"></param>
        /// <returns></returns>
        private string LoadAndExecuteFile(CommMessage message)
        {
            StringBuilder builder = new StringBuilder();
            Int32.TryParse(message.Arguments[CommCommands.RepositoryArgs.FILECOUNT], out int filesToLoad);
            string sessionId = message.Arguments[CommCommands.RepositoryArgs.SESSIONID];
            if (filesToLoad > 0)
            {
                AppDomainSetup setup = new AppDomainSetup
                {
                    ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                };
                var domain = AppDomain.CreateDomain(sessionId, AppDomain.CurrentDomain.Evidence, setup);
                var proxy = (DllLoaderWrapper)domain.CreateInstanceAndUnwrap(
                    Assembly.GetExecutingAssembly().FullName,
                    typeof(DllLoaderWrapper).FullName);
                for (int i = 0; i < filesToLoad; i++)
                {
                    string fileToLoad = message.Arguments[CommCommands.RepositoryArgs.FILENAME + i];
                    fileToLoad = Path.Combine(this.defaultConfigs[identity].BaseDirectory, sessionId, fileToLoad);
                    if (File.Exists(fileToLoad))
                    {
                        try {
                            builder.Append("Loading file:" + fileToLoad);
                            builder.Append("Executing test cases for file: " + fileToLoad).AppendLine();
                            builder.Append('-', 100).AppendLine();
                            builder.Append(proxy.LoadAndExecDLL(Path.GetFullPath(fileToLoad)));
                            builder.Append('-', 100).AppendLine();

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            builder.AppendLine().Append(ex.Message);
                        }
                    }
                }
                AppDomain.Unload(domain);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Write test log file for a test session.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="log"></param>
        private void WriteLogFile(string filePath, string log)
        {
            if (File.Exists(filePath))
            {
                StreamWriter writer = new StreamWriter(filePath);
                writer.Write(log);
                writer.Flush();
                writer.Close();
                Console.WriteLine("Test Log file: " + filePath);
            }
        }

        /// <summary>
        /// Handle communication message.
        /// </summary>
        /// <param name="msg"></param>
        protected override void HandleCommunicationMessage(CommMessage msg)
        {
            requestQueue.EnQ(msg);
        }

        /// <summary>
        /// Request handler thread. This thread handles requests.
        /// </summary>
        private void HandleRequests()
        {
            while(!base.shutDown)
            {
                var msg = requestQueue.DeQ();
                commObject.SetBaseDirectory(defaultConfigs[identity].BaseDirectory);
                if (msg.Command == CommCommands.EXECTESTCASES)
                {
                    CreateSessionDirectory(msg);
                }
                else if (msg.Command == CommCommands.TESTFILEREQUEST)
                {
                    ExecuteTestCases(msg);
                }
            }
        }
        
        /// <summary>
        /// Create session directory for new test request.
        /// </summary>
        /// <param name="msg"></param>
        private void CreateSessionDirectory(CommMessage msg)
        {
            string sessionID = msg.Arguments[CommCommands.RepositoryArgs.SESSIONID];
            string sessionDir = Path.Combine(this.commObject.GetBaseDirectory(), sessionID);
            Directory.CreateDirectory(sessionDir);
            CommMessage message = new CommMessage(CommMessage.MessageType.Request)
            {
                To = msg.From,
                Command = CommCommands.TESTFILEREQUEST,
                Arguments = msg.Arguments
            };
            this.commObject.PostMessage(message);
        }

        public static void Main(string[] args)
        {
            TestHarnessService service = new TestHarnessService();
            service.StartService();
        }

        /// <summary>
        /// Start service.
        /// </summary>
        public void StartService()
        {
            this.StartListener(null);
        }
    }
#if (TEST_TESTHARNESSERVICE)
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Testing the TestHarnessService.");
            var svc = new TestHarnessService();
            svc.StartService();

            CommMessage msg = new CommMessage(CommMessage.MessageType.Request)
            {
                To = "http://localhost:8090/",  //  String
                Command = CommCommands.EXECTESTCASES,
            };
            msg.Arguments[CommCommands.RepositoryArgs.SESSIONID] = Guid.NewGuid().ToString();
            CommunicationObject obj = new CommunicationObject(null);
            //  Post msg to execute test cases. 
            //  Add other parameters.
            obj.PostMessage(msg);

        }
    }
#endif
}
