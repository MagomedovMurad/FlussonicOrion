using FlussonnicOrion.Api;
using FlussonnicOrion.Models;
using System;
using System.Timers;

namespace FlussonnicOrion.Flussonic
{
    public class FlussonicClient: IFlussonic
    {
        private FlussonicApi _flussonicApi;
        private Timer _timer;
        public event EventHandler<FlussonicEvent> NewEvent;

        public void Start()
        {
            _timer = new Timer();
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Stop();
            _timer.Dispose();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var events = _flussonicApi.GetEvents();
            foreach (var @event in events)
                NewEvent?.Invoke(this, @event);
        }
    }
}
