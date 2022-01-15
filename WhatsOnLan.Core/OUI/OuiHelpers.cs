namespace WhatsOnLan.Core.OUI
{
    public static class OuiHelpers
    {
        public const string IeeeOuiCsvFileUrl = "http://standards-oui.ieee.org/oui/oui.csv";

        public static async Task DownloadOuiCsvFileAsync(string path)
        {
            HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(new Uri(IeeeOuiCsvFileUrl));
            using FileStream fileStream = new(path, FileMode.Create);
            await response.Content.CopyToAsync(fileStream);
        }

        public static IEnumerable<OuiDataRow> ReadOuiCsvFileLines(string path)
        {
            foreach (string line in File.ReadLines(path).Skip(1)) // Skip first headers row.
            {
                // Headers: Registry,Assignment,Organization Name,Organization Address
                string[] tokens = line.Split(',');
                string assignment = tokens[1];
                string organization = tokens[2].Trim('"');
                yield return new OuiDataRow(assignment, organization);
            }
        }
    }
}