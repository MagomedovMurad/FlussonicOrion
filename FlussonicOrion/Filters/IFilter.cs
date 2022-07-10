using FlussonicOrion.Models;
using System;
using System.Threading.Tasks;

namespace FlussonicOrion.Filters
{
    public interface IFilter
    {
        //event PassRequestHandler NewRequest;
        void Subscribe(Func<string, PassageDirection, Task> handler);
        void AddRequest(string licensePlate, PassageDirection direction);
        void RemoveRequest(string licensePlate);
    }
}
