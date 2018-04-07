//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  RepositoryFileService.cs                                                //
//      This package provides the implementation of file services required  //
//      for the federation server.                                          //
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
 *  This package can be used by all the servers to write logs.
 *  It offers a singleton logger instance which is currently not
 *  thread safe because it isnt used much.
 *  
 * Interfaces and types
 * ====================
 *  RepositoryFileService   -   This class implements the IFileService
 *                              methods and provides the functionality of
 *                              streaming files over WCF.
 *                              
 * Required files
 * ==============
 *  RepositoryFileService.cs
 *  
 * Dependencies
 * ============
 * MessagePassingCommunication.dll
 * 
 * Public Interface
 * ================
 * No public interface. All communication happens through message passing.
 * 
 * Version
 * =======
 *  V1.0: Created package and basic functionality added.
 *  V2.0: Added communication capabilities and functionality for
 *        sending directiories to client.
 */

using System;
using System.IO;
using MessagePassingCommunuication;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MockRepository.BusinessLogic
{
    class RepositoryService : CommunicationBase
    {
        Object locker = new object();

        static void Main(string[] args)
        {
            Console.WriteLine("Starting repository");
            try
            {
                RepositoryService service = new RepositoryService();
                service.StartService();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }

        }

        public void StartService()
        {
            base.StartListener();
            this.storagePath = this.commObject.Configuration.BaseDirectory;
            this.buildLogsFolder = CheckAndCreateDirectories(buildLogsFolder);
            this.buildRequestFolder = CheckAndCreateDirectories(buildRequestFolder);
            this.testLogsFolder = CheckAndCreateDirectories(testLogsFolder);
        }

        private string CheckAndCreateDirectories(string directoryName)
        {
            directoryName = Path.Combine(Path.GetFullPath(storagePath), directoryName);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            return Path.GetFullPath(directoryName);
        }

        //  Will find a better deisng next time. Time crunch. :(
        private void OnFileRequestReceived(string filename, FileArgs args)
        {
            switch (args?.Identity)
            {
                case Identity.Client:
                    this.SetBaseDirectory(this.buildRequestFolder);
                    break;
                case Identity.ChildProcess:
                    this.SetBaseDirectory(this.buildLogsFolder);
                    break;
                case Identity.TestHarness:
                    this.SetBaseDirectory(this.testLogsFolder);
                    break;
                default:
                    this.SetBaseDirectory(this.storagePath);
                    break;
            }
        }

        public RepositoryService() : base(Identity.Repository)
        {

            MessageReceiver.FileRequestReceived += OnFileRequestReceived;
        }


        private string storagePath = null;
        private string buildLogsFolder = "BuildLogs";
        private string buildRequestFolder = "BuildRequests";
        private string testLogsFolder = "TestLogs";
        /// <summary>
        /// Handle requests.
        /// </summary>
        /// <param name="msg"></param>
        protected override void HandleCommunicationMessage(CommMessage msg)
        {
            if (msg.Command == CommCommands.FILEREQUEST)
                SendRequestedFiles(msg);
            else if (msg.Command == CommCommands.GETDIRCONTENTS)
            {
                if (msg.Identity == Identity.Client)
                    SendDirectories(msg);
            }
            else if (msg.Command == CommCommands.BUILDREQUEST)
            {
                ProcessBuildRequest(msg);
            }
        }

        private void ProcessBuildRequest(CommMessage msg)
        {
            string file = msg.Arguments[CommCommands.BuildReqArguements.PROJECT];
            file = Path.Combine(this.buildRequestFolder, file);
            if (File.Exists(file))
            {

                bool fileSent = this.commObject.PostFile(file, this.defaultConfigs[Identity.MotherProcess].ServiceURL);
                string notifMsg = fileSent ? "Build request sent to mother builder." : "Build request failed.";

                CommMessage buildReq = new CommMessage(CommMessage.MessageType.Request);
                buildReq.Arguments[CommCommands.CLIENTDETAILS] = msg.From;
                buildReq.Arguments[CommCommands.BuildReqArguements.PROJECT] = Path.GetFileName(file);
                buildReq.Command = msg.Command;
                buildReq.To = defaultConfigs[Identity.MotherProcess].ServiceURL;
                commObject.PostMessage(buildReq);

                SendNotification(msg.From, notifMsg);
            }
        }

        /// <summary>
        /// Send directories if requested by anyone.
        /// </summary>
        /// <param name="msg"></param>
        private void SendDirectories(CommMessage msg)
        {
            Dictionary<string, string> dirAndFileArgs = new Dictionary<string, string>();
            List<string> dir = new List<string>(), files = new List<string>();
            string originalDir = "$";
            if (msg.Arguments.ContainsKey(CommCommands.RepositoryArgs.DIRNAME))
            {
                originalDir = msg.Arguments[CommCommands.RepositoryArgs.DIRNAME];
            }
            string directory = Path.Combine(storagePath, originalDir != "$" ? originalDir : string.Empty);
            if (Directory.Exists(directory))
            {
                files.AddRange(Directory.GetFiles(directory));
                for (int i = 0; i < files.Count; i++)
                    files[i] = Path.GetFileName(files[i]);
                dir.AddRange(Directory.GetDirectories(directory));
                for (int i = 0; i < dir.Count; i++)
                    dir[i] = Path.GetFileName(dir[i]);

                //dir[i] = dir[i].Substring(directory.Length + 1);
            }
            dirAndFileArgs.Add(CommCommands.RepositoryArgs.FILECOUNT, files.Count.ToString());
            dirAndFileArgs.Add(CommCommands.RepositoryArgs.DIRCOUNT, dir.Count.ToString());
            for (int i = 0; i < files.Count; i++)
                dirAndFileArgs.Add(CommCommands.RepositoryArgs.FILENAME + i, files[i]);
            for (int i = 0; i < dir.Count; i++)
                dirAndFileArgs.Add(CommCommands.RepositoryArgs.DIRNAME + i, dir[i]);
            string currDirName = "$\\";
            if (originalDir != "$")
                currDirName += originalDir + "\\";
            dirAndFileArgs.Add(CommCommands.RepositoryArgs.CURRDIR, currDirName);
            CommMessage replyMessage = new CommMessage(CommMessage.MessageType.Reply)
            {
                Command = CommCommands.GETDIRCONTENTS,
                Arguments = dirAndFileArgs,
                To = msg.From
            };
            commObject.PostMessage(replyMessage);
        }


        private void SendNotification(string address, string message)
        {
            CommMessage notification = new CommMessage(CommMessage.MessageType.Request)
            {
                Command = CommCommands.NOTIFICATION,
                To = address
            };
            notification.Arguments.Add(CommCommands.NotificationArgs.NOTIFICATIONMSG, message);
            commObject.PostMessage(notification);

        }


        /// <summary>
        /// Send the required files to the destination URL's connection.
        /// It sends a copy successful message to the destination receiver after 
        /// sending all the files successfully.
        /// </summary>
        /// <param name="message"></param>
        private void SendRequestedFiles(CommMessage message)
        {
            if (int.TryParse(message.Arguments[CommCommands.RepositoryArgs.FILECOUNT], out int fileCount) && fileCount > 0)
            {
                int streamFileCount = 0;
                Monitor.Enter(locker);
                try
                {
                    commObject.PostMessage(new CommMessage(CommMessage.MessageType.Connect) { To = message.From });
                    for (int i = 0; i < fileCount; i++)
                    {
                        string fileToStream = storagePath + "\\" + message.Arguments[CommCommands.RepositoryArgs.FILENAME + i];
                        if (File.Exists(fileToStream))
                        {
                            try
                            {
                                if (commObject.PostFile(fileToStream, message.From))
                                    streamFileCount++;
                                else
                                    Console.WriteLine("Connection bombed out. Someone changed connection.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("exception occured while streaming file. - " + ex.Message);
                            }
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(locker);
                }
                CommMessage msg = new CommMessage(CommMessage.MessageType.Reply)
                {
                    To = message.From
                };
                msg.Command = CommCommands.FILEREQUESTCOMPLETE;
                msg.Arguments.Add(CommCommands.RepositoryArgs.SESSIONID, message.Arguments[CommCommands.RepositoryArgs.SESSIONID]);
                msg.Arguments.Add(CommCommands.RepositoryArgs.FILECOUNT, streamFileCount.ToString());
                msg.Arguments.Add(CommCommands.AUTODISCONNECT, "");
                this.commObject.PostMessage(msg);
            }
        }
    }

#if (TEST_COMMBASE)
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Testing RepositoryFileService.");
            string port = "8120";
            string host = "http://localhost";

            //  start service
            CommunicationBase comObj = new CommunicationBase(Identity.Unidentified);

            CommMessage msg = new CommMessage(CommMessage.MessageType.Connect);
            msg.Arguments.Add("ARG1", "VAL1");
            CommunicationObject obj = new CommunicationObject(null);
            msg.To = host + ":" + port;
            obj.PostMessage(msg);
            Console.WriteLine("Posting message to Repository.");

        }
    }
#endif
}
