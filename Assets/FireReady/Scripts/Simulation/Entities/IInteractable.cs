namespace FireReady.Simulation
{
    // Anything the trainee can tap. The InteractionManager only knows this interface.
    public interface IInteractable
    {
        bool CanInteract { get; }
        void OnTap(InteractionContext context);
    }
}
