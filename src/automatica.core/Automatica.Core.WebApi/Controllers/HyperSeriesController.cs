using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.HyperSeries;
using Automatica.Core.HyperSeries.Model;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.WebApi.Controllers
{
   

    [Route("webapi/hyperseries")]
    public class HyperSeriesController : ControllerBase
    {
        /// <summary>
        /// Upper bound of the values of one request (the client asks for a few hundred at most)
        /// </summary>
        public const int MaxCount = 10_000;

        private readonly IHyperSeriesRepository _repository;
        private readonly INodeInstanceCache _nodeInstanceCache;

        public HyperSeriesController(IHyperSeriesRepository repository, INodeInstanceCache nodeInstanceCache)
        {
            _repository = repository;
            _nodeInstanceCache = nodeInstanceCache;
        }

        [HttpGet("{aggregationType}")]
        [Authorize(Policy = Role.ViewerRole)]
        public async Task<List<AggregatedRecordValue>> GetHourlyValues([FromRoute]AggregationType aggregationType, [FromQuery] Guid id, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int? count)
        {
            if (!_repository.IsActivated)
            {
                return new List<AggregatedRecordValue>();
            }

            // nodes of other user groups look like nodes without values
            var node = _nodeInstanceCache.Get(id);
            if (node == null || !CanAccess(node.This2UserGroup))
            {
                return new List<AggregatedRecordValue>();
            }

            var take = Math.Clamp(count ?? 10, 1, MaxCount);
            return await _repository.GetAggregatedValues(aggregationType, id, startDate, endDate, take);
        }

        private bool CanAccess(Guid? userGroup)
        {
            if (userGroup == null || User.IsInRole(Role.AdminRole))
            {
                return true;
            }

            var group = userGroup.ToString();
            return User.HasClaim(a => a.Type == UserGroup.ClaimType && a.Value == group);
        }
    }
}
