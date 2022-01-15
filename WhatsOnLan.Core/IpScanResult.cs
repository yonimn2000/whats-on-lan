using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace WhatsOnLan.Core
{
    public class IpScanResult
    {
        public IPAddress IpAddress { get; internal set; } = IPAddress.None;
        public PhysicalAddress MacAddress { get; internal set; } = PhysicalAddress.None;
        public string Hostname { get; internal set; } = string.Empty;
        public bool RespondedToPing { get; internal set; } = false;

        public bool RespondedToArp => !MacAddress.Equals(PhysicalAddress.None);
        public bool IsOnline => RespondedToArp || RespondedToPing;
        public bool HasHostname => Hostname.Length > 0;

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append(IpAddress);

            if (IsOnline)
            {
                if (RespondedToArp)
                {
                    stringBuilder.Append(' ');
                    stringBuilder.Append(MacAddress);
                }

                if (HasHostname)
                {
                    stringBuilder.Append(' ');
                    stringBuilder.Append(Hostname);
                }

                if (RespondedToPing)
                    stringBuilder.Append(" [Pings]");
            }
            else
                stringBuilder.Append(" [Offline]");

            return stringBuilder.ToString();
        }
    }
}