using System.Net.NetworkInformation;

namespace WhatsOnLan.Core.OUI
{
    public class OuiMatcher
    {
        private IReadOnlyDictionary<string, string> Matcher { get; set; }

        public OuiMatcher(string path) : this(OuiHelpers.ReadOuiCsvFileLines(path)) { }

        public OuiMatcher(IEnumerable<OuiDataRow> ouiDataRows)
        {
            Dictionary<string, string> matcher = new Dictionary<string, string>();
            foreach (OuiDataRow row in ouiDataRows)
            {
                if (matcher.ContainsKey(row.Assignment))
                    matcher[row.Assignment] += " OR" + row.Organization;
                else
                    matcher[row.Assignment] = row.Organization;
            }
            Matcher = matcher;
        }

        public string? Match(PhysicalAddress mac)
        {
            string assignment = mac.ToString().Substring(0, 6);
            return Matcher.ContainsKey(assignment) ? Matcher[assignment] : null;
        }
    }
}