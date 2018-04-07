//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  WPFCommunicationBridge Package                                          //
//      This package handles communication from client and systems in       //
//      federation servers.                                                 //
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
 * Send and reveive messages.
 *  
 * 
 * Types and interfaces
 * ====================
 * IMessageDispatcher - This interface is used by the GUI client to dispatch messages.
 *                      It offers an event which will be used raised whenever a message
 *                      is received. Subscribe to this event to reveive messages.
 * 
 * WPFCommunicationBridge - This class implements the IMessageDispatcher event.
 * 
 * Required files
 * ==============
 * MessageHandler.cs
 *
 * 
 * Dependencies
 * ============
 * MessagePassingCommunication.dll - This Class depends on the dll for all the 
 *                                   message passing infrastructure.
 *  
 * Public Interface
 * ================
 *     DispatchMessage  -   Uses the base communication object to dispatch the message
 *     MessageReceived  -   Raises this event whenever a message is received.
 * 
 * Version
 * =======
 * V1.0 - Added package which supports basic functionality.
 * 
 */


using MessagePassingCommunuication;
using System;

namespace GuiClient
{

    /// <summary>
    /// Delegate which defines the signature for MessageReceived event.
    /// </summary>
    /// <param name="obj"></param>
    internal delegate void MessageHandler(CommMessage obj);

    /// <summary>
    /// This class implements the IMessageDispatcher interface.
    /// Contains logic for dispatching message and raising event.
    /// </summary>
    class WPFCommunicationBridge : CommunicationBase, IMessageDispatcher
    {
        /// <summary>
        /// WPF client will subscribe to this Event to get the message.
        /// </summary>
        public event MessageHandler MessageReceived;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="baseAddress"></param>
        /// <param name="port"></param>
        public WPFCommunicationBridge() : base(Identity.Client) {

            CommConfig config = new CommConfig(base.identity, "localhost", "8085", null);
            this.StartListener(config);
        }

        /// <summary>
        /// Dispatches request to comm object.
        /// </summary>
        /// <param name="message"></param>
        public void DispatchMessage(CommMessage message)
        {
            this.commObject.PostMessage(message);
        }

        /// <summary>
        /// On receiving a message, raises the event.
        /// </summary>
        /// <param name="msg"></param>
        protected override void HandleCommunicationMessage(CommMessage msg)
        {
            this.MessageReceived(msg);
        }

        public string GetURL(Identity identity)
        {
            return base.defaultConfigs[identity].ServiceURL;
        }

        public bool SendFile(string filename, string address)
        {
            return this.commObject.PostFile(filename, address);
        }
    }

    /// <summary>
    /// Message dispatcher interface.
    /// </summary>
    internal interface IMessageDispatcher
    {
        void DispatchMessage(CommMessage message);
        string GetURL(Identity identity);

        bool SendFile(string filename, string address);

        event MessageHandler MessageReceived;

    }

#if (TEST_WCFMESSAGEHANDLER)
    class Program
    {

        private static void ReplyHandler(CommMessage msg)
        {
            Console.WriteLine(msg);
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Testing the Wcomponent.");
            IMessageDispatcher dispatcher = new WPFCommunicationBridge("http://localhost", "8010");
            //  Subscribe to the Event for replies.
            dispatcher.MessageReceived += ReplyHandler;
            //  Dispatch message to URL.
            dispatcher.DispatchMessage(new CommMessage(CommMessage.MessageType.Request)
            {
                To = "http://localhost:8100",
                Command = "DoSomething"
            });
        }
    }
#endif
}