using FlussonnicOrion.Models;
using System;

namespace FlussonnicOrion.Filters
{
    public interface IFilter
    {
        event EventHandler<PassRequest> NewRequest;
        void AddRequest(string licensePlate, PassageDirection direction);
        void RemoveRequest(string licensePlate);
    }
}
