using System.Net;

namespace YonatanMankovich.WhatsOnLan.Core.Helpers
{
    /// <summary>
    /// Provides helper methods for working with <see cref="IPAddress"/>es.
    /// </summary>
    public static class IpAddressHelpers
    {
        /// <summary>
        /// Creates a sortable string representation of the <see cref="IPAddress"/>
        /// where every octet is always three characters long (###.###.###.###).
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <returns>A sortable string representation of the <see cref="IPAddress"/>.</returns>
        public static string ToSortableString(this IPAddress address)
        {
            string[] octets = new string[4];
            byte[] addressBytes = address.GetAddressBytes();

            for (int i = 0; i < octets.Length; i++)
                octets[i] = addressBytes[i].ToString("D3");

            return string.Join('.', octets);
        }
    }
}