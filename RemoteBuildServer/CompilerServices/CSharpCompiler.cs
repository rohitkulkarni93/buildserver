//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  CSharpCompiler Package                                                  //
//      This package performs the building of C# .csproj files.             //
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
 * This package builds the C# project files. It interacts with the repository,
 * to fetch all project file and source code files. Then it uses the CSC.exe
 * to build the files into exe or dll as required. It then forwards forwards
 * it to the testharness for executing test cases if the request intends so.
 * 
 * Types and interfaces
 * ====================
 * CSharpCompiler           -   Provides logic for building C# project files.
 * CSharpEnvironmentManager -   Helps manage the Environment for CSC.exe.
 * 
 * Dependencies
 * ============
 * ICompilerService -   Interface which needs to be implemented.
 * BaseCompiler     -   Base class which provides services.
 * 
 * Public Interface
 * ================
 * ICompiler cscompiler = new CSharpCompiler();
 * cscompiler.Compile(buildRequest);
 * 
 * Version
 * =======
 * V1.0 - Added package which supports basic functionality.
 * 
 * V2.0 - Integrated it with test harness.
 * 
 */


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Text;
using MessagePassingCommunuication;
using System.Reflection;
using MessagePassingCommunuication.Commons;
using System.Linq;
using System.Threading;

namespace RemoteBuildServer.CompilerServices
{
    class BuildRequestDO
    {
        public int Language { get; set; }
        public string CompilerOptions { get; set; }
        public List<string> FilesToBuild { get; set; }
        public string ClientID { get; set; }
        public string ClientRequestFile { get; set; }
        public string OutputFile { get; set; }
        public string SessionDir { get; set; }
        public string SessionId { get; set; }
    }

    class CSharpCompiler : BaseCompiler
    {
        private List<string> files = new List<string>();
        private BuildRequestDO currentBuildRequest = null;
        private string directory = string.Empty;

        internal CSharpCompiler(
            CommunicationObject host,
            Dictionary<Identity, CommConfig> defaultConfigs)
            : base(host, defaultConfigs)
        {
        }


        private void SendReadyMessage()
        {
            this.files.Clear();
            this.currentBuildRequest = null;
            this.CommunicationObject.SetBaseDirectory(this.directory);
            CommMessage readyMessage = new CommMessage(CommMessage.MessageType.Request)
            {
                Command = CommCommands.READY,
                To = defaultConfigs[Identity.MotherProcess].ServiceURL
            };
            CommunicationObject.PostMessage(readyMessage);
        }

        /// <summary>
        /// Logic to compile c# code.
        /// </summary>
        /// <param name="sessionID"></param>
        /// <param name="buildRequestDO"></param>
        private void Compile(BuildRequestDO buildRequest)
        {
            this.SessionID = "Change";
            this.currentBuildRequest = buildRequest;
            directory = CommunicationObject.GetBaseDirectory();
            string sessionDir = Path.GetFullPath(Directory.CreateDirectory(Path.Combine(CommunicationObject.GetBaseDirectory(), this.SessionID)).FullName);
            currentBuildRequest.SessionDir = sessionDir;
            currentBuildRequest.SessionId = this.SessionID;
            CommunicationObject.SetBaseDirectory(sessionDir);
            Console.WriteLine("Build request received. RequestID - " + SessionID);
            List<string> projectFiles = buildRequest.FilesToBuild;
            GetFilesToBuild(projectFiles);
        }

        /// <summary>
        /// Parse the comm message and create build request.
        /// </summary>
        /// <param name="msg"></param>
        private void ProcessbuildRequest(CommMessage msg)
        {
            if (msg != null)
            {
                string requestFile = msg.Arguments[CommCommands.BuildReqArguements.PROJECT];
                requestFile = Path.Combine(this.CommunicationObject.GetBaseDirectory(), requestFile);
                if (File.Exists(requestFile))
                {
                    try
                    {
                        ClientRequestDO requestDO = ClientRequestDO.GenerateClientRequestFromXML(requestFile);
                        if (requestDO != null)
                        {
                            BuildRequestDO request = new BuildRequestDO
                            {
                                ClientID = msg.Arguments[CommCommands.CLIENTDETAILS],
                                Language = ParseLanguage(requestDO.Language),
                                FilesToBuild = requestDO.BuildFiles.ToList(),
                                CompilerOptions = requestDO.CompilerOptions,
                                ClientRequestFile = Path.GetFileName(requestFile)

                            };
                            this.Compile(request);
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        private int ParseLanguage(string language)
        {
            if (language == "C#") return 1;
            if (language == "Java") return 2;
            return -1;
        }

        private void GetFilesToBuild(List<string> projectFile)
        {
            CommMessage msg = new CommMessage(CommMessage.MessageType.Request)
            {
                Command = CommCommands.FILEREQUEST
            };
            msg.Arguments.Add(CommCommands.RepositoryArgs.SESSIONID, this.SessionID);
            msg.Arguments.Add(CommCommands.RepositoryArgs.FILECOUNT, projectFile.Count.ToString());

            if (projectFile.Count > 0)
            {
                for (int i = 0; i < projectFile.Count; i++)
                {
                    msg.Arguments.Add(CommCommands.RepositoryArgs.FILENAME + i, projectFile[i]);
                    this.files.Add(Path.GetFileName(projectFile[i]));
                }
                msg.To = defaultConfigs[Identity.Repository].ServiceURL;
                if (msg.To != string.Empty)
                {
                    base.CommunicationObject.PostMessage(msg);
                }
            }
            else
            {
                Console.WriteLine("Nothing to build.");
                SendReadyMessage();
            }
        }
        /// <summary>
        /// Create process and configure the properties of the process
        /// object.
        /// </summary>
        /// <param name="workingDir"></param>
        /// <returns></returns>
        private Process CreateProcess(string workingDir)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WorkingDirectory = workingDir;
            return p;
        }

        private void OnFileRequestReceived(string filename, FileArgs args)
        {

        }

        /// <summary>
        /// Read the configuration of project file.
        /// </summary>
        /// <param name="projectFile">File name</param>
        /// <param name="compilerOptions">Compiler options in request.</param>
        /// <returns></returns>
        private string ReadConfiguration(string projectFile, string compilerOptions)
        {
            if (File.Exists(projectFile))
            {
                string outName = Path.GetFileName(projectFile);
                outName = outName.Substring(0, outName.Length - 7);
                XmlDocument doc = new XmlDocument();
                doc.Load(projectFile);
                var list = doc.GetElementsByTagName("OutputType");
                if (list.Count > 0)
                {
                    var outputNodeEnum = list.GetEnumerator();
                    while (outputNodeEnum.MoveNext())
                        if (outputNodeEnum.Current is XmlElement node)
                        {
                            if ((node.InnerText == "Exe"))
                                outName += ".exe";
                            else
                                outName += ".dll";
                            compilerOptions += " /target:" + node.InnerText + " ";
                            compilerOptions += " /out:" + outName + " ";
                        }
                }
            }
            return compilerOptions;
        }

        /// <summary>
        /// Get build request details to log to file.
        /// </summary>
        /// <param name="buildRequest"></param>
        /// <returns></returns>
        private string WriteBuildRequestLog(BuildRequestDO buildRequest)
        {
            StringBuilder logWriter = new StringBuilder();
            logWriter.Append("Build request - " + this.SessionID + Environment.NewLine).AppendLine();
            logWriter.Append("Language = C#").AppendLine();
            logWriter.Append("Compiler Flags = " + buildRequest.CompilerOptions).AppendLine();
            return logWriter.ToString();
        }

        /// <summary>
        /// Check if build succeeded or failed.
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        private bool DidBuildSucceed(string output)
        {
            bool buildSuccessful = true;
            if (output.IndexOf("Error", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                buildSuccessful = false;
            }
            return buildSuccessful;
        }

        public override void ExecuteCommCommand(CommMessage msg)
        {
            if (msg.Command == CommCommands.FILEREQUESTCOMPLETE)
            {
                ProceedWithBuild(msg);
            }
            else if (msg.Command == CommCommands.BUILDREQUEST)
            {
                ProcessbuildRequest(msg);
            }
            else if (msg.Command == CommCommands.READY)
            {
                SendReadyMessage();
            }
            else if (msg.Command == CommCommands.TESTFILEREQUEST)
            {
                SendBuildFileToRepository();
            }
        }

        private void SendBuildFileToRepository()
        {
            FileArgs args = new FileArgs() { SessionId = currentBuildRequest.SessionId };
            this.CommunicationObject.PostFile(currentBuildRequest.ClientRequestFile, defaultConfigs[Identity.TestHarness].ServiceURL, args);
            var buildFiles = Directory.GetFiles(currentBuildRequest.SessionDir, "*.exe").ToList();
            buildFiles.AddRange(Directory.GetFiles(currentBuildRequest.SessionDir, "*.dll"));

            CommMessage testharnessmsg = new CommMessage(CommMessage.MessageType.Request);
            testharnessmsg.Command = CommCommands.TESTFILEREQUEST;
            testharnessmsg.To = defaultConfigs[Identity.TestHarness].ServiceURL;

            if (buildFiles.Count > 0)
            {
                for (int i = 0; i < buildFiles.Count; i++)
                {
                    if (this.CommunicationObject.PostFile(buildFiles[i], defaultConfigs[Identity.TestHarness].ServiceURL, args))
                    {
                        testharnessmsg.Arguments[CommCommands.RepositoryArgs.FILENAME + i] = Path.GetFileName(buildFiles[i]);
                    }
                }
                testharnessmsg.Arguments[CommCommands.RepositoryArgs.FILECOUNT] = testharnessmsg.Arguments.Count.ToString();
                testharnessmsg.Arguments[CommCommands.RepositoryArgs.SESSIONID] = currentBuildRequest.SessionId;
                testharnessmsg.Arguments[CommCommands.CLIENTDETAILS] = currentBuildRequest.ClientID;
                CommunicationObject.PostMessage(testharnessmsg);
                SendNotification(this.currentBuildRequest.ClientID, "[" + currentBuildRequest.SessionId + "] forwarded to Test Harness.");
            }
            SendReadyMessage();
        }

        /// <summary>
        /// Check if all files have received and proceed with building them.
        /// </summary>
        /// <param name="msg"></param>
        private void ProceedWithBuild(CommMessage msg)
        {
            if (this.files.Count > 0)
            {
                string baseDir = CommunicationObject.GetBaseDirectory();
                bool allFilesPresent = true;
                for (int i = 0; i < files.Count; i++)
                {
                    allFilesPresent &= File.Exists(Path.Combine(baseDir, files[i]));
                }
                if (allFilesPresent)
                {
                    Console.WriteLine("All files received and proceeding with build");
                    if (BuildFiles())
                    {
                        ForwardRequestToTestHarness();
                    }
                    else
                    {
                        SendReadyMessage();
                    }
                }
                else
                {
                    Console.WriteLine("Some files are missings. Aborting build.");
                    this.SendReadyMessage();
                }
            }
        }

        /// <summary>
        /// Forwards the request to test harness.
        /// </summary>
        private void ForwardRequestToTestHarness()
        {

            //  Send file to test harness.
            CommMessage msg = new CommMessage(CommMessage.MessageType.Request)
            {
                To = this.defaultConfigs[Identity.TestHarness].ServiceURL,
                Command = CommCommands.EXECTESTCASES,
            };
            msg.Arguments[CommCommands.RepositoryArgs.SESSIONID] = currentBuildRequest.SessionId;
            CommunicationObject.PostMessage(msg);
        }


        /// <summary>
        /// Construct the build string.
        /// </summary>
        /// <param name="compilerOptions"></param>
        /// <returns></returns>
        private string ConstructBuildString(string compilerOptions)
        {
            StringBuilder builder = new StringBuilder();
            var packagesPath = Assembly.GetExecutingAssembly().Location;
            if (packagesPath.LastIndexOf("deployment", StringComparison.CurrentCultureIgnoreCase) != -1
                && packagesPath.LastIndexOf("deployment", StringComparison.CurrentCultureIgnoreCase) < packagesPath.Length)
            {
                packagesPath = packagesPath.Substring(0, packagesPath.LastIndexOf("deployment", StringComparison.CurrentCultureIgnoreCase));
                packagesPath += "packages\\Microsoft.Net.Compilers.2.3.2\\tools\\";
                if (Directory.Exists(packagesPath))
                {
                    builder.Append("/C").Append(Path.Combine(packagesPath, "csc.exe "));
                    builder.Append(compilerOptions);
                    builder.Append(" *.cs");
                }
                Console.WriteLine("BuildString = " + builder.ToString());
            }
            return builder.ToString();
        }

        /// <summary>
        /// Create process and run CSC.exe to build the c# files.
        /// </summary>
        private bool BuildFiles()
        {
            bool buildSuccessful = false;
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("Build Started:" + base.SessionID).AppendLine();
                stringBuilder.Append(WriteBuildRequestLog(currentBuildRequest)).AppendLine();

                //  Create process and start csc.
                Process p = this.CreateProcess(CommunicationObject.GetBaseDirectory());
                string arguments = ConstructBuildString(this.currentBuildRequest.CompilerOptions);
                p.StartInfo.Arguments = arguments;
                SendNotification(currentBuildRequest.ClientID, "[" + this.currentBuildRequest.SessionId + "] Build started.");
                p.Start();
                p.WaitForExit(30000);
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                buildSuccessful = DidBuildSucceed(output);

                //  Write results to console.
                stringBuilder.Append("Process complete:").AppendLine().Append(output).AppendLine().Append(error);
                string buildResult = "Build complete. Result - " + (buildSuccessful ? "Build Succeeded." : "Build Failed.");
                stringBuilder.Append(buildResult).AppendLine();
                Console.WriteLine(output + "\r\n" + error + "\r\n" + buildResult);
                Console.WriteLine("\nOutput of build: " + Path.GetFullPath(this.CommunicationObject.GetBaseDirectory()));
                Console.WriteLine(new string('-', 100));
                SendNotification(currentBuildRequest.ClientID, "[" + this.currentBuildRequest.SessionId + "] Build finished! Result - "
                    + (DidBuildSucceed(output) ? "Build Succeeded." : "Build Failed."));

                //  Write build log file.
                string filename = WriteLogFile(stringBuilder);
                SendLogFileToRepository(filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong in ChildBuilder while building files.\n" + ex.Message);
                SendNotification(currentBuildRequest.ClientID, "Build failed due to exception - " + ex.Message);
            }
            return buildSuccessful;
        }

        /// <summary>
        /// Send the log file to repository using comm object.
        /// </summary>
        /// <param name="fileName"></param>
        private void SendLogFileToRepository(string fileName)
        {
            if (File.Exists(fileName))
            {
                this.CommunicationObject.PostFile(fileName, defaultConfigs[Identity.Repository].ServiceURL);
            }
        }

        /// <summary>
        /// Generate log file for build request.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        string WriteLogFile(StringBuilder builder)
        {
            string buildLogFileName =
                "BuildLog_" +       //  Build log
                currentBuildRequest.ClientRequestFile.Substring(0, currentBuildRequest.ClientRequestFile.Length - 4) + // Client file name
                "BuilderAt" + base.CommunicationObject.Configuration.Port + "_" + //    Built by child process at port
                DateTime.Now.ToString("ddMMyy_hhmmss_") + //    built at time
                ".txt"; //  extension.
            buildLogFileName = Path.Combine(this.CommunicationObject.GetBaseDirectory(), buildLogFileName);
            try
            {
                var fs = File.Create(buildLogFileName);
                StreamWriter writer = new StreamWriter(fs);
                writer.Write(builder.ToString());
                writer.Flush();
                writer.Close();
                return buildLogFileName;
            }
            catch (Exception)
            {
                Console.WriteLine("Exception occured during writing log file.");
            }
            return buildLogFileName;
        }

        /// <summary>
        /// Send notification to client.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="message"></param>
        private void SendNotification(string address, string message)
        {
            CommMessage msg = new CommMessage(CommMessage.MessageType.Reply)
            {
                To = address,
                Command = CommCommands.NOTIFICATION
            };
            msg.Arguments[CommCommands.NotificationArgs.NOTIFICATIONMSG] = message;
            base.CommunicationObject.PostMessage(msg);
        }
    }


#if (TEST_CSHARPCOMPILER)
    class Program
    {
        public static void Main(String[] args)
        {
            var compiler = new CSharpCompiler(
            new CommunicationObject(
            new CommConfig(Identity.ChildProcess, "http://localhost", "8020", "./ChildDir")), null);

            //  Create a build request message for compiler.
            CommMessage msg = new CommMessage(CommMessage.MessageType.Request);
            compiler.ExecuteCommCommand(msg);
        }
    }

#endif
}