//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  CommConfig.cs                                                           //
//      Structure for holding the configuration object                      //
//                                                                          //
//  Language:     C#, VS 2017                                               //
//  Platform:     Macbook Pro 2017, Bootcamp Windows 10                     //
//  Application:  Federation Server                                         //
//  Author:       Rohit Kulkarni, Syracuse University                       //
//                rvkulkar@syr.edu                                          //
//////////////////////////////////////////////////////////////////////////////
using System;

namespace MessagePassingCommunuication
{
    public class CommConfig
    {
        public readonly Identity Identity;
        public string BaseAddress { get; set; }
        public int Port { get; set; }
        public string BaseDirectory { get; set; }
        public string ServiceURL
        {
            get
            {
                return "http://" + BaseAddress + ":" + Port;
            }
        }

        public CommConfig(Identity identity, string baseAddress, string port, string baseDirectory)
        {
            this.Identity = identity;
            this.BaseAddress = baseAddress;
            int prt = -1;
            Int32.TryParse(port, out prt);
            this.Port = prt;
            this.BaseDirectory = baseDirectory;
        }
    }
}
