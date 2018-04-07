//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  ProcessPool Package                                                     //
//      This package has is used for creating child builder process.        //
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
 * This package performs the job of spawning child processes.
 *  
 * 
 * Types and interfaces
 * ====================
 * ProcessPool - This class contains operations to Create child processes.
 * 
 * Required files
 * ==============
 * ProcessPool.cs
 * 
 * 
 * Public Interface
 * ================
 *      CreateProcesses - This method creates child processes.
 *                        Args: numberOfProcesses - number of processes to create
 *                              hostAddress       - Address of the machine to create
 *                                                  processes
 *                              startPort         - Initial portNumber for the first process
 *                                                  Successive port numbers will be incremented
 *                                                  and assigned automatically.
 *                              buildServerHostAddr - Address of build server to pass as args to 
 *                                                    child process.
 *                              buildServerPort     - Port of build server to pass as args to child
 *                                                    process.
 *                                                                
 * Version
 * =======
 * V1.0 - Added package which supports basic functionality.
 * 
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace BuilderProcess
{
    class ProcessPool
    {
        public List<string> CreateProcesses(int numberOfProcesses, string hostAddress, ref int startPort)
        {
            int ports = startPort;
            List<string> processes = new List<string>();
            for (int i = 0; i < numberOfProcesses; i++)
                processes.Add(CreateProcess(hostAddress, ports++));

            startPort = ports;
            return processes;
        }

        /// <summary>
        /// Start a child process.
        /// </summary>
        /// <param name="baseAddress"></param>
        /// <param name="port"></param>
        private string CreateProcess(string baseAddress, int port)
        {
            string processArgs = baseAddress + " " + port + " ";
            Process p = new Process();
            p.StartInfo.FileName = "ChildBuilderProcess.exe";
            p.StartInfo.Arguments = processArgs;
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            p.StartInfo.UseShellExecute = true;
            p.Start();
            return CreateProcessURL(baseAddress, port.ToString());
        }

        private string CreateProcessURL(string baseAddress, string port)
        {
            return "http://" + baseAddress + ":" + port;
        }
    }

#if (TEST_PROCESSPOOL)
    public class Program
    {
        public static void Main()
        {
            string localhost = "http://localhost";
            Console.WriteLine("Testing the Process Pool component.\n");
            ProcessPool pool = new ProcessPool();
            pool.CreateProcesses(3, localhost, 8200, localhost, 8100);
            Console.WriteLine("3 Processes should be created.");
        }
    }
#endif
}
