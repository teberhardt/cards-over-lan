using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan.Game
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	internal sealed class ClientObjectPolicyAttribute : Attribute
	{
		public ClientObjectPolicyAttribute(ClientObjectPolicyType policyType = ClientObjectPolicyType.OptOut)
		{
			PolicyType = policyType;
		}

		public ClientObjectPolicyType PolicyType { get; }
	}
}
