using System;
using System.Numerics;

namespace DungeonLife
{
    public class ThreadSafeRandom : IRandom
    {
        private static readonly Random _global = new Random();
        [ThreadStatic] private static Random _local;

        static ThreadSafeRandom()
        {
            Instance = new ThreadSafeRandom();
        }

        private ThreadSafeRandom()
        {
        }

        public static ThreadSafeRandom Instance { get; }

        private void Verify()
        {
            if (_local == null)
            {
                int seed;
                lock (_global)
                {
                    seed = _global.Next();
                }
                _local = new Random(seed);
            }
        }

        public int Next()
        {
            Verify();
            return _local.Next();
        }

        public int Next(int maxValue)
        {
            Verify();
            return _local.Next(maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            Verify();
            return _local.Next(minValue, maxValue);
        }

        public void NextBytes(byte[] buffer)
        {
            Verify();
            _local.NextBytes(buffer);
        }

        public void NextBytes(Span<byte> buffer)
        {
            Verify();
            _local.NextBytes(buffer);
        }

        public double NextDouble()
        {
            Verify();
            return _local.NextDouble();
        }

        public Vector2 NextDirection()
        {
            return Vector2.Normalize(new Vector2((float)NextDouble(), (float)NextDouble()) * 2.0f - Vector2.One);
        }

        public TEnum NextEnum<TEnum>()
            where TEnum : struct, Enum
        {
            var values = Enum.GetValues<TEnum>();
            var index = Next(0, values.Length);
            return values[index];
        }
    }
}
