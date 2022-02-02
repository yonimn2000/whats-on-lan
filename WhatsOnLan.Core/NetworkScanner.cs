﻿using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using YonatanMankovich.WhatsOnLan.Core.Exceptions;
using YonatanMankovich.WhatsOnLan.Core.Hardware;
using YonatanMankovich.WhatsOnLan.Core.Network;

namespace YonatanMankovich.WhatsOnLan.Core
{
    /// <summary>
    /// Represents a network scanner.
    /// </summary>
    public class NetworkScanner : INetworkScanner
    {
        /// <summary>
        /// The network scanner options.
        /// </summary>
        public NetworkScannerOptions Options { get; set; } = new NetworkScannerOptions();

        /// <summary>
        /// Gets or sets the network interface of the scanner.
        /// </summary>
        public PcapNetworkInterface Interface { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="NetworkScanner"/> objects with a <see cref="PcapNetworkInterface"/>.
        /// </summary>
        /// <param name="interfaces">The network interface to initialize the scanner with.</param>
        public NetworkScanner(PcapNetworkInterface interfaces)
        {
            Interface = interfaces;
        }

        /// <summary>
        /// Scans a given <see cref="IPAddress"/> on the current network <see cref="Interface"/> 
        /// and returns the <see cref="IpScanResult"/>.
        /// </summary>
        /// <param name="ipAddress">The IP address to scan (must be on the same network as the <see cref="Interface"/>).</param>
        /// <returns>The <see cref="IpScanResult"/>.</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<IpScanResult> ScanIpAddressAsync(IPAddress ipAddress)
        {
            if (!IsIpAddressOnCurrentNetwork(ipAddress))
                throw new IpAddressNotOnNetworkException(ipAddress, Interface.Network, Interface.SubnetMask);

            IpScanResult scanResult = new IpScanResult();

            scanResult.IpAddress = ipAddress;
            scanResult.WasPinged = Options.SendPings;
            scanResult.WasArpRequested = Options.SendArpRequest;

            if (Options.SendPings)
                scanResult.RespondedToPing = await Pinger.PingAsync(ipAddress);

            if (Options.SendArpRequest)
            {
                ArpMapper arpMapper = new ArpMapper(Interface)
                {
                    Timeout = Options.ArpTimeout
                };
                PhysicalAddress macAddress = await Task.Run(() => arpMapper.MapIpAddressToMacAddress(ipAddress));
                if (!macAddress.Equals(PhysicalAddress.None))
                {
                    scanResult.MacAddress = macAddress;
                    scanResult.Manufacturer = Options.OuiMatcher?.GetOrganizationName(macAddress);
                }
            }

            if (Options.ResolveHostnames && (scanResult.RespondedToPing || scanResult.RespondedToArp))
                scanResult.Hostname =
                    await HostnameResolver.ResolveHostnameAsync(ipAddress, Options.StripDnsSuffix ? Interface.DnsSuffix : "");

            return scanResult;
        }

        public bool IsIpAddressOnCurrentNetwork(IPAddress ipAddress)
        {
            return IpAddressHelpers.IsOnSameNetwork(ipAddress, Interface.IpAddress, Interface.SubnetMask);
        }

        /// <summary>
        /// Scans all the possible IP addresses on the network of the <see cref="Interface"/> asynchronously.
        /// </summary>
        /// <returns>The <see cref="IpScanResult"/>s of the network scan.</returns>
        public Task<IEnumerable<IpScanResult>> ScanNetworkAsync() => Task.Run(ScanNetwork);

        /// <summary>
        /// Scans all the possible IP addresses on the network of the <see cref="Interface"/>.
        /// </summary>
        /// <returns>The <see cref="IpScanResult"/>s of the network scan.</returns>
        public IEnumerable<IpScanResult> ScanNetwork()
        {
            Debug.WriteLine("Getting all IP addresses...");
            IEnumerable<IPAddress> ipAddresses = Interface.GetAllNetworkHostIpAddresses();
            IEnumerable<IPAddress> respondingHosts = Array.Empty<IPAddress>();
            IDictionary<IPAddress, PhysicalAddress> macs;
            IDictionary<IPAddress, bool> pings;
            IDictionary<IPAddress, string> hostnames;

            if (Options.SendArpRequest)
            {
                Debug.WriteLine("Getting all MAC addresses...");
                ArpMapper arpMapper = new ArpMapper(Interface)
                {
                    Timeout = Options.ArpTimeout
                };
                macs = arpMapper.MapIpAddressesToMacAddresses(ipAddresses);
                respondingHosts = macs.Where(m => !m.Value.Equals(PhysicalAddress.None)).Select(m => m.Key);
            }
            else
                macs = ipAddresses.ToDictionary(ip => ip, ip => PhysicalAddress.None);

            if (Options.SendPings)
            {
                Debug.WriteLine("Sending all pings...");
                pings = Pinger.Ping(ipAddresses);
                respondingHosts = respondingHosts.Union(pings.Where(p => p.Value).Select(p => p.Key));
            }
            else
                pings = ipAddresses.ToDictionary(ip => ip, ip => false);

            if (Options.ResolveHostnames)
            {
                Debug.WriteLine("Resolving all responding hostnames...");
                hostnames = HostnameResolver.ResolveHostnames(respondingHosts, Options.StripDnsSuffix ? Interface.DnsSuffix : "");
            }
            else
                hostnames = ipAddresses.ToDictionary(ip => ip, ip => string.Empty);

            Debug.WriteLine("Generating scan results...");
            foreach (IPAddress ip in ipAddresses)
            {
                IpScanResult scanResult = new IpScanResult();

                scanResult.IpAddress = ip;
                scanResult.WasPinged = Options.SendPings;
                scanResult.WasArpRequested = Options.SendArpRequest;

                if (Options.SendPings && pings.ContainsKey(ip))
                    scanResult.RespondedToPing = pings[ip];

                if (Options.ResolveHostnames && hostnames.ContainsKey(ip))
                    scanResult.Hostname = hostnames[ip];

                if (Options.SendArpRequest)
                {
                    PhysicalAddress macAddress = macs[ip];
                    if (!macAddress.Equals(PhysicalAddress.None))
                    {
                        scanResult.MacAddress = macAddress;
                        scanResult.Manufacturer = Options.OuiMatcher?.GetOrganizationName(macAddress);
                    }
                }

                yield return scanResult;
            }
            Debug.WriteLine("Done generating scan results!");
        }
    }
}