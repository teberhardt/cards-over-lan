using System;

namespace CardsOverLan.Game
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ClientObjectPolicyAttribute : Attribute
    {
        public ClientObjectPolicyAttribute(ClientObjectPolicyType policyType = ClientObjectPolicyType.OptOut)
        {
            PolicyType = policyType;
        }

        public ClientObjectPolicyType PolicyType { get; }
    }
}