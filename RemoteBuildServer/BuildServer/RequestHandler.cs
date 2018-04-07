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
/*
 * Package operations
 * ==================
 *  This package contains the RequestHandler. The request handler
 *  creates a blocking queue and starts listening for build requests.
 *  The moment a build request it received it is enqueued. 
 *  Another thread which handles requests is blocked on deq. This thread 
 *  parses the request and creates compiler object according to the request
 *  type. This compiler handles the compilation of the build request.
 * 
 * Types and interfaces
 * ====================
 * IBuildEngine -   Defines the contract for interacting with the build engine
 * RequestHandler - Implements the above interface and provides logic for 
 *                  handling it. It also creates the compiler objects.
 * 
 * Public Interface
 * ================
 *  BuildRequestDO brDo = new BuildRequestDO();
 *  IBuildEngine obj = new RequestHandler();
 *  obj.ProcessBuildRequest(brDO);
 * 
 * Version
 * =======
 * V1.0 [9.25.17] - Added package which supports basic functionality.
 * 
 * V1.1 [9.29.17] - Integrated blocking queue to make it asynchronous,
 *                  and easy to modify for next project.
 * 
 * v2.0 [10.23.17]- Removed the build request queue.
 * 
 */

using RemoteBuildServer.Interfaces;
using Commons;
using RemoteBuildServer.CompilerServices;
using System;

namespace RemoteBuildServer.BuildServer
{
    public class RequestHandler : IBuildEngine
    {
        /// <summary>
        /// Execute the build requests.
        /// </summary>
        /// <param name="buildRequest"></param>
        public void ProcessBuildRequest(BuildRequestDO buildRequest)
        {
            ICompilerService compiler = CompilerFactory.GetCompiler(buildRequest.Language);
            if (compiler != null)
                compiler.Compile(buildRequest);
            else
                Console.WriteLine("Compiler Language not supported.");
        }
    }

#if (TEST_BUILDENGINE)
    class Program
    {
        public static void Main(string[] args)
        {
            ConfigurationManager.Instance.AddConfig("LocalStoragePath", "../localstorage", true);
            ConfigurationManager.Instance.AddConfig("RepositoryEP", "http://localhost:4567/repServiceEndPoint", true);

            Console.WriteLine("\n  Testing the build engine (build request handler)");
            IBuildEngine buildEngine = new RequestHandler();
            buildEngine.ProcessBuildRequest(new BuildRequestDO()
            {
                //  The project file name which exists in repository
                ProjectFileFQN = @"../RandomDir/Foo.csproj",
                //  Compiler flags
                CompilerOptions = "/Target:library /out:food.dll /platform:x86",
                //  Language = 1 for C#
                Language = 1,
                //  Set this to true if this library contains test cases
                ExecTestCases = false
            });
        }
    }
#endif
}
