using System;
using UnityEngine;
using UnityHelper.ModSystem;

namespace UnityHelper
{
    public class UnityHelperAddon : MonoBehaviour
    {
        public static UnityHelperAddon UnityHelper { get; private set; }
        
        private ModLoader _modLoader;
        
        [Header("Mod Loader Settings")] 
        [SerializeField]
        [Tooltip("Do you want to enable UnityHelper's built-in mod support?")]
        private bool modLoaderEnabled = true;
        
        [SerializeField] 
        [Tooltip("When this is set to true, no matter what method is being accessed, if it is in a dangerous namespace, " +
                 "don't load the mod.")]
        private bool banAllNamespaces = true;
        
        // --- Dangerous Types ---
        // Use StartsWith for namespaces (include trailing '.' if needed)
        // Use exact full names for specific classes
        [SerializeField]
        [Tooltip(
            "Dangerous namespaces for mods to have access to, we have banned a lot by default, change them as you need.")]
        private string[] dangerousTypes =
        {
            // Namespaces (match anything within)
            "System.IO.",
            "System.Net.",
            "System.Reflection.Emit.",
            "System.Runtime.InteropServices.",
            "System.Security.AccessControl.",
            "System.Management.",
            "System.IO.IsolatedStorage.",

            // Specific Classes (match exactly)
            "System.Diagnostics.Process",
            "System.Reflection.Assembly" // Loading/interacting with assemblies directly
            // Add other specific dangerous classes here if needed
        };

        // --- Dangerous Methods ---
        // These are checked *in combination* with DangerousTypes
        [SerializeField]
        [Tooltip("Dangerous methods for mods to have to, we have banned a lot by default, change them as you need.")]
        private string[] dangerousMethods =
        {
            // IO Methods
            "Delete", "Move", "CreateDirectory", "Copy", "AppendAllText", "WriteAllBytes", "WriteAllText",
            "SetAccessControl", "AddAccessRule", "RemoveAccessRule", "SetAccessRule", "GetAccessControl",
            "Open", "Create", // FileStream constructors are often just ".ctor", so check common factory methods too

            // Process Methods
            "Start", "Kill", "GetProcesses",

            // Reflection/Assembly Methods
            "Load", "LoadFrom", "LoadFile", "GetExecutingAssembly", "Invoke", // Check context carefully
            "CreateInstance", // System.Activator

            // Networking Methods
            "Connect", "Bind", "Listen", "Send", "Receive", "GetResponse", "DownloadFile",
            "UploadFile", // Check common methods on Socket, TcpClient, WebClient etc.

            // Interop Methods
            "GetDelegateForFunctionPointer", "PtrToStructure", "StructureToPtr",

            // Add other specific dangerous method names
            ".ctor" // Check constructors of dangerous types explicitly if needed
        };

        public bool ModLoaderEnabled() => modLoaderEnabled;

        public bool BanAllNamespaces() => banAllNamespaces;
        
        public string[] DangerousTypes() => dangerousTypes;
        
        public string[] DangerousMethods() => dangerousMethods;
        
        public ModLoader ModLoader() => _modLoader;
        
        private void Awake()
        {
            if (UnityHelper == null)
            {
                UnityHelper = this;
                DontDestroyOnLoad(gameObject); // Keep this object alive across scenes
            }
            else
            {
                Destroy(gameObject); // Destroy duplicate instance
            }
            
            if (ModLoaderEnabled())
                _modLoader = new ModLoader();
        }
    }
}