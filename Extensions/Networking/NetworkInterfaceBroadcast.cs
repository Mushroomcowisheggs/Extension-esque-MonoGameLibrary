using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MonoGameLibrary.Extensions.Networking {
    /// <summary>
    /// Enumerates the addresses a datagram must be sent to for every machine on the local network to see it.
    /// </summary>
    /// <remarks>
    /// A single limited broadcast to <c>255.255.255.255</c> is not enough in practice: many stacks route it
    /// out of one interface only, which is why a player with a virtual adapter (a VPN, a hypervisor switch or
    /// a container bridge) can be invisible to the rest of the network. The helper therefore also computes the
    /// directed broadcast address of every usable IPv4 interface.
    /// </remarks>
    internal static class NetworkInterfaceBroadcast {
        /// <summary>
        /// Builds the list of endpoints that a room advert must be sent to.
        /// </summary>
        /// <param name="port">The discovery port.</param>
        /// <param name="flagLimited">True to include the limited broadcast address.</param>
        /// <param name="flagSubnet">True to include the directed broadcast address of every interface.</param>
        /// <param name="flagLoopback">True to include the loopback interface, which reaches other instances on this machine.</param>
        /// <returns>The distinct endpoints to send to.</returns>
        internal static List<IPEndPoint> GetEndpoints(int port, bool flagLimited, bool flagSubnet, bool flagLoopback) {
            List<IPAddress> addresses = new List<IPAddress>();
            if (flagLimited) {
                addresses.Add(IPAddress.Broadcast);
            }
            if (flagSubnet) {
                CollectSubnetBroadcasts(addresses, flagLoopback);
            }
            if (addresses.Count == 0) {
                // A host with no usable interface can still reach its own machine.
                addresses.Add(IPAddress.Loopback);
            }
            
            List<IPEndPoint> endpoints = new List<IPEndPoint>(addresses.Count);
            HashSet<string> setSeen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < addresses.Count; index += 1) {
                IPAddress address = addresses[index];
                if (!setSeen.Add(address.ToString())) {
                    continue;
                }
                endpoints.Add(new IPEndPoint(address, port));
            }
            return endpoints;
        }
        
        /// <summary>
        /// Adds the directed broadcast address of every usable IPv4 interface.
        /// </summary>
        /// <param name="addresses">The list to add to.</param>
        /// <param name="flagLoopback">True to include the loopback interface.</param>
        private static void CollectSubnetBroadcasts(List<IPAddress> addresses, bool flagLoopback) {
            NetworkInterface[] interfacesNetwork;
            try {
                interfacesNetwork = NetworkInterface.GetAllNetworkInterfaces();
            } catch (NetworkInformationException) {
                return;
            }
            
            for (int index = 0; index < interfacesNetwork.Length; index += 1) {
                NetworkInterface adapter = interfacesNetwork[index];
                if (adapter.OperationalStatus != OperationalStatus.Up) {
                    continue;
                }
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel) {
                    continue;
                }
                if (!flagLoopback && adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback) {
                    continue;
                }
                
                UnicastIPAddressInformationCollection addressesUnicast;
                try {
                    addressesUnicast = adapter.GetIPProperties().UnicastAddresses;
                } catch (NetworkInformationException) {
                    continue;
                } catch (PlatformNotSupportedException) {
                    continue;
                }
                
                for (int indexAddress = 0; indexAddress < addressesUnicast.Count; indexAddress += 1) {
                    UnicastIPAddressInformation information = addressesUnicast[indexAddress];
                    if (information.Address.AddressFamily != AddressFamily.InterNetwork) {
                        continue;
                    }
                    IPAddress mask = information.IPv4Mask;
                    if (mask == null) {
                        continue;
                    }
                    byte[] bytesAddress = information.Address.GetAddressBytes();
                    byte[] bytesMask = mask.GetAddressBytes();
                    if (bytesAddress.Length != 4 || bytesMask.Length != 4) {
                        continue;
                    }
                    byte[] bytesBroadcast = new byte[4];
                    for (int indexByte = 0; indexByte < 4; indexByte += 1) {
                        bytesBroadcast[indexByte] = (byte)(bytesAddress[indexByte] | (byte)(~bytesMask[indexByte]));
                    }
                    addresses.Add(new IPAddress(bytesBroadcast));
                }
            }
        }
    }
}
