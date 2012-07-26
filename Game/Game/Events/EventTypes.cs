namespace IsoGame.Events
{
    public enum EventType
    {
        AllEvents,
        //  UI
        MenuCancel, MenuChange, MenuHover, MenuSelect,
        //  Game states
        Loading, Saving, GetReady, Paused, Exiting,
        //  Animation events
        SprLoop, SprJump, SprOverlay, StepLeft, StepRight, Hit, Fire, Sound, Pickup, UnknownAnimationEvent, 
        CharacterBusy, CharacterAvailable,
        //  Player commands
        MoveForward, MoveBack, MoveLeft, MoveRight,
        //  Misc
        Impact
    }
}