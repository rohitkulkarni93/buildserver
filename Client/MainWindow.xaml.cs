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
/*
 * Package operations
 * ==================
 * This package is the gui client. It the main purpose of this package is to
 * interact with the federation servers.
 *   
 * 
 * Types and interfaces
 * ====================
 * MainWindow - This class is the client window built using WPF.
 * 
 * Required files
 * ==============
 * MainWindow.xaml
 * ConsoleHandler.cs
 * EventHandler.cs
 *
 * 
 * Dependencies
 * ============
 * MessagePassingCommunication.dll - This Class depends on the dll for all the 
 *                                   message passing infrastructure.
 *  
 * Public Interface
 * ================
 * 
 * 
 * Version
 * =======
 * V1.0 - Added package which supports basic functionality.
 * 
 */


using MessagePassingCommunuication;
using MessagePassingCommunuication.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace GuiClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IMessageDispatcher dispatcher = new WPFCommunicationBridge();

        private Dictionary<String, Action<CommMessage>> commandList
            = new Dictionary<string, Action<CommMessage>>();

        private Thread automator = null;

        public MainWindow()
        {
            InitializeComponent();
            if (!Directory.Exists(LocalDirectory))
                Directory.CreateDirectory(LocalDirectory);
            LocalDirectory = Path.GetFullPath(LocalDirectory);
            this.BrowseRepositoryListBox.SelectionMode = SelectionMode.Extended;
            AddRepositoryReplyHandler();
            AddNotificationMessageHandler();
            this.dispatcher.MessageReceived += this.MsgHandler;
            //this.dispatcher.MessageReceived += AutomateRequirementDemonstration;
            this.Loaded += MainWindow_Loaded;
            (ConsoleOutTB.Document.Blocks.FirstBlock as Paragraph).LineHeight = 2;
            automator = new Thread(KeepSendingRequests);
        }

        private volatile byte state = 0;

        /// <summary>
        /// Automate the demonstrations.
        /// </summary>
        /// <param name="msg"></param>
        private void AutomateRequirementDemonstration(CommMessage msg)
        {
            if (state == 0)
            {
                Dispatcher.Invoke(() =>
                {
                    OutputToConsole("Directory contents fetched. Will navigate to BuildRequests dir in 5 seconds.");
                });
                Thread.Sleep(3000);
                Dispatcher.Invoke(() =>
                {
                    this.NavigateDirectory("BuildRequests");
                    state++;
                });
            }
            else if (state == 1)
            {
                Thread.Sleep(3000);
                Dispatcher.Invoke(() =>
                {
                    this.automator.Start();
                    state++;
                });
            }
            else if (state == 3)
            {
                Dispatcher.Invoke(() =>
                {
                    CreateNewBuildRequest();
                });
                state++;
                dispatcher.MessageReceived -= AutomateRequirementDemonstration;
                Thread.Sleep(5000);
                Dispatcher.Invoke(() =>
                {
                    OutputToConsole("Quitting the child processes.");
                    QuitChildProcesses();
                });
            }
        }

        /// <summary>
        /// Create new build request automated.
        /// </summary>
        private void CreateNewBuildRequest()
        {
            this.ProcessNewRequest();
            ClientRequestDO.GetCurrentRequest().BuildFiles.Add(@"TestProjectWithWarnings\Warnings.cs");
            ClientRequestDO.GetCurrentRequest().BuildFiles.Add(@"TestProjectWithWarnings\TestDriver1.cs");
            ClientRequestDO.GetCurrentRequest().TestDrivers.Add(@"TestProjectWithWarnings\TestDriver1.cs");
            OutputToConsole("Added 2 files to build files and 1 to test driver.");
            OutputToConsole(@"\tTestProjectWithWarnings\Warnings.cs");
            OutputToConsole(@"\tTestProjectWithWarnings\TestDriver1.cs");
            OutputToConsole("TestDrivers => ");
            OutputToConsole(@"\tTestProjectWithWarnings\TestDriver1.cs");
            SaveNewRequestToRepository(true);
            BrowseRepositoryListBox.SelectedIndex = 1;
            BrowseDirectory(BrowseRepositoryListBox);
        }

        /// <summary>
        /// Automate build request sending.
        /// </summary>
        private void KeepSendingRequests()
        {
            int requests = 0, totalRequests = 15;
            while (requests < totalRequests)
            {
                //  Dispatch build request to main thread for processing.
                Dispatcher.Invoke(() =>
                {
                    BrowseRepositoryListBox.SelectedIndex = requests % 2 + 1;
                    InvokeBuildRequest();
                });
                requests++;
                //  Insert small delay.
                Thread.Sleep(1000);
            }
            Thread.Sleep(5000);
            Dispatcher.Invoke(() =>
            {
                OutputToConsole("Creating new build request.");
                BrowseRepositoryListBox.SelectedIndex = 0;
                BrowseDirectory(BrowseRepositoryListBox);
                state++;
            });
        }

        /// <summary>
        /// Demo requirements once the client is up and running.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DemoRequirements();
            ConnectToRepository();
            CreateChildProcesses(2);
        }

        /// <summary>
        /// Demonstrating requirements.
        /// </summary>
        private void DemoRequirements()
        {
            Console.WriteLine("\n\n\n");
            Console.WriteLine("-------------------- SMA Project 4 --------------------");
            Console.WriteLine("Rohit Kulkarni");
            Console.WriteLine("SUID - 618809126");
            Console.WriteLine("Email - rvkulkar@syr.edu");
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine("Requirements: ");
            Console.WriteLine("\n\nReq1: This project has been developed in Visual Studio 2017"
                + " using C# and the .Net Framework");
            Console.WriteLine("\n\nReq2: " +
                "A message passing communication service has been built." +
                "All the packages in the communication service are in MessagePassingCommunication" +
                "project. The communication service is built using the Comm Prototype provided by" +
                "Professor Jim Fawcett.");
            Console.WriteLine("\n\nReq3: " +
                "The Communication Service supports accessing build requests by Pool Processes " +
                "from the mother Builder process, sending and receiving build requests, " +
                " and sending and receiving files");
            Console.WriteLine("\n\nReq 4:" +
                "The MotherBuilderProcess project provides a Process Pool component that creates a specified number of processes on command.");
            Console.WriteLine("\n\nReq 5:" +
                "Pool Processes use Communication prototype to access messages from the mother Builder process." +
                "They also perform building of the requests.");
            Console.WriteLine("\n\nReq 6:" +
                "The solution includes a GuiClient built using WPF. It offers accessing the repository" +
                ", starting child processes, stopping child processes.");
            Console.WriteLine("\n\nReq 7:" +
                "The GUI provides a functionality to start and stop child builder processes.");
            Console.WriteLine("\n\nReq 8:" +
                "The GUI enables building test requests by selecting file names from the Mock Repository.");
            Console.WriteLine("\n\nReq 9:" +
                "The above functionality is distributed in the following 3 differnt visual studio projects:" +
                "\n1) MotheBuilderProcess " +
                "\n2) MessagePassingCommunication" +
                "\n3) GuiClient");
        }

        /// <summary>
        /// Private class used to store items for repository
        /// listbox.
        /// </summary>
        private class ListItem
        {
            public ListItem()
            {
            }

            public string Item { get; set; }
            public string Type { get; set; }
            public override string ToString()
            {
                if (Type == CommCommands.RepositoryArgs.DIRNAME)
                    return "[DIR]\t" + Item;
                return "\t" + Item;
            }

            public override bool Equals(object obj)
            {
                return this.Item == (obj as ListItem).Item && this.Type == (obj as ListItem).Type;
            }

            public override int GetHashCode()
            {
                return this.Item.GetHashCode() ^ this.Type.GetHashCode();
            }
        }

        /// <summary>
        /// Quit child processes.
        /// </summary>
        private void QuitChildProcesses()
        {
            CommMessage msg = new CommMessage(CommMessage.MessageType.Request)
            {
                Command = CommCommands.STOPCHILDPROCESS,
                To = this.dispatcher.GetURL(Identity.MotherProcess)
            };
            this.dispatcher.DispatchMessage(msg);
        }

        /// <summary>
        /// Add notification handler.
        /// </summary>
        private void AddNotificationMessageHandler()
        {
            commandList.Add(CommCommands.NOTIFICATION, (msg) =>
            {
                OutputToConsole(msg.Arguments[CommCommands.NotificationArgs.NOTIFICATIONMSG], msg.Identity);
            });
        }

        /// <summary>
        /// This method has the logic to populate the repository listbox.
        /// </summary>
        private void AddRepositoryReplyHandler()
        {
            commandList.Add(CommCommands.GETDIRCONTENTS, (msg) =>
            {
                int fileCount = int.Parse(msg.Arguments[CommCommands.RepositoryArgs.FILECOUNT]);
                int dirCount = int.Parse(msg.Arguments[CommCommands.RepositoryArgs.DIRCOUNT]);
                List<ListItem> files = new List<ListItem>();
                List<ListItem> dirs = new List<ListItem>();
                this.BrowseRepositoryListBox.Items.Clear();
                string currDir = msg.Arguments[CommCommands.RepositoryArgs.CURRDIR];
                if (currDir != "$\\")
                    BrowseRepositoryListBox.Items.Add(new ListItem() { Item = "..", Type = CommCommands.RepositoryArgs.DIRNAME });
                for (int i = 0; i < dirCount; i++)
                {
                    var listItem = new ListItem()
                    {
                        Item = msg.Arguments[CommCommands.RepositoryArgs.DIRNAME + i],
                        Type = CommCommands.RepositoryArgs.DIRNAME
                    };
                    BrowseRepositoryListBox.Items.Add(listItem);
                }
                for (int i = 0; i < fileCount; i++)
                {
                    var listItem = new ListItem()
                    {
                        Item = msg.Arguments[CommCommands.RepositoryArgs.FILENAME + i],
                        Type = CommCommands.RepositoryArgs.FILENAME
                    };
                    BrowseRepositoryListBox.Items.Add(listItem);
                }
                this.CurrentDirectoryLabel.Content = currDir;
                BuildRequestBtn.IsEnabled = currDir == @"$\BuildRequests\";
            });
        }

        /// <summary>
        /// Invoke action associated with command on main thread.
        /// </summary>
        /// <param name="msg"></param>
        private void MsgHandler(CommMessage msg)
        {
            if (msg != null && msg.Command != null && commandList.ContainsKey(msg.Command))
            {
                Dispatcher.Invoke(commandList[msg.Command], new object[] { msg });
                AutomateRequirementDemonstration(msg);
            }
        }

        /// <summary>
        /// Connect to repository server.
        /// </summary>
        private void ConnectToRepository()
        {
            try
            {
                //  Try connecting to repository server
                CommMessage msg = new CommMessage(CommMessage.MessageType.Connect)
                {
                    To = this.dispatcher.GetURL(Identity.Repository)
                };
                msg.Command = CommCommands.GETDIRCONTENTS;
                this.dispatcher.DispatchMessage(msg);

                CommMessage msg2 = new CommMessage(CommMessage.MessageType.Request)
                {
                    To = this.dispatcher.GetURL(Identity.Repository)
                };
                msg2.Command = CommCommands.GETDIRCONTENTS;
                this.dispatcher.DispatchMessage(msg2);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Repository URL button click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BrowseRepository_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedIndex != -1)
            {
                BrowseDirectory(listBox);
            }
        }

        /// <summary>
        /// Key down handled enter and backspace for easy naviation of repository files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BrowseRepository_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is ListBox listBox && listBox.SelectedIndex != -1)
                {
                    BrowseDirectory(listBox);
                }
            }
            else if (e.Key == Key.Back)
            {
                if (sender is ListBox listbox)
                {
                    if (listbox.Items[0] is ListItem item && item.Item == "..")
                    {
                        listbox.SelectedIndex = 0;
                        BrowseDirectory(listbox);
                    }
                }
            }
        }

        /// <summary>
        /// Browse repository directory.
        /// </summary>
        /// <param name="listBox"></param>
        private void BrowseDirectory(ListBox listBox)
        {
            ListItem selectedItem = (ListItem)listBox.Items[listBox.SelectedIndex];
            if (selectedItem.Type == CommCommands.RepositoryArgs.DIRNAME)
            {
                string dir = (string)this.CurrentDirectoryLabel.Content;
                if (selectedItem.Item == "..")
                {
                    dir = dir.Substring(0, dir.Length - 1);
                    dir = dir.Substring(0, dir.LastIndexOf('\\'));
                }
                else
                    dir = dir + selectedItem.Item;
                if (dir != "$") dir = dir.Substring(2);
                CommMessage msg = new CommMessage(CommMessage.MessageType.Request)
                {
                    To = dispatcher.GetURL(Identity.Repository),
                    Command = CommCommands.GETDIRCONTENTS
                };
                dispatcher.DispatchMessage(msg);
                msg.Arguments.Add(CommCommands.RepositoryArgs.DIRNAME, dir);
                //Console.WriteLine("Inspect me " + msg);
            }
        }

        /// <summary>
        /// Create child processes.
        /// </summary>
        /// <param name="numberOfProcs"></param>
        private void CreateChildProcesses(int numberOfProcs)
        {
            if (numberOfProcs > 10)
                numberOfProcs = 10;
            if (numberOfProcs < 0)
                numberOfProcs = 1;
            CommMessage msg = new CommMessage(CommMessage.MessageType.Request)
            {
                Command = CommCommands.STARTCHILDPROCESS
            };
            msg.Arguments.Add(CommCommands.ChildProcArguements.NOOFPROCS, numberOfProcs.ToString());
            msg.To = this.dispatcher.GetURL(Identity.MotherProcess);
            dispatcher.DispatchMessage(msg);
            OutputToConsole("Request sent to Mother Builder to create " + numberOfProcs + " processes.");
        }

        /// <summary>
        /// Send quit message to mother process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

    }
}

