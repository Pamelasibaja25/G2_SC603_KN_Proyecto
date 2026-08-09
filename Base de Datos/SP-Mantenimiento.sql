USE DB_Orion_Fit;
alter table `mantenimiento`
    add column `tipo` enum('preventivo','correctivo','calibracion','limpieza')
    not null default 'preventivo'
    after `id_equipo`;
    
    -- =====================================================================
-- stored procedures para la tabla `mantenimiento`.
-- mismo estilo y convenciones que sp-equipos.sql (prefijo p_ en
-- parametros, un bloque delimiter por procedimiento).
-- =====================================================================

delimiter $$

create procedure sp_agregarmantenimiento(
    in p_id_equipo int,
    in p_tipo varchar(30),
    in p_fecha date,
    in p_descripcion varchar(255),
    in p_costo decimal(10,2),
    in p_estado varchar(30)
)
begin
    insert into mantenimiento (
        id_equipo,
        tclienteipo,
        fecha,
        descripcion,
        costo,
        estado
    )
    values (
        p_id_equipo,
        p_tipo,
        p_fecha,
        p_descripcion,
        p_costo,
        p_estado
    );
end $$

delimiter ;

delimiter $$

create procedure sp_editarmantenimiento(
    in p_id_mantenimiento int,
    in p_id_equipo int,
    in p_tipo varchar(30),
    in p_fecha date,
    in p_descripcion varchar(255),
    in p_costo decimal(10,2),
    in p_estado varchar(30)
)
begin
    update mantenimiento
    set
        id_equipo = p_id_equipo,
        tipo = p_tipo,
        fecha = p_fecha,
        descripcion = p_descripcion,
        costo = p_costo,
        estado = p_estado
    where id_mantenimiento = p_id_mantenimiento;
end $$

delimiter ;

delimiter $$

create procedure sp_eliminarmantenimiento(
    in p_id_mantenimiento int
)
begin
    delete from mantenimiento
    where id_mantenimiento = p_id_mantenimiento;
end $$

delimiter ;

-- hu #39: cierra un mantenimiento programado, marcandolo como completado
-- y actualizando fecha/tipo/costo/descripcion con los datos reales.
delimiter $$

create procedure sp_completarmantenimiento(
    in p_id_mantenimiento int,
    in p_tipo varchar(30),
    in p_fecha date,
    in p_descripcion varchar(255),
    in p_costo decimal(10,2)
)
begin
    update mantenimiento
    set
        tipo = p_tipo,
        fecha = p_fecha,
        descripcion = p_descripcion,
        costo = p_costo,
        estado = 'completado'
    where id_mantenimiento = p_id_mantenimiento;
end $$

delimiter ;
