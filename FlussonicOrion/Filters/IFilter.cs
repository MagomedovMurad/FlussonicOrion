using FlussonicOrion.Models;
using System;

namespace FlussonicOrion.Filters
{
    public interface IFilter
    {
        event EventHandler<PassRequest> NewRequest;
        void AddRequest(string licensePlate, PassageDirection direction);
        void RemoveRequest(string licensePlate);
    }
}
