//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  BuildServer Package                                                     //
//      This is the build server management package. This package is        //
//      responsible for handling the lifetime of the build request          //
//      handlers. This design gives flexibility of spawing multiple build   //
//      request handlers and balance the load as and when required.         //
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
 *  The purpose of this package is to start request handler instances and
 *  control their object lifetimes. The package offers follwoing operations:
 *      1) StartBuildServer     - Create build request handler and host it.
 *      2) RestartBuildServer   - Stop a request handler and start again.
 *      3) ConfigureBuildServer - Change configuration like endpointaddress 
 *                                and localstorage path
 *      4) ShutdownBuildServer  - Shutdown the build server.
 * 
 * Interfaces and types
 * ====================
 *  BuildServerRemote - This class contains the logic to host the 
 *                      request handler instances. Currently only one instance
 *                      is spawned.
 *  IBuildServer      - This interface defines the contract for the build server
 *                      management interface.
 *  
 * Required files
 * ==============
 *  BuildServer.cs
 *  IBuildServer.cs
 * 
 * Dependencies
 * ============
 *  ConfigurationManager    -   This is a global shared threadsafe cache 
 *                              implemented using a .NET dictionary. The build
 *                              server stores configuration in the global cache.
 * 
 * Public Interface
 * ================
 * IBuildServer remote = ne BuildServerRemote();
 * remote.StartBuildServer();
 * remote.ShutDownBuildServer();
 * remote.RestartBuildServer();
 * remote.ConfigureBuildServer(new BuildServerConfigDO() { });
 * 
 * Version
 * =======
 * V1.0 - Created package and added basic functionality.
 */

using System;
using Commons;
using System.ServiceModel;
using System.IO;
using RemoteBuildServer.Utilities;

namespace RemoteBuildServer.BuildServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class BuildServerRemote : IBuildServer
    {
        private WCFServiceHost buildRequestHandlerHost = new WCFServiceHost();
        private RequestHandler buildRequestHandler = new RequestHandler();
        bool buildServerStarted = false;
        /// <summary>
        /// Configure the build request handlers.
        /// </summary>
        /// <param name="config"></param>
        public void ConfigureBuildServer(BuildServerConfigDO config)
        {
            var alias = ConfigurationManager.Instance;
            alias.AddConfig("BuildServerManagerServiceEP", config.ServiceEndpointAddress, true);
            alias.AddConfig("LocalStoragePath", config.LocalStoragePath, true);
        }

        /// <summary>
        /// Stop the build request handler.
        /// </summary>
        public void ShutdownBuildServer()
        {
            buildRequestHandlerHost.Close(new TimeSpan(0, 0, 60));
        }

        /// <summary>
        /// Restart the build request handler.
        /// </summary>
        public void RestartBuildServer()
        {
            buildRequestHandlerHost.Close(new TimeSpan(0, 0, 60));
            buildRequestHandlerHost.Open();
        }

        /// <summary>
        /// Start the build request handler.
        /// </summary>
        public void StartBuildServer()
        {
            if (!buildServerStarted)
            {
                var configManager = ConfigurationManager.Instance;
                if (configManager.GetConfigValue("LocalStoragePath") != string.Empty)
                {
                    if (!Directory.Exists(configManager.GetConfigValue("LocalStoragePath")))
                        Directory.CreateDirectory(configManager.GetConfigValue("LocalStoragePath"));
                }
                buildRequestHandlerHost = new WCFServiceHost(typeof(RequestHandler),
                    new Uri(configManager.GetConfigValue("BuildServerManagerServiceEP")));
                buildRequestHandlerHost.AddServiceEndpoint(typeof(IBuildEngine),
                    new WSHttpBinding(), "Bld");
                buildRequestHandlerHost.Open();
                buildServerStarted = true;
            }
        }

        public BuildServerRemote()
        {
            var alias = ConfigurationManager.Instance;
            alias.AddConfig("BuildServerManagerServiceEP", "http://localhost:5001/BuildRequestHandler", true);
            alias.AddConfig("LocalStoragePath", "BuildServerStorage", true);
            alias.AddConfig("RepBuildLogsPath", "../../../Repository/buildLogs/");
        }
    }

#if (TEST_BUILDSERVER)
    class Program
    {
        public static void Main(string [] args)
        {
            Console.WriteLine("\n  Testing the build server interface.");
            Console.WriteLine("\n  Configuring and hosting the build server");
            string address = @"http://localhost:5000/Services";
            WCFServiceHost buildServerHost = new WCFServiceHost(
               typeof(BuildServerRemote), new Uri(address));
            buildServerHost.AddServiceEndpoint(typeof(IBuildServer),
                new WSHttpBinding(), "BuildServerMgmt");
            buildServerHost.Open();
            Console.WriteLine("\n  Build server is listening at:" +
                address);
            Console.ReadKey();
        }
    }
#endif
}
