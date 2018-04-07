//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  CommCommands.cs                                                         //
//      This file contains all global constants                             //
//                                                                          //
//  Language:     C#, VS 2017                                               //
//  Platform:     Macbook Pro 2017, Bootcamp Windows 10                     //
//  Application:  Federation Server                                         //
//  Author:       Rohit Kulkarni, Syracuse University                       //
//                rvkulkar@syr.edu                                          //
//////////////////////////////////////////////////////////////////////////////
namespace MessagePassingCommunuication
{
    public class SystemConfiguration
    {
        public const string ServerConfigurationFileName = "server.config";
        public const string Port = ":port";
        public const string BaseAddress = ":baseAddr";
        public const string Url = ":url";
        public const string BaseDirectory = ":baseDir";
    }

    public class CommCommands
    {
        public const string AUTODISCONNECT = "AUTODISCONN";
        public const string SHUTDOWN = "SHUTDOWN";
        public const string ACKNOWLEDGE = "ACKNOWLEDGE";
        public const string READY = "READY";
        
        public class ConfigurationArgs
        {
            public const string MOTHERBUILDERURL = "BUILDSERVERURL";
            public const string REPOEPURL = "REPOEPURL";
            public const string BUILDSERVERSTORAGEPATH = "BUILDSERVERSTORAGEPATH";
        }

        #region BUILDREQUEST Commands
        public const string BUILDREQUEST = "BUILDREQUEST";

        public class BuildReqArguements
        {
            public const string BUILDREQUESTID = "BUILDREQUESTID";
            public const string NUMBEROFPROJECTS = "NUMBEROFPROJECTS";
            public const string PROJECT = "PRJECT";
            public const string LANGUAGE = "LANGUAGE";
            public const string COMPILEROPTIONS = "COMPILEROPTIONS";
        }
        #endregion 

        #region CHILDPROCESS Commands
        public const string STARTCHILDPROCESS = "STARTCHILDPROCESS";
        public const string STOPCHILDPROCESS = "STOPCHILDPROCESS";

        public class ChildProcArguements
        {
            public const string NOOFPROCS = "NOOFPROC";
        }

        #endregion

        #region FILEREQUEST Commands
        public const string FILEREQUEST = "FILEREQUEST";
        public const string FILEREQUESTCOMPLETE = "FILEREQCOMPLETE";

        public class FileReqArguments
        {
            public const string FILENAME = "FILENAME";
        }
        #endregion

        #region COMPILER Commands
        public class CompilerRequests
        {
            public const string GETFILE = "GETFILE";
            public const string ISPROJFILE = "ISPROJFILE";
            public const string NOTIFYCLIENT = "NOTIFYCLIENT";
        }
        #endregion

        public const string GETDIRCONTENTS = "GETDIRCONTENTS";
        public class RepositoryArgs
        {
            public const string DIRNAME = "DIRNAME";
            public const string DIRCOUNT = "DIRCOUNT";
            public const string FILECOUNT = "FILECOUNT";
            public const string FILENAME = "FILE";
            public const string CURRDIR = "CURRDIR";
            public const string SESSIONID = "SESSIONID";
        }

        #region TESTHARNESS Commands

        public const string EXECTESTCASES = "EXECTESTCASES";
        public const string TESTFILEREQUEST = "TESTFILEREQUEST";
        public class TestHarnessArgs
        {
            public const string DLLTOLOAD = "DLLTOLOAD";
        }

        #endregion

        #region NOTIFICATION Commands

        public const string CLIENTDETAILS = "CLIENTDETAILS";
        public const string NOTIFICATION = "NOTIFICATION";
        public class NotificationArgs
        {
            public const string NOTIFICATIONMSG = "NOTIFMSG";
        }

        #endregion
        
        public class ClientRequests
        {
            public const string NEGOTIATEFILEPATH = "NEGOTIATEFILEPATH";
            public const string REPOFILEPATH = "FILEPATH";
            public const string CLIENTFILEPATH = "CLIENTFP";
        }
    }

    public enum Identity
    {
        Unidentified,
        MotherProcess,
        ChildProcess,
        Repository,
        Client,
        TestHarness
    }
}
