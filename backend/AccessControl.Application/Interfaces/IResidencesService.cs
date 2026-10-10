using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Residence;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IResidencesService
    {
        /// <summary>Busca por identificador o por nombre del propietario.</summary>
        Task<PaginatedResultDto<ResidenceDto>> SearchAsync(string? search, int pageNumber, int pageSize);
    }
}
