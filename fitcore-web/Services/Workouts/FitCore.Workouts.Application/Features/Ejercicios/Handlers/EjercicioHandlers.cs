using FitCore.Workouts.Application.Abstractions;
using FitCore.Workouts.Application.Features.Ejercicios.Commands;
using FitCore.Workouts.Application.Features.Ejercicios.Queries;
using FitCore.Workouts.Domain.Entidades;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Workouts.Application.Features.Ejercicios.Handlers;

public sealed class GetEjerciciosQueryHandler : IRequestHandler<GetEjerciciosQuery, IReadOnlyList<EjercicioResult>>
{
    private readonly IWorkoutsDbContext context;

    public GetEjerciciosQueryHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<IReadOnlyList<EjercicioResult>> Handle(GetEjerciciosQuery request, CancellationToken cancellationToken)
        => await context.Ejercicios.AsNoTracking()
            .Select(item => new EjercicioResult(item.Id, item.Nombre, item.GrupoMuscular, item.DescripcionOrientativa))
            .ToListAsync(cancellationToken);
}

public sealed class GetEjercicioByIdQueryHandler : IRequestHandler<GetEjercicioByIdQuery, EjercicioResult?>
{
    private readonly IWorkoutsDbContext context;

    public GetEjercicioByIdQueryHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<EjercicioResult?> Handle(GetEjercicioByIdQuery request, CancellationToken cancellationToken)
        => await context.Ejercicios.AsNoTracking()
            .Where(item => item.Id == request.Id)
            .Select(item => new EjercicioResult(item.Id, item.Nombre, item.GrupoMuscular, item.DescripcionOrientativa))
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed class CreateEjercicioCommandHandler : IRequestHandler<CreateEjercicioCommand, EjercicioResult>
{
    private readonly IWorkoutsDbContext context;

    public CreateEjercicioCommandHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<EjercicioResult> Handle(CreateEjercicioCommand request, CancellationToken cancellationToken)
    {
        var ejercicio = new Ejercicio
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre,
            GrupoMuscular = request.GrupoMuscular,
            DescripcionOrientativa = request.DescripcionOrientativa
        };
        context.Ejercicios.Add(ejercicio);
        await context.SaveChangesAsync(cancellationToken);
        return new EjercicioResult(ejercicio.Id, ejercicio.Nombre, ejercicio.GrupoMuscular, ejercicio.DescripcionOrientativa);
    }
}

public sealed class UpdateEjercicioCommandHandler : IRequestHandler<UpdateEjercicioCommand, EjercicioResult?>
{
    private readonly IWorkoutsDbContext context;

    public UpdateEjercicioCommandHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<EjercicioResult?> Handle(UpdateEjercicioCommand request, CancellationToken cancellationToken)
    {
        var ejercicio = await context.Ejercicios.FindAsync([request.Id], cancellationToken);
        if (ejercicio is null)
        {
            return null;
        }
        ejercicio.Nombre = request.Nombre;
        ejercicio.GrupoMuscular = request.GrupoMuscular;
        ejercicio.DescripcionOrientativa = request.DescripcionOrientativa;
        await context.SaveChangesAsync(cancellationToken);
        return new EjercicioResult(ejercicio.Id, ejercicio.Nombre, ejercicio.GrupoMuscular, ejercicio.DescripcionOrientativa);
    }
}

public sealed class DeleteEjercicioCommandHandler : IRequestHandler<DeleteEjercicioCommand, bool>
{
    private readonly IWorkoutsDbContext context;

    public DeleteEjercicioCommandHandler(IWorkoutsDbContext context) => this.context = context;

    public async Task<bool> Handle(DeleteEjercicioCommand request, CancellationToken cancellationToken)
    {
        var ejercicio = await context.Ejercicios.FindAsync([request.Id], cancellationToken);
        if (ejercicio is null)
        {
            return false;
        }
        context.Ejercicios.Remove(ejercicio);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
