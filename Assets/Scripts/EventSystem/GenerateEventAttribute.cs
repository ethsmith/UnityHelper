using System;

namespace EventSystem
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class GenerateEventAttribute : Attribute
    {
        public bool CanBeCancelled { get; }

        public GenerateEventAttribute(bool canBeCancelled = false)
        {
            CanBeCancelled = canBeCancelled;
        }
    }
}