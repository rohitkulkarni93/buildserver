//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  BuildEngine Package                                                     //
//      This is the build engine package. This package contains the         //
//      code for build request handler. Build request handler is as it      //
//      sounds a request handler which process a build request and          //
//      handles it accordingly.                                             //
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
    /// This interface is exposed as wcf service endpoint.
    /// </summary>
    [ServiceContract]
    public interface IBuildEngine
    {
        [OperationContract]
        void ProcessBuildRequest(BuildRequestDO buildRequest);
    }
}
