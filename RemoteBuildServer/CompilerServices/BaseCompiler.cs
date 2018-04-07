//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  BaseCompiler Package                                                    //
//      This is the base class for different compilers. It composes some    //
//      important services which will be required by all the derived        //
//      classes.                                                            //
//                                                                          //
//  Language:     C#, VS 2017                                               //
//  Platform:     Macbook Pro 2017, Bootcamp Windows 10                     //
//  Application:  Federation Server                                         //
//  Author:       Rohit Kulkarni, Syracuse University                       //
//                rvkulkar@syr.edu                                          //
//////////////////////////////////////////////////////////////////////////////

using RemoteBuildServer.Interfaces;
using System;
using MessagePassingCommunuication;
using System.Collections.Generic;

namespace RemoteBuildServer.CompilerServices
{
    abstract class BaseCompiler : ICompilerService
    {
        //  Unique build ID.
        private Guid guid = Guid.NewGuid();
        private CommunicationObject communicationObject;
        protected Dictionary<Identity, CommConfig> defaultConfigs =
            new Dictionary<Identity, CommConfig>();
        public abstract void ExecuteCommCommand(CommMessage msg);
        
        protected string SessionID
        {
            get
            {
                return guid.ToString();
            }
            set
            {
                this.guid = Guid.NewGuid();
            }
        }

        protected CommunicationObject CommunicationObject
        {
            get => communicationObject;
        }

        protected BaseCompiler(CommunicationObject host, Dictionary<Identity, CommConfig> cfg)
        {
            communicationObject = host;
            this.defaultConfigs = cfg;
        }
    }
}
