using System.Net.NetworkInformation;

namespace WhatsOnLan.Core.OUI
{
    public interface IOuiMatcher
    {
        string GetOrganizationName(PhysicalAddress macAddress);
        string GetOrganizationName(string macAddress);
    }
}