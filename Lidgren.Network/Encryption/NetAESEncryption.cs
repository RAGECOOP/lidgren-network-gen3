using System.Security.Cryptography;

namespace Lidgren.Network
{
	/// <summary>
	/// 
	/// </summary>
	public class NetAESEncryption : NetCryptoProviderBase
	{
		/// <summary>
		/// 
		/// </summary>
		public NetAESEncryption(NetPeer peer)
			: base(peer, new AesCryptoServiceProvider())
		{
		}

		/// <summary>
		/// 
		/// </summary>
		public NetAESEncryption(NetPeer peer, string key)
			: base(peer, new AesCryptoServiceProvider())
		{
			SetKey(key);
		}

		/// <summary>
		/// 
		/// </summary>
		public NetAESEncryption(NetPeer peer, byte[] data, int offset, int count)
			: base(peer, new AesCryptoServiceProvider())
		{
			SetKey(data, offset, count);
		}
	}
}
