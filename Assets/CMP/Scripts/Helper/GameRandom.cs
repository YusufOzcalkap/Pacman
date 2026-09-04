using System.Collections.Generic;

namespace CMP.Scripts.Helper
{
    /// <summary>
    /// Seed'lenebilir rastgelelik kaynağı.
    /// UnityEngine.Random yerine bunu kullanıyoruz çünkü Scatter davranışının
    /// tekrar edilebilir olması gerekiyor: aynı seed her zaman aynı yapay zeka
    /// hareketini üretir, bu da hem hata ayıklamayı hem testi mümkün kılar.
    /// </summary>
    public class GameRandom
    {
        private readonly System.Random _random;

        public GameRandom(int seed)
        {
            _random = new System.Random(seed);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return _random.Next(minInclusive, maxExclusive);
        }

        public T Pick<T>(IReadOnlyList<T> items)
        {
            return items[Range(0, items.Count)];
        }
    }
}
