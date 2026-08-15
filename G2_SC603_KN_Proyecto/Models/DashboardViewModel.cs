using System;
using System.Collections.Generic;

namespace G2_SC603_KN_Proyecto.Models;

public class DashboardViewModel
{
    public int ClientesActivos { get; set; }
    public decimal IngresosMes { get; set; }
    public int AsistenciaHoy { get; set; }
    public int ConfirmadosWodManana { get; set; }
    public List<ConfirmadosDiaVM> ConfirmadosPorDia { get; set; } = new();
    public int MembresiasPorVencer { get; set; }
    public int MembresiasPendientesDePago { get; set; }

    public List<AsistenciaSemanalVM> AsistenciaSemanal { get; set; } = new();
    public List<AsistenciaSemanalVM> AsistenciaMensual { get; set; } = new();
    public List<AsistenciaSemanalVM> AsistenciaRango { get; set; } = new();
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public bool RangoInvalido { get; set; }
    public List<VencimientoVM> Vencimientos { get; set; } = new();
    public List<RankingClienteVM> RankingClientes { get; set; } = new();
    public DateOnly? RankingInicio { get; set; }
    public DateOnly? RankingFin { get; set; }
    public decimal IngresosHoy { get; set; }
    public List<PagoHoyVM> PagosHoy { get; set; } = new();
    public List<AlertaStockVM> AlertasStock { get; set; } = new();
}

public class AsistenciaSemanalVM
{
    public string Dia { get; set; } = "";
    public int Cantidad { get; set; }
}

public class ConfirmadosDiaVM
{
    public DateOnly Fecha { get; set; }

    /// <summary>Nombres de los WOD publicados ese día (columnas de la tabla).</summary>
    public List<string> Wods { get; set; } = new();

    public List<ClienteConfirmadoVM> Clientes { get; set; } = new();

    public int Confirmados => Clientes.Count;
}

public class ClienteConfirmadoVM
{
    public string Nombre { get; set; } = "";

    /// <summary>Solo aplica al día de hoy: si ya hizo check-in físico.</summary>
    public bool YaIngreso { get; set; }

    /// <summary>Uno por cada WOD en ConfirmadosDiaVM.Wods (mismo orden): true si el cliente confirmó ese WOD.</summary>
    public List<bool> ConfirmoPorWod { get; set; } = new();

    /// <summary>Horario(s) que eligió el cliente para asistir, ej: "6 AM, 7 PM".</summary>
    public string? Horarios { get; set; }
}

public class VencimientoVM
{
    public int IdCliente { get; set; }
    public string Cliente { get; set; } = "";
    public DateOnly FechaFin { get; set; }
    public int DiasRestantes { get; set; }
}

public class RankingClienteVM
{
    public string Cliente { get; set; } = "";
    public int Asistencias { get; set; }
}

public class PagoHoyVM
{
    public string Cliente { get; set; } = "";
    public decimal Monto { get; set; }
    public string Metodo { get; set; } = "";
}

public class AlertaStockVM
{
    public string Producto { get; set; } = "";
    public int CantidadActual { get; set; }
    public int StockMinimo { get; set; }
}