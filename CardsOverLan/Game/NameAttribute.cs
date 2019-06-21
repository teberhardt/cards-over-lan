using System;

namespace CardsOverLan.Game
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NameAttribute : Attribute
    {
        public string Name { get; }

        public NameAttribute(string name)
        {
            Name = name;
        }
    }
}