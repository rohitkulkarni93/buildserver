//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//  ConfigurationManager.cs                                                 //
//      A simple singleton config manager which offers a dictionary         //
//      based storage and serilized access to the dictionary.               //
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
 *  Offers 2 simple functions to add and retrieve configuration values.
 *  
 * Types and interfaces
 * ====================
 * IConfigurationManager    -   Defines a contract for config managers.
 * ConfigurationManager     -   Singleton hopefully threadsafe implementation.
 * 
 * Public Interface
 * ================
 * var configMgr = ConfigurationManager.Instance;
 * configMgr.AddConfig("Key", "hello", true);
 * string val = configMgr
 * 
 * Version
 * =======
 * V1.0 Created package with basic functionality.
 * 
 * TODO: Make interface generic.
 * 
 * v2.0 Moved this to MessagePassingCommunication.
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MessagePassingCommunuication.Utilities
{
    /// <summary>
    /// Interface to access the configuration manager.
    /// </summary>
    public interface IConfigurationManager
    {
        string GetConfigValue(string key);

        void AddConfig(string key, string value, bool replace = false);
    }   

    /// <summary>
    /// Singleton implementation of the configurationmanager class.
    /// </summary>
    public class ConfigManager : IConfigurationManager
    {
        private static Dictionary<string, string> config = new Dictionary<string, string>();

        private static ConfigManager instance = null;
        public static IConfigurationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ConfigManager();
                }
                return instance as IConfigurationManager;
            }
        }

        private ConfigManager()
        {

        }

        /// <summary>
        /// Retrieve the value for a given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public string GetConfigValue(string key)
        {
            try
            {
                string value = config[key];
                return value;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Add a value to dictionary. Replace value if already present.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="replace"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddConfig(string key, string value, bool replace = false)
        {
            if (config.ContainsKey(key) && replace)
                config[key] = value;
            else
                config.Add(key, value);
        }
    }
}
