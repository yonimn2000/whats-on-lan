using System.Net.NetworkInformation;

namespace YonatanMankovich.WhatsOnLan.Core.Helpers
{
    /// <summary>
    /// Provides helper methods for working with <see cref="PhysicalAddress"/>es.
    /// </summary>
    public static class PhysicalAddressHelpers
    {
        /// <summary>
        /// Creates a string representation of the <see cref="PhysicalAddress"/>
        /// where every octet is split by a colon (##:##:##:##:##:##).
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <returns>A sortable string representation of the <see cref="PhysicalAddress"/>.</returns>
        public static string ToColonString(this PhysicalAddress address)
            => string.Join(":", address.GetAddressBytes().Select(b => b.ToString("X2")));
    }
}