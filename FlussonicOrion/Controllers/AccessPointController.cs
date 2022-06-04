using FlussonicOrion.Filters;
using FlussonicOrion.Models;
using FlussonicOrion.OrionPro;
using FlussonicOrion.OrionPro.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlussonicOrion.Controllers
{
    public class AccessPointController
    {
        private ILogger _logger;
        private IOrionClient _orionClient;
        private IAccessController _accessController;
        private IFilter _filter;
        public int Id;

        public AccessPointController(int id, IFilter filter, ILogger logger, IAccessController accessController, IOrionClient orionClient)
        {
            Id = id;
            _filter = filter;
            _logger = logger;
            _accessController = accessController;
            _orionClient = orionClient;
            _filter.NewRequest += Filter_NewRequest;
        }

        public void OnEnter(string licensePlate, PassageDirection direction)
        {
            _filter.AddRequest(licensePlate, direction);
        }
        public void OnLeave(string licensePlate)
        {
            _filter.RemoveRequest(licensePlate);
        }

        private void Filter_NewRequest(object sender, PassRequest request)
        {
            var accessResults = _accessController.CheckAccess(request.LicensePlate, Id, request.Direction);
            var allowedAccessResult = accessResults.Where(x => x.AccessAllowed)
                                                   .OrderByDescending(x => x.StartDateTime)
                                                   .FirstOrDefault();
            foreach (var result in accessResults)
            {
                var text = $"Доступ {(result.AccessAllowed ? "разрешен" : "запрещен")}. {result.PersonData}. {result.Reason}";
                _logger.LogInformation(text);
            }

            if (allowedAccessResult != null)
            {
                _logger.LogInformation($"Отправка команды на открытие двери {Id} для {allowedAccessResult.PersonData}");
                _orionClient.ControlAccesspoint(Id, AccesspointCommand.ProvisionOfAccess, Convert(request.Direction), allowedAccessResult.PersonId).Wait();
            }

            AddExternalEvents(accessResults.Except(new[] { allowedAccessResult }).ToList(), Id).Wait();
            AddAdditionalExternalEvents(accessResults, request.LicensePlate, Id).Wait();
        }
        private ActionType Convert(PassageDirection passageDirection)
        {
            switch (passageDirection)
            {
                case PassageDirection.Entry:
                    return ActionType.Entry;

                case PassageDirection.Exit:
                    return ActionType.Exit;

                default: throw new InvalidOperationException($"Тип {passageDirection} не поддерживается");
            }
        }
        private async Task AddExternalEvents(IEnumerable<AccessRequestResult> requestResults, int accesspointId)
        {
            foreach (var accessResult in requestResults)
            {
                var eventType = accessResult.AccessAllowed ? EventType.AccessGranted : EventType.AccessDenied;
                await _orionClient.AddExternalEvent(0, accesspointId, ItemType.ACCESSPOINT, (int)eventType, accessResult.KeyId, accessResult.PersonId, null);
            }
        }
        private async Task AddAdditionalExternalEvents(IEnumerable<AccessRequestResult> requestResults, string number, int accesspointId)
        {
            foreach (var accessResult in requestResults)
            {
                var text = $"{number}. {(accessResult.AccessAllowed ? "Доступ" : "Запрет")}. {accessResult.Reason}";// {accessResult.PersonData}";
                await _orionClient.AddExternalEvent(0, accesspointId, ItemType.ACCESSPOINT, (int)EventType.ThirdPartyEvent, accessResult.KeyId, accessResult.PersonId, text);
            }
        }
    }
}

