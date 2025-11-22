using AccessControl.Common.DTOs.Person;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IPersonService
    {
        Task<List<PersonDto>> SearchPersonsAsync(string documentNumber);
        Task<PersonDto> CreatePersonAsync(CreatePersonDto dto);
        Task<PersonDto> UpdatePersonAsync(Guid id, UpdatePersonDto dto);
    }
}
