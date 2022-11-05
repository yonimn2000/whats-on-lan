using System.Text.RegularExpressions;

namespace YonatanMankovich.WhatsOnLan.Core.OUI
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
            if (!IsAssignmentOfCorrectLength(assignment))
                throw new ArgumentException("OUI assignment string must be exactly six characters long. " +
                    $"Given length: {assignment.Length}.", assignment);

            if (!IsAssignmentOfCorrectFormat(assignment))
                throw new ArgumentException("OUI assignment string must be a hexadecimal string of " +
                    "exactly six characters long.", assignment);

            if (!IsOrganizationValid(organization))
                throw new ArgumentException("OUI organization name must not be empty.", organization);

            Assignment = assignment.ToUpper();
            Organization = organization;
        }

        /// <summary>
        /// Gets a value indicating whether an OUI assignment is of the correct length.
        /// </summary>
        /// <param name="assignment">The OUI assignment.</param>
        /// <returns>
        /// Returns <see langword="true"/> if the given OUI assignment is of the correct length;
        /// <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsAssignmentOfCorrectLength(string assignment) => assignment.Length == 6;

        /// <summary>
        /// Gets a value indicating whether an OUI assignment is of the correct format of six consecutive hexadecimal digits.
        /// </summary>
        /// <param name="assignment">The OUI assignment.</param>
        /// <returns>
        /// Returns <see langword="true"/> if the given OUI assignment is of the correct format;
        /// <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsAssignmentOfCorrectFormat(string assignment) => Regex.IsMatch(assignment, "[0-9a-fA-F]{6}");

        /// <summary>
        /// Gets a value indicating whether an OUI assignment is of the correct format of six consecutive hexadecimal digits.
        /// </summary>
        /// <param name="assignment">The OUI assignment.</param>
        /// <returns>
        /// Returns <see langword="true"/> if the given OUI assignment is of the correct format;
        /// <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsAssignmentValid(string assignment)
            => IsAssignmentOfCorrectLength(assignment) && IsAssignmentOfCorrectFormat(assignment);

        /// <summary>
        /// Gets a value indicating whether an OUI organization <see cref="string"/> is valid.
        /// The organization must be a non-empty string.
        /// </summary>
        /// <param name="organization">The OUI organization.</param>
        /// <returns>
        /// Returns <see langword="true"/> if the given OUI organization <see cref="string"/> is valid
        /// (a non-empty string); <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsOrganizationValid(string organization) => !string.IsNullOrWhiteSpace(organization);
    }
}