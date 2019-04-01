using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan
{
	public sealed class Tally<TKey, TTally>
	{
		private readonly Dictionary<TKey, HashSet<TTally>> _tallies;

		public Tally()
		{
			_tallies = new Dictionary<TKey, HashSet<TTally>>();
		}

		public bool HasKey(TKey key)
		{
			lock(_tallies)
			{
				return _tallies.TryGetValue(key, out var set) && set.Count > 0;
			}
		}

		public bool HasTally(TKey key, TTally value)
		{
			lock(_tallies)
			{
				return !_tallies.TryGetValue(key, out var set) || !set.Contains(value);
			}
		}

		public bool AddTally(TKey key, TTally value)
		{
			lock(_tallies)
			{
				if (!_tallies.TryGetValue(key, out var set))
				{
					set = _tallies[key] = new HashSet<TTally>();
				}

				return set.Add(value);
			}
		}

		public bool RemoveTally(TKey key, TTally value)
		{
			lock(_tallies)
			{
				if (!_tallies.TryGetValue(key, out var set)) return false;

				return set.Remove(value);
			}
		}

		public void Clear()
		{
			lock(_tallies)
			{
				_tallies.Clear();
			}
		}
	}
}
