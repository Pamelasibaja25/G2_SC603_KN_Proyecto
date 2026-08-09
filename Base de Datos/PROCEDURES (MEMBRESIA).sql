USE DB_Orion_Fit;
drop procedure if exists sp_obtenerclientesmembresias;
drop procedure if exists sp_actualizarestadosmembresias;
drop procedure if exists sp_agregarclientemembresia;
drop procedure if exists sp_actualizarclientemembresia;
drop procedure if exists sp_obtenerhistorialmembresia;
drop procedure if exists sp_obtenermembresiasproximasvencer;

/*events*/
create event if not exists eventoactualizarestados
on schedule every 1 day
starts current_timestamp
do
    call sp_actualizarestadosmembresias();


/*procedures*/
delimiter $$

create procedure sp_obtenerclientesmembresias()
begin
    select 
        c.nombre as cliente,
        m.nombre as tipoplan,
        cm.fecha_inicio as fechainicio,
        cm.fecha_fin as fechafin,
        m.precio as precio,
        cm.estado as estado,
       c.id_cliente as idcliente,
        m.id_membresia as idmembresia
    from cliente_membresia cm
    inner join cliente c on cm.id_cliente = c.id_cliente
    inner join membresia m on cm.id_membresia = m.id_membresia;
end$$

delimiter ;

delimiter $$

create procedure sp_actualizarestadosmembresias()
begin
    -- actualizar a 'vencida' si la fecha_fin ya pasó y no está suspendida
set sql_safe_updates = 0;

update cliente_membresia
set estado = 'vencida'
where fecha_fin < curdate()
  and estado not in ('suspendida', 'vencida');

set sql_safe_updates = 1;

end$$

delimiter ;

delimiter $$

create procedure sp_agregarclientemembresia(
    in p_id_cliente int,
    in p_id_membresia int,
    in p_fecha_inicio date,
    in p_fecha_fin date,
    in p_estado varchar(20)
)
begin
    -- verificar si el cliente ya tiene una membresía 
    if not exists (
        select 1
        from cliente_membresia
        where id_cliente = p_id_cliente
    ) then
        -- insertar nueva membresía
        insert into cliente_membresia (
            id_cliente, id_membresia, fecha_inicio, fecha_fin, estado
        ) values (
            p_id_cliente, p_id_membresia, p_fecha_inicio, p_fecha_fin, p_estado
        );
    else
        -- opcional: lanzar un error o mensaje
        signal sqlstate '45000'
            set message_text = 'el cliente ya tiene una membresía activa';
    end if;
end$$

delimiter ;

delimiter $$

create procedure sp_actualizarclientemembresia(
    in p_id_cliente int,
    in p_id_membresia int,
    in p_fecha_inicio date,
    in p_fecha_fin date,
    in p_estado varchar(20)
)
begin
    -- guardar la información actual en el historial
    insert into historial_membresias (
        id_cliente,
        id_membresia,
        fecha_inicio,
        fecha_fin
    )
    select
        id_cliente,
        id_membresia,
        fecha_inicio,
        fecha_fin
    from cliente_membresia
    where id_cliente = p_id_cliente;
    
    -- actualiza la membresía del cliente si existe
    update cliente_membresia
    set id_membresia = p_id_membresia,
        fecha_inicio = p_fecha_inicio,
        fecha_fin = p_fecha_fin,
        estado = p_estado
    where id_cliente = p_id_cliente;
    
end$$

delimiter ;

delimiter $$

create procedure sp_obtenerhistorialmembresia(
    in p_id_cliente int
)
begin
    select
        h.id_historial,
        h.id_cliente,
        c.nombre as cliente,
        h.id_membresia,
        m.nombre as membresia,
        h.fecha_inicio,
        h.fecha_fin
    from historial_membresias h
    inner join cliente c
        on h.id_cliente = c.id_cliente
    inner join membresia m
        on h.id_membresia = m.id_membresia
    where h.id_cliente = p_id_cliente
    order by h.id_historial desc;
end$$

delimiter ;
delimiter $$

create procedure sp_obtenermembresiasproximasvencer()
begin

    select
        cm.id_cliente as idcliente,
        c.nombre as cliente,
        m.nombre as membresia,
        cm.fecha_fin as fechafin,
        datediff(cm.fecha_fin, curdate()) as diasrestantes
    from cliente_membresia cm
        inner join cliente c
            on c.id_cliente = cm.id_cliente
        inner join membresia m
            on m.id_membresia = cm.id_membresia
    where cm.estado = 'activa'
        and cm.fecha_fin between curdate() and date_add(curdate(), interval 15 day)
    order by cm.fecha_fin asc;

end$$

delimiter ;
