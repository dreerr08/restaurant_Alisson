using System;

namespace AliGame.Items
{
    public enum CutPhase
    {
        /// <summary>The key that opened the minigame is still down; nothing counts until it is released once.</summary>
        WaitingRelease,
        Idle,
        /// <summary>The key is held and the bar is filling.</summary>
        Charging,
        /// <summary>The bar is full: releasing now (inside the window) makes the cut count.</summary>
        Window,
        /// <summary>The window ran out while the key was still held. The cut is lost; release the key to try again.</summary>
        Missed,
        Finished,
        Cancelled
    }

    /// <summary>
    /// The rules of the cutting minigame, without any Unity types. Hold the key for Hold Seconds until the bar is full,
    /// then release it within Release Window seconds: that is one cut. Releasing too early only throws away the charge,
    /// and holding past the window is a miss that does not count. Cuts already made are never lost.
    /// After Required Cuts good releases the minigame is finished.
    /// </summary>
    public class CutMinigameModel
    {
        public CutMinigameModel(float holdSeconds, int requiredCuts, float releaseWindow = 1f)
        {
            HoldSeconds = Math.Max(0.05f, holdSeconds);
            RequiredCuts = Math.Max(1, requiredCuts);
            ReleaseWindow = Math.Max(0.05f, releaseWindow);
        }

        public float HoldSeconds { get; }

        public int RequiredCuts { get; }

        /// <summary>Seconds after the bar is full during which releasing the key counts.</summary>
        public float ReleaseWindow { get; }

        public int CutsDone { get; private set; }

        /// <summary>Seconds held toward the current cut (up to Hold Seconds).</summary>
        public float Charge { get; private set; }

        /// <summary>Seconds spent inside the release window.</summary>
        public float WindowElapsed { get; private set; }

        public CutPhase Phase { get; private set; } = CutPhase.WaitingRelease;

        public bool IsOver => Phase == CutPhase.Finished || Phase == CutPhase.Cancelled;

        /// <summary>The key went down and the charge began.</summary>
        public event Action ChargeStarted;

        /// <summary>The key was released before the bar was full. Nothing counts.</summary>
        public event Action ChargeAborted;

        /// <summary>The bar is full and the release window opened.</summary>
        public event Action WindowOpened;

        /// <summary>The key was released inside the window: the cut counts. The argument is how many cuts are done.</summary>
        public event Action<int> CutSucceeded;

        /// <summary>The key was held past the window. The cut does not count.</summary>
        public event Action CutMissed;

        public event Action FinishedAll;

        public void Update(bool held, float deltaTime)
        {
            switch (Phase)
            {
                case CutPhase.WaitingRelease:
                    if (!held) Phase = CutPhase.Idle;
                    break;

                case CutPhase.Idle:
                    if (!held) break;
                    Phase = CutPhase.Charging;
                    Charge = 0f;
                    ChargeStarted?.Invoke();
                    Charge += deltaTime;
                    CheckChargeFull();
                    break;

                case CutPhase.Charging:
                    if (!held)
                    {
                        Charge = 0f;
                        Phase = CutPhase.Idle;
                        ChargeAborted?.Invoke();
                        break;
                    }

                    Charge += deltaTime;
                    CheckChargeFull();
                    break;

                case CutPhase.Window:
                    if (!held)
                    {
                        Succeed();
                        break;
                    }

                    WindowElapsed += deltaTime;
                    if (WindowElapsed >= ReleaseWindow) Miss();
                    break;

                case CutPhase.Missed:
                    if (!held) Phase = CutPhase.Idle;
                    break;
            }
        }

        public void Cancel()
        {
            if (IsOver) return;
            Phase = CutPhase.Cancelled;
            Charge = 0f;
            WindowElapsed = 0f;
        }

        private void CheckChargeFull()
        {
            if (Charge < HoldSeconds) return;

            WindowElapsed = Charge - HoldSeconds;
            Charge = HoldSeconds;
            Phase = CutPhase.Window;
            WindowOpened?.Invoke();

            if (WindowElapsed >= ReleaseWindow) Miss();
        }

        private void Succeed()
        {
            CutsDone++;
            Charge = 0f;
            WindowElapsed = 0f;
            bool last = CutsDone >= RequiredCuts;
            Phase = last ? CutPhase.Finished : CutPhase.Idle;

            CutSucceeded?.Invoke(CutsDone);
            if (last) FinishedAll?.Invoke();
        }

        private void Miss()
        {
            Charge = 0f;
            WindowElapsed = 0f;
            Phase = CutPhase.Missed;
            CutMissed?.Invoke();
        }
    }
}
