using System;

namespace CardsOverLan.Game
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal sealed class ClientFacingAttribute : Attribute
    {}
}