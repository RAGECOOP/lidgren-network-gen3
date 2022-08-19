using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ZeroTier;
using System.Net;
using System.Net.Sockets;

namespace UnitTests
{
    internal class ZerotierTest
    {
        public static void NodeTest()
        {
            var networkId = 0x8056c2e21c000001;
            var node = new ZeroTier.Core.Node();
            Console.WriteLine("blah");
            node.Start();   // Network activity only begins after calling Start()
            while (!node.Online)
            {
                Thread.Sleep(50);
            }
            Console.WriteLine("Id            : " + node.IdString);
            Console.WriteLine("Version       : " + node.Version);
            Console.WriteLine("PrimaryPort   : " + node.PrimaryPort);
            Console.WriteLine("SecondaryPort : " + node.SecondaryPort);
            Console.WriteLine("TertiaryPort  : " + node.TertiaryPort);

            node.Join(networkId);
            Console.WriteLine("Waiting for join to complete...");
            while (node.Networks.Count == 0)
            {
                Thread.Sleep(50);
            }
        }
    }
}
