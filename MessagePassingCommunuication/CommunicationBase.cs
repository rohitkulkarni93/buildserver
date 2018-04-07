//////////////////////////////////////////////////////////////////////////////
//  CommunicationBase.cs                                                    //
//      This package is the base for all remoting objects like MotheBuilder //
//      child builder, test harness and repository.                         //
//  Language:     C#, VS 2017                                               //
//  Platform:     Macbook Pro 2017, Bootcamp Windows 10                     //
//  Application:  Federation Server                                         //
//  Author:       Rohit Kulkarni, Syracuse University                       //
//                rvkulkar@syr.edu                                          //
//////////////////////////////////////////////////////////////////////////////
/*
 * Package operations
 * ==================
 *  The CommunicationBase class defines all the message handling operation. 
 *  The differnt classes like mother builder, child builder can derive from  
 *  this class.
 *  Operations of some of the methods is as follows:
 *      StartListener - This method will start the message handler thread.
 *      HandleCommunicationMessage  - This is a virtual method meant to be overidden
 *                                    by the deriving class. The derived class will
 *                                    receive all the communication messages from
 *                                    different sources in this class.
 *      OnShutdownInitiated - This is a virtual method
 * 
 * Interfaces and types
 * ====================
 *  CommunicationBase -     This class provides base for communication. It composes 
 *                          a CommunicationObject which can be used for message
 *                          passing.
 *        
 * Public Interface
 * ================
 *  This class has no public methods. It only has protected methods.
 *
 * Version
 * =======
 * V1.0 - Created package and added basic functionality.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;

namespace MessagePassingCommunuication
{
    public class CommunicationBase
    {
        #region Variables and Properties

        protected readonly Identity identity;

        //  Store default configuration.
        protected Dictionary<Identity, CommConfig> defaultConfigs =
            new Dictionary<Identity, CommConfig>();

        private Thread MessageHandlerThread = null;

        protected CommunicationObject commObject = null;

        //  Keep checking this variable for shutdown.
        protected volatile bool shutDown = false;
        #endregion

        protected void SetBaseDirectory(string directory)
        {
            commObject.SetBaseDirectory(directory);
        }

        /// <summary>
        /// Configure communication object. Set the base directory.
        /// </summary>
        /// <param name="configuration"></param>
        protected void ConfigureCommObject(CommConfig configuration)
        {
            try
            {
                if (configuration == null)
                    configuration = defaultConfigs[this.identity];
                this.commObject = new CommunicationObject(configuration);
                if (this.commObject != null && configuration.BaseDirectory != null)
                {
                    if (!Directory.Exists(configuration.BaseDirectory))
                        configuration.BaseDirectory = Directory.CreateDirectory(configuration.BaseDirectory).FullName;
                    else
                        configuration.BaseDirectory = Path.GetFullPath(configuration.BaseDirectory);
                    this.commObject.SetBaseDirectory(configuration.BaseDirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Load configuration file.
        /// </summary>
        /// <param name="filename"></param>
        private void LoadConfigurationFile(string filename = "")
        {
            filename = (filename == "") ? 
                SystemConfiguration.ServerConfigurationFileName :
                filename;

            if (File.Exists(filename))
            {
                int times = 0;
                while (times < 10)
                {
                    try
                    {
                        XmlDocument document = new XmlDocument();
                        document.Load(filename);
                        var elem = (document.ChildNodes[1] as XmlNode).ChildNodes;
                        if (elem.Count > 0)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                var config = new CommConfig(
                                    (Identity)Enum.Parse(typeof(Identity), elem[i].Name),
                                    elem[i].ChildNodes[0].InnerText,
                                    elem[i].ChildNodes[1].InnerText,
                                    elem[i].ChildNodes[2].InnerText);
                                this.defaultConfigs.Add(config.Identity, config);
                            }
                            break;
                        }
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(1000);
                        this.defaultConfigs.Clear();
                        times++;
                    }
                }
            }
            else
            {
                throw new FileNotFoundException("Application needs " + filename + " to execute.");
            }
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="identity"></param>
        public CommunicationBase(Identity identity)
        {
            this.identity = identity;
            this.LoadConfigurationFile();
        }

        /// <summary>
        /// Communication Messages will be handled by this method.
        /// </summary>
        protected virtual void HandleCommMessage()
        {
            while (!shutDown)
            {
                try
                {
                    var msg = commObject.GetMessage();
                    if (msg.Type == CommMessage.MessageType.Connect)
                    {
                        continue;
                    }
                    else if (msg.Type == CommMessage.MessageType.CloseReceiver)
                    {
                        Console.WriteLine("Shutdown initiated.");
                        shutDown = true;
                        OnShutdownInitiated(msg);
                        this.commObject.PostMessage(new CommMessage(CommMessage.MessageType.CloseSender));
                        break;
                    }
                    this.HandleCommunicationMessage(msg);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debugger.Launch();
                    Console.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Start the receiver and the listener thread.
        /// </summary>
        /// <param name="config"></param>
        protected void StartListener(CommConfig config = null)
        {
            ConfigureCommObject(config);
            this.MessageHandlerThread = new Thread(HandleCommMessage);
            this.MessageHandlerThread.IsBackground = true;
            if (this.MessageHandlerThread.ThreadState != ThreadState.Running)
                this.MessageHandlerThread.Start();
        }

        protected virtual void HandleCommunicationMessage(CommMessage msg) { }

        protected virtual void OnShutdownInitiated(CommMessage msg) { }
    }


#if (TEST_COMMBASE)
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Testing Comm Base class.");
            string port = "8100";
            string host = "http://localhost";

            //  start service
            CommunicationBase comObj = new CommunicationBase(Identity.Unidentified);

            CommMessage msg = new CommMessage(CommMessage.MessageType.Connect);
            msg.Arguments.Add("ARG1", "VAL1");
            CommunicationObject obj = new CommunicationObject(null);
            msg.To = host + ":" + port;
            obj.PostMessage(msg);
            Console.WriteLine("Posting message to comm base.");

        }
    }
#endif
}
