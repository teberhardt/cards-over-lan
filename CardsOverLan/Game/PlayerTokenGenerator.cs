using System;
using System.Linq;
using System.Security.Cryptography;

namespace CardsOverLan.Game
{
	internal sealed class PlayerTokenGenerator : IDisposable
	{
		private const int TokenSize = 16;

		private bool _disposed;
		private readonly object _disposeLock = new object();
		private readonly object _genLock = new object();
		private readonly byte[] _buffer;
		private readonly RNGCryptoServiceProvider _rng;

		public PlayerTokenGenerator()
		{
			_rng = new RNGCryptoServiceProvider();
			_buffer = new byte[TokenSize];
		}

		public string CreateToken()
		{
			lock(_genLock)
			{
				_rng.GetBytes(_buffer, 0, TokenSize);
				return String.Join("", _buffer.Select(b => b.ToString("X2").ToLowerInvariant()));
			}
		}

		public void Dispose()
		{
			lock(_disposeLock)
			{
				if (_disposed) throw new ObjectDisposedException($"Tried to dispose a {nameof(PlayerTokenGenerator)} that was already disposed.");

				_rng.Dispose();

				_disposed = true;
			}
		}
	}
}
