using FlussonnicOrion.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlussonnicOrion.Flussonic
{
    public interface IFlussonic
    {
        event EventHandler<FlussonicEvent> NewEvent;
        void Start();
        void Stop();
    }
}
