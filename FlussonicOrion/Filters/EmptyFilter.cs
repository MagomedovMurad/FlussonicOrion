using FlussonicOrion.Models;
using System;
using System.Threading.Tasks;

namespace FlussonicOrion.Filters
{
    internal class EmptyFilter : IFilter
    {
        //public event PassRequestHandler NewRequest;
        private Func<string, PassageDirection, Task> _handler;
        public void Subscribe(Func<string, PassageDirection, Task> handler)
        {
            _handler = handler;
        }

        public void AddRequest(string licensePlate, PassageDirection direction)
        {
            _handler.Invoke(licensePlate, direction);
        }

        public void RemoveRequest(string licensePlate)
        {
            
        }
    }
}
