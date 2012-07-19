namespace IsoGame.Events
{
    public enum EventType
    {
        AllEvents,
        //  UI
        MenuCancel, MenuChange, MenuHover, MenuSelect,
        //  Translation
        MoveForward, MoveBack, MoveLeft, MoveRight,
        //  Rotation
        Yaw, Pitch, Roll, YawLeft, YawRight, PitchUp, PitchDown, RollClockwise, RollAntiClockwise,
        //  Game states
        Loading, Saving, GetReady, Paused, Exiting,
        //  
        Collision,
    }
}