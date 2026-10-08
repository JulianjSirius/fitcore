using FitCore.Identity.Application.Abstractions;
using FitCore.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Identity.Application.Features.Membresias;

public sealed class MembresiaException(string message) : Exception(message);

// Único punto que activa o extiende membresías. Lo usa el registro manual de pagos
// y lo usará el webhook de Wompi cuando un pago pendiente quede aprobado.
// No guarda cambios: quien lo llama hace SaveChanges dentro de su transacción.
public sealed class MembresiaService
{
    private readonly IAppDbContext _context;

    public MembresiaService(IAppDbContext context) => _context = context;

    public async Task<Membresia> AplicarPagoAprobadoAsync(Pago pago, CancellationToken cancellationToken)
    {
        var ahora = DateTime.UtcNow;

        var plan = await _context.Planes.FirstOrDefaultAsync(p => p.Id == pago.PlanId, cancellationToken)
            ?? throw new MembresiaException("El plan no existe.");

        var titular = await _context.Users.FirstOrDefaultAsync(u => u.Id == pago.UsuarioId, cancellationToken)
            ?? throw new MembresiaException("El usuario titular no existe.");

        if (plan.MaxBeneficiarios > 1 && pago.AcompananteUsuarioId is null)
        {
            throw new MembresiaException($"El plan {plan.Nombre} requiere un acompañante.");
        }
        if (plan.MaxBeneficiarios == 1 && pago.AcompananteUsuarioId is not null)
        {
            throw new MembresiaException($"El plan {plan.Nombre} no admite acompañante.");
        }

        var vigenteTitular = await _context.Membresias
            .Include(m => m.Plan)
            .Where(EstaVigente(ahora))
            .FirstOrDefaultAsync(m => m.UsuarioTitularId == titular.Id || m.AcompananteUsuarioId == titular.Id, cancellationToken);

        if (vigenteTitular is not null && vigenteTitular.UsuarioTitularId != titular.Id)
        {
            throw new MembresiaException(
                "El usuario es acompañante en otra membresía Duo vigente; no puede ser titular hasta que esa venza.");
        }
        if (vigenteTitular is not null && vigenteTitular.PlanId != plan.Id)
        {
            throw new MembresiaException(
                $"El usuario ya tiene el plan {vigenteTitular.Plan.Nombre} vigente hasta el {vigenteTitular.FechaVencimiento:yyyy-MM-dd}. " +
                "Solo se puede renovar el mismo plan o esperar a que venza.");
        }

        User? acompanante = null;
        if (pago.AcompananteUsuarioId is { } acompananteId)
        {
            if (acompananteId == titular.Id)
            {
                throw new MembresiaException("El acompañante debe ser una persona distinta al titular.");
            }

            acompanante = await _context.Users.FirstOrDefaultAsync(u => u.Id == acompananteId, cancellationToken)
                ?? throw new MembresiaException("El acompañante no está registrado.");

            Guid? membresiaActualId = vigenteTitular?.Id;
            var otraVigente = await _context.Membresias
                .Where(EstaVigente(ahora))
                .AnyAsync(m => (m.UsuarioTitularId == acompananteId || m.AcompananteUsuarioId == acompananteId)
                    && (membresiaActualId == null || m.Id != membresiaActualId), cancellationToken);
            if (otraVigente)
            {
                throw new MembresiaException("El acompañante ya tiene otra membresía vigente.");
            }
        }

        Membresia membresia;
        if (vigenteTitular is not null)
        {
            // Renovación anticipada: el nuevo periodo empieza donde termina el actual.
            membresia = vigenteTitular;
            membresia.FechaVencimiento = membresia.FechaVencimiento.AddMonths(plan.DuracionMeses);

            if (membresia.AcompananteUsuarioId is { } anteriorId && anteriorId != pago.AcompananteUsuarioId)
            {
                var anterior = await _context.Users.FirstOrDefaultAsync(u => u.Id == anteriorId, cancellationToken);
                if (anterior is not null)
                {
                    ActualizarCopia(anterior, EstadosMembresia.Inactivo, anterior.TipoMembresia, null);
                }
            }
            membresia.AcompananteUsuarioId = pago.AcompananteUsuarioId;
        }
        else
        {
            membresia = new Membresia
            {
                Id = Guid.NewGuid(),
                PlanId = plan.Id,
                UsuarioTitularId = titular.Id,
                AcompananteUsuarioId = pago.AcompananteUsuarioId,
                Estado = EstadosMembresia.Activa,
                FechaInicio = ahora,
                FechaVencimiento = ahora.AddMonths(plan.DuracionMeses),
                FechaCreacion = ahora
            };
            _context.Membresias.Add(membresia);
        }

        ActualizarCopia(titular, EstadosMembresia.Activa, plan.Codigo, membresia.FechaVencimiento);
        if (acompanante is not null)
        {
            ActualizarCopia(acompanante, EstadosMembresia.Activa, plan.Codigo, membresia.FechaVencimiento);
        }

        pago.MembresiaId = membresia.Id;
        pago.Estado = EstadosPago.Aprobado;
        return membresia;
    }

    // Vigente = marcada Activa y con fecha futura (no depende de que el proceso de
    // vencimiento ya haya corrido).
    public static System.Linq.Expressions.Expression<Func<Membresia, bool>> EstaVigente(DateTime ahora)
        => m => m.Estado == EstadosMembresia.Activa && m.FechaVencimiento > ahora;

    public static void ActualizarCopia(User usuario, string estado, string tipo, DateTime? vencimiento)
    {
        usuario.EstadoMembresia = estado;
        usuario.TipoMembresia = tipo;
        usuario.FechaVencimientoMembresia = vencimiento;
    }
}
