using System.Net.NetworkInformation;

namespace YonatanMankovich.WhatsOnLan.Core.OUI
{
    /// <summary>
    /// Represents an OUI matcher for matching MAC addresses to the corresponding organization name 
    /// (NIC manufacturer) using the IEEE OUI dataset.
    /// </summary>
    public class OuiMatcher : IOuiMatcher
    {
        private IReadOnlyDictionary<string, string> Matcher { get; }

        /// <summary>
        /// Initializes an instance of the <see cref="OuiMatcher"/> class that reads the OUI data from a file at
        /// the given path. The given OUI CSV file must contain 'Assignment' and 'Organization Name' columns.
        /// The CSV file must have a header row.
        /// </summary>
        /// <param name="path">The location of the OUI CSV file.</param>
        public OuiMatcher(string path) : this(OuiCsvFileHelpers.ReadOuiCsvFileLines(path)) { }

        /// <summary>
        /// Initializes an instance of the <see cref="OuiMatcher"/> class with the given <see cref="OuiAssignment"/> objects.
        /// </summary>
        /// <param name="ouiAssignments">The OUI Assignments to use when initialzing the new <see cref="OuiMatcher"/> object.</param>
        public OuiMatcher(IEnumerable<OuiAssignment> ouiAssignments)
        {
            Matcher = OuiCsvFileHelpers.ConvertOuiAssignmentsToDictionary(ouiAssignments);
        }

        public string GetOrganizationName(PhysicalAddress macAddress)
        {
            return GetOrganizationName(macAddress.ToString());
        }

        public string GetOrganizationName(string macAddress)
        {
            string assignment = macAddress.Substring(0, 6);
            return Matcher.ContainsKey(assignment) ? Matcher[assignment] : string.Empty;
        }
    }
}