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

using System;
using System.Linq;
using System.Windows;
using MessagePassingCommunuication.Commons;
using MessagePassingCommunuication;
using System.Collections.Generic;
using System.IO;

namespace GuiClient
{
    /// <summary>
    /// Partial class MainWindow. This file has code related to event handlers.
    /// </summary>
    public partial class MainWindow
    {
        private string LocalDirectory = "./ClientStorage";

        /// <summary>
        /// Create child processes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateProcessBtn_Click(object sender, RoutedEventArgs e)
        {
            string noProcs = this.NumberOfProcessesTb.Text;
            Int32.TryParse(noProcs, out int numberOfProcesses);
            CreateChildProcesses(numberOfProcesses);
        }

        /// <summary>
        /// Stop child processes. Send quit message to processes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopProcessBtn_Click(object sender, RoutedEventArgs e)
        {
            QuitChildProcesses();
        }

        /// <summary>
        /// New request button click handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewRequestBtn_Click(object sender, RoutedEventArgs e)
        {
            ProcessNewRequest();
        }

        /// <summary>
        /// Process new request.
        /// </summary>
        private void ProcessNewRequest()
        {
            if (ClientRequestDO.NewRequest())
            {
                OutputToConsole("New request created. You can add files to it.", MessagePassingCommunuication.Identity.Client);
                NewRequestBtn.Content = "Cancel Request";
                AddBuildFileBtn.IsEnabled = true;
                AddTestFileBtn.IsEnabled = true;
                SaveFileBtn.IsEnabled = true;
            }
            else
            {
                ClientRequestDO.CancelRequest();
                OutputToConsole("Request canceled.", MessagePassingCommunuication.Identity.Client);
                NewRequestBtn.Content = "New Request";
                AddBuildFileBtn.IsEnabled = false;
                AddTestFileBtn.IsEnabled = false;
                SaveFileBtn.IsEnabled = false;
            }
        }

        /// <summary>
        /// Add build files to current request click event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddBuildFileBtn_Click(object sender, RoutedEventArgs e)
        {
            AddFileToCurrentRequest();
        }

        /// <summary>
        /// Add test files to current build request click event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddTestFileBtn_Click(object sender, RoutedEventArgs e)
        {
            AddFileToCurrentRequest(true);
        }

        /// <summary>
        /// Save the current request to repository click event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveFileBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveNewRequestToRepository();
        }

        /// <summary>
        /// Add files to current client request.
        /// </summary>
        /// <param name="isTestFile">Whether file is test file or build file.</param>
        private void AddFileToCurrentRequest(bool isTestFile = false)
        {
            var items = (List<ListItem>)BrowseRepositoryListBox.SelectedItems.Cast<ListItem>().ToList();
            string baseDir = (string)this.CurrentDirectoryLabel.Content;
            int filesAdded = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Type == CommCommands.RepositoryArgs.FILENAME)
                {
                    HashSet<String> correctHashSet = null;
                    string item = baseDir + items[i].Item;
                    if (item.Length > 2)
                        item = item.Substring(2);
                    correctHashSet = isTestFile ? ClientRequestDO.GetCurrentRequest().TestDrivers : ClientRequestDO.GetCurrentRequest().BuildFiles;
                    filesAdded += correctHashSet.Add(item) ? 1 : 0;
                }
            }
            if (isTestFile)
                OutputToConsole(filesAdded + " files added to test drivers.");
            else
                OutputToConsole(filesAdded + " files added to build files.");
        }

        /// <summary>
        /// Logic to send file to repository.
        /// </summary>
        private void SaveNewRequestToRepository(bool displayXMLToConsole = false)
        {
            try
            {
                string fileName = "ClientReq_" + DateTime.Now.ToString("ddMMyy_hhmmss") + ".xml";
                bool fileSaved = false;
                if (ClientRequestDO.GetCurrentRequest() != null)
                {
                    ClientRequestDO.GenerateXML(fileName, out string xmlOutput);
                    if(displayXMLToConsole)
                    {
                        OutputToConsole(xmlOutput);
                    }
                }
                if (File.Exists(fileName))
                {
                    fileSaved = this.dispatcher.SendFile(fileName, this.dispatcher.GetURL(Identity.Repository));
                    if (fileSaved)
                    {
                        File.Delete(fileName);
                        NewRequestBtn.Content = "New Request";
                        AddBuildFileBtn.IsEnabled = false;
                        AddTestFileBtn.IsEnabled = false;
                        SaveFileBtn.IsEnabled = false;
                        OutputToConsole("Client request saved to repository: " + fileName);
                    }
                    else
                    {
                        OutputToConsole("Something went wrong. File (" + fileName + ") not saved.");
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Build request button click event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BuildRequestBtn_Click(object sender, RoutedEventArgs e)
        {
            InvokeBuildRequest();
        }

        /// <summary>
        /// Send build request message to repisitory.
        /// </summary>
        private void InvokeBuildRequest()
        {
            var selectedItems = this.BrowseRepositoryListBox.SelectedItems;
            if (selectedItems != null && selectedItems.Count > 0)
            {
                var selItems = selectedItems.Cast<ListItem>().
                    Where(x => x.Type == CommCommands.RepositoryArgs.FILENAME).
                    ToList();
                for (int i = 0; i < selItems.Count; i++)
                {
                    CommMessage buildRequest = new CommMessage(CommMessage.MessageType.Request)
                    {
                        Command = CommCommands.BUILDREQUEST,
                        To = dispatcher.GetURL(Identity.Repository)
                    };
                    buildRequest.Arguments.Add(CommCommands.BuildReqArguements.PROJECT, selItems[i].Item);
                    dispatcher.DispatchMessage(buildRequest);
                    OutputToConsole("Build request sent to repository.");
                }
            }
        }

        /// <summary>
        /// For automation select a directory and navigate to it.
        /// </summary>
        /// <param name="directory"></param>
        void NavigateDirectory(string directory)
        {
            var filesAndDirs = BrowseRepositoryListBox.Items.Cast<ListItem>().ToList();
            int index = filesAndDirs.IndexOf(new ListItem() { Item = directory, Type = CommCommands.RepositoryArgs.DIRNAME });
            if (index != -1)
            {
                BrowseRepositoryListBox.SelectedIndex = index;
                BrowseDirectory(BrowseRepositoryListBox);
            }
        }
    }
}
