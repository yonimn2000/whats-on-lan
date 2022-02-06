namespace YonatanMankovich.WhatsOnLan.Core.EventArgs
{
    /// <summary>
    /// Represents the class that contains progress changed event data.
    /// </summary>
    public class ProgressChangedEventArgs : System.EventArgs
    {
        private const int MinProgress = 0;
        private const int MaxProgress = 100;

        private int progress;

        /// <summary>
        /// The progress;
        /// </summary>
        public int Progress
        {
            get => progress;
            set
            {
                if (MinProgress <= value && value <= MaxProgress)
                    progress = value;
                else
                    throw new ArgumentOutOfRangeException("Progress",
                        $"Progress value must be between {MinProgress} and {MaxProgress}");
            }
        }

        /// <summary>
        /// The status message.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// The completion status.
        /// </summary>
        public bool IsComplete => Progress == MaxProgress;
    }
}