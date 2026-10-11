using AccessControl.Application.Exceptions;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Units
{
    /// <summary>
    /// Catálogos que cada conjunto puede ajustar. Los valores de aquí son los iniciales: rigen
    /// mientras el conjunto no guarde su propia lista.
    /// </summary>
    public static class Catalogs
    {
        public const string StructureType = "structure-type";
        public const string UnitType = "unit-type";
        public const string DocumentType = "document-type";
        public const string VisitReason = "visit-reason";
        public const string VehicleType = "vehicle-type";
        public const string RejectionReason = "rejection-reason";
        public const string AccessPoint = "access-point";
        public const string IncidentCategory = "incident-category";

        public static readonly IReadOnlyDictionary<string, (string Code, string Label)[]> Defaults =
            new Dictionary<string, (string, string)[]>
            {
                [StructureType] = new[] { ("torre", "Torre"), ("bloque", "Bloque"), ("etapa", "Etapa"), ("piso", "Piso"), ("manzana", "Manzana") },
                [UnitType] = new[] { ("apartamento", "Apartamento"), ("casa", "Casa"), ("local", "Local"), ("oficina", "Oficina") },
                [DocumentType] = new[] { ("CC", "Cédula de ciudadanía"), ("CE", "Cédula de extranjería"), ("TI", "Tarjeta de identidad"), ("PA", "Pasaporte"), ("NIT", "NIT"), ("NA", "Sin documento") },
                [VisitReason] = new[] { ("social", "Visita social"), ("domicilio", "Domicilio"), ("servicio", "Servicio técnico"), ("otro", "Otro") },
                [VehicleType] = new[] { ("automovil", "Automóvil"), ("motocicleta", "Motocicleta"), ("bicicleta", "Bicicleta"), ("otro", "Otro") },
                [RejectionReason] = new[] { ("sin-autorizacion", "Sin autorización"), ("autorizacion-vencida", "Autorización vencida o revocada"), ("no-confirmado", "El residente no confirmó"), ("documento", "No presentó documento"), ("otro", "Otro") },
                [AccessPoint] = new[] { ("principal", "Portería principal") },
                [IncidentCategory] = new[] { ("seguridad", "Seguridad"), ("mantenimiento", "Mantenimiento"), ("convivencia", "Convivencia"), ("otro", "Otro") },
            };

        public static bool Exists(string catalog) => Defaults.ContainsKey(catalog);
    }

    public interface ICatalogService
    {
        /// <summary>Lista del conjunto si la tiene; si no, la inicial.</summary>
        Task<List<CatalogItemDto>> GetAsync(string catalog, bool onlyActive = false);

        /// <summary>Reemplaza la lista del conjunto.</summary>
        Task<List<CatalogItemDto>> ReplaceAsync(string catalog, IReadOnlyList<CatalogItemDto> items);

        /// <summary>Lanza si el código no es un valor activo del catálogo.</summary>
        Task EnsureValidAsync(string catalog, string code, string fieldName);

        Task<string> LabelAsync(string catalog, string code);
    }

    public class CatalogService : ICatalogService
    {
        private readonly ApplicationDbContext _db;
        private readonly Dictionary<string, List<CatalogItemDto>> _cache = new();

        public CatalogService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<CatalogItemDto>> GetAsync(string catalog, bool onlyActive = false)
        {
            if (!Catalogs.Exists(catalog))
            {
                throw new NotFoundException("El catálogo no existe.");
            }

            if (!_cache.TryGetValue(catalog, out var items))
            {
                items = await _db.CatalogItems.AsNoTracking()
                    .Where(c => c.Catalog == catalog)
                    .OrderBy(c => c.Order).ThenBy(c => c.Label)
                    .Select(c => new CatalogItemDto { Code = c.Code, Label = c.Label, Order = c.Order, IsActive = c.IsActive })
                    .ToListAsync();

                if (items.Count == 0)
                {
                    items = Catalogs.Defaults[catalog]
                        .Select((d, index) => new CatalogItemDto { Code = d.Code, Label = d.Label, Order = index + 1, IsActive = true })
                        .ToList();
                }
                _cache[catalog] = items;
            }

            return onlyActive ? items.Where(i => i.IsActive).ToList() : items;
        }

        public async Task<List<CatalogItemDto>> ReplaceAsync(string catalog, IReadOnlyList<CatalogItemDto> items)
        {
            if (!Catalogs.Exists(catalog))
            {
                throw new NotFoundException("El catálogo no existe.");
            }
            if (items.Count == 0 || items.All(i => !i.IsActive))
            {
                throw new BusinessRuleException("El catálogo debe conservar al menos un valor activo.");
            }

            var codes = items.Select(i => i.Code.Trim()).ToList();
            if (codes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != codes.Count)
            {
                throw new BusinessRuleException("Hay códigos repetidos en el catálogo.");
            }

            var existing = await _db.CatalogItems.Where(c => c.Catalog == catalog).ToListAsync();
            // Un código en uso no se elimina: se deja inactivo para que lo ya registrado siga legible
            foreach (var row in existing.Where(e => !codes.Contains(e.Code, StringComparer.OrdinalIgnoreCase)))
            {
                row.IsActive = false;
            }

            var order = 1;
            foreach (var item in items)
            {
                var code = item.Code.Trim();
                var row = existing.FirstOrDefault(e => string.Equals(e.Code, code, StringComparison.OrdinalIgnoreCase));
                if (row == null)
                {
                    row = new CatalogItem { Catalog = catalog, Code = code };
                    _db.CatalogItems.Add(row);
                }
                row.Label = item.Label.Trim();
                row.Order = order++;
                row.IsActive = item.IsActive;
            }

            await _db.SaveChangesAsync();
            _cache.Remove(catalog);
            return await GetAsync(catalog);
        }

        public async Task EnsureValidAsync(string catalog, string code, string fieldName)
        {
            var items = await GetAsync(catalog, onlyActive: true);
            if (!items.Any(i => string.Equals(i.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new BusinessRuleException($"El valor de \"{fieldName}\" no es válido.");
            }
        }

        public async Task<string> LabelAsync(string catalog, string code)
        {
            var items = await GetAsync(catalog);
            return items.FirstOrDefault(i => string.Equals(i.Code, code, StringComparison.OrdinalIgnoreCase))?.Label ?? code;
        }
    }
}
