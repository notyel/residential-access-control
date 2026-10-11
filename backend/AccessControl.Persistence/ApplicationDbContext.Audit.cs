using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AccessControl.Persistence
{
    // Bitácora automática: cada alta, cambio o baja de una entidad de negocio deja una fila en
    // AuditLogs dentro de la misma transacción (si el guardado falla, la fila tampoco queda).
    public partial class ApplicationDbContext
    {
        public const string ActionCreated = "created";
        public const string ActionUpdated = "updated";
        public const string ActionDeleted = "deleted";

        // No se auditan: la propia bitácora, los enlaces de un solo uso y el control interno de permisos
        private static readonly HashSet<Type> NotAuditedTypes = new()
        {
            typeof(AuditLog), typeof(UserToken), typeof(AppliedPermissionDefault), typeof(AccessEvent)
        };

        // Ruido: cambian en cada guardado y no dicen nada que no esté ya en la fila de bitácora
        private static readonly HashSet<string> IgnoredProperties = new()
        {
            nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.UpdatedAt),
            nameof(AuditableEntity.CreatedById), nameof(AuditableEntity.UpdatedById)
        };

        private static readonly JsonSerializerOptions AuditJson = new() { WriteIndented = false };

        private List<AuditLog> BuildAuditEntries(DateTime now)
        {
            var logs = new List<AuditLog>();

            foreach (var entry in ChangeTracker.Entries().ToList())
            {
                if (NotAuditedTypes.Contains(entry.Metadata.ClrType)) continue;
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

                var changes = new Dictionary<string, object?>();
                foreach (var property in entry.Properties)
                {
                    if (IgnoredProperties.Contains(property.Metadata.Name)) continue;
                    if (property.Metadata.PropertyInfo?.IsDefined(typeof(NotAuditedAttribute), true) == true) continue;

                    switch (entry.State)
                    {
                        case EntityState.Added when property.CurrentValue != null:
                            changes[property.Metadata.Name] = new { @new = Readable(property.CurrentValue) };
                            break;
                        case EntityState.Deleted when property.OriginalValue != null:
                            changes[property.Metadata.Name] = new { old = Readable(property.OriginalValue) };
                            break;
                        case EntityState.Modified when property.IsModified && !Equals(property.OriginalValue, property.CurrentValue):
                            changes[property.Metadata.Name] = new { old = Readable(property.OriginalValue), @new = Readable(property.CurrentValue) };
                            break;
                    }
                }

                // Un guardado que no cambió ningún campo auditable no deja rastro
                if (entry.State == EntityState.Modified && changes.Count == 0) continue;

                logs.Add(new AuditLog
                {
                    ComplexId = ComplexOf(entry),
                    UserId = _currentUser?.UserId,
                    UserName = _currentUser?.UserName,
                    OccurredAt = now,
                    Action = entry.State switch
                    {
                        EntityState.Added => ActionCreated,
                        EntityState.Deleted => ActionDeleted,
                        _ => ActionUpdated
                    },
                    EntityType = entry.Metadata.ClrType.Name,
                    EntityId = KeyOf(entry),
                    Changes = JsonSerializer.Serialize(changes, AuditJson),
                    IpAddress = _currentUser?.IpAddress
                });
            }

            return logs;
        }

        private static object? Readable(object? value) => value is Enum ? value.ToString() : value;

        private Guid? ComplexOf(EntityEntry entry)
        {
            return entry.Entity switch
            {
                IComplexScoped scoped => scoped.ComplexId,
                User user => user.ComplexId,
                // crear o cambiar un conjunto es una acción de plataforma
                Complex => null,
                _ => CurrentComplexId
            };
        }

        private static string KeyOf(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            return key == null
                ? string.Empty
                : string.Join("/", key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString()));
        }
    }
}
