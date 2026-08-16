namespace G2_SC603_KN_Proyecto.Models;

/// <summary>Horarios fijos del gimnasio, iguales a los que la dueña ya usa en
/// la encuesta de WhatsApp para preguntar a qué hora va a llegar cada cliente.</summary>
public static class HorariosWod
{
    public static readonly string[] Opciones =
    {
        "5 AM", "6 AM", "7 AM", "8 AM",
        "4 PM", "5 PM", "6 PM", "7 PM"
    };

    /// <summary>Convierte "5 AM" / "7 PM" a la hora real, para poder
    /// compararla contra la hora actual y bloquear horarios ya pasados.</summary>
    public static TimeOnly ParseHora(string horario)
    {
        return TimeOnly.ParseExact(horario, "h tt", System.Globalization.CultureInfo.InvariantCulture);
    }
}
