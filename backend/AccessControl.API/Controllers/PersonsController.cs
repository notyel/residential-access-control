using AccessControl.API.Infrastructure;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Person;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.PeopleRead, Permissions.PeopleWrite)]
    public class PersonsController : ControllerBase
    {
        private readonly IPersonService _personService;

        public PersonsController(IPersonService personService)
        {
            _personService = personService;
        }

        [HttpGet]
        public async Task<IActionResult> SearchPersons([FromQuery] string? documentNumber)
        {
            var persons = await _personService.SearchPersonsAsync(documentNumber);
            return Ok(new ResponseModel<List<PersonDto>>(true, "Persons retrieved successfully.", persons));
        }

        [HttpPost]
        [HasPermission(Permissions.PeopleWrite)]
        public async Task<IActionResult> CreatePerson([FromBody] CreatePersonDto dto)
        {
            var person = await _personService.CreatePersonAsync(dto);
            return Ok(new ResponseModel<PersonDto>(true, "Person created successfully.", person));
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.PeopleWrite)]
        public async Task<IActionResult> UpdatePerson(Guid id, [FromBody] UpdatePersonDto dto)
        {
            var person = await _personService.UpdatePersonAsync(id, dto);
            return Ok(new ResponseModel<PersonDto>(true, "Person updated successfully.", person));
        }
    }
}
