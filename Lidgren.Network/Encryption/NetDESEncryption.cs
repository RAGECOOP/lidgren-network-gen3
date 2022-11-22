using System.Security.Cryptography;

namespace Lidgren.Network
{
	/// <summary>
	/// 
	/// </summary>
	public class NetDESEncryption : NetCryptoProviderBase
	{
		/// <summary>
		/// 
		/// </summary>
		public NetDESEncryption(NetPeer peer)
			: base(peer, new DESCryptoServiceProvider())
		{
		}

		/// <summary>
		/// 
		/// </summary>
		public NetDESEncryption(NetPeer peer, string key)
			: base(peer, new DESCryptoServiceProvider())
		{
			SetKey(key);
		}

		/// <summary>
		/// 
		/// </summary>
		public NetDESEncryption(NetPeer peer, byte[] data, int offset, int count)
			: base(peer, new DESCryptoServiceProvider())
		{
			SetKey(data, offset, count);
		}
	}
}
