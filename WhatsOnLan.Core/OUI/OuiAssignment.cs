namespace WhatsOnLan.Core.OUI
{
    /// <summary>
    /// Represents an IEEE OUI assignment.
    /// </summary>
    public class OuiAssignment
    {
        /// <summary>
        /// Gets the first six characters of the OUI assignment.
        /// </summary>
        public string Assignment { get; }

        /// <summary>
        /// Gets the name of the organization the OUI is assigned to.
        /// </summary>
        public string Organization { get; }

        /// <summary>
        /// Initializes an instance of the <see cref="OuiAssignment"/> class using the given assignment and organization name.
        /// </summary>
        /// <param name="assignment">The six-character OUI assignment.</param>
        /// <param name="organization">The organization name</param>
        /// <exception cref="ArgumentException"></exception>
        public OuiAssignment(string assignment, string organization)
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