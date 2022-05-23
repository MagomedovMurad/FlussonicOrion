using FlussonnicOrion.Models;
using System;

namespace FlussonnicOrion.Filters
{
    internal class EmptyFilter : IFilter
    {
        public event EventHandler<PassRequest> NewRequest;

        public void AddRequest(string licensePlate, PassageDirection direction)
        {
            var request = new PassRequest();
            request.LicensePlate = licensePlate;
            request.EnterInFrameTime = DateTime.Now;
            request.Direction = direction;
            NewRequest.Invoke(this, request);
        }

        public void RemoveRequest(string licensePlate)
        {
            throw new NotImplementedException();
        }
    }
}
