namespace WhatsOnLan.Core.OUI
{
    public class OuiDataRow
    {
        public string Assignment { get; private set; }
        public string Organization { get; private set; }

        public OuiDataRow(string assignment, string organization)
        {
            if (assignment.Length != 6)
                throw new ArgumentException("OUI assignment string must be exatly six characters long.");
            
            if (string.IsNullOrWhiteSpace(organization))
                throw new ArgumentException("OUI organization name must not be empty.");

            Assignment = assignment;
            Organization = organization;
        }
    }
}