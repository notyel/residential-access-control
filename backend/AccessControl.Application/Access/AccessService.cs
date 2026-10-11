using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Parking;
using AccessControl.Application.Policies;
using AccessControl.Application.Security;
using AccessControl.Application.Services;
using AccessControl.Application.Units;
using AccessControl.Application.Vehicles;
using AccessControl.Common.DTOs.Access;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Person;
using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AccessControl.Application.Access
{
    public interface IAccessService
    {
        /// <summary>Lo que portería necesita al buscar por documento, nombre o unidad.</summary>
        Task<LookupResultDto> LookupAsync(string? term);

        /// <summary>Registra el ingreso de un visitante: crea la estancia y su evento de entrada.</summary>
        Task<VisitDto> CheckInAsync(CheckInDto dto);

        Task<VisitDto> CheckOutAsync(Guid visitId, CheckOutDto dto);

        /// <summary>Deja constancia de un ingreso rechazado. No crea estancia.</summary>
        Task<AccessEventDto> RejectAsync(RejectDto dto);

        /// <summary>Entrada o salida de un propietario u ocupante con vínculo vigente.</summary>
        Task<AccessEventDto> RegisterResidentAsync(ResidentMovementDto dto);

        /// <summary>Salida de un vehículo que quedó dentro (por ejemplo, el visitante salió antes sin él).</summary>
        Task<AccessEventDto> RegisterVehicleExitAsync(Guid vehicleId, string? notes);

        /// <summary>Anula un evento registrado por error: agrega un evento de anulación; el original queda.</summary>
        Task<AccessEventDto> VoidAsync(Guid eventId, string reason);

        Task<PaginatedResultDto<VisitDto>> GetVisitsAsync(VisitFilterDto filter, IReadOnlyCollection<Guid>? unitScope = null);
        Task<VisitDto> GetVisitAsync(Guid visitId, IReadOnlyCollection<Guid>? unitScope = null);
    }

    public class AccessService : IAccessService
    {
        public const int MinExceptionReasonLength = 10;

        private readonly ApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IDateTimeService _dateTime;
        private readonly IPolicyService _policies;
        private readonly ICatalogService _catalogs;
        private readonly IAuthorizationService _authorizations;
        private readonly IVehicleService _vehicles;
        private readonly IParkingService _parking;

        public AccessService(
            ApplicationDbContext db,
            ICurrentUser currentUser,
            IDateTimeService dateTime,
            IPolicyService policies,
            ICatalogService catalogs,
            IAuthorizationService authorizations,
            IVehicleService vehicles,
            IParkingService parking)
        {
            _vehicles = vehicles;
            _parking = parking;
            _db = db;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _policies = policies;
            _catalogs = catalogs;
            _authorizations = authorizations;
        }

        // ---------- Búsqueda de portería ----------

        public async Task<LookupResultDto> LookupAsync(string? term)
        {
            var result = new LookupResultDto();
            var text = (term ?? string.Empty).Trim();
            if (text.Length < 2) return result;

            var today = _dateTime.Today;
            var pattern = $"%{text}%";

            // autorizaciones que aún podrían usarse (hoy o después), por visitante o por unidad
            var authorizations = await _db.VisitAuthorizations
                .Include(a => a.Unit).Include(a => a.AuthorizedBy)
                .Where(a => a.EndDate >= today && a.Status != AuthorizationStatus.Consumed
                    && (EF.Functions.ILike(a.VisitorName, pattern)
                        || (a.VisitorDocument != null && a.VisitorDocument == text)
                        || EF.Functions.ILike(a.Unit.Label, pattern)))
                .OrderBy(a => a.StartDate)
                .Take(20)
                .ToListAsync();
            result.Authorizations = await _authorizations.ToDtosAsync(authorizations);

            var owners = await _db.UnitOwnerships
                .Where(o => o.StartDate <= today && (o.EndDate == null || o.EndDate >= today)
                    && (EF.Functions.ILike(o.Person.FirstName + " " + o.Person.LastName, pattern)
                        || o.Person.DocumentNumber == text || EF.Functions.ILike(o.Unit.Label, pattern)))
                .Select(o => new ResidentMatchDto
                {
                    PersonId = o.PersonId, Name = o.Person.FirstName + " " + o.Person.LastName,
                    UnitId = o.UnitId, UnitLabel = o.Unit.Label, Kind = PersonKind.Owner
                })
                .Take(15).ToListAsync();
            var occupants = await _db.Occupancies
                .Where(o => o.Status == OccupancyStatus.Active && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today)
                    && (EF.Functions.ILike(o.Person.FirstName + " " + o.Person.LastName, pattern)
                        || o.Person.DocumentNumber == text || EF.Functions.ILike(o.Unit.Label, pattern)))
                .Select(o => new ResidentMatchDto
                {
                    PersonId = o.PersonId, Name = o.Person.FirstName + " " + o.Person.LastName,
                    UnitId = o.UnitId, UnitLabel = o.Unit.Label, Kind = PersonKind.Resident
                })
                .Take(15).ToListAsync();
            // quien es propietario y además vive allí aparece una vez, como propietario
            result.Residents = owners
                .Concat(occupants.Where(r => !owners.Any(o => o.PersonId == r.PersonId && o.UnitId == r.UnitId)))
                .OrderBy(r => r.UnitLabel).ThenBy(r => r.Name)
                .ToList();

            var people = await _db.Persons.Where(p => p.DocumentNumber == text).Take(5).ToListAsync();
            result.People = people.Select(PeopleService.ToDto).ToList();

            return result;
        }

        // ---------- Ingreso ----------

        public async Task<VisitDto> CheckInAsync(CheckInDto dto)
        {
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == dto.UnitId && u.IsActive)
                ?? throw new BusinessRuleException("La unidad indicada no existe o está inactiva.");

            var reason = string.IsNullOrWhiteSpace(dto.Reason) ? "otro" : dto.Reason.Trim();
            await _catalogs.EnsureValidAsync(Catalogs.VisitReason, reason, "Motivo de la visita");
            if (!string.IsNullOrWhiteSpace(dto.AccessPoint))
            {
                await _catalogs.EnsureValidAsync(Catalogs.AccessPoint, dto.AccessPoint, "Punto de acceso");
            }

            // ---- cómo se verifica el ingreso: el servidor decide con el estado de este instante ----
            VisitAuthorization? authorization = null;
            VerificationResult verification;
            string? exceptionReason = null;
            Guid? confirmedBy = null;

            if (dto.IsException)
            {
                if (!_currentUser.HasPermission(Permissions.AccessException))
                {
                    throw new ForbiddenException("No tiene permiso para registrar accesos excepcionales.");
                }
                exceptionReason = (dto.ExceptionReason ?? string.Empty).Trim();
                if (exceptionReason.Length < MinExceptionReasonLength)
                {
                    throw new BusinessRuleException($"Un acceso excepcional exige un motivo de al menos {MinExceptionReasonLength} caracteres.");
                }
                if (dto.ApprovedById.HasValue && !await _db.Users.AnyAsync(u => u.Id == dto.ApprovedById.Value))
                {
                    throw new BusinessRuleException("El usuario que autorizó el acceso excepcional no existe.");
                }
                verification = VerificationResult.Exception;
            }
            else if (dto.AuthorizationId.HasValue)
            {
                var (found, validity) = await _authorizations.EvaluateAsync(dto.AuthorizationId.Value);
                if (found.UnitId != unit.Id)
                {
                    throw new BusinessRuleException("La autorización es para otra unidad.");
                }
                if (!validity.IsValid)
                {
                    // vencida, revocada o no aplicable ahora: no permite un ingreso normal
                    throw new ConflictException(validity.Reason!);
                }
                authorization = found;
                verification = VerificationResult.PriorAuthorization;
            }
            else
            {
                var policy = await _policies.GetAsync(ComplexPolicies.VisitsWithoutAuthorization);
                switch (policy)
                {
                    case ComplexPolicies.NotAllowed:
                        throw new ConflictException("Este conjunto no admite visitas sin autorización previa. Rechace el ingreso o regístrelo como acceso excepcional.");

                    case ComplexPolicies.ResidentConfirmation:
                        if (!dto.ConfirmedByPersonId.HasValue || string.IsNullOrWhiteSpace(dto.ConfirmationChannel))
                        {
                            throw new BusinessRuleException("Indique qué ocupante de la unidad confirmó la visita y por qué medio.");
                        }
                        if (!await HasCurrentLinkAsync(dto.ConfirmedByPersonId.Value, unit.Id))
                        {
                            throw new BusinessRuleException("Quien confirma debe ser propietario u ocupante vigente de la unidad de destino.");
                        }
                        confirmedBy = dto.ConfirmedByPersonId;
                        verification = VerificationResult.ResidentConfirmation;
                        break;

                    default:
                        verification = VerificationResult.GuardApproval;
                        break;
                }
            }

            var person = await ResolveVisitorAsync(dto, authorization);
            // el vehículo de la visita queda registrado (o se reutiliza) y enlazado a la visita y a su entrada
            var plate = NormalizePlate(dto.VehiclePlate);
            var vehicle = plate == null ? null : await _vehicles.ResolveForVisitAsync(plate, dto.VehicleType, dto.VehicleBrand, dto.VehicleColor);
            var operatorId = _currentUser.UserId ?? throw new ForbiddenException("Sesión no válida.");
            var now = _dateTime.UtcNow;

            var visit = new Visit
            {
                Person = person,
                UnitId = unit.Id,
                Reason = reason,
                ReasonNote = Blank.ToNull(dto.ReasonNote),
                Vehicle = vehicle,
                Verification = verification,
                AuthorizationId = authorization?.Id,
                RegisteredById = operatorId
            };
            var entry = new AccessEvent
            {
                Type = AccessEventType.Entry,
                OccurredAt = now,
                Person = person,
                PersonName = $"{person.FirstName} {person.LastName}",
                PersonDocument = person.DocumentType == "NA" ? null : $"{person.DocumentType} {person.DocumentNumber}",
                PersonKind = PersonKind.Visitor,
                UnitId = unit.Id,
                VisitId = visit.Id,
                Vehicle = vehicle,
                VehiclePlate = vehicle?.Plate,
                OperatorId = operatorId,
                AccessPoint = Blank.ToNull(dto.AccessPoint),
                Notes = Blank.ToNull(dto.Notes),
                Verification = verification,
                Reason = exceptionReason,
                // si el operador mismo lo autorizó, queda él como responsable
                ApprovedById = verification == VerificationResult.Exception ? dto.ApprovedById ?? operatorId
                    : verification == VerificationResult.GuardApproval ? operatorId : null,
                ConfirmedByPersonId = confirmedBy,
                ConfirmationChannel = confirmedBy.HasValue ? dto.ConfirmationChannel!.Trim() : null
            };
            visit.EntryEvent = entry;

            if (authorization is { Kind: AuthorizationKind.Single })
            {
                authorization.Status = AuthorizationStatus.Consumed;
                authorization.ConsumedAt = now;
            }

            if (dto.ParkingSpaceId.HasValue)
            {
                if (vehicle == null)
                {
                    throw new BusinessRuleException("Para asignar un parqueadero indique la placa del vehículo.");
                }
                await _parking.AttachOccupancyAsync(dto.ParkingSpaceId.Value, vehicle, unit.Id, visit.Id, dto.IsException, dto.ExceptionReason, now);
            }

            // estancia, entrada y parqueadero se guardan juntos: no puede quedar uno sin los otros
            _db.Visits.Add(visit);
            await SaveWithParkingAsync();

            return await GetVisitAsync(visit.Id);
        }

        private async Task<Person> ResolveVisitorAsync(CheckInDto dto, VisitAuthorization? authorization)
        {
            var personId = dto.PersonId ?? authorization?.VisitorPersonId;
            if (personId.HasValue)
            {
                return await _db.Persons.FirstOrDefaultAsync(p => p.Id == personId.Value)
                    ?? throw new BusinessRuleException("La persona indicada no existe.");
            }

            if (dto.NewPerson == null)
            {
                throw new BusinessRuleException("Indique una persona existente o los datos de una nueva.");
            }

            var documentRequired = await _policies.GetBoolAsync(ComplexPolicies.VisitorDocumentRequired);
            var documentNumber = (dto.NewPerson.DocumentNumber ?? string.Empty).Trim();
            var documentType = (dto.NewPerson.DocumentType ?? string.Empty).Trim().ToUpperInvariant();

            if (documentNumber.Length == 0 || documentType is "" or "NA")
            {
                if (documentRequired)
                {
                    throw new BusinessRuleException("El documento del visitante es obligatorio en este conjunto.");
                }
                // sin documento: se le asigna un identificador interno para poder registrarlo
                documentType = "NA";
                documentNumber = "SD-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            }
            else
            {
                await _catalogs.EnsureValidAsync(Catalogs.DocumentType, documentType, "Tipo de documento");
                if (await _db.Persons.AnyAsync(p => p.DocumentNumber == documentNumber))
                {
                    throw new ConflictException("Ya existe una persona con ese número de documento.");
                }
            }

            var person = new Person
            {
                FirstName = dto.NewPerson.FirstName.Trim(),
                LastName = dto.NewPerson.LastName.Trim(),
                DocumentType = documentType,
                DocumentNumber = documentNumber,
                Phone = Blank.ToNull(dto.NewPerson.Phone),
                Email = Blank.ToNull(dto.NewPerson.Email)
            };
            _db.Persons.Add(person);
            return person;
        }

        // ---------- Salida ----------

        public async Task<VisitDto> CheckOutAsync(Guid visitId, CheckOutDto dto)
        {
            var visit = await _db.Visits.Include(v => v.Person).Include(v => v.Vehicle).FirstOrDefaultAsync(v => v.Id == visitId)
                ?? throw new NotFoundException("La visita no existe.");
            if (visit.VoidEventId.HasValue)
            {
                throw new ConflictException("La entrada de esta visita fue anulada.");
            }
            if (dto.Administrative && string.IsNullOrWhiteSpace(dto.Reason))
            {
                throw new BusinessRuleException("El cierre administrativo de una visita exige un motivo.");
            }

            var operatorId = _currentUser.UserId ?? throw new ForbiddenException("Sesión no válida.");
            var exit = new AccessEvent
            {
                Type = AccessEventType.Exit,
                OccurredAt = _dateTime.UtcNow,
                PersonId = visit.PersonId,
                PersonName = $"{visit.Person.FirstName} {visit.Person.LastName}",
                PersonDocument = visit.Person.DocumentType == "NA" ? null : $"{visit.Person.DocumentType} {visit.Person.DocumentNumber}",
                PersonKind = PersonKind.Visitor,
                UnitId = visit.UnitId,
                VisitId = visit.Id,
                // si sale sin su vehículo, la salida no lo incluye y el vehículo sigue contando como dentro
                VehicleId = dto.WithoutVehicle ? null : visit.VehicleId,
                VehiclePlate = dto.WithoutVehicle ? null : visit.Vehicle?.Plate,
                OperatorId = operatorId,
                Notes = Blank.ToNull(dto.Notes),
                IsAdministrative = dto.Administrative,
                Reason = dto.Administrative ? dto.Reason!.Trim() : null
            };
            _db.AccessEvents.Add(exit);
            await _db.SaveChangesAsync();

            // Enlace condicional: si dos porteros marcan la salida a la vez, solo uno la registra
            var linked = await _db.Visits
                .Where(v => v.Id == visitId && v.ExitEventId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.ExitEventId, exit.Id).SetProperty(v => v.UpdatedAt, exit.OccurredAt));
            if (linked == 0)
            {
                // la salida ya existía: el evento recién creado sobra y se anula, sin borrarlo
                _db.AccessEvents.Add(VoidEvent(exit, operatorId, "Salida duplicada: la visita ya tenía salida registrada"));
                await _db.SaveChangesAsync();
                throw new ConflictException("La visita ya tiene salida registrada.");
            }

            if (exit.VehicleId.HasValue)
            {
                // la salida del vehículo libera su parqueadero: nadie tiene que desocuparlo a mano
                await _parking.ReleaseVehicleAsync(exit.VehicleId.Value, exit.OccurredAt);
                await _db.SaveChangesAsync();
            }

            return await GetVisitAsync(visitId);
        }

        // ---------- Rechazos, residentes y anulaciones ----------

        public async Task<AccessEventDto> RejectAsync(RejectDto dto)
        {
            await _catalogs.EnsureValidAsync(Catalogs.RejectionReason, dto.ReasonCode, "Motivo del rechazo");
            if (dto.UnitId.HasValue && !await _db.Units.AnyAsync(u => u.Id == dto.UnitId.Value))
            {
                throw new BusinessRuleException("La unidad indicada no existe.");
            }
            if (dto.PersonId.HasValue && !await _db.Persons.AnyAsync(p => p.Id == dto.PersonId.Value))
            {
                throw new BusinessRuleException("La persona indicada no existe.");
            }

            var rejection = new AccessEvent
            {
                Type = AccessEventType.Rejection,
                OccurredAt = _dateTime.UtcNow,
                PersonId = dto.PersonId,
                PersonName = dto.PersonName.Trim(),
                PersonDocument = Blank.ToNull(dto.PersonDocument),
                PersonKind = PersonKind.Visitor,
                UnitId = dto.UnitId,
                VehiclePlate = NormalizePlate(dto.VehiclePlate),
                OperatorId = _currentUser.UserId ?? throw new ForbiddenException("Sesión no válida."),
                Reason = await _catalogs.LabelAsync(Catalogs.RejectionReason, dto.ReasonCode),
                Notes = Blank.ToNull(dto.Notes)
            };
            _db.AccessEvents.Add(rejection);
            await _db.SaveChangesAsync();

            return await GetEventAsync(rejection.Id);
        }

        public async Task<AccessEventDto> RegisterResidentAsync(ResidentMovementDto dto)
        {
            if (dto.Type is not (AccessEventType.Entry or AccessEventType.Exit))
            {
                throw new BusinessRuleException("Indique si es una entrada o una salida.");
            }

            var person = await _db.Persons.FirstOrDefaultAsync(p => p.Id == dto.PersonId)
                ?? throw new BusinessRuleException("La persona indicada no existe.");
            var today = _dateTime.Today;
            var isOwner = await _db.UnitOwnerships.AnyAsync(o => o.UnitId == dto.UnitId && o.PersonId == person.Id
                && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today));
            var isOccupant = await _db.Occupancies.AnyAsync(o => o.UnitId == dto.UnitId && o.PersonId == person.Id
                && o.Status == OccupancyStatus.Active && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today));

            if (!isOwner && !isOccupant)
            {
                // sin vínculo vigente no entra como residente: es un visitante o un rechazo
                throw new ConflictException("Esa persona no tiene un vínculo vigente con la unidad.");
            }

            var operatorId = _currentUser.UserId ?? throw new ForbiddenException("Sesión no válida.");
            Vehicle? vehicle = null;
            string? exceptionReason = null;
            var plate = NormalizePlate(dto.VehiclePlate);
            if (dto.VehicleId.HasValue || plate != null)
            {
                vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => dto.VehicleId.HasValue ? v.Id == dto.VehicleId.Value : v.Plate == plate)
                    ?? throw new BusinessRuleException("El vehículo no está registrado.");

                // la salida siempre se registra; la entrada exige autorización vigente en esa unidad
                if (dto.Type == AccessEventType.Entry && !await _vehicles.IsAuthorizedAsync(vehicle.Id, dto.UnitId!.Value))
                {
                    if (!dto.IsException)
                    {
                        throw new ConflictException("El vehículo no tiene autorización vigente en esa unidad. Rechace su ingreso o regístrelo como acceso excepcional.");
                    }
                    if (!_currentUser.HasPermission(Permissions.AccessException))
                    {
                        throw new ForbiddenException("No tiene permiso para registrar accesos excepcionales.");
                    }
                    exceptionReason = (dto.ExceptionReason ?? string.Empty).Trim();
                    if (exceptionReason.Length < MinExceptionReasonLength)
                    {
                        throw new BusinessRuleException($"Un acceso excepcional exige un motivo de al menos {MinExceptionReasonLength} caracteres.");
                    }
                }
            }

            var movement = new AccessEvent
            {
                Type = dto.Type,
                OccurredAt = _dateTime.UtcNow,
                PersonId = person.Id,
                PersonName = $"{person.FirstName} {person.LastName}",
                PersonDocument = person.DocumentType == "NA" ? null : $"{person.DocumentType} {person.DocumentNumber}",
                PersonKind = isOccupant ? PersonKind.Resident : PersonKind.Owner,
                UnitId = dto.UnitId,
                VehicleId = vehicle?.Id,
                VehiclePlate = vehicle?.Plate,
                OperatorId = operatorId,
                Notes = Blank.ToNull(dto.Notes),
                Verification = exceptionReason != null ? VerificationResult.Exception : null,
                Reason = exceptionReason,
                ApprovedById = exceptionReason != null ? operatorId : null
            };
            _db.AccessEvents.Add(movement);
            if (vehicle != null && dto.Type == AccessEventType.Exit)
            {
                await _parking.ReleaseVehicleAsync(vehicle.Id, movement.OccurredAt);
            }
            else if (dto.ParkingSpaceId.HasValue)
            {
                if (vehicle == null)
                {
                    throw new BusinessRuleException("Para ocupar un parqueadero indique el vehículo.");
                }
                await _parking.AttachOccupancyAsync(dto.ParkingSpaceId.Value, vehicle, dto.UnitId, null, dto.IsException, dto.ExceptionReason, movement.OccurredAt);
            }
            await SaveWithParkingAsync();

            return await GetEventAsync(movement.Id);
        }

        public async Task<AccessEventDto> RegisterVehicleExitAsync(Guid vehicleId, string? notes)
        {
            // la entrada abierta dice con quién y a qué unidad entró
            var entry = await VehicleService.OpenEntries(_db).FirstOrDefaultAsync(e => e.VehicleId == vehicleId)
                ?? throw new ConflictException("El vehículo no figura dentro del conjunto.");

            var exit = new AccessEvent
            {
                Type = AccessEventType.Exit,
                OccurredAt = _dateTime.UtcNow,
                PersonId = entry.PersonId,
                PersonName = entry.PersonName,
                PersonDocument = entry.PersonDocument,
                PersonKind = entry.PersonKind,
                UnitId = entry.UnitId,
                VehicleId = vehicleId,
                VehiclePlate = entry.VehiclePlate,
                OperatorId = _currentUser.UserId ?? throw new ForbiddenException("Sesión no válida."),
                Notes = Blank.ToNull(notes)
            };
            _db.AccessEvents.Add(exit);
            await _parking.ReleaseVehicleAsync(vehicleId, exit.OccurredAt);
            await _db.SaveChangesAsync();

            return await GetEventAsync(exit.Id);
        }

        /// <summary>La base decide si el parqueadero sigue libre: dos ingresos simultáneos al mismo espacio no pueden ganar ambos.</summary>
        private async Task SaveWithParkingAsync()
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.ExclusionViolation })
            {
                throw new ConflictException(ParkingService.OverlapMessage);
            }
        }

        public async Task<AccessEventDto> VoidAsync(Guid eventId, string reason)
        {
            var original = await _db.AccessEvents.FirstOrDefaultAsync(e => e.Id == eventId)
                ?? throw new NotFoundException("El evento no existe.");
            if (original.Type == AccessEventType.Void)
            {
                throw new ConflictException("Una anulación no se anula.");
            }
            if (await _db.AccessEvents.AnyAsync(e => e.VoidsEventId == eventId))
            {
                throw new ConflictException("El evento ya estaba anulado.");
            }

            var operatorId = _currentUser.UserId ?? throw new ForbiddenException("Sesión no válida.");
            var voiding = VoidEvent(original, operatorId, reason.Trim());
            _db.AccessEvents.Add(voiding);

            if (original.VisitId.HasValue)
            {
                var visit = await _db.Visits.FirstAsync(v => v.Id == original.VisitId.Value);
                if (original.Type == AccessEventType.Entry)
                {
                    // la estancia nunca ocurrió: deja de contar como presente
                    visit.VoidEventId = voiding.Id;
                }
                else if (original.Type == AccessEventType.Exit && visit.ExitEventId == original.Id)
                {
                    // la salida fue un error: la visita vuelve a estar dentro
                    visit.ExitEventId = null;
                }
            }

            if (original is { Type: AccessEventType.Entry, VehicleId: not null })
            {
                // la entrada no ocurrió: el parqueadero que se le había dado queda libre
                await _parking.ReleaseVehicleAsync(original.VehicleId.Value, voiding.OccurredAt, cancelled: true);
            }

            await _db.SaveChangesAsync();
            return await GetEventAsync(voiding.Id);
        }

        private AccessEvent VoidEvent(AccessEvent original, Guid operatorId, string reason) => new()
        {
            Type = AccessEventType.Void,
            OccurredAt = _dateTime.UtcNow,
            PersonId = original.PersonId,
            PersonName = original.PersonName,
            PersonDocument = original.PersonDocument,
            PersonKind = original.PersonKind,
            UnitId = original.UnitId,
            VisitId = original.VisitId,
            VehiclePlate = original.VehiclePlate,
            OperatorId = operatorId,
            Reason = reason,
            VoidsEventId = original.Id
        };

        // ---------- Consultas ----------

        public async Task<PaginatedResultDto<VisitDto>> GetVisitsAsync(VisitFilterDto filter, IReadOnlyCollection<Guid>? unitScope = null)
        {
            var query = _db.Visits.AsQueryable();

            if (unitScope != null)
            {
                query = query.Where(v => unitScope.Contains(v.UnitId));
            }
            if (filter.StartDate.HasValue)
            {
                var startUtc = _dateTime.ToUtc(filter.StartDate.Value);
                query = query.Where(v => v.EntryEvent.OccurredAt >= startUtc);
            }
            if (filter.EndDate.HasValue)
            {
                var endUtc = _dateTime.ToUtc(filter.EndDate.Value);
                query = query.Where(v => v.EntryEvent.OccurredAt <= endUtc);
            }
            if (filter.Status == VisitStatus.Inside)
            {
                // presente = con entrada, sin salida y sin anular; se calcula, no hay contador
                query = query.Where(v => v.ExitEventId == null && v.VoidEventId == null);
            }
            else if (filter.Status == VisitStatus.CheckedOut)
            {
                query = query.Where(v => v.ExitEventId != null);
            }
            if (filter.UnitId.HasValue)
            {
                query = query.Where(v => v.UnitId == filter.UnitId.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var pattern = $"%{filter.Search.Trim()}%";
                query = query.Where(v => EF.Functions.ILike(v.Person.FirstName + " " + v.Person.LastName, pattern)
                    || EF.Functions.ILike(v.Person.DocumentNumber, pattern));
            }
            if (!string.IsNullOrWhiteSpace(filter.Plate))
            {
                var plate = NormalizePlate(filter.Plate);
                query = query.Where(v => v.Vehicle != null && v.Vehicle.Plate == plate);
            }
            if (filter.Verification.HasValue)
            {
                query = query.Where(v => v.Verification == filter.Verification.Value);
            }
            if (filter.OperatorId.HasValue)
            {
                query = query.Where(v => v.RegisteredById == filter.OperatorId.Value);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(v => v.EntryEvent.OccurredAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(VisitToDto)
                .ToListAsync();
            await MarkForReviewAsync(items);

            return new PaginatedResultDto<VisitDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<VisitDto> GetVisitAsync(Guid visitId, IReadOnlyCollection<Guid>? unitScope = null)
        {
            var query = _db.Visits.Where(v => v.Id == visitId);
            if (unitScope != null)
            {
                query = query.Where(v => unitScope.Contains(v.UnitId));
            }

            // Una visita ajena responde igual que una inexistente
            var visit = await query.Select(VisitToDto).FirstOrDefaultAsync()
                ?? throw new NotFoundException("La visita no existe.");
            await MarkForReviewAsync(new[] { visit });
            return visit;
        }

        private async Task MarkForReviewAsync(IReadOnlyCollection<VisitDto> visits)
        {
            // parqueadero que ocupa el vehículo de cada visita
            var withVehicle = visits.Where(v => v.VehicleId.HasValue).Select(v => v.Id).ToList();
            if (withVehicle.Count > 0)
            {
                var parked = await _db.ParkingAssignments
                    .Where(a => a.VisitId != null && withVehicle.Contains(a.VisitId.Value)
                        && a.Class == ParkingAssignmentClass.Occupancy && a.Status == ParkingAssignmentStatus.Active)
                    .Select(a => new { a.VisitId, a.Space.Code })
                    .ToListAsync();
                foreach (var visit in visits)
                {
                    visit.ParkingSpaceCode = parked.FirstOrDefault(p => p.VisitId == visit.Id)?.Code;
                }
            }

            // visitante que ya salió pero dejó el vehículo: se avisa en la visita
            var left = visits.Where(v => v.VehicleId.HasValue && v.ExitAt != null).ToList();
            if (left.Count > 0)
            {
                var vehicleIds = left.Select(v => v.VehicleId!.Value).ToList();
                var inside = await VehicleService.OpenEntries(_db)
                    .Where(e => vehicleIds.Contains(e.VehicleId!.Value))
                    .Select(e => new { e.VehicleId, e.VisitId })
                    .ToListAsync();
                foreach (var visit in left)
                {
                    visit.VehicleStillInside = inside.Any(e => e.VehicleId == visit.VehicleId && e.VisitId == visit.Id);
                }
            }

            if (visits.All(v => v.ExitAt != null || v.IsVoided)) return;

            var hours = await _policies.GetIntAsync(ComplexPolicies.VisitReviewAfterHours);
            var limit = _dateTime.UtcNow.AddHours(-hours);
            foreach (var visit in visits)
            {
                visit.NeedsReview = visit.ExitAt == null && !visit.IsVoided && visit.EntryAt < limit;
            }
        }

        private async Task<AccessEventDto> GetEventAsync(Guid id)
        {
            return await _db.AccessEvents.Where(e => e.Id == id).Select(EventToDto(_db)).FirstAsync();
        }

        private async Task<bool> HasCurrentLinkAsync(Guid personId, Guid unitId)
        {
            var today = _dateTime.Today;
            return await _db.UnitOwnerships.AnyAsync(o => o.UnitId == unitId && o.PersonId == personId
                       && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                   || await _db.Occupancies.AnyAsync(o => o.UnitId == unitId && o.PersonId == personId
                       && o.Status == OccupancyStatus.Active && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today));
        }

        public static string? NormalizePlate(string? plate)
        {
            if (string.IsNullOrWhiteSpace(plate)) return null;
            // misma placa escrita "abc-123" o "ABC 123" debe compararse igual
            var normalized = new string(plate.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            return normalized.Length == 0 ? null : normalized;
        }

        internal static readonly Expression<Func<Visit, VisitDto>> VisitToDto = v => new VisitDto
        {
            Id = v.Id,
            Person = new PersonDto
            {
                Id = v.Person.Id,
                FirstName = v.Person.FirstName,
                LastName = v.Person.LastName,
                DocumentType = v.Person.DocumentType,
                DocumentNumber = v.Person.DocumentNumber,
                Phone = v.Person.Phone,
                Email = v.Person.Email
            },
            UnitId = v.UnitId,
            UnitLabel = v.Unit.Label,
            Reason = v.Reason,
            ReasonNote = v.ReasonNote,
            VehicleId = v.VehicleId,
            VehiclePlate = v.Vehicle == null ? null : v.Vehicle.Plate,
            Verification = v.Verification,
            AuthorizationId = v.AuthorizationId,
            AuthorizedByName = v.Authorization != null
                ? v.Authorization.AuthorizedBy.FirstName + " " + v.Authorization.AuthorizedBy.LastName
                : null,
            ExceptionReason = v.Verification == VerificationResult.Exception ? v.EntryEvent.Reason : null,
            EntryEventId = v.EntryEventId,
            EntryAt = v.EntryEvent.OccurredAt,
            EntryOperator = v.EntryEvent.Operator.FirstName + " " + v.EntryEvent.Operator.LastName,
            ExitEventId = v.ExitEventId,
            ExitAt = v.ExitEvent == null ? null : v.ExitEvent.OccurredAt,
            ExitOperator = v.ExitEvent == null ? null : v.ExitEvent.Operator.FirstName + " " + v.ExitEvent.Operator.LastName,
            ExitIsAdministrative = v.ExitEvent != null && v.ExitEvent.IsAdministrative,
            IsVoided = v.VoidEventId != null
        };

        internal static Expression<Func<AccessEvent, AccessEventDto>> EventToDto(ApplicationDbContext db) => e => new AccessEventDto
        {
            Id = e.Id,
            Type = e.Type,
            OccurredAt = e.OccurredAt,
            PersonId = e.PersonId,
            PersonName = e.PersonName,
            PersonDocument = e.PersonDocument,
            PersonKind = e.PersonKind,
            UnitId = e.UnitId,
            UnitLabel = e.Unit == null ? null : e.Unit.Label,
            VisitId = e.VisitId,
            VehicleId = e.VehicleId,
            VehiclePlate = e.VehiclePlate,
            OperatorId = e.OperatorId,
            OperatorName = e.Operator.FirstName + " " + e.Operator.LastName,
            AccessPoint = e.AccessPoint,
            Notes = e.Notes,
            Verification = e.Verification,
            Reason = e.Reason,
            IsAdministrative = e.IsAdministrative,
            VoidsEventId = e.VoidsEventId,
            IsVoided = db.AccessEvents.Any(x => x.VoidsEventId == e.Id)
        };
    }
}
