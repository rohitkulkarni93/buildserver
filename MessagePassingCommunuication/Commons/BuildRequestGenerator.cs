//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  BuildRequestGenerator Package                                           //
//      This package has functionality for creating build requests xml      //
//      files and parsing them.                                             //
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
 * This package is responsible for creating XML file structure. It serilizes
 * the ClientRequestDO to XML and recreates the request object from XML file.
 * 
 * Types and interfaces
 * ====================
 * ClientRequestDO -    Performs the serilization and construction of ClientRequestDO
 *                      Also creates handles creation of client build requests.
 * 
 * Required files
 * ==============
 * BuildRequestGenerator.cs
 * 
 * Public Interface
 * ================
 * GenerateXML      -   This method serilizes the ClientRequestDO and creates XML file
 * NewRequest       -   Creates a new instance of ClientRequestDO
 * CancelRequest    -   Cancels the current request.
 * GetCurrentRequest-   Returns the current request which is active or created using 
 *                      the NewRequest method.
 *                    
 * GenerateClientRequestFromXML -   This method constructs the XML object from the file.
 * Version
 * =======
 * V1.0 - Added package which supports basic functionality.
 * 
 */

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace MessagePassingCommunuication.Commons
{
    public class ClientRequestDO
    {
        private static ClientRequestDO activeRequest = null;

        public String ClientID { get; set; }

        public HashSet<String> BuildFiles { get; set; }

        public HashSet<String> TestDrivers { get; set; }

        public string CompilerOptions { get; set; }

        public string Language { get; set; }

        public ClientRequestDO()
        {
            ClientID = string.Empty;
            BuildFiles = new HashSet<string>();
            TestDrivers = new HashSet<string>();
            CompilerOptions = string.Empty;
            Language = string.Empty;
        }

        /// <summary>
        /// Seralize ClientRequestDO to XML.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="xmlOutput"></param>
        /// <param name="requestDO"></param>
        public static void GenerateXML(string fileName, out string xmlOutput, ClientRequestDO requestDO = null)
        {
            xmlOutput = string.Empty;
            try
            {
                using (XmlWriter writer = XmlWriter.Create(fileName, new XmlWriterSettings() { Indent = true }))
                {
                    if (requestDO == null)
                        requestDO = activeRequest;
                    XmlSerializer serializer = new XmlSerializer(typeof(ClientRequestDO));
                    serializer.Serialize(writer, requestDO);
                    writer.Flush();
                    writer.Close();
                    activeRequest = null;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Function: GenerateXML: " + ex.Message);
            }
        } 
        
        /// <summary>
        /// Generate a new request.
        /// </summary>
        /// <returns></returns>
        public static bool NewRequest()
        {
            if(activeRequest == null)
            {
                activeRequest = new ClientRequestDO();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Cancel the current request.
        /// </summary>
        public static void CancelRequest()
        {
            activeRequest = null;
        }

        /// <summary>
        /// Returns the active request.
        /// </summary>
        /// <returns></returns>
        public static ClientRequestDO GetCurrentRequest()
        {
            return activeRequest;
        }

        /// <summary>
        /// Reconstruct the ClientRequestDO object from XML file.
        /// </summary>
        /// <param name="xmlFileName"></param>
        /// <returns></returns>
        public static ClientRequestDO GenerateClientRequestFromXML(string xmlFileName)
        {
            try
            {
                ClientRequestDO requestDO = null;
                using (XmlReader reader = XmlReader.Create(xmlFileName))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ClientRequestDO));
                    requestDO = (ClientRequestDO)serializer.Deserialize(reader);
                }
                return requestDO;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Function: GenerateClientRequestFromXML: " + ex.Message);
                return null;
            }
        }
    }


#if (TEST_CLIENTREQDO)
    class Program
    {
        public static void Main(String[] args)
        {
            ClientRequestDO.NewRequest();
            ClientRequestDO.GetCurrentRequest().BuildFiles.Add("Program.cs");
            ClientRequestDO.GetCurrentRequest().TestDrivers.Add("TestDriver.cs");
            ClientRequestDO.GenerateXML("yo", out string xml, null);
            //  File saved.


            var reqDo = ClientRequestDO.GenerateClientRequestFromXML("File.xml");
            //  Reconstructed.
        }
    }

#endif
}
