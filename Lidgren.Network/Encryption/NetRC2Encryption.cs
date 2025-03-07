using System;
using System.Security.Cryptography;

namespace Lidgren.Network
{
    [Obsolete("Broken")]
    public class NetRC2Encryption : NetCryptoProviderBase
    {
        public NetRC2Encryption(NetPeer peer)
            : base(peer, new RC2CryptoServiceProvider())
        {
        }

        public NetRC2Encryption(NetPeer peer, string key)
            : base(peer, new RC2CryptoServiceProvider())
        {
            SetKey(key);
        }

        public NetRC2Encryption(NetPeer peer, byte[] data, int offset, int count)
            : base(peer, new RC2CryptoServiceProvider())
        {
            SetKey(data, offset, count);
        }
    }
}
