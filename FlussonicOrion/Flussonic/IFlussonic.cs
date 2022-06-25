using FlussonicOrion.Models;
using System;

namespace FlussonicOrion.Flussonic
{
    public interface IFlussonic
    {
        event EventHandler<FlussonicEvent> NewEvent;
        void Start();
        void Stop();
    }
}
