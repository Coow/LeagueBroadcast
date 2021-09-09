using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Common.Interface
{
    public abstract class TickController
    {
        private static Dictionary<uint, TickTimer> _timers = new();

        private static uint _tickRate = 500;

        public uint TickRateInMS
        {
            get { return _tickRate; }
            set { _tickRate = value; }
        }

        public uint TicksPerSecond
        {
            get { return 1 / _tickRate; }
            set { _tickRate = 1 / value; }
        }

        public static bool AddTickable(ITickable toTick, uint milliseconds = 0)
        {
            if (toTick.IsTicking())
            {
                return false;
            }

            if(milliseconds == 0)
            {
                milliseconds = _tickRate;
            }

            if (_timers[milliseconds] is null)
            {
                _timers.Add(milliseconds, new TickTimer(milliseconds).With(toTick));
                return true;
            }

            _timers[milliseconds].Add(toTick);
            return true;
        }

        public static bool RemoveTickable(ITickable tickable)
        {
            return _timers.Values.ToList().Any(timer => timer.Remove(tickable));
        }

        public static bool IsTicking(ITickable tickable)
        {
            return _timers.Values.ToList().Any(timer => timer.IsTicking(tickable));
        }

        public static void Stop()
        {
            _timers.Values.ToList().ForEach(timer => timer.Stop());
            _timers.Clear();
        }
    }


    public class TickTimer : IDisposable
    {
        private readonly Timer _timer;
        private readonly HashSet<ITickable> toTick;

        public TickTimer(uint milliseconds)
        {
            var autoEvent = new AutoResetEvent(false);
            _timer = new Timer(DoTick, autoEvent!, milliseconds, milliseconds);
            toTick = new HashSet<ITickable>();
        }

        public bool Add(ITickable tickable)
        {
            return toTick.Add(tickable);
        }

        public bool Remove(ITickable tickable)
        {
            return toTick.Remove(tickable);
        }

        public bool IsTicking(ITickable tickable)
        {
            return toTick.Contains(tickable);
        }


        public TickTimer With(ITickable tickable)
        {
            Add(tickable);
            return this;
        }

        public void Stop()
        {
            Dispose();
        }

        private void DoTick(object? State)
        {
            foreach (ITickable toTick in toTick)
            {
                toTick.DoTick();
            }
        }

        protected virtual void Dispose(bool disposing)
        {

            if (disposing)
            {
                _timer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
