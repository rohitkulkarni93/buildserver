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

using Commons;
using System.ServiceModel;

namespace RemoteBuildServer.BuildServer
{
    /// <summary>
    /// Build server management interface.
    /// </summary>
    [ServiceContract]
    public interface IBuildServer
    {
        [OperationContract]
        void StartBuildServer();

        [OperationContract]
        void RestartBuildServer();

        [OperationContract]
        void ShutdownBuildServer();

        [OperationContract]
        void ConfigureBuildServer(BuildServerConfigDO config);
    }
}
