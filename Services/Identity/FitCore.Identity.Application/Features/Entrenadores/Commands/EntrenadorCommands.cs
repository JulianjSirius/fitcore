using FitCore.Identity.Application.Features.Entrenadores.Queries;
using MediatR;

namespace FitCore.Identity.Application.Features.Entrenadores.Commands;

public sealed record CreateEntrenadorCommand(
    string Nombre,
    string Especialidad,
    string Horario,
    string Email,
    string Contrasena,
    int Telefono) : IRequest<EntrenadorResult>;

public sealed record UpdateEntrenadorCommand(
    Guid Id,
    string Nombre,
    string Especialidad,
    string Horario,
    string Email,
    string Contrasena,
    int Telefono) : IRequest<bool>;
