using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.HyperSeries;
using Automatica.Core.HyperSeries.Model;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
   

    [Route("webapi/hyperseries")]
    public class HyperSeriesController
    {
        private readonly IHyperSeriesRepository _repository;

        public HyperSeriesController(IHyperSeriesRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{aggregationType}")]
        [Authorize(Policy = Role.ViewerRole)]
        public async Task<List<AggregatedRecordValue>> GetHourlyValues([FromRoute]AggregationType aggregationType, [FromQuery] Guid id, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int? count)
        {
            if (!_repository.IsActivated)
            {
                return new List<AggregatedRecordValue>();
            }

            return await _repository.GetAggregatedValues(aggregationType, id, startDate, endDate, count ?? 10);
        }
    }
}
