﻿using Microsoft.AspNetCore.Authorization;
using FlightPlanner.Core.Validations;
using FlightPlanner.Core.Services;
using FlightPlanner.Core.Models;
using Microsoft.AspNetCore.Mvc;
using FlightPlanner.Models;
using AutoMapper;

namespace FlightPlanner.Controllers
{
    [Route("admin-api")]
    [ApiController, Authorize]
    public class AdminApiController : ControllerBase
    {
        private static readonly object taskLock = new();
        private readonly IFlightService _flightService;
        private readonly IFlightValidator _flightValidator;
        private readonly IMapper _mapper;

        public AdminApiController(IFlightService flightService,
            IFlightValidator flightValidator,
            IMapper mapper)
        {
            _flightService = flightService;
            _flightValidator = flightValidator;
            _mapper = mapper;
        }

        [Route("flights/{id}")]
        [HttpGet]
        public IActionResult GetFlight(int id)
        {
            Flight flight = _flightService.GetCompleteFlightById(id);

            if (flight == null)
            {
                return NotFound(); //404
            }
            
            var response = _mapper.Map<FlightRequest>(flight);

            return Ok(response); //200
        }

        [Route("flights")]
        [HttpPut]
        public IActionResult PutFlight(FlightRequest request)
        {
            lock (taskLock)
            {
                var flight = _mapper.Map<Flight>(request);

                if (!_flightValidator.IsFlightValid(flight))
                {
                return BadRequest(); //400
                }

                if (_flightService.GetCompleteFlightById(flight.Id) != null ||
                    _flightService.Exists(flight))
                {
                    return Conflict(); //409
                }

                var result = _flightService.Create(flight);

                if (result.Success)
                {
                    request = _mapper.Map<FlightRequest>(flight);

                    return Created("",request); //201
                }

                return Problem(result.FormattedErrors);
            }
        }

        [Route("flights/{id}")]
        [HttpDelete]
        public IActionResult DeleteFlight(int id)
        {
            Flight flight = _flightService.GetCompleteFlightById(id);

            if (flight != null)
            {
                var result = _flightService.Delete(flight);

                if (result.Success)
                {
                    return Ok();
                }

                return Problem(result.FormattedErrors);
            }

            return Ok();
        }
    }
}
