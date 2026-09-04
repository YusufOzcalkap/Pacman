namespace CMP.Scripts
{
    /// <summary>
    /// Oyunun bir dalgası: belirli bir süre boyunca geçerli olan yapay zeka modu.
    /// Dalgalar sırayla işletilir, sonuncusu sonsuza kadar sürer.
    /// </summary>
    public readonly struct ModeWave
    {
        public readonly GameMode Mode;
        public readonly float Duration;

        public ModeWave(GameMode mode, float duration)
        {
            Mode = mode;
            Duration = duration;
        }
    }
}
