using System;

namespace CardsOverLan
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal sealed class ClientIgnoreAttribute : Attribute
    {
    }
}