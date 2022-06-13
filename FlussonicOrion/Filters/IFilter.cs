using FlussonicOrion.Models;
using FlussonicOrion.Utils;
using System;

namespace FlussonicOrion.Filters
{
    public interface IFilter
    {
        event PassRequestHandler NewRequest;
        void AddRequest(string licensePlate, PassageDirection direction);
        void RemoveRequest(string licensePlate);
    }
}
