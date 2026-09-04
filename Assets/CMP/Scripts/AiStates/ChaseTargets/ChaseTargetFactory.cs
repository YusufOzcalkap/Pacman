namespace CMP.Scripts.AiStates.ChaseTargets
{
    public static class ChaseTargetFactory
    {
        public static IChaseTarget Create(GhostPersonality personality)
        {
            return personality switch
            {
                GhostPersonality.Pinky => new PinkyChaseTarget(),
                GhostPersonality.Inky => new InkyChaseTarget(),
                GhostPersonality.Clyde => new ClydeChaseTarget(),
                _ => new BlinkyChaseTarget(),
            };
        }
    }
}
