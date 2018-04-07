//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  DllLoader.cs                                                            //
//      DllLoader package contains types for loading types and executing    //
//      test cases.                                                         //
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
 *  This package can be used to load assemblies. Then find test drivers in
 *  the assembly and execute the test cases.
 *  
 * Interfaces and types
 * ====================
 *  DllLoaderWrapper    -   This type derives from MarshalByRefObject,
 *                          and is used for cross app-domain calls to
 *                          the DllLoaders methods.
 *                          
 *  DLLLoader           -   This type contains the business logic for 
 *                          executing the test cases.
 *                         
 * Required files
 * ==============
 *  DllLoader.cs
 * 
 * Public Interface
 * ================
 *  DllLoaderWrapper proxy = new DllLoaderWrapper();
 *  string logs = proxy.LoadAndExecDLL(dllName);
 *  
 * Version
 * =======
 *  V1.0: Created package and basic functionality added.
 *  V1.2:   Made the DllLoaderWrapper inherit the MarshalByRefObject
 *          since it threw exceptions randomly.
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MockTestHarness
{
    /// <summary>
    /// Proxy class to load assemblies in a different app domain.
    /// This class just creates an instance of the DllLoader and
    /// invokes the method on it.
    /// </summary>
    public class DllLoaderWrapper : MarshalByRefObject
    {
        private DLLLoader loader = new DLLLoader();
        public string LoadAndExecDLL(string assemblyPath)
        {
            return loader.LoadAndExecuteTestCases(assemblyPath);
        }
    }

    /// <summary>
    /// This class loads the assemblies and executes test cases.
    /// </summary>
    internal class DLLLoader
    {
        /// <summary>
        /// Scans for all test drivers to invoke the Test method on them.
        /// </summary>
        /// <param name="assemblyPath"></param>
        /// <returns>Consolidated output of test cases.</returns>
        public string LoadAndExecuteTestCases(string assemblyPath)
        {
            StringWriter logBuilder = new StringWriter();
            try
            {
                Assembly assembly = null;
                if (File.Exists(assemblyPath))
                    assembly = Assembly.LoadFrom(assemblyPath);
                else
                    assembly = null;

                if (assembly != null)
                {
                    var testDrivers = assembly.GetTypes().
                        Where(x => x.GetMethod("Test") != null && !x.IsInterface).ToList();
                    if (testDrivers != null && testDrivers.Count() > 0)
                    {
                        Console.WriteLine("Loading test drivers from assembly: " + Path.GetFileName(assemblyPath));
                        TextWriter consoleOut = Console.Out;
                        Console.SetOut(logBuilder);
                        ExecuteTestCases(testDrivers, logBuilder);
                        Console.SetOut(consoleOut);
                        Console.WriteLine(logBuilder.ToString());
                    }
                    else
                    {
                        string output = "No test drivers found in assembly: " + Path.GetFileName(assemblyPath);
                        logBuilder.WriteLine(output);
                        Console.WriteLine(output);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return logBuilder.ToString();
        }

        /// <summary>
        /// Executes "Test" method on all the types in "testDrivers"
        /// Logs the result of each execution. Captures any exceptions
        /// in the test case execution and lets displays on console.
        /// </summary>
        /// <param name="testDrivers"></param>
        /// <param name="logBuilder"></param>
        private void ExecuteTestCases(List<Type> testDrivers, StringWriter logBuilder)
        {
            foreach (Type type in testDrivers)
            {
                bool testCaseExecutionResult = false;
                try
                {
                    logBuilder.WriteLine("Executing driver: [" + type.Name + "]:");
                    object objectHandle = (Activator.CreateInstance(type));
                    MethodInfo info = type.GetMethod("Test");
                    if (info != null && objectHandle != null)
                        testCaseExecutionResult = (bool)info.Invoke(objectHandle, null);
                }
                catch (Exception ex)
                {
                    logBuilder.WriteLine("Exception Occured: " + ex.InnerException.Message);
                }
                logBuilder.WriteLine("Test result: [" + type.Name + "]" + (testCaseExecutionResult ? "Pass" : "Fail"));
            }
        }
    }
#if(TEST_DLLLOADER)
    class Program
    {
        public static void Main(String[] args)
        {
            Console.WriteLine("\n  Testing DLL Loader package:");
            string dllName = @"TestCaseLoader.dll";
            DllLoaderWrapper wrapper = new DllLoaderWrapper();
            string data = wrapper.LoadAndExecDLL(dllName);
            Console.WriteLine(data);
        }
    }
#endif
}
