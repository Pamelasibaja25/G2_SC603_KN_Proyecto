USE DB_Orion_Fit;

-- stored procedures: clientes (editar y eliminar)


drop procedure if exists sp_editarcliente;
drop procedure if exists sp_eliminarcliente;
drop procedure if exists sp_obtenerwods;
drop procedure if exists sp_agregarwod;
drop procedure if exists sp_obtenerejercicios;

delimiter $$

-- editar datos personales de un cliente
-- historia: modificar datos del cliente

create procedure sp_editarcliente(
    in pidcliente     int,
    in pnombre        varchar(100),
    in pcedula        varchar(20),
    in ptelefono      varchar(20),
    in pcorreo        varchar(100),
    in pfechanacimiento date,
    in pestado        varchar(20)
)
begin
    if not exists (select 1 from cliente where id_cliente = pidcliente) then
        signal sqlstate '45000'
            set message_text = 'el cliente no existe en el sistema.';
    else
        update cliente
        set nombre           = pnombre,
            cedula           = pcedula,
            telefono         = ptelefono,
            correo           = pcorreo,
            fecha_nacimiento = pfechanacimiento,
            estado           = pestado
        where id_cliente = pidcliente;
    end if;
end$$


-- eliminar cliente inactivo
-- historia: eliminar clientes inactivos

create procedure sp_eliminarcliente(
    in pidcliente int
)
begin
    declare clienteestado varchar(20);
    declare clienteidusuario int;

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
        delete from cliente_membresia where id_cliente = pidcliente;
        delete from cliente_rutina    where id_cliente = pidcliente;
        delete from asistencia        where id_cliente = pidcliente;
        delete from cliente           where id_cliente = pidcliente;
        delete from usuario           where id_usuario = clienteidusuario;
    end if;
end$$


-- obtener todos los wods con sus ejercicios
-- historia: publicar entrenamiento diario

create procedure sp_obtenerwods()
begin
    select
        r.id_rutina      as idrutina,
        r.nombre         as nombre,
        r.objetivo       as objetivo,
        e.id_entrenador  as identrenador,
        e.nombre         as nombreentrenador,
        re.id_rutina_ejercicio as idrutinaejercicio,
        ej.nombre        as nombreejercicio,
        re.series        as series,
        re.repeticiones  as repeticiones,
        re.descanso      as descanso
    from rutina r
    inner join entrenador e  on r.id_entrenador = e.id_entrenador
    left join rutina_ejercicio re on r.id_rutina = re.id_rutina
    left join ejercicio ej   on re.id_ejercicio = ej.id_ejercicio
    order by r.id_rutina desc;
end$$


-- obtener lista de ejercicios disponibles
-- para el dropdown del modal wod

create procedure sp_obtenerejercicios()
begin
    select
        id_ejercicio   as idejercicio,
        nombre         as nombre,
        grupo_muscular as grupomuscular
    from ejercicio
    order by nombre asc;
end$$


-- agregar wod (rutina + ejercicios)
-- historia: publicar entrenamiento diario

create procedure sp_agregarwod(
    in pidentrenador int,
    in pnombre       varchar(100),
    in pobjetivo     varchar(255),
    in pejercicios   longtext
)
begin
    declare nuevarutinaid int;
    declare totalejercicios int;
    declare indice int default 0;
    declare pidejercicio int;
    declare pseries int;
    declare prepeticiones int;
    declare pdescanso int;

    if pnombre is null or trim(pnombre) = '' then
        signal sqlstate '45000'
            set message_text = 'el nombre del entrenamiento es obligatorio.';
    end if;

    if json_length(pejercicios) = 0 then
        signal sqlstate '45000'
            set message_text = 'debe incluir al menos un ejercicio en el wod.';
    end if;

    insert into rutina (id_entrenador, nombre, objetivo)
    values (pidentrenador, pnombre, pobjetivo);

    set nuevarutinaid = last_insert_id();
    set totalejercicios = json_length(pejercicios);

    while indice < totalejercicios do
        set pidejercicio  = json_unquote(json_extract(pejercicios, concat('$[', indice, '].idejercicio')));
        set pseries       = json_unquote(json_extract(pejercicios, concat('$[', indice, '].series')));
        set prepeticiones = json_unquote(json_extract(pejercicios, concat('$[', indice, '].repeticiones')));
        set pdescanso     = json_unquote(json_extract(pejercicios, concat('$[', indice, '].descanso')));

        insert into rutina_ejercicio (id_rutina, id_reserva, id_ejercicio, series, repeticiones, descanso)
        values (nuevarutinaid, 1, pidejercicio, pseries, prepeticiones, pdescanso);

        set indice = indice + 1;
    end while;
end$$

delimiter ;

-- primero verificá si el sp existe
show procedure status where db = 'db_orion_fit' and name = 'sp_obtenerclientesresumen';

call sp_obtenerclientesresumen();
insert into usuario (username, contrasena, rol) values ('cliente1', sha2('cliente1', 256), 'user');
insert into cliente (id_usuario, nombre, cedula, telefono, correo, fecha_nacimiento, estado) values (last_insert_id(), 'ana rodríguez mora', '112233445', '88881111', 'ana@correo.com', '1995-03-15', 'activo');
insert into usuario (username, contrasena, rol) values ('cliente2', sha2('cliente2', 256), 'user');
insert into cliente (id_usuario, nombre, cedula, telefono, correo, fecha_nacimiento, estado) values (last_insert_id(), 'carlos jiménez vega', '998877665', '77772222', 'carlos@correo.com', '1990-07-20', 'inactivo');
select * from cliente;

insert into ejercicio (nombre, grupo_muscular, descripcion) values
('burpees', 'cuerpo completo', 'ejercicio de alta intensidad'),
('air squat', 'piernas', 'sentadilla sin peso');

-- primero hacemos la columna id_reserva opcional en la tabla
alter table rutina_ejercicio 
    drop foreign key fk_reserva_ejercicio;

alter table rutina_ejercicio 
    modify column id_reserva int null;

alter table rutina_ejercicio
    add constraint fk_reserva_ejercicio 
    foreign key (id_reserva) references reserva(id_reserva)
    on delete set null;

-- recrear el sp sin el id_reserva hardcodeado
drop procedure if exists sp_agregarwod;

delimiter $$
create procedure sp_agregarwod(
    in pidentrenador int,
    in pnombre       varchar(100),
    in pobjetivo     varchar(255),
    in pejercicios   longtext
)
begin
    declare nuevarutinaid int;
    declare totalejercicios int;
    declare indice int default 0;
    declare pidejercicio int;
    declare pseries int;
    declare prepeticiones int;
    declare pdescanso int;

    if pnombre is null or trim(pnombre) = '' then
        signal sqlstate '45000'
            set message_text = 'el nombre del entrenamiento es obligatorio.';
    end if;

    if json_length(pejercicios) = 0 then
        signal sqlstate '45000'
            set message_text = 'debe incluir al menos un ejercicio en el wod.';
    end if;

    insert into rutina (id_entrenador, nombre, objetivo)
    values (pidentrenador, pnombre, pobjetivo);

    set nuevarutinaid = last_insert_id();
    set totalejercicios = json_length(pejercicios);

    while indice < totalejercicios do
        set pidejercicio  = json_unquote(json_extract(pejercicios, concat('$[', indice, '].idejercicio')));
        set pseries       = json_unquote(json_extract(pejercicios, concat('$[', indice, '].series')));
        set prepeticiones = json_unquote(json_extract(pejercicios, concat('$[', indice, '].repeticiones')));
        set pdescanso     = json_unquote(json_extract(pejercicios, concat('$[', indice, '].descanso')));

        -- id_reserva va null porque un wod no requiere reserva
        insert into rutina_ejercicio (id_rutina, id_reserva, id_ejercicio, series, repeticiones, descanso)
        values (nuevarutinaid, null, pidejercicio, pseries, prepeticiones, pdescanso);

        set indice = indice + 1;
    end while;
end$$
delimiter ;

