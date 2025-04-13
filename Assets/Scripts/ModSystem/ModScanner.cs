using System;
using System.Linq;
using Mono.Cecil;
using UnityEngine;

namespace ModSystem
{
    public class ModScanner
    {
        private static readonly string[] DangerousTypes =
        {
            // Access Control & IO
            "System.Security.AccessControl.FileSystemAccessRule",
            "System.Security.AccessControl",
            "System.IO.FileInfo",
            "System.IO.FileStream",
            "System.IO.DirectoryInfo",
            "System.Security.AccessControl.DirectorySecurity",
            "System.IO.File",
            "System.IO",

            // Networking
            "System.Net",

            // Reflection & Code Emission
            "System.Reflection.Emit",
            "System.Reflection",

            // Debugging & Diagnostics
            "System.Diagnostics",

            // Interop (dangerous when interacting with native code)
            "System.Runtime.InteropServices",

            // Compression (could be used to manipulate files)
            "System.IO.Compression",

            // Threading & Tasks
            "System.Threading",

            // Cryptography
            "System.Security.Cryptography",

            // System management (WMI, etc.)
            "System.Management",

            // Isolated storage (could be misused for sandboxing bypass)
            "System.IO.IsolatedStorage"
        };

        private static readonly string[] DangerousMethods =
        {
            // Dangerous Access Control methods
            "SetAccessControl", // Can be used to modify file/directory security settings
            "AddAccessRule", // Adds a rule to the security settings, potentially granting more access
            "RemoveAccessRule", // Removes a rule that could restrict access, allowing broader access
            "SetAccessRule", // Sets a new access rule, overriding security permissions
            "GetAccessControl", // Can be used to read or modify file/directory security settings

            // Dangerous File/Directory methods
            "CreateDirectory", // Creates a new directory, could be used to create files outside the game folder
            "Delete", // Deletes files or directories, could be used maliciously
            "Copy", // Copies files or directories, could be used to move files outside the allowed directories
            "Move", // Moves files or directories
            "AppendAllText", // Could write to files outside the game directory
            "WriteAllBytes", // Could write to files outside the game directory

            // Process management methods (external execution)
            "Start", // Starts an external process, could run unauthorized programs
            "GetProcesses", // Could list all processes on the system
            "Kill", // Kills processes, could interfere with system processes

            // Reflection-based methods (dangerous for code injection)
            "Emit", // Generates MSIL code dynamically, allowing code injection
            "Invoke", // Invokes methods dynamically, potentially executing malicious code
            "Load", // Loads assemblies dynamically, could load unauthorized or modified code
            "LoadFrom", // Loads an assembly from a file path, possibly from outside the game folder
            "GetExecutingAssembly", // Could be used to inspect or load other assemblies

            // Networking methods (external access)
            "Create", // Creates a network request, could connect to unauthorized servers
            "GetResponse", // Gets a response from a network request, could send sensitive data
            "BeginGetResponse", // Begins a network request asynchronously
            "Connect", // Connects to a network socket, potentially to external systems
            "Send", // Sends data over a network socket, could send sensitive or unauthorized data

            // Cryptographic methods (could be misused)
            "Aes.Create", // Creates a new AES encryption instance, could be used to hide malicious code
            "RSA.Create", // Creates a new RSA encryption instance
            "MD5.Create", // Creates a new MD5 hashing instance, could be used to hash files
            "HMACSHA256.Create", // Creates a new HMACSHA256 hashing instance, could be used to verify unauthorized data

            // Threading methods (can be used to manipulate or hijack execution)
            "Start", // Starts a thread, could be used to run malicious code in parallel
            "Join", // Blocks the current thread until another thread completes, could cause hangs or exploits
            "Run", // Starts a new task, could be used for malicious asynchronous behavior
            "Wait" // Blocks the current thread until a task completes, could be used for denial-of-service attacks
        };


        public static bool IsModSafe(string dllPath)
        {
            try
            {
                var assembly = AssemblyDefinition.ReadAssembly(dllPath);
                foreach (var type in assembly.MainModule.Types)
                {
                    // Check if any type is from a dangerous namespace or class
                    if (DangerousTypes.Any(dangerousType => type.FullName.Contains(dangerousType)))
                    {
                        Debug.LogWarning($"[SECURITY] Dangerous type found in mod: {type.FullName}");
                        return false; // Reject mod with dangerous type
                    }

                    // Check if any method references dangerous access control methods
                    foreach (var method in type.Methods)
                        if (method.HasBody)
                            foreach (var instruction in method.Body.Instructions)
                                if (instruction.Operand is MethodReference methodRef)
                                    if (DangerousMethods.Any(dangerousMethod =>
                                            methodRef.FullName.Contains(dangerousMethod)))
                                    {
                                        Debug.LogWarning(
                                            $"[SECURITY] Dangerous method call detected: {methodRef.FullName}");
                                        return false; // Reject mod with dangerous method call
                                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SECURITY] Failed to inspect mod: {dllPath}\n{ex.Message}");
                return false;
            }

            return true;
        }
    }
}