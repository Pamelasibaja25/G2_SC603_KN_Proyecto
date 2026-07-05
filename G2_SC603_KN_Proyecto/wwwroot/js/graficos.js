
function cambiarPeriodo(periodo) {

    document.querySelectorAll(".tab-btn")
        .forEach(x => x.classList.remove("active"));

    if (periodo === "semana") {
        document.getElementById("btnSemana").classList.add("active");
        cargarGrafico(asistenciaSemana);
    }
    else if (periodo === "mes") {
        document.getElementById("btnMes").classList.add("active");
        cargarGrafico(asistenciaMes);
    }
    else {
        document.getElementById("btnRango").classList.add("active");
        cargarGrafico(asistenciaRango);
    }

}

let attendanceChart;

function cargarGrafico(datos) {

    const labels = datos.map(x => x.Dia);
    const valores = datos.map(x => x.Cantidad);

    // Sin datos
    const mensaje = document.getElementById("chartEmptyMsg");
    if (mensaje) {
        const sinDatos = valores.length === 0 || valores.every(v => v === 0);
        mensaje.style.display = sinDatos ? "block" : "none";
    }

    if (attendanceChart) {
        attendanceChart.destroy();
    }

    attendanceChart = new Chart(
        document.getElementById("attendanceChart"),
        {
            type: "bar",

            data: {
                labels: labels,
                datasets: [{
                    label: "Asistencias",
                    data: valores,
                    borderWidth: 1
                }]
            },

            options: {
                responsive: true,
                maintainAspectRatio: false
            }
        });
}


document.addEventListener("DOMContentLoaded", function () {

    if (asistenciaRango.length > 0) {
        cambiarPeriodo("rango");
    }
    else {
        cambiarPeriodo("semana");
    }

});
