using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan.Game.ContractResolvers
{
	internal sealed class ClientFacingContractResolver : DefaultContractResolver
	{
		public static ClientFacingContractResolver Instance { get; } = new ClientFacingContractResolver();

		public ClientFacingContractResolver()
		{
		}

		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			var prop = base.CreateProperty(member, memberSerialization);
			var policy = member.DeclaringType.GetCustomAttribute<ClientObjectPolicyAttribute>()?.PolicyType ?? ClientObjectPolicyType.OptOut;
			var attrCf = member.GetCustomAttribute<ClientFacingAttribute>();
			bool shouldIgnore = member.GetCustomAttribute<ClientIgnoreAttribute>() != null;

			bool shouldSerialize = (policy == ClientObjectPolicyType.OptIn && attrCf != null) || !shouldIgnore;

			if (!shouldSerialize)
			{
				prop.ShouldSerialize = o => false;
			}

			return prop;
		}
	}
}
