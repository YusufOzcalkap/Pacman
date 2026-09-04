using UnityEngine;

namespace CMP.Scripts.AiStates.ChaseTargets
{
    /// <summary>
    /// Bir hayaletin takip hedefini belirler.
    ///
    /// Chase davranışının tamamı (kavşak kuralları, en kısa yol, geri dönüş yasağı)
    /// bütün hayaletlerde ortaktır; kişilikleri ayıran tek şey bu hedef hücredir.
    /// </summary>
    public interface IChaseTarget
    {
        Vector2Int GetTargetCell(GhostBlackboard blackboard);
    }
}
