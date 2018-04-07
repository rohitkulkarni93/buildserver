////////////////////////////////////////////////////////////////////////////////
////                                                                          //
////  FileService Package                                                     //
////      This package consists of the IFileService interface and the         //
////      FileServiceClient. The primary responsibilities of the package are  //
////      communicating with the repository and performing file operations.   //
////                                                                          //
////  Language:     C#, VS 2017                                               //
////  Platform:     Macbook Pro 2017, Bootcamp Windows 10                     //
////  Application:  Federation Server                                         //
////  Author:       Rohit Kulkarni, Syracuse University                       //
////                rvkulkar@syr.edu                                          //
////////////////////////////////////////////////////////////////////////////////
///*
// * Package operations
// * ==================
// *  This package performs the following tasks:
// *  1) Create a directory for build request.
// *  2) Talk to the repository service and fetch the code files.
// *  3) Write the build log file.
// *  4) Copy the build log file to the repository.
// *  5) Copy the built dlls / exe to test harness storage path.
// * 
// * Interfaces and types
// * ====================
// *  IFileServiceClient  -    This interface is exposed to the CompilerService 
// *                           through the BaseCompiler type.
// *  FileServiceClient   -    This class contains all the logic to perform the  
// *                           above mentioned operations.
// *  
// * Required files
// * ==============
// *  FileServiceClient.cs
// * 
// * Dependencies
// * ============
// *  ConfigurationManager    -   Depends on configuration manager to get configuration
// *                              for repository.
// *  IFileService            -   The actual repository endpoint interface
// *  
// * Public interface
// * ================
// * IFileServiceClient cli = new FileServiceClient();
// * cli.FetchFileFromRepository("HelloWorld.cs", @"\HelloWorld\"
// * cli.GetSessionDirPath();
// * cli.WriteBuildLogFile("hello world");
// * cli.SendFileToTestHarness("fileName");
// * cli.SendBuildLogFileToRepository();
// * 
// * Version
// * =======
// * V1.0 - Created package and added basic functionality.
// */


//using MessagePassingCommunuication.Utilities;
//using System;
//using System.Collections.Generic;
//using System.IO;

//namespace RemoteBuildServer
//{
//    /// <summary>
//    /// This is an interface which is exposed to the Compilers to interact
//    /// with the Repository.
//    /// </summary>
//    internal interface IFileSvc
//    {
//        string GetSessionDirPath();

//        string WriteBuildLogFile(string data);

//        void SendFileToTestHarness(String fileName);

//        void SendBuildLogFileToRepository();
//    }

//    /// <summary>
//    /// This class contains the logic to interact with the repository.
//    /// It also handles file operations for the compiler.
//    /// </summary>
//    internal class FileServiceClient : IFileSvc
//    {
//        private List<string> projDirectories = new List<string>();
//        private string sessionDirectory = string.Empty;
//        private string buildid = string.Empty;
//        private string buildFileName = string.Empty;

//        //private BasicHttpBinding binding = new BasicHttpBinding
//        //{
//        //    ReceiveTimeout = new TimeSpan(0, 10, 0),
//        //    SendTimeout = new TimeSpan(0, 10, 0),
//        //    TransferMode = TransferMode.Streamed
//        //};

//        public FileServiceClient(string buildid)
//        {
//            this.buildid = buildid;
//           // this.sessionDirectory =
//             //       ConfigManager.Instance.
//               //     GetConfigValue("LocalStoragePath")
//                 //   + "\\" + buildid;
//            buildFileName = "buildlog_" + buildid + ".txt";
//            if (!Directory.Exists(this.sessionDirectory))
//                Directory.CreateDirectory(sessionDirectory);
//            buildFileName = this.sessionDirectory + "\\" + buildFileName;
//            if (!File.Exists(buildFileName))
//                File.Create(buildFileName).Close();
//        }

//        /// <summary>
//        /// Open a connection to repository and fetch the file.
//        /// Store it at the project directory.
//        /// </summary>
//        /// <param name="fileToFetch"></param>
//        /// <param name="projectDir"></param>
//        /// <returns></returns>
   
//        /// <summary>
//        /// Create a directory with the name of project.
//        /// </summary>
//        /// <param name="projectFileName"></param>
//        /// <returns></returns>
//        private string CreateProjectDirectory(string projectFileName)
//        {
//            if (projectFileName != null && projectFileName.Length > 0 &&
//                projectFileName.EndsWith(".csproj", StringComparison.InvariantCultureIgnoreCase))
//            {
//                string dirName = projectFileName.Substring(0, projectFileName.Length - 7);
//                if (!Directory.Exists(sessionDirectory + "\\" + dirName))
//                {
//                    string dir = Path.GetFullPath(Directory.CreateDirectory(sessionDirectory + "\\" + dirName).FullName);
//                    projDirectories.Add(dir);
//                    return dir;
//                }
//            }
//            return string.Empty;
//        }

//        /// <summary>
//        /// Get full path of the session directory.
//        /// </summary>
//        /// <returns></returns>
//        public string GetSessionDirPath()
//        {
//            return Path.GetFullPath(sessionDirectory);
//        }

//        /// <summary>
//        /// Send files to the test harness base location.
//        /// The base location can be retrieved by invoking
//        /// RelativePath property getter on an instance 
//        /// of the TestHarnessService type.
//        /// </summary>
//        /// <param name="fileName"></param>
//        public void SendFileToTestHarness(string fileName)
//        {
//            SendBuiltFilesToTestHarness();
//        }

//        /// <summary>
//        /// Actually copy files to test harness session directory.
//        /// </summary>
//        private void SendBuiltFilesToTestHarness()
//        {
//            List<string> filesToTransfer = new List<string>();
//            foreach (string dir in this.projDirectories)
//            {
//                if (Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly) != null)
//                    filesToTransfer.AddRange(Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly));
//                if (Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly) != null)
//                    filesToTransfer.AddRange(Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly));
//            }
//            if (filesToTransfer.Count > 0)
//            {
//                string testHarnessTempDirectory = string.Empty;
//                //<TODO>
//                //    TestHarnessService service = new TestHarnessService();
//                //    if (!Directory.Exists(Path.Combine(service.RelativePath, buildid)))
//                //        testHarnessTempDirectory = Directory.CreateDirectory(Path.Combine(service.RelativePath, buildid)).FullName;
//                //    foreach (string fileToTransfer in filesToTransfer)
//                //    {
//                //        File.Copy(fileToTransfer, testHarnessTempDirectory + "\\" + Path.GetFileName(fileToTransfer));
//                //    }
//                //    service.LoadAndExecuteTestCases(this.buildid);
//            }
//        }

//        public string WriteBuildLogFile(string data)
//        {
//            StreamWriter writer = new StreamWriter(buildFileName, true);
//            writer.Write(data);
//            writer.Close();
//            return buildFileName;
//        }

//        /// <summary>
//        /// Copies the file to the file repository using File.Copy because I
//        /// ran out of time before project deadline. It should happen through WCF
//        /// stream instead. Change this implementation i am not satisfied.
//        /// </summary>
//        /// <param name="fileName"></param>
//        private void SendBuildFileToRepository()
//        {
//            string buildLogsDir = ConfigManager.Instance.GetConfigValue("RepBuildLogsPath");
//            try
//            {
//                if (Directory.Exists(Path.GetFullPath(buildLogsDir)))
//                {
//                    File.Copy(Path.Combine(this.sessionDirectory, buildFileName),
//                        Path.Combine(Path.GetFullPath(buildLogsDir), buildFileName));
//                }
//            }
//            catch (Exception) { }
//            finally { }
//        }

//        /// <summary>
//        /// Send build log file to repository.
//        /// </summary>
//        public void SendBuildLogFileToRepository()
//        {
//            SendBuildFileToRepository();
//        }
//    }

//#if (TEST_FILESERVICECLIENT)
//    class Program
//    {
//        public static void Main(String[] args)
//        {
//            Console.WriteLine("\n  Testing the file service client.");
//            Console.WriteLine("\n  Add configuration to manager.");
//            ConfigurationManager.Instance.AddConfig("LocalStoragePath", "../localstorage", true);
//            ConfigurationManager.Instance.AddConfig("RepositoryEP", "http://localhost:4567/repServiceEndPoint", true);
//            IFileServiceClient client = new FileServiceClient(Guid.NewGuid().ToString());
//            //  Gives the session directory path.
//            Console.WriteLine("\n  SessionDir: " + client.GetSessionDirPath());
//            Console.WriteLine("\n  Fetched file name: " + client.FetchFileFromRepository("relative/repository/address/of/file.extension", ""));
//            client.SendFileToTestHarness("");
//            client.WriteBuildLogFile("Log this data to build file");
//            client.SendBuildLogFileToRepository();  //  Only if repository is configured.
//            Console.WriteLine("\n  Log file should be created in the SessionDir.");

//        }
//    }
//#endif
//}
