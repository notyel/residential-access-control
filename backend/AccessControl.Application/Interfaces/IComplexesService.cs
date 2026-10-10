using AccessControl.Common.DTOs.Complex;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IComplexesService
    {
        Task<List<ComplexDto>> GetAllAsync();
        Task<ComplexDto> GetByIdAsync(Guid id);

        /// <summary>Crea el conjunto y su primer administrador.</summary>
        Task<ComplexDto> CreateAsync(CreateComplexDto dto);

        Task<ComplexDto> UpdateAsync(Guid id, SaveComplexDto dto);

        /// <summary>Un conjunto inactivo no permite iniciar sesión a sus usuarios.</summary>
        Task<ComplexDto> SetActiveAsync(Guid id, bool isActive);
    }
}
