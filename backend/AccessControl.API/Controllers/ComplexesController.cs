using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Complex;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class ComplexesController : ControllerBase
    {
        private readonly IComplexesService _complexesService;

        public ComplexesController(IComplexesService complexesService)
        {
            _complexesService = complexesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var complexes = await _complexesService.GetAllAsync();
            return Ok(new ResponseModel<List<ComplexDto>>(true, "Conjuntos consultados.", complexes));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var complex = await _complexesService.GetByIdAsync(id);
            return Ok(new ResponseModel<ComplexDto>(true, "Conjunto consultado.", complex));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateComplexDto dto)
        {
            var complex = await _complexesService.CreateAsync(dto);
            return Ok(new ResponseModel<ComplexDto>(true, "Conjunto creado.", complex));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveComplexDto dto)
        {
            var complex = await _complexesService.UpdateAsync(id, dto);
            return Ok(new ResponseModel<ComplexDto>(true, "Conjunto actualizado.", complex));
        }

        [HttpPost("{id}/activate")]
        public async Task<IActionResult> Activate(Guid id)
        {
            var complex = await _complexesService.SetActiveAsync(id, true);
            return Ok(new ResponseModel<ComplexDto>(true, "Conjunto activado.", complex));
        }

        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var complex = await _complexesService.SetActiveAsync(id, false);
            return Ok(new ResponseModel<ComplexDto>(true, "Conjunto desactivado.", complex));
        }
    }
}
