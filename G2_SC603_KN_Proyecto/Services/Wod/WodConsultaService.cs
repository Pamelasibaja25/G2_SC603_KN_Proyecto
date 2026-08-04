using G2_SC603_KN_Proyecto.Models;
using G2_SC603_KN_Proyecto.Models.ViewModels.Wod;
using Microsoft.EntityFrameworkCore;

namespace G2_SC603_KN_Proyecto.Services.Wod
{
    /// <summary>
    /// Implementación de <see cref="IWodConsultaService"/>.
    /// Responsabilidad única (SRP): construir las distintas vistas de
    /// consulta de entrenamientos (historial, diario, detalle) a partir del
    /// contexto de Entity Framework, aplicando las reglas de visibilidad por
    /// rol. Depende únicamente de la abstracción del contexto inyectada por
    /// el contenedor de dependencias (DIP).
    /// </summary>
    public class WodConsultaService : IWodConsultaService
    {
        private const string RolAdministrador = "ADMIN";
        private const string RolEntrenador = "TRAINER";
        private const string RolCliente = "USER";

        private readonly DbOrionFitContext _context;

        public WodConsultaService(DbOrionFitContext context)
        {
            _context = context;
        }

        public async Task<List<WodHistorialItemViewModel>> ObtenerEntrenamientoDiarioAsync(int idUsuario, string rol)
        {
            DateOnly manana = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            if (rol == RolCliente)
            {
                int? idCliente = await ObtenerIdClienteAsync(idUsuario);
                if (idCliente == null)
                {
                    return new List<WodHistorialItemViewModel>();
                }

                return await _context.ClienteRutinas
                    .AsNoTracking()
                    .Where(cr => cr.IdCliente == idCliente.Value && cr.FechaAsignacion == manana)
                    .OrderByDescending(cr => cr.IdClienteRutina)
                    .Select(cr => new WodHistorialItemViewModel
                    {
                        IdClienteRutina = cr.IdClienteRutina,
                        IdRutina = cr.IdRutina,
                        Nombre = cr.IdRutinaNavigation.Nombre,
                        Objetivo = cr.IdRutinaNavigation.Objetivo,
                        Imagen = cr.IdRutinaNavigation.Imagen,
                        NombreEntrenador = cr.IdRutinaNavigation.IdEntrenadorNavigation.Nombre,
                        Fecha = cr.FechaAsignacion,
                        EstadoAsistencia = cr.EstadoAsistencia,
                        CantidadEjercicios = cr.IdRutinaNavigation.RutinaEjercicios.Count
                    })
                    .ToListAsync();
            }

            // Entrenador / Administrador: ven lo asignado para mañana, junto con el cliente correspondiente.
            return await _context.ClienteRutinas
                .AsNoTracking()
                .Where(cr => cr.FechaAsignacion == manana)
                .OrderBy(cr => cr.IdClienteNavigation.Nombre)
                .Select(cr => new WodHistorialItemViewModel
                {
                    IdClienteRutina = cr.IdClienteRutina,
                    IdRutina = cr.IdRutina,
                    Nombre = cr.IdRutinaNavigation.Nombre,
                    Objetivo = cr.IdRutinaNavigation.Objetivo,
                    Imagen = cr.IdRutinaNavigation.Imagen,
                    NombreEntrenador = cr.IdRutinaNavigation.IdEntrenadorNavigation.Nombre,
                    Fecha = cr.FechaAsignacion,
                    EstadoAsistencia = cr.EstadoAsistencia,
                    CantidadEjercicios = cr.IdRutinaNavigation.RutinaEjercicios.Count,
                    NombreCliente = cr.IdClienteNavigation.Nombre
                })
                .ToListAsync();
        }

        public async Task<WodDetalleViewModel?> ObtenerDetalleAsync(int idRutina, int idUsuario, string rol)
        {
            WodDetalleViewModel? detalle = await _context.Rutinas
                .AsNoTracking()
                .Where(r => r.IdRutina == idRutina)
                .Select(r => new WodDetalleViewModel
                {
                    IdRutina = r.IdRutina,
                    IdEntrenador = r.IdEntrenador,
                    Nombre = r.Nombre,
                    Objetivo = r.Objetivo,
                    NombreEntrenador = r.IdEntrenadorNavigation.Nombre,
                    Ejercicios = r.RutinaEjercicios.Select(re => new WodEjercicioDetalleViewModel
                    {
                        Nombre = re.IdEjercicioNavigation.Nombre,
                        GrupoMuscular = re.IdEjercicioNavigation.GrupoMuscular,
                        Series = re.Series,
                        Repeticiones = re.Repeticiones,
                        Descanso = re.Descanso
                    }).ToList(),
                    ClientesAsignados = r.ClienteRutinas.Select(cr => new WodClienteAsignadoViewModel
                    {
                        NombreCliente = cr.IdClienteNavigation.Nombre,
                        FechaAsignacion = cr.FechaAsignacion
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (detalle == null)
            {
                return null;
            }

            bool tieneAcceso = await TieneAccesoAlDetalleAsync(detalle, idUsuario, rol);
            return tieneAcceso ? detalle : null;
        }

        /// <summary>
        /// Valida si el usuario actual tiene permiso para ver el detalle de
        /// una rutina específica, según su rol:
        /// - Administrador: acceso total.
        /// - Entrenador: solo a las rutinas que él mismo publicó.
        /// - Cliente: solo a las rutinas que tiene asignadas.
        /// </summary>
        private async Task<bool> TieneAccesoAlDetalleAsync(WodDetalleViewModel detalle, int idUsuario, string rol)
        {
            if (rol == RolAdministrador)
            {
                return true;
            }

            if (rol == RolEntrenador)
            {
                int? idEntrenador = await ObtenerIdEntrenadorAsync(idUsuario);
                return idEntrenador.HasValue && idEntrenador.Value == detalle.IdEntrenador;
            }

            if (rol == RolCliente)
            {
                int? idCliente = await ObtenerIdClienteAsync(idUsuario);
                if (idCliente == null)
                {
                    return false;
                }

                return await _context.ClienteRutinas
                    .AsNoTracking()
                    .AnyAsync(cr => cr.IdRutina == detalle.IdRutina && cr.IdCliente == idCliente.Value);
            }

            return false;
        }

        private async Task<int?> ObtenerIdClienteAsync(int idUsuario)
        {
            return await _context.Clientes
                .AsNoTracking()
                .Where(c => c.IdUsuario == idUsuario)
                .Select(c => (int?)c.IdCliente)
                .FirstOrDefaultAsync();
        }

        private async Task<int?> ObtenerIdEntrenadorAsync(int idUsuario)
        {
            return await _context.Entrenadors
                .AsNoTracking()
                .Where(e => e.IdUsuario == idUsuario)
                .Select(e => (int?)e.IdEntrenador)
                .FirstOrDefaultAsync();
        }
    }
}
