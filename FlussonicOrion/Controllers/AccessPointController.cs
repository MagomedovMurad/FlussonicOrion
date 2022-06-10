using FlussonicOrion.Filters;
using FlussonicOrion.Models;
using FlussonicOrion.OrionPro;
using FlussonicOrion.OrionPro.Enums;
using FlussonicOrion.Utils;
using Microsoft.Extensions.Logging;
using System;

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
            var result = _accessController.CheckAccess(request.LicensePlate, Id, request.Direction);
            if (result.AccessAllowed)
            {
                _orionClient.ControlAccesspoint(Id, AccesspointCommand.ProvisionOfAccess, Convert(request.Direction), result.Person.Id).Wait();
                _logger.LogInformation($"Отправлена команда на открытие двери {Id} для {result.Person}");
            }
            else
            {
                SaveAccessDeniedEvent(result);
            }

            SaveAccessResultEvent(result, request.LicensePlate);
        }

        private async void SaveAccessResultEvent(AccessRequestResult result, string licensePlate)
        {
            var access = result.AccessAllowed ? "Доступ" : "Запрет";
            var company = result.Person?.Company ?? "";
            var fullName = result.Person is null ? "" : $"{result.Person.LastName} {result.Person.FirstName[0]}.{result.Person.MiddleName[0]}.";

            var eventText = ShortStringHelper.CreateSring(licensePlate, access, company, fullName, result.Reason);

            await _orionClient.AddExternalEvent(
                    0,
                    Id,
                    ItemType.ACCESSPOINT,
                    (int)EventType.ThirdPartyEvent,
                    result.KeyId,
                    result.Person?.Id ?? 0,
                    eventText);

            _logger.LogInformation($"Сохранено событие: {eventText}");
        }
        private async void SaveAccessDeniedEvent(AccessRequestResult result)
        {
            await _orionClient.AddExternalEvent(
                0,
                Id,
                ItemType.ACCESSPOINT, 
                (int)EventType.AccessDenied,
                result.KeyId,
                result.Person?.Id ?? 0, 
                null);
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
    }
}

