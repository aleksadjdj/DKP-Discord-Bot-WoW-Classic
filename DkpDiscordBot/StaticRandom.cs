using System;
using System.Threading;

namespace DkpDiscordBot
{
    public static class StaticRandom
    {
        private static int _seed;

        private static readonly ThreadLocal<Random> ThreadLocal = new ThreadLocal<Random>
            (() => new Random(Interlocked.Increment(ref _seed)));

        static StaticRandom()
        {
            _seed = (Environment.TickCount * 1000) + DateTime.Now.Millisecond;
        }

        //public static Random Instance { get { return threadLocal.Value; } }
        public static Random Instance => ThreadLocal.Value;
    }
}
