namespace G2_SC603_KN_Proyecto.Helpers
{
    //centraliza la zona horaria de Costa Rica para que no dependa de la del servidor
    public static class ZonaHoraria
    {
        private static readonly TimeZoneInfo ZonaCR =
            TimeZoneInfo.CreateCustomTimeZone("Costa_Rica_Fijo", TimeSpan.FromHours(-6), "Costa Rica", "Costa Rica");

        /// <summary>Fecha y hora actual en Costa Rica (equivalente a DateTime.Now, pero correcto sin importar en qué zona horaria corra el servidor).</summary>
        public static DateTime Ahora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaCR);

        /// <summary>Día calendario actual en Costa Rica (equivalente a DateOnly.FromDateTime(DateTime.Today)).</summary>
        public static DateOnly Hoy => DateOnly.FromDateTime(Ahora);

        /// <summary>Medianoche de hoy en Costa Rica, como DateTime (equivalente a DateTime.Today) — útil para comparar contra columnas DateTime en vez de DateOnly.</summary>
        public static DateTime HoyDateTime => Hoy.ToDateTime(TimeOnly.MinValue);

        /// <summary>
        /// Hora (24h) a partir de la cual se considera "cerrado" el día operativo
        /// del gimnasio — después del último horario de WOD (7 PM) más un
        /// margen razonable. Ajustar acá si el horario de cierre cambia.
        /// </summary>
        private const int HoraCierreDiaOperativo = 21; // 9 PM

        /// <summary>
        /// Día "operativo", para vistas tipo cola (Calendario de la Semana en
        /// Reservas, "Quiénes van a asistir" en Dashboard): igual a Hoy hasta
        /// las 9 PM; de ahí en adelante ya se considera terminado el día y
        /// esas cajas rotan solas para mostrar el siguiente. No usar esto
        /// para fechas que se guardan en la base de datos (pagos, check-in,
        /// asignación de WOD) — para eso siempre corresponde el día
        /// calendario real, que es Hoy.
        /// </summary>
        public static DateOnly DiaOperativo => Ahora.Hour >= HoraCierreDiaOperativo ? Hoy.AddDays(1) : Hoy;
    }
}
