////////////////////////////////////////////////////////////////////
// PCommService.cs - service interface for PluggableComm           //
// ver 1.0                                                         //
// Jim Fawcett, CSE681-OnLine, Summer 2017                         //
/////////////////////////////////////////////////////////////////////
/*
 * Started this project with C# Console Project wizard
 * - Added references to:
 *   - System.ServiceModel
 *   - System.Runtime.Serialization
 *   - System.Threading;
 *   - System.IO;
 *   
 * Package Operations:
 * -------------------
 * This package defines three classes:
 * - Sender which implements the public methods:
 *   -------------------------------------------
 *   - connect          : opens channel and attempts to connect to an endpoint, 
 *                        trying multiple times to send a connect message
 *   - close            : closes channel
 *   - postMessage      : posts to an internal thread-safe blocking queue, which
 *                        a sendThread then dequeues msg, inspects for destination,
 *                        and calls connect(address, port)
 *   - postFile         : attempts to upload a file in blocks
 *   - getLastError     : returns exception messages on method failure
 * - Receiver which implements the public methods:
 *   ---------------------------------------------
 *   - start            : creates instance of ServiceHost which services incoming messages
 *   - postMessage      : Sender proxies call this message to enqueue for processing
 *   - getMessage       : called by Receiver application to retrieve incoming messages
 *   - close            : closes ServiceHost
 *   - openFileForWrite : opens a file for storing incoming file blocks
 *   - writeFileBlock   : writes an incoming file block to storage
 *   - closeFile        : closes newly uploaded file
 * - Comm which implements, using Sender and Receiver instances, the public methods:
 *   -------------------------------------------------------------------------------
 *   - postMessage      : send CommMessage instance to a Receiver instance
 *   - getMessage       : retrieves a CommMessage from a Sender instance
 *   - postFile         : called by a Sender instance to transfer a file
 *    
 * The Package also implements the class TestPCommService with public methods:
 * ---------------------------------------------------------------------------
 * - testSndrRcvr()     : test instances of Sender and Receiver
 * - testComm()         : test Comm instance
 * - compareMsgs        : compare two CommMessage instances for near equality
 * - compareFileBytes   : compare two files byte by byte
 *   
 * Maintenance History:
 * --------------------
 * ver 1.0 : 14 Jun 2017
 * - first release
 * 
 ------------------------------------------------------------------------------------
 * Added references to:
 * - System.ServiceModel
 * - System.Runtime.Serialization
 *
 *
 * This package provides:
 * ----------------------
 * - IMessageReceiver         : interface used for message passing and file transfer
 * - CommMessage              : class representing serializable messages
 * 
 * Required Files:
 * ---------------
 * - Sender.cs         : Service interface and Message definition
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 15 Jun 2017
 * - first release
 * 
 * -----------------------------------------------------------------------------------
 * Changes by @Rohit Kulkarni
 * 
 * Renamed some class names from IPluggableComm to IMessageReceiver
 * 
 * 
 * 
 */

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading;

namespace MessagePassingCommunuication
{
    [ServiceContract]
    public interface IMessageReceiver : IDisposable
    {
        [OperationContract]
        void PostMessage(CommMessage connectMsg);
        [OperationContract]
        bool OpenFileForWrite(string fileName, FileArgs args);
        [OperationContract]
        void CloseFile();
        [OperationContract]
        bool WriteFileBlock(byte[] block);

    }

    public class FileArgs
    {
        public Identity Identity { get; set; }

        public string SessionId { get; set; }
    }

    public class MessageSender
    {

        private IMessageReceiver channel;
        private ChannelFactory<IMessageReceiver> factory = null;
        private BlockingQueue<CommMessage> sndQ = null;
        private string fromAddress = "";
        Thread sndThread = null;
        int tryCount = 0, maxCount = 5;
        string lastError = "";
        string lastUrl = "";
        protected readonly Identity identity;

        /*----< constructor >------------------------------------------*/

        public MessageSender(string serviceUrl, Identity identity)
        {
            fromAddress = serviceUrl;
            this.identity = identity;
            sndQ = new BlockingQueue<CommMessage>();
            sndThread = new Thread(ThreadProc);
            sndThread.Start();
        }
        /*----< creates proxy with interface of remote instance >------*/
        private void CreateSendChannel(string address)
        {
            try
            {
                EndpointAddress baseAddress = new EndpointAddress(address);
                WSHttpBinding binding = new WSHttpBinding();
                factory = new ChannelFactory<IMessageReceiver>(binding, address);
                channel = factory.CreateChannel();
                lastUrl = address;
            }
            catch (Exception)
            {
                channel = null;
            }
        }

        /*----< attempts to connect to Receiver instance >-------------*/
        /*
         * - attempts a finite number of times to connect to a Receiver
         * - first attempt to send will throw exception of no listener
         *   at the specified endpoint
         * - to test that we attempt to send a connect message
         */
        public bool Connect(string toAddress)
        {
            int timeToSleep = 500;
            CreateSendChannel(toAddress);
            CommMessage connectMsg = new CommMessage(CommMessage.MessageType.Connect)
            {
                From = this.fromAddress
            };
            while (true)
            {
                try
                {
                    channel.PostMessage(connectMsg);
                    tryCount = 0;
                    return true;
                }
                catch (Exception ex)
                {
                    if (++tryCount < maxCount)
                    {
                        Console.WriteLine("failed to connect - waiting to try again");
                        Thread.Sleep(timeToSleep);
                    }
                    else
                    {
                        Console.WriteLine("failed to connect - quitting");
                        lastError = ex.Message;
                        return false;
                    }
                }
            }
        }
        /*----< closes Sender's proxy >--------------------------------*/
        public void Close()
        {
            try
            {
                if (factory != null)
                {
                    factory.Close();
                }
            }
            catch (Exception)
            {
                factory = null;
            }
        }
        /*----< processing for receive thread >------------------------*/
        /*
         * - send thread dequeues send message and posts to channel proxy
         * - thread inspects message and routes to appropriate specified endpoint
         */

        void ThreadProc()
        {
            while (true)
            {
                CommMessage msg = sndQ.DeQ();
                if (msg.Type == CommMessage.MessageType.CloseSender)
                {
                    break;
                }
                if (msg.To == lastUrl)
                {
                    try
                    {
                        channel.PostMessage(msg);
                        if (msg.AutoDisconnect)
                        {
                            factory?.Close();
                            lastUrl = "";
                        }
                    }
                    catch (Exception) { }
                }
                else
                {
                    Close();
                    if (!Connect(msg.To))
                    {
                        Console.WriteLine(msg + "... is lost...");
                        continue;
                    }
                    lastUrl = msg.To;
                    channel.PostMessage(msg);
                }
            }
        }
        /*----< main thread enqueues message for sending >-------------*/

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void PostMessage(CommMessage msg)
        {
            msg.To += MessageReceiver.Endpoint;
            msg.From = this.fromAddress;
            Console.WriteLine(string.Format("Sending from {0} to {1} -> {2}", msg.From, msg.To, msg.Command));
            sndQ.EnQ(msg);
        }

        public bool PostFile(string fileName, string address, FileArgs args = null)
        {
            FileStream fs = null;
            long bytesRemaining;
            try
            {
                if (args != null)
                    args.Identity = identity;
                else
                    args = new FileArgs() { Identity = identity };
                string path = Path.Combine(fileName);
                fs = File.OpenRead(path);
                bytesRemaining = fs.Length;
                EndpointAddress baseAddress = new EndpointAddress(address + MessageReceiver.Endpoint);
                var factory1 = new ChannelFactory<IMessageReceiver>(new WSHttpBinding(), baseAddress);
                using (IMessageReceiver chanl = factory1.CreateChannel())
                {
                    chanl.OpenFileForWrite(Path.GetFileName(fileName), args);
                    while (true)
                    {
                        long bytesToRead = Math.Min(1024, bytesRemaining);
                        byte[] blk = new byte[bytesToRead];
                        long numBytesRead = fs.Read(blk, 0, (int)bytesToRead);
                        bytesRemaining -= numBytesRead;

                        chanl.WriteFileBlock(blk);
                        if (bytesRemaining <= 0)
                            break;
                    }
                    chanl.CloseFile();
                }
                fs.Close();
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                Console.WriteLine("Reason for bombout : " + ex.Message);
                return false;
            }
            return true;
        }

    }

    public delegate void OnFileRequestReceived(string fileName, FileArgs args);


    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class MessageReceiver : IMessageReceiver
    {
        public static event OnFileRequestReceived FileRequestReceived;
        public const string Endpoint = "/IMessageReceiver";
        private static BlockingQueue<CommMessage> ReceiveQueue { get; set; } = null;
        private ServiceHost commHost = null;
        private FileStream fs = null;
        private string lastError = "";
        private static string baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private string dir { get; set; } = baseDirectory;
        /*----< constructor >------------------------------------------*/

        public MessageReceiver()
        {
            if (ReceiveQueue == null)
                ReceiveQueue = new BlockingQueue<CommMessage>();
        }
        /*----< create ServiceHost listening on specified endpoint >---*/
        /*
         * baseAddress is of the form: http://IPaddress or http://networkName
         */
        public void Start(string serviceUrl)
        {
            CreateCommHost(serviceUrl + Endpoint);
        }

        public void SetBaseDirectory(string baseDir)
        {
            if (Directory.Exists(baseDir))
            {
                baseDirectory = baseDir;
            }
        }

        public string GetBaseDirectory() { return baseDirectory; }

        public void CreateCommHost(string address)
        {
            try
            {
                WSHttpBinding binding = new WSHttpBinding();
                Uri baseAddress = new Uri(address);
                commHost = new ServiceHost(typeof(MessageReceiver), baseAddress);
                commHost.AddServiceEndpoint(typeof(IMessageReceiver), binding, baseAddress);
                commHost.Open();
                Console.WriteLine("Listening at address: " + address);
            }
            catch (Exception)
            { }
        }

        public void PostMessage(CommMessage msg)
        {
            msg.ThreadId = Thread.CurrentThread.ManagedThreadId;
            ReceiveQueue.EnQ(msg);
        }

        public CommMessage GetMessage()
        {
            CommMessage msg = ReceiveQueue.DeQ();
            if (msg.Type == CommMessage.MessageType.CloseReceiver)
            {
                Close();
            }
            return msg;
        }
        /*----< close ServiceHost >------------------------------------*/

        public void Close()
        {
            commHost.Close();
        }
        /*---< called by Sender's proxy to open file on Receiver >-----*/

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool OpenFileForWrite(string name, FileArgs args = null)
        {
            int retry = 5, tries = 0;
            while (tries < retry)
            {
                try
                {
                    FileRequestReceived?.Invoke(name, args);
                    dir = baseDirectory;
                    string writePath = Path.Combine(dir, name);
                    fs = File.OpenWrite(writePath);
                    return fs.CanWrite;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    tries++;
                    Thread.Sleep(1000);
                }
            }
            return false;
        }
        /*----< write a block received from Sender instance >----------*/

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool WriteFileBlock(byte[] block)
        {
            try
            {
                fs.Write(block, 0, block.Length);
                fs.Flush();
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }
        /*----< close Receiver's uploaded file >-----------------------*/

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CloseFile()
        {
            fs.Close();
        }

        public void Dispose()
        {

        }
    }


    public class CommunicationObject
    {
        private MessageReceiver receiver;
        private MessageSender sender;
        public CommConfig Configuration { get; private set; }

        /*----< constructor >------------------------------------------*/
        /*
         * - starts listener listening on specified endpoint
         */
        public CommunicationObject(CommConfig config)
        {
            this.Configuration = config;
            receiver = new MessageReceiver();
            receiver.Start(this.Configuration.ServiceURL);
            sender = new MessageSender(this.Configuration.ServiceURL, config.Identity);
        }

        /// <summary>
        /// Get base directory for performing file operations.
        /// </summary>
        /// <returns></returns>
        public string GetBaseDirectory()
        {
            return receiver.GetBaseDirectory();
        }

        /// <summary>
        /// Set base directory for performing file operations.
        /// </summary>
        /// <param name="directory"></param>
        public void SetBaseDirectory(string directory)
        {
            receiver.SetBaseDirectory(directory);
        }

        /// <summary>
        /// Send the communication message to the address.
        /// </summary>
        /// <param name="msg"></param>
        public void PostMessage(CommMessage msg)
        {
            msg.Identity = Configuration.Identity;
            sender.PostMessage(msg);
        }
        /// <summary>
        /// Get the communicaton message from receiver.
        /// </summary>
        /// <returns></returns>
        public CommMessage GetMessage()
        {
            return receiver.GetMessage();
        }

        /// <summary>
        /// Upload file to the address
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool PostFile(string filename, string Addr, FileArgs args = null)
        {
            return sender.PostFile(filename, Addr, args);
        }
    }
}
