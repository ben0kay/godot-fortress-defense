// Shared initial construction state. Only the level's work manager advances elapsed time.
using System;

public enum BuildingWorkPhase { Operational, AwaitingDrone, Constructing }

public partial class Building
{
    protected virtual float InitialConstructionSeconds => 0;
    public BuildingWorkPhase WorkPhase { get; private set; }
    public bool RequiresConstruction => ConstructionDuration > 0;
    public bool IsOperational => WorkPhase == BuildingWorkPhase.Operational;
    public float ConstructionDuration { get; private set; }
    public double ConstructionElapsed { get; private set; }
    public float ConstructionProgress => IsOperational ? 1 : (float)(ConstructionElapsed / ConstructionDuration);
    public event Action<Building> WorkStarted, WorkProgressChanged, WorkCompleted, WorkCancelled;
    private double _nextWorkNotice = .1;
    private float _nextProgressThreshold = float.PositiveInfinity;

    private void InitializeConstruction()
    {
        float seconds = InitialConstructionSeconds;
        if (!float.IsFinite(seconds) || seconds < 0)
            throw new InvalidOperationException("Construction duration must be finite and nonnegative.");
        ConstructionDuration = seconds;
        if (RequiresConstruction) WorkPhase = BuildingWorkPhase.AwaitingDrone;
    }

    internal void StartConstruction()
    {
        WorkPhase = BuildingWorkPhase.Constructing;
        WorkStarted?.Invoke(this);
    }

    // An artwork threshold can request a notification between the regular HUD samples.
    internal void RequestWorkProgressAt(float threshold)
    {
        if (threshold > ConstructionProgress)
            _nextProgressThreshold = Math.Min(_nextProgressThreshold, threshold);
    }

    internal void AdvanceConstruction(double delta)
    {
        ConstructionElapsed = Math.Min(ConstructionDuration, ConstructionElapsed + delta);
        if (ConstructionElapsed >= ConstructionDuration)
        {
            WorkPhase = BuildingWorkPhase.Operational;
            NotifyVisionChanged();
            WorkCompleted?.Invoke(this);
        }
        else if (ConstructionElapsed >= _nextWorkNotice || ConstructionProgress >= _nextProgressThreshold)
        {
            _nextWorkNotice = ConstructionElapsed + .1;
            if (ConstructionProgress >= _nextProgressThreshold) _nextProgressThreshold = float.PositiveInfinity;
            WorkProgressChanged?.Invoke(this);
        }
    }
}
