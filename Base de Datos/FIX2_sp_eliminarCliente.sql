-- Ejecutar en local y Railway. Reemplaza la version anterior.
DROP PROCEDURE IF EXISTS sp_eliminarCliente;

DELIMITER $$
CREATE PROCEDURE sp_eliminarCliente(
    IN pIdCliente INT
)
BEGIN
    DECLARE clienteEstado VARCHAR(20);
    DECLARE clienteIdUsuario INT;
    DECLARE tablaHistorialExiste INT;

    SELECT estado, id_usuario
    INTO clienteEstado, clienteIdUsuario
    FROM Cliente
    WHERE id_cliente = pIdCliente;

    IF clienteEstado IS NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El cliente no existe en el sistema.';
    ELSEIF clienteEstado != 'Inactivo' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Solo se pueden eliminar clientes con estado Inactivo.';
    ELSE
        -- Notificaciones del cliente
        DELETE FROM Notificacion WHERE id_cliente = pIdCliente;

        -- Historial de membresias: solo si la tabla existe en esta BD
        -- (evita romper la eliminacion si el ambiente no tiene esta tabla).
        SELECT COUNT(*) INTO tablaHistorialExiste
        FROM information_schema.tables
        WHERE table_schema = DATABASE() AND table_name = 'historial_membresias';

        IF tablaHistorialExiste > 0 THEN
            SET @sqlHistorial = CONCAT('DELETE FROM historial_membresias WHERE id_cliente = ', pIdCliente);
            PREPARE stmtHistorial FROM @sqlHistorial;
            EXECUTE stmtHistorial;
            DEALLOCATE PREPARE stmtHistorial;
        END IF;

        -- Ventas del cliente (primero el detalle, luego la venta)
        DELETE dv FROM Detalle_Venta dv
            INNER JOIN Venta v ON dv.id_venta = v.id_venta
            WHERE v.id_cliente = pIdCliente;
        DELETE FROM Venta WHERE id_cliente = pIdCliente;

        -- Pagos (dependen de Cliente_Membresia)
        DELETE p FROM Pago p
            INNER JOIN Cliente_Membresia cm ON p.id_cliente_membresia = cm.id_cliente_membresia
            WHERE cm.id_cliente = pIdCliente;
        DELETE FROM Cliente_Membresia WHERE id_cliente = pIdCliente;

        -- Reservas del cliente
        DELETE FROM Reserva WHERE id_cliente = pIdCliente;

        -- WOD asignados y asistencias
        DELETE FROM Cliente_Rutina WHERE id_cliente = pIdCliente;
        DELETE FROM Asistencia     WHERE id_cliente = pIdCliente;

        -- Cliente y su usuario asociado
        DELETE FROM Cliente WHERE id_cliente = pIdCliente;
        DELETE FROM Usuario WHERE id_usuario = clienteIdUsuario;
    END IF;
END $$
DELIMITER ;
