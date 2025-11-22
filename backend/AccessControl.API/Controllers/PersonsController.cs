using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Person;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonsController : ControllerBase
    {
        private readonly IPersonService _personService;

        public PersonsController(IPersonService personService)
        {
            _personService = personService;
        }

        [HttpGet]
        [Authorize(Policy = "AdminOrGuard")]
        public async Task<IActionResult> SearchPersons([FromQuery] string documentNumber)
        {
            try
            {
                var persons = await _personService.SearchPersonsAsync(documentNumber);
                return Ok(new ResponseModel<object>(true, "Persons retrieved successfully.", persons));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred while retrieving persons.", null, new() { ex.Message }));
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminOrGuard")]
        public async Task<IActionResult> CreatePerson([FromBody] CreatePersonDto dto)
        {
            try
            {
                var person = await _personService.CreatePersonAsync(dto);
                return Ok(new ResponseModel<PersonDto>(true, "Person created successfully.", person));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ResponseModel<string>(false, ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred while creating the person.", null, new() { ex.Message }));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrGuard")]
        public async Task<IActionResult> UpdatePerson(Guid id, [FromBody] UpdatePersonDto dto)
        {
            try
            {
                var person = await _personService.UpdatePersonAsync(id, dto);
                if (person == null) return NotFound(new ResponseModel<string>(false, "Person not found.", null));
                return Ok(new ResponseModel<PersonDto>(true, "Person updated successfully.", person));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred while updating the person.", null, new() { ex.Message }));
            }
        }
    }
}
