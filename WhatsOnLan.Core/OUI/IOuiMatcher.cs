using System.Net.NetworkInformation;

namespace WhatsOnLan.Core.OUI
{
    /// <summary>
    /// Defines methods for matching MAC addresses to the corresponding organization name 
    /// (NIC manufacturer) using the IEEE OUI dataset.
    /// </summary>
    public interface IOuiMatcher
    {
        /// <summary>
        /// Gets the organization name (NIC manufacturer) from a <see cref="PhysicalAddress"/> using the IEEE OUI dataset.
        /// </summary>
        /// <param name="macAddress">The <see cref="PhysicalAddress"/> to be used in the organization name lookup.</param>
        /// <returns>The organization name (NIC manufacturer).</returns>
        string GetOrganizationName(PhysicalAddress macAddress);

        /// <summary>
        /// Gets the organization name (NIC manufacturer) from a MAC address using the IEEE OUI dataset.
        /// </summary>
        /// <param name="macAddress">The MAC address to be used in the organization name lookup.</param>
        /// <returns>The organization name (NIC manufacturer).</returns>
        string GetOrganizationName(string macAddress);
    }
}