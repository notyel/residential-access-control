using AccessControl.Application.Exceptions;
using AccessControl.Common.DTOs.Policy;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Policies
{
    public interface IPolicyService
    {
        /// <summary>Valor vigente en el conjunto de la sesión: el guardado o, si no hay, el por defecto.</summary>
        Task<string> GetAsync(string key);
        Task<bool> GetBoolAsync(string key);
        Task<int> GetIntAsync(string key);

        /// <summary>Catálogo completo con el valor vigente de cada política.</summary>
        Task<List<PolicyDto>> GetAllAsync();

        /// <summary>Cambia las políticas indicadas; las demás quedan como están.</summary>
        Task<List<PolicyDto>> UpdateAsync(IDictionary<string, string> values);
    }

    /// <summary>
    /// Las reglas las aplica el servidor: los servicios preguntan aquí antes de permitir una
    /// operación. La interfaz solo adapta lo que muestra.
    /// </summary>
    public class PolicyService : IPolicyService
    {
        private readonly ApplicationDbContext _db;

        // Una petición suele consultar varias políticas: se leen una sola vez
        private Dictionary<string, string>? _stored;

        public PolicyService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<string> GetAsync(string key)
        {
            var definition = ComplexPolicies.Find(key)
                ?? throw new InvalidOperationException($"La política '{key}' no existe en el catálogo.");

            var stored = await LoadAsync();
            // un valor guardado que ya no es válido (el catálogo cambió) cede ante el valor por defecto
            return stored.TryGetValue(key, out var value) && ComplexPolicies.Normalize(definition, value) is { } valid
                ? valid
                : definition.DefaultValue;
        }

        public async Task<bool> GetBoolAsync(string key) => await GetAsync(key) == "true";

        public async Task<int> GetIntAsync(string key) => int.Parse(await GetAsync(key));

        public async Task<List<PolicyDto>> GetAllAsync()
        {
            var result = new List<PolicyDto>();
            foreach (var definition in ComplexPolicies.All)
            {
                var value = await GetAsync(definition.Key);
                result.Add(new PolicyDto
                {
                    Key = definition.Key,
                    Group = definition.Group,
                    Label = definition.Label,
                    Description = definition.Description,
                    Kind = definition.Kind.ToString(),
                    Value = value,
                    DefaultValue = definition.DefaultValue,
                    IsDefault = value == definition.DefaultValue,
                    Min = definition.Kind == PolicyKind.Number ? definition.Min : null,
                    Max = definition.Kind == PolicyKind.Number ? definition.Max : null,
                    Options = definition.Options?.Select(o => new PolicyOptionDto { Value = o.Value, Label = o.Label }).ToList()
                });
            }
            return result;
        }

        public async Task<List<PolicyDto>> UpdateAsync(IDictionary<string, string> values)
        {
            var rows = await _db.ComplexPolicies.ToListAsync();

            foreach (var (key, raw) in values)
            {
                var definition = ComplexPolicies.Find(key)
                    ?? throw new BusinessRuleException($"La política '{key}' no existe.");
                var value = ComplexPolicies.Normalize(definition, raw)
                    ?? throw new BusinessRuleException($"El valor indicado para \"{definition.Label}\" no es válido.");

                var row = rows.FirstOrDefault(r => r.Key == key);
                if (value == definition.DefaultValue)
                {
                    // volver al valor por defecto es dejar de guardar una excepción
                    if (row != null) _db.ComplexPolicies.Remove(row);
                }
                else if (row == null)
                {
                    _db.ComplexPolicies.Add(new ComplexPolicy { Key = key, Value = value });
                }
                else
                {
                    row.Value = value;
                }
            }

            await _db.SaveChangesAsync();
            _stored = null;
            return await GetAllAsync();
        }

        private async Task<Dictionary<string, string>> LoadAsync()
        {
            return _stored ??= await _db.ComplexPolicies.AsNoTracking().ToDictionaryAsync(p => p.Key, p => p.Value);
        }
    }
}
