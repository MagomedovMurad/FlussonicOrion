using FlussonicOrion.Models;
using FlussonicOrion.Utils;

namespace FlussonicOrion.Filters
{
    internal class EmptyFilter : IFilter
    {
        public event PassRequestHandler NewRequest;

        public void AddRequest(string licensePlate, PassageDirection direction)
        {
            NewRequest.Invoke(licensePlate, direction);
        }

        public void RemoveRequest(string licensePlate)
        {
            
        }
    }
}
