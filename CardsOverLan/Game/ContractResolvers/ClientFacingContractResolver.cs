using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace CardsOverLan.Game.ContractResolvers
{
    internal sealed class ClientFacingContractResolver : DefaultContractResolver
    {
        public static ClientFacingContractResolver Instance { get; } = new ClientFacingContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var prop = base.CreateProperty(member, memberSerialization);
            var policy = member.DeclaringType.GetCustomAttribute<ClientObjectPolicyAttribute>()?.PolicyType ?? ClientObjectPolicyType.OptOut;
            var attrCf = member.GetCustomAttribute<ClientFacingAttribute>();
            var shouldIgnore = member.GetCustomAttribute<ClientIgnoreAttribute>() != null;

            var shouldSerialize = policy == ClientObjectPolicyType.OptIn && attrCf != null || !shouldIgnore;

            if (!shouldSerialize)
            {
                prop.ShouldSerialize = o => false;
            }

            return prop;
        }
    }
}