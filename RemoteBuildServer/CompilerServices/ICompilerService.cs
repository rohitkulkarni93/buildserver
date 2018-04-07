//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  ICompilerService.cs                                                     //
//      Interface which should be implemented by all compiler services      //
//                                                                          //
//  Language:     C#, VS 2017                                               //
//  Platform:     Macbook Pro 2017, Bootcamp Windows 10                     //
//  Application:  Federation Server                                         //
//  Author:       Rohit Kulkarni, Syracuse University                       //
//                rvkulkar@syr.edu                                          //
//////////////////////////////////////////////////////////////////////////////

using MessagePassingCommunuication;

namespace RemoteBuildServer.Interfaces
{
    interface ICompilerService
    {
        void ExecuteCommCommand(CommMessage msg);
    }
}
