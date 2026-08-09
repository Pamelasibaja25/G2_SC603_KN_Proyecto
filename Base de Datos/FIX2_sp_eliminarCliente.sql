USE DB_Orion_Fit;
drop procedure if exists sp_eliminarcliente;

delimiter $$
create procedure sp_eliminarcliente(
    in pidcliente int
)
begin
    declare clienteestado varchar(20);
    declare clienteidusuario int;
    declare tablahistorialexiste int;

    select estado, id_usuario
    into clienteestado, clienteidusuario
    from cliente
    where id_cliente = pidcliente;

    if clienteestado is null then
        signal sqlstate '45000'
            set message_text = 'el cliente no existe en el sistema.';
    elseif clienteestado != 'inactivo' then
        signal sqlstate '45000'
            set message_text = 'solo se pueden eliminar clientes con estado inactivo.';
    else
        -- notificaciones del cliente
        delete from notificacion where id_cliente = pidcliente;

        -- historial de membresias: solo si la tabla existe en esta bd
        -- (evita romper la eliminacion si el ambiente no tiene esta tabla).
        select count(*) into tablahistorialexiste
        from information_schema.tables
        where table_schema = database() and table_name = 'historial_membresias';

        if tablahistorialexiste > 0 then
            set @sqlhistorial = concat('delete from historial_membresias where id_cliente = ', pidcliente);
            prepare stmthistorial from @sqlhistorial;
            execute stmthistorial;
            deallocate prepare stmthistorial;
        end if;

        -- ventas del cliente (primero el detalle, luego la venta)
        delete dv from detalle_venta dv
            inner join venta v on dv.id_venta = v.id_venta
            where v.id_cliente = pidcliente;
        delete from venta where id_cliente = pidcliente;

        -- pagos (dependen de cliente_membresia)
        delete p from pago p
            inner join cliente_membresia cm on p.id_cliente_membresia = cm.id_cliente_membresia
            where cm.id_cliente = pidcliente;
        delete from cliente_membresia where id_cliente = pidcliente;

        -- reservas del cliente
        delete from reserva where id_cliente = pidcliente;

        -- wod asignados y asistencias
        delete from cliente_rutina where id_cliente = pidcliente;
        delete from asistencia     where id_cliente = pidcliente;

        -- cliente y su usuario asociado
        delete from cliente where id_cliente = pidcliente;
        delete from usuario where id_usuario = clienteidusuario;
    end if;
end $$
delimiter ;

