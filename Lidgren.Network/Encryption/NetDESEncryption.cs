using System;
using System.Security.Cryptography;

namespace Lidgren.Network
{
    [Obsolete("Broken")]
    public class NetDESEncryption : NetCryptoProviderBase
    {
        public NetDESEncryption(NetPeer peer)
            : base(peer, new DESCryptoServiceProvider())
        {
        }

        public NetDESEncryption(NetPeer peer, string key)
            : base(peer, new DESCryptoServiceProvider())
        {
            SetKey(key);
        }

        public NetDESEncryption(NetPeer peer, byte[] data, int offset, int count)
            : base(peer, new DESCryptoServiceProvider())
        {
            SetKey(data, offset, count);
        }
    }
}
