using System.Security.Cryptography;

namespace Lidgren.Network
{
	/// <summary>
	/// 
	/// </summary>
	public class NetRC2Encryption : NetCryptoProviderBase
	{
		/// <summary>
		/// 
		/// </summary>
		public NetRC2Encryption(NetPeer peer)
			: base(peer, new RC2CryptoServiceProvider())
		{
		}

		/// <summary>
		/// 
		/// </summary>
		public NetRC2Encryption(NetPeer peer, string key)
			: base(peer, new RC2CryptoServiceProvider())
		{
			SetKey(key);
		}

		/// <summary>
		/// 
		/// </summary>
		public NetRC2Encryption(NetPeer peer, byte[] data, int offset, int count)
			: base(peer, new RC2CryptoServiceProvider())
		{
			SetKey(data, offset, count);
		}
	}
}
