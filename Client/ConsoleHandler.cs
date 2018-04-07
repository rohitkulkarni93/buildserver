//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  GUI Package                                                             //
//      This package offers a simple WPF based User interface prototype     //
//      for the end user to interact with the federation server systems.    //
//                                                                          //
//  Language:     C#, VS 2017                                               //
//  Platform:     Macbook Pro 2017, Bootcamp Windows 10                     //
//  Application:  Federation Server                                         //
//  Author:       Rohit Kulkarni, Syracuse University                       //
//                rvkulkar@syr.edu                                          //
//////////////////////////////////////////////////////////////////////////////

using MessagePassingCommunuication;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using System.Windows.Media;

namespace GuiClient
{
    /// <summary>
    /// Same partial class. This file has code related to console output handling.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Single method to append text to the console text box.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="identity"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void OutputToConsole(string msg, Identity identity = Identity.Client)
        {
            TextRange tr =
                new TextRange(this.ConsoleOutTB.Document.ContentEnd, this.ConsoleOutTB.Document.ContentEnd)
                {
                    Text = "[" + identity + "] " + msg + "\n"
                };
            tr.ApplyPropertyValue(TextElement.BackgroundProperty, GetBrush(identity));
            this.ConsoleOutTB.ScrollToEnd();
        }

        /// <summary>
        /// To differenciate between the different messages on console get brush color 
        /// depending on identity.
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        private Brush GetBrush(Identity identity)
        {
            switch (identity)
            {
                case Identity.MotherProcess:
                    return Brushes.Honeydew;
                case Identity.ChildProcess:
                    return Brushes.Aqua;
                case Identity.TestHarness:
                    return Brushes.Beige;
                case Identity.Repository:
                    return Brushes.AliceBlue;
                default:
                    return Brushes.White;
            }
        }
    }
}