using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using UnityEngine;
// Required for Instructions
using PointerType = Mono.Cecil.PointerType;

namespace UnityHelper.ModSystem
{
    public class ModScanner
    {
        // Instance field to store the setting
        private readonly bool _banAllInNamespace;

        // --- Dangerous Types ---
        // Use StartsWith for namespaces (include trailing '.' if needed)
        // Use exact full names for specific classes
        private static readonly string[] DangerousTypes =
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
        private static readonly string[] DangerousMethods =
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


        // Constructor
        public ModScanner(bool banAllInNamespace = false)
        {
            _banAllInNamespace = banAllInNamespace;
        }

        public bool IsModSafe(string dllPath)
        {
            try
            {
                var assembly =
                    AssemblyDefinition.ReadAssembly(dllPath,
                        new ReaderParameters { ReadSymbols = false }); // Don't need symbols usually
                var mainModule = assembly.MainModule;

                // Iterate through all types DEFINED in the mod assembly
                foreach (var typeDef in mainModule.Types)
                {
                    // --- Basic Checks on the Type Definition ---

                    // Check Base Type
                    if (typeDef.BaseType != null && this.IsDangerousType(typeDef.BaseType))
                    {
                        Debug.LogWarning(
                            $"[SECURITY] Type '{typeDef.FullName}' inherits from dangerous base type: {typeDef.BaseType.FullName}");
                        return false;
                    }

                    // Check Implemented Interfaces
                    foreach (var iface in typeDef.Interfaces)
                        if (this.IsDangerousType(iface.InterfaceType))
                        {
                            Debug.LogWarning(
                                $"[SECURITY] Type '{typeDef.FullName}' implements dangerous interface: {iface.InterfaceType.FullName}");
                            return false;
                        }

                    // --- Checks on Members of the Type ---

                    // Check Fields
                    foreach (var field in typeDef.Fields)
                        if (this.IsDangerousType(field.FieldType))
                        {
                            Debug.LogWarning(
                                $"[SECURITY] Field '{field.FullName}' uses dangerous type: {field.FieldType.FullName}");
                            return false;
                        }

                    // Check Properties (check the PropertyType)
                    foreach (var prop in typeDef.Properties)
                        if (this.IsDangerousType(prop.PropertyType))
                        {
                            Debug.LogWarning(
                                $"[SECURITY] Property '{prop.FullName}' uses dangerous type: {prop.PropertyType.FullName}");
                            return false;
                        }

                    // Check Methods (Signatures and Body)
                    foreach (var method in typeDef.Methods)
                    {
                        // Check Return Type
                        if (this.IsDangerousType(method.ReturnType))
                        {
                            Debug.LogWarning(
                                $"[SECURITY] Method '{method.FullName}' returns dangerous type: {method.ReturnType.FullName}");
                            return false;
                        }

                        // Check Parameter Types
                        foreach (var param in method.Parameters)
                            if (this.IsDangerousType(param.ParameterType))
                            {
                                Debug.LogWarning(
                                    $"[SECURITY] Method '{method.FullName}' has parameter '{param.Name}' of dangerous type: {param.ParameterType.FullName}");
                                return false;
                            }

                        // Check Method Body Instructions
                        if (method.HasBody)
                            foreach (var instruction in method.Body.Instructions)
                                // Check for dangerous method calls
                                if (instruction.Operand is MethodReference methodRef)
                                {
                                    Debug.Log($"[ScannerDebug] Checking Instruction Operand (Method): {methodRef.FullName}");

                                    // Check if the declaring type is dangerous
                                    bool typeIsDangerous = this.IsDangerousType(methodRef.DeclaringType);

                                    if (typeIsDangerous)
                                    {
                                        // If banning all in namespace is enabled, and the type is dangerous, fail immediately.
                                        if (_banAllInNamespace)
                                        {
                                            Debug.LogWarning($"[SECURITY] Dangerous type '{methodRef.DeclaringType.FullName}' used (namespace ban active). Banned method call: {methodRef.FullName}");
                                            return false;
                                        }
                                        // Otherwise (if not banning all in namespace), check the specific method name against the list.
                                        else
                                        {
                                            string methodName = methodRef.Name;
                                            bool methodIsDangerous = DangerousMethods.Any(dangerousMethodName => methodName == dangerousMethodName);
                                            if (methodIsDangerous)
                                            {
                                                Debug.LogWarning($"[SECURITY] Dangerous method call detected in '{method.FullName}': Call to {methodRef.FullName} (Type and Method match)");
                                                return false;
                                            }
                                            // Type was dangerous, but this specific method is allowed. Continue scanning.
                                        }
                                    }
                                    // Type was not dangerous, continue scanning.
                                }
                                // Check for references to dangerous types (e.g., loading a type)
                                else if (instruction.Operand is TypeReference typeRef)
                                {
                                    if (this.IsDangerousType(typeRef))
                                    {
                                        Debug.LogWarning(
                                            $"[SECURITY] Instruction in '{method.FullName}' references dangerous type: {typeRef.FullName}");
                                        return false;
                                    }
                                }
                                // Check for references to dangerous fields
                                else if (instruction.Operand is FieldReference fieldRef)
                                {
                                    if (this.IsDangerousType(fieldRef.DeclaringType)) // Check the type the field belongs to
                                    {
                                        Debug.LogWarning(
                                            $"[SECURITY] Instruction in '{method.FullName}' references field '{fieldRef.Name}' from dangerous type: {fieldRef.DeclaringType.FullName}");
                                        return false;
                                    }

                                    if (this.IsDangerousType(fieldRef.FieldType)) // Check the field's own type
                                    {
                                        Debug.LogWarning(
                                            $"[SECURITY] Instruction in '{method.FullName}' references dangerous field '{fieldRef.Name}' of type: {fieldRef.FieldType.FullName}");
                                        return false;
                                    }
                                }
                    } // End method loop
                } // End type loop
            }
            catch (BadImageFormatException ex)
            {
                Debug.LogError(
                    $"[SECURITY] Failed to read mod assembly (Bad Image Format): {dllPath}. Error: {ex.Message}");
                return false; // Treat unreadable assemblies as unsafe
            }
            catch (Exception ex)
            {
                // Log detailed error for debugging
                Debug.LogError(
                    $"[SECURITY] Error scanning mod '{dllPath}': {ex.GetType().Name} - {ex.Message}\nStackTrace: {ex.StackTrace}");
                return false; // Fail safe on any unexpected error
            }

            Debug.Log($"[SECURITY] Mod '{Path.GetFileName(dllPath)}' passed safety checks.");
            return true;
        }

        // Helper to check if a TypeReference itself is considered dangerous
        // Made non-static to potentially access instance fields in future if needed, though not strictly required for current logic
        private bool IsDangerousType(TypeReference typeRef)
        {
            if (typeRef == null) return false;

            // Handle generics (check element type and arguments)
            if (typeRef is GenericInstanceType genericType)
            {
                if (this.IsDangerousType(genericType.ElementType)) return true;
                foreach (var arg in genericType.GenericArguments)
                    if (this.IsDangerousType(arg))
                        return true;
                return false; // Generic definition itself might be safe, but its arguments make it dangerous
            }

            // Handle arrays (check element type)
            if (typeRef is ArrayType arrayType) return this.IsDangerousType(arrayType.ElementType);

            // Handle pointers/references (check element type)
            if (typeRef is PointerType pointerType) return this.IsDangerousType(pointerType.ElementType);
            if (typeRef is ByReferenceType byRefType) return this.IsDangerousType(byRefType.ElementType);

            // Check the FullName against the dangerous list
            var fullName = typeRef.FullName;

            // Normalize name (Cecil might include return type for methods, remove generic backticks etc.)
            fullName = fullName.Split(' ')[0]; // Get part before space if any (like return type)
            fullName = fullName.Split('<')[0]; // Get part before generic marker
            fullName = fullName.Replace("&", ""); // Remove by-ref marker

            // Inside IsDangerousType, before the final return
            var isMatch = DangerousTypes.Any(dangerous =>
                fullName.Equals(dangerous, StringComparison.Ordinal) ||
                (dangerous.EndsWith(".") && fullName.StartsWith(dangerous, StringComparison.Ordinal))
            );

            Debug.Log(
                $"[ScannerDebug] IsDangerousType Check: Type='{typeRef?.FullName ?? "null"}', Normalized='{fullName}', Result={isMatch}");
            return isMatch;
        }

        // Helper to check if a MethodReference is a call to a dangerous method on a dangerous type
        // Kept for potential direct use, but primary logic moved to IsModSafe for the new flag.
        // Made non-static.
        private bool IsDangerousMethodCall(MethodReference methodRef)
        {
            // Inside IsDangerousMethodCall
            if (methodRef == null || methodRef.DeclaringType == null) return false;

            bool typeIsDangerous = this.IsDangerousType(methodRef.DeclaringType); 
            Debug.Log($"[ScannerDebug] IsDangerousMethodCall Check: Method='{methodRef.FullName}', DeclaringTypeDangerous={typeIsDangerous}");

            if (!typeIsDangerous)
            {
                return false; // Method is on a safe type
            }

            string methodName = methodRef.Name;
            bool methodIsDangerous = DangerousMethods.Any(dangerousMethodName => methodName == dangerousMethodName); 
            Debug.Log($"[ScannerDebug] IsDangerousMethodCall Check: MethodName='{methodName}', NameInList={methodIsDangerous}");

            return methodIsDangerous;
        }
    }
}