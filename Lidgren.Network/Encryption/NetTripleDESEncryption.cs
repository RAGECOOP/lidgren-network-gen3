using System;
using System.Security.Cryptography;

namespace Lidgren.Network
{
    [Obsolete("Broken")]
    public class NetTripleDESEncryption : NetCryptoProviderBase
    {
        public NetTripleDESEncryption(NetPeer peer)
            : base(peer, new TripleDESCryptoServiceProvider())
        {
        }

        public NetTripleDESEncryption(NetPeer peer, string key)
            : base(peer, new TripleDESCryptoServiceProvider())
        {
            SetKey(key);
        }

        public NetTripleDESEncryption(NetPeer peer, byte[] data, int offset, int count)
            : base(peer, new TripleDESCryptoServiceProvider())
        {
            SetKey(data, offset, count);
        }
    }
}
