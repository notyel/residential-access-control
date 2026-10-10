using AccessControl.Common.DTOs.Menu;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IMenuService
    {
        /// <summary>
        /// Opciones que habilitan los permisos efectivos de la sesión. El superadministrador
        /// recibe además "Conjuntos", y las del conjunto solo cuando tiene uno elegido.
        /// </summary>
        Task<IEnumerable<MenuDto>> GetMenuAsync();
    }
}
