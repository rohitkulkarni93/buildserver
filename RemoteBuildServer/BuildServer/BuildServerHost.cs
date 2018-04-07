//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  BuildServerHost.cs                                                      //
//      This is essentially the entry point for the build server. This      //
//      class configures and hosts the build server as a wcf service.       //
//                                                                          //
//  Language:     C#, VS 2017                                               //
//  Platform:     Macbook Pro 2017, Bootcamp Windows 10                     //
//  Application:  Federation Server                                         //
//  Author:       Rohit Kulkarni, Syracuse University                       //
//                rvkulkar@syr.edu                                          //
//////////////////////////////////////////////////////////////////////////////

using Commons;
using RemoteBuildServer.Utilities;
using System;
using System.ServiceModel;

namespace RemoteBuildServer.BuildServer
{
#if (TEST_OLDHOST)

    /// <summary>
    /// Can be hosted as a wcf service or even as a windows service which
    /// runs in background infinitely.
    /// </summary>
    public class BuildServerHost
    {
        public static void Main(string[] args)
        {
            BuildServerHost host = new BuildServerHost();
            ConfigurationManager.Instance.AddConfig("RepositoryEP", "http://localhost:4321/Services");
            host.HostBuildServerAsWCFService(args);
        }

        public void HostBuildServerAsWCFService(string[] args)
        {
            string address = @"http://localhost:5000/Services";
            ConfigurationManager.
                Instance.AddConfig("BuildServerMgrEP", address);
            IFederationServerLogger logger =
                ServiceProvider.GetService<IFederationServerLogger>();
            logger.WriteDebugLog("Starting the BuildServerRemote object as service.");
            BuildServerConfigDO obj = new BuildServerConfigDO();
            WCFServiceHost buildServerHost = new WCFServiceHost(
                typeof(BuildServerRemote), new Uri(address));
            buildServerHost.AddServiceEndpoint(typeof(IBuildServer),
                new WSHttpBinding(), "BuildServerMgmt");
            try
            {
                buildServerHost.Open();
                Console.WriteLine("Build server is listening at:" + address);
                Console.Write("Press q to shut down the server: ");
                Console.WriteLine();
                Console.WriteLine();
                ConsoleKeyInfo keyInfo;
                do
                {
                    keyInfo = Console.ReadKey();
                } while (keyInfo.Key != ConsoleKey.Q);
                buildServerHost.Close(new TimeSpan(0, 1, 0));
            }
            catch(Exception ex)
            {
                Console.WriteLine("Please start in admin mode. " + ex.Message);
                Console.ReadKey();
            }
        }
    }
#endif
}
