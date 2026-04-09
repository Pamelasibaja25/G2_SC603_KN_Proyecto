function ConsultarNombre() {

    let identificacion = $("#Cedula").val();
    $("#Nombre").val("");

    if (identificacion.length >= 9) {

        $.ajax({
            type: 'GET',
            url: "https://apis.gometa.org/cedulas/" + identificacion,
            dataType: 'json',
            success: function (result) {
                $("#Nombre").val(result.nombre);
            }
        });

    }
}

$(document).ready(function () {

    $("#FormRegistroClientes").validate({
        rules: {
            Cedula: { required: true },
            Nombre: { required: true },
            Correo: { required: true, email: true },
            Telefono: { required: true },
            FechaInscripcion: { required: true },
            Direccion: { required: true }
        },
        messages: {
            Cedula: {
                required: "* Requerido"
            },
            Nombre: {
                required: "* Requerido"
            },
            Correo: {
                required: "* Requerido",
                email: "* Formato",
            },
            Telefono: { required: "* Requerido" },
            FechaInscripcion: { required: "* Requerido" },
            Direccion: { required: "* Requerido" }
        },
        errorElement: "span",
        errorClass: "text-danger"
    });
    $("#FormRegistroMembresia").validate({
        rules: {
            Membresia: {
                required: true
            },
            Cliente: {
                required: true
            },
            Tipo: {
                required: true
            }
        },
        messages: {
            Membresia: {
                required: "* Seleccione una membresía"
            },
            Cliente: {
                required: "* Seleccione un cliente"
            },
            Tipo: {
                required: "* Seleccione un estado"
            }
        },
        errorElement: "span",
        errorClass: "text-danger"
    });

});
