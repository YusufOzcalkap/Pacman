namespace CMP.Scripts
{
    /// <summary>
    /// Aktif level için hesaplanmış efektif değerler.
    /// GameSettings temel sabitleri, LevelDefinition çarpanları tutar; ikisinin
    /// çarpımı burada bir kez hesaplanır ve oyun boyunca buradan okunur.
    /// </summary>
    public class LevelRules
    {
        public readonly float PacmanStepDuration;
        public readonly float GhostStepDuration;
        public readonly float FrightenedStepDuration;
        public readonly float EatenStepDuration;
        public readonly float FrightenedDuration;
        public readonly int GhostCount;

        public LevelRules(LevelDefinition definition)
        {
            // Çarpan hızı ifade eder; adım süresi hızın tersi olduğu için bölünür.
            PacmanStepDuration = GameSettings.PacmanMovementDuration / definition.PacmanSpeedMultiplier;
            GhostStepDuration = GameSettings.AiMovementDuration / definition.GhostSpeedMultiplier;
            FrightenedStepDuration = GameSettings.FrightenedMovementDuration / definition.GhostSpeedMultiplier;
            EatenStepDuration = GameSettings.EatenMovementDuration / definition.GhostSpeedMultiplier;
            FrightenedDuration = definition.FrightenedDuration;
            GhostCount = definition.GhostCount;
        }
    }
}
