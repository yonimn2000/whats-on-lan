namespace YonatanMankovich.WhatsOnLan.Core.OUI
{
    /// <summary>
    /// Provides methods to work with the IEEE OUI CSV file.
    /// </summary>
    public static class OuiCsvFileHelpers
    {
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

                // Skip invalid records.
                if (rowTokens.Length <= Math.Max(indexOfAssignmentColumn, indexOfOrganizationNameColumn))
                    continue;

                string assignment = rowTokens[indexOfAssignmentColumn];
                string organization = rowTokens[indexOfOrganizationNameColumn].Trim('"');
                yield return new OuiAssignment(assignment, organization);
            }
        }

        /// <summary>
        /// Converts the given <see cref="OuiAssignment"/> objects <see cref="IEnumerable{T}"/> to a dictionary.
        /// </summary>
        /// <param name="ouiAssignments">The OUI assignments.</param>
        /// <returns>The OUI assignments dictionary where the keys are the assignments and values are the organizations.</returns>
        public static IReadOnlyDictionary<string, string> ConvertOuiAssignmentsToDictionary(IEnumerable<OuiAssignment> ouiAssignments)
        {
            Dictionary<string, string> matcher = new Dictionary<string, string>();
            foreach (OuiAssignment ouiAssignment in ouiAssignments)
            {
                if (matcher.ContainsKey(ouiAssignment.Assignment))
                    matcher[ouiAssignment.Assignment] += " OR" + ouiAssignment.Organization;
                else
                    matcher[ouiAssignment.Assignment] = ouiAssignment.Organization;
            }
            return matcher;
        }
    }
}