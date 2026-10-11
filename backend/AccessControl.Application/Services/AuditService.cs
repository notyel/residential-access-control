using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs.Audit;
using AccessControl.Common.DTOs.Common;
using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IDateTimeService _dateTime;

        public AuditService(ApplicationDbContext db, ICurrentUser currentUser, IDateTimeService dateTime)
        {
            _db = db;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public void Record(string action, Guid? complexId = null, Guid? userId = null, string? userName = null,
            string? entityType = null, string? entityId = null, object? details = null)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                ComplexId = complexId ?? _currentUser.ComplexId,
                UserId = userId ?? _currentUser.UserId,
                UserName = userName ?? _currentUser.UserName,
                OccurredAt = _dateTime.UtcNow,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Changes = details == null ? null : JsonSerializer.Serialize(details),
                IpAddress = _currentUser.IpAddress
            });
        }

        public async Task<PaginatedResultDto<AuditLogDto>> SearchAsync(AuditFilterDto filter)
        {
            var query = _db.AuditLogs.AsNoTracking();

            if (filter.StartDate.HasValue)
            {
                var start = _dateTime.ToUtc(filter.StartDate.Value);
                query = query.Where(a => a.OccurredAt >= start);
            }
            if (filter.EndDate.HasValue)
            {
                var end = _dateTime.ToUtc(filter.EndDate.Value);
                query = query.Where(a => a.OccurredAt <= end);
            }
            if (filter.UserId.HasValue)
            {
                query = query.Where(a => a.UserId == filter.UserId.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter.EntityType))
            {
                query = query.Where(a => a.EntityType == filter.EntityType.Trim());
            }
            if (!string.IsNullOrWhiteSpace(filter.Action))
            {
                query = query.Where(a => a.Action == filter.Action.Trim());
            }
            if (!string.IsNullOrWhiteSpace(filter.EntityId))
            {
                query = query.Where(a => a.EntityId == filter.EntityId.Trim());
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.OccurredAt)
                .ThenByDescending(a => a.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    OccurredAt = a.OccurredAt,
                    UserId = a.UserId,
                    UserName = a.UserName,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    Changes = a.Changes,
                    IpAddress = a.IpAddress
                })
                .ToListAsync();

            return new PaginatedResultDto<AuditLogDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }
    }
}
