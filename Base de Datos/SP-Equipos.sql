USE DB_Orion_Fit;
delimiter $$

create procedure sp_editarequipo(
    in p_id_equipo int,
    in p_nombre varchar(100),
    in p_estado varchar(30),
    in p_fecha_compra date,
    in p_costo decimal(10,2)
)
begin
    update equipo
    set
        nombre = p_nombre,
        estado = p_estado,
        fecha_compra = p_fecha_compra,
        costo = p_costo
    where id_equipo = p_id_equipo;
end $$

delimiter ;

delimiter $$

create procedure sp_agregarequipo(
    in p_nombre varchar(100),
    in p_estado varchar(30),
    in p_fecha_compra date,
    in p_costo decimal(10,2)
)
begin
    insert into equipo (
        nombre,
        estado,
        fecha_compra,
        costo
    )
    values (
        p_nombre,
        p_estado,
        p_fecha_compra,
        p_costo
    );
end $$

delimiter ;

delimiter $$

create procedure sp_eliminarequipo(
    in p_id_equipo int
)
begin
    delete from equipo
    where id_equipo = p_id_equipo;
end $$

delimiter ;
