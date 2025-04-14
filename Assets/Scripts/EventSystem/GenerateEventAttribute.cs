using System;

namespace EventSystem
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GenerateEventAttribute : Attribute
    {
        public GenerateEventAttribute(bool canBeCancelled = false)
        {
            CanBeCancelled = canBeCancelled;
        }

        public bool CanBeCancelled { get; }
    }
}