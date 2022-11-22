using System.Security.Cryptography;

namespace Lidgren.Network
{
	/// <summary>
	/// 
	/// </summary>
	public class NetTripleDESEncryption : NetCryptoProviderBase
	{
		/// <summary>
		/// 
		/// </summary>
		public NetTripleDESEncryption(NetPeer peer)
			: base(peer, new TripleDESCryptoServiceProvider())
		{
		}

		/// <summary>
		/// 
		/// </summary>
		public NetTripleDESEncryption(NetPeer peer, string key)
			: base(peer, new TripleDESCryptoServiceProvider())
		{
			SetKey(key);
		}

		/// <summary>
		/// 
		/// </summary>
		public NetTripleDESEncryption(NetPeer peer, byte[] data, int offset, int count)
			: base(peer, new TripleDESCryptoServiceProvider())
		{
			SetKey(data, offset, count);
		}
	}
}
