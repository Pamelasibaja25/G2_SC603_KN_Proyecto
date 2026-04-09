
    function FiltrarTabla() {
        let filtro = $("#txtBuscar").val().toLowerCase();
    $("#tablaClientes tr").filter(function () {
        $(this).toggle($(this).text().toLowerCase().indexOf(filtro) > -1);
        });
    }
    $("#txtBuscar").on("keyup", function () {
        FiltrarTabla();
    });