namespace FireReady.Simulation
{
    public class TrainingTask
    {
        public string Id { get; }
        public string Description { get; }
        public bool IsRequired { get; }
        public string LinkedFireId { get; }
        public bool IsComplete { get; private set; }
        public bool IsAvailable { get; private set; }
        public float CompletedAt { get; private set; } = -1f;

        public TrainingTask(string id, string description, bool isRequired, bool available, string linkedFireId = null)
        {
            Id = id;
            Description = description;
            IsRequired = isRequired;
            IsAvailable = available;
            LinkedFireId = linkedFireId;
        }

        public void Unlock()
        {
            IsAvailable = true;
        }

        public void MarkComplete(float atSeconds)
        {
            IsAvailable = true;
            IsComplete = true;
            CompletedAt = atSeconds;
        }

        // A fire that reignites puts its task back on the list.
        public void Reopen()
        {
            IsComplete = false;
            CompletedAt = -1f;
        }
    }
}
