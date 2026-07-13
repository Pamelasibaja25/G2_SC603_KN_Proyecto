using System;
using System.Collections.Generic;

namespace G2_SC603_KN_Proyecto.Models;

public class DashboardViewModel
{
    public int ClientesActivos { get; set; }
    public decimal IngresosMes { get; set; }
    public int AsistenciaHoy { get; set; }
    public int MembresiasPorVencer { get; set; }

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

public class TopHorarioVM
{
    public int Hora { get; set; }
    public int Asistencias { get; set; }
}

public class AlertaStockVM
{
    public string Producto { get; set; } = "";
    public int CantidadActual { get; set; }
    public int StockMinimo { get; set; }
}