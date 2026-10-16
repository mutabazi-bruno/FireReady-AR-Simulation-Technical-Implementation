using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    // FR-23 and FR-07: resume, reposition, restart or quit. Quitting saves the session as Abandoned.
    public class PauseScreen : UIScreen
    {
        [SerializeField] Button resumeButton;
        [SerializeField] Button repositionButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button quitButton;

        protected override void BindButtons()
        {
            Bind(resumeButton, () => Sim.ResumeSession());
            Bind(repositionButton, () => Sim.Reposition());
            Bind(restartButton, () => UI.Confirm.Ask(
                "Restart the session?",
                "This attempt is saved as Abandoned and a new one starts in the same spot.",
                "Restart",
                () => Sim.RestartSession()));
            Bind(quitButton, () => UI.Confirm.Ask(
                "Quit to the main menu?",
                "This attempt is saved in your history as Abandoned.",
                "Quit",
                () => Sim.QuitToMenu()));
        }
    }
}
