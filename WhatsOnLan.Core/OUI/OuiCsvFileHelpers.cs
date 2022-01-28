namespace YonatanMankovich.WhatsOnLan.Core.OUI
{
    /// <summary>
    /// Provides methods to work with the IEEE OUI CSV file.
    /// </summary>
    public static class OuiCsvFileHelpers
    {
        /// <summary>
        /// The URL of the IEEE OUI CSV file.
        /// </summary>
        public const string IeeeOuiCsvFileUrl = "http://standards-oui.ieee.org/oui/oui.csv";

        /// <summary>
        /// Downloads the OUI CSV file from the specified URL to the specified location.
        /// </summary>
        /// <param name="path">The location to download the OUI CSV file to.</param>
        /// <param name="ouiCsvUrl">The URL of the OUI CSV file.</param>
        public static async Task DownloadOuiCsvFileAsync(string path, string ouiCsvUrl = IeeeOuiCsvFileUrl)
        {
            HttpResponseMessage response = await new HttpClient().GetAsync(new Uri(ouiCsvUrl));
            using FileStream fileStream = new FileStream(path, FileMode.Create);
            await response.Content.CopyToAsync(fileStream);
        }

        /// <summary>
        /// Reads the lines of the OUI CSV file that contains 'Assignment' and 'Organization Name' columns.
        /// The CSV file must have a header row.
        /// </summary>
        /// <param name="path">The location of the OUI CSV file.</param>
        /// <returns>Instances of the <see cref="OuiAssignment"/> class for each line read.</returns>
        public static IEnumerable<OuiAssignment> ReadOuiCsvFileLines(string path)
        {
            string[] headerRowTokens = File.ReadLines(path).First().Split(',');
            int indexOfAssignmentColumn = Array.IndexOf(headerRowTokens, "Assignment");
            int indexOfOrganizationNameColumn = Array.IndexOf(headerRowTokens, "Organization Name");

            if (indexOfAssignmentColumn < 0)
                throw new InvalidDataException("The given OUI CSV file is missing the 'Assignment' column.");

            if (indexOfOrganizationNameColumn < 0)
                throw new InvalidDataException("The given OUI CSV file is missing the 'Organization Name' column.");

            foreach (string line in File.ReadLines(path).Skip(1)) // Skip first headers row.
            {
                // Headers: Registry,Assignment,Organization Name,Organization Address
                string[] rowTokens = line.Split(',');
                string assignment = rowTokens[indexOfAssignmentColumn];
                string organization = rowTokens[indexOfOrganizationNameColumn].Trim('"');
                yield return new OuiAssignment(assignment, organization);
            }
        }
    }
}