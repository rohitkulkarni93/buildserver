//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  CommMessage class                                                       //
//      Instances of this class will be sent and received                   //
//                                                                          //
//  Language:     C#, VS 2017                                               //
//  Platform:     Macbook Pro 2017, Bootcamp Windows 10                     //
//  Application:  Federation Server                                         //
//  Source:       Professor Jim Fawcett, Syracuse University.               //
//  Author:       Rohit Kulkarni, Syracuse University                       //
//                                                                          //
//////////////////////////////////////////////////////////////////////////////
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace MessagePassingCommunuication
{
    /// <summary>
    /// CommMessage class.
    /// </summary>
    [DataContract]
    public class CommMessage
    {
        public enum MessageType
        {
            [EnumMember]
            Connect,           // try to connect.
            [EnumMember]
            Request,           // request for action from receiver
            [EnumMember]
            Reply,             // response to a request
            [EnumMember]
            CloseSender,       // close down client
            [EnumMember]
            CloseReceiver      // close down server for graceful termination
        }

        /*----< constructor requires message type >--------------------*/

        public CommMessage(MessageType mt)
        {
            Type = mt;
        }
        /*----< data members - all serializable public properties >----*/

        public bool AutoDisconnect = true;
        
        [DataMember]
        public MessageType Type { get; set; } = MessageType.Connect;

        [DataMember]
        public string To { get; set; }

        [DataMember]
        public string From { get; set; }

        [DataMember]
        public Identity Identity { get; set; }

        [DataMember]
        public string Command { get; set; }

        [DataMember]
        public Dictionary<string, string> Arguments { get; set; } = new Dictionary<string, string>();

        [DataMember]
        public int ThreadId { get; set; } = Thread.CurrentThread.ManagedThreadId;

        [DataMember]
        public string ErrorMsg { get; set; } = "no error";

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Communication Message");
            builder.Append("MessageType     : ").Append(Type.ToString()).AppendLine();
            builder.Append("To              : ").Append(To).AppendLine();
            builder.Append("From            : ").Append(From).AppendLine();
            builder.Append("Sender Identity : ").Append(Identity).AppendLine();
            builder.Append("Command         : ").Append(Command).AppendLine();
            builder.Append("Arguments       : ").Append(Arguments.Count).AppendLine();
            if (Arguments.Count > 0)
            {
                foreach (var arg in Arguments)
                    builder.Append(" ").Append(arg.Key).Append(":").Append(arg.Value).AppendLine();
            }
            builder.Append("ThreadId        : ").Append(ThreadId).AppendLine();
            builder.Append("ErrorMsg        : ").Append(ErrorMsg).AppendLine().AppendLine();
            return builder.ToString();
        }
    }
}
