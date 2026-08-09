-- =====================================================================
-- MIGRACION: renombrar TODOS los SPs a camelCase real
-- (sp_EditarEquipo -> sp_editarEquipo, etc.)
-- Ejecutar completo en local Y en Railway. Es re-ejecutable (idempotente).
-- =====================================================================
USE DB_Orion_Fit;
-- 1) eliminar los sps viejos (con mayuscula despues de sp_)
drop procedure if exists sp_actualizarclientemembresia;
drop procedure if exists sp_actualizarcontraseña;
drop procedure if exists sp_actualizarestadosmembresias;
drop procedure if exists sp_agregarcliente;
drop procedure if exists sp_agregarclientemembresia;
drop procedure if exists sp_agregarequipo;
drop procedure if exists sp_agregarmantenimiento;
drop procedure if exists sp_agregarusuario;
drop procedure if exists sp_agregarwod;
drop procedure if exists sp_completarmantenimiento;
drop procedure if exists sp_editarcliente;
drop procedure if exists sp_editarequipo;
drop procedure if exists sp_editarmantenimiento;
drop procedure if exists sp_editarusuario;
drop procedure if exists sp_eliminarcliente;
drop procedure if exists sp_eliminarequipo;
drop procedure if exists sp_eliminarmantenimiento;
drop procedure if exists sp_obtenerclientesmembresias;
drop procedure if exists sp_obtenerclientesresumen;
drop procedure if exists sp_obtenerejercicios;
drop procedure if exists sp_obtenerhistorialmembresia;
drop procedure if exists sp_obtenermembresiasproximasvencer;
drop procedure if exists sp_obtenerusuarioconnombre;
drop procedure if exists sp_obtenerusuariosconnombre;
drop procedure if exists sp_obtenerwods;

-- 2) eliminar por si ya existieran los nuevos (re-ejecucion segura)
drop procedure if exists sp_actualizarclientemembresia;
drop procedure if exists sp_actualizarcontraseña;
drop procedure if exists sp_actualizarestadosmembresias;
drop procedure if exists sp_agregarcliente;
drop procedure if exists sp_agregarclientemembresia;
drop procedure if exists sp_agregarequipo;
drop procedure if exists sp_agregarmantenimiento;
drop procedure if exists sp_agregarusuario;
drop procedure if exists sp_agregarwod;
drop procedure if exists sp_completarmantenimiento;
drop procedure if exists sp_editarcliente;
drop procedure if exists sp_editarequipo;
drop procedure if exists sp_editarmantenimiento;
drop procedure if exists sp_editarusuario;
drop procedure if exists sp_eliminarcliente;
drop procedure if exists sp_eliminarequipo;
drop procedure if exists sp_eliminarmantenimiento;
drop procedure if exists sp_obtenerclientesmembresias;
drop procedure if exists sp_obtenerclientesresumen;
drop procedure if exists sp_obtenerejercicios;
drop procedure if exists sp_obtenerhistorialmembresia;
drop procedure if exists sp_obtenermembresiasproximasvencer;
drop procedure if exists sp_obtenerusuarioconnombre;
drop procedure if exists sp_obtenerusuariosconnombre;
drop procedure if exists sp_obtenerwods;

-- 3) crear los sps con el nombre camelcase correcto
delimiter $$

create procedure sp_agregarwod(
    in pidentrenador int,
    in pnombre       varchar(100),
    in pobjetivo     varchar(255),
    in pimagen       varchar(255),
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

    insert into rutina (id_entrenador, nombre, objetivo, imagen)
    values (pidentrenador, pnombre, pobjetivo, pimagen);

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
end $$

create procedure sp_obtenerwods()
begin
    select
        r.id_rutina      as idrutina,
        r.nombre         as nombre,
        r.objetivo       as objetivo,
        r.imagen         as imagen,
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
end $$

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

create procedure sp_eliminarequipo(
    in p_id_equipo int
)
begin
    delete from equipo
    where id_equipo = p_id_equipo;
end $$

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
        tipo,
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

create procedure sp_eliminarmantenimiento(
    in p_id_mantenimiento int
)
begin
    delete from mantenimiento
    where id_mantenimiento = p_id_mantenimiento;
end $$

create procedure sp_actualizarcontraseña(
    in pidusuario int,
    in ppasswordactual varchar(255),
    in ppasswordnueva varchar(255)
)
begin
    declare vpassword varchar(255);

    -- obtener la contraseña actual (hash) del usuario
    select contrasena into vpassword
    from usuario
    where id_usuario = pidusuario;

    -- validar si coincide el hash
    if vpassword = sha2(ppasswordactual,256) then
        update usuario
        set contrasena = sha2(ppasswordnueva, 256)
        where id_usuario = pidusuario;
    else
        signal sqlstate '45000'
            set message_text = 'la contraseña actual no es correcta';
    end if;
end $$

create procedure sp_agregarusuario(
    in pnombre varchar(100),
    in ptelefono varchar(20),
    in pcorreo varchar(100),
    in prol varchar(100),
    in pusername varchar(100)
)
begin
	if exists (select 1 from usuario where username = pusername) then
        signal sqlstate '45000'
            set message_text = 'revisa el user, ya que se encuentra duplicado';
    else
		insert into usuario (username, contrasena, rol)
		values (pusername, sha2(pusername, 256), prol);
		set @nuevousuarioid = last_insert_id();
        
        if prol = 'admin' or prol = 'reception' then
			insert into administrador (id_usuario, nombre, telefono, correo)
			values (@nuevousuarioid, pnombre, ptelefono, pcorreo);
        
        else
			insert into entrenador (id_usuario, nombre, telefono, correo)
			values (@nuevousuarioid, pnombre, ptelefono, pcorreo);
        
        end if;
	end if;
end $$

create procedure sp_editarusuario(
    in pnombre varchar(100),
    in ptelefono varchar(20),
    in pcorreo varchar(100),
    in prol varchar(100),
    in pusername varchar(100)
)
begin
    declare nuevousuarioid int;

    -- verificar si existe el usuario
    select id_usuario into nuevousuarioid
    from usuario
    where username = pusername;

    if nuevousuarioid is null then
        signal sqlstate '45000'
            set message_text = 'revisa el user, ya que no se encuentra';
    else
        update usuario
        set rol = prol
        where id_usuario = nuevousuarioid;

        case 
        when prol = 'admin' or prol = 'reception' then
            if exists (select 1 from administrador where id_usuario = nuevousuarioid) then
                update administrador
                set nombre = pnombre, telefono = ptelefono, correo = pcorreo
                where id_usuario = nuevousuarioid;
            else
                insert into administrador (id_usuario, nombre, telefono, correo)
                values (nuevousuarioid, pnombre, ptelefono, pcorreo);
            end if;
		when prol = 'user' then
            if exists (select 1 from cliente where id_usuario = nuevousuarioid) then
                update cliente
                set nombre = pnombre, telefono = ptelefono, correo = pcorreo
                where id_usuario = nuevousuarioid;
            else
                insert into cliente (id_usuario, nombre, telefono, correo)
                values (nuevousuarioid, pnombre, ptelefono, pcorreo);
            end if;
        when prol = 'trainer' then
            if exists (select 1 from entrenador where id_usuario = nuevousuarioid) then
                update entrenador
                set nombre = pnombre, telefono = ptelefono, correo = pcorreo
                where id_usuario = nuevousuarioid;
            else
                insert into entrenador (id_usuario, nombre, telefono, correo)
                values (nuevousuarioid, pnombre, ptelefono, pcorreo);
            end if;
		end case;
    end if;
end $$

create procedure sp_obtenerusuarioconnombre(in pid int)
begin
    select u.username,
           u.rol,
           case 
               when u.rol = 'admin' or u.rol = 'reception' then a.nombre
               when u.rol = 'user' then c.nombre
               when u.rol = 'trainer' then e.nombre
               else ''
           end as nombre,
           case 
               when u.rol = 'admin' or u.rol = 'reception' then a.telefono
               when u.rol = 'user' then c.telefono
               when u.rol = 'trainer' then e.telefono
               else ''
           end as telefono,
           case 
               when u.rol = 'admin' or u.rol = 'reception' then a.correo
               when u.rol = 'user' then c.correo
               when u.rol = 'trainer' then e.correo
               else ''
           end as correo
    from usuario u
    left join administrador a on u.id_usuario = a.id_usuario
    left join cliente c on u.id_usuario = c.id_usuario
    left join entrenador e on u.id_usuario = e.id_usuario
    where u.id_usuario = pid;
end $$

create procedure sp_obtenerusuariosconnombre()
begin
    select u.username,
           u.rol,
           case 
               when u.rol = 'admin' or u.rol = 'reception' then a.nombre
               when u.rol = 'user' then c.nombre
               when u.rol = 'trainer' then e.nombre
               else ''
           end as nombre,
           case 
               when u.rol = 'admin' or u.rol = 'reception' then a.telefono
               when u.rol = 'user' then c.telefono
               when u.rol = 'trainer' then e.telefono
               else ''
           end as telefono,
           case 
               when u.rol = 'admin' or u.rol = 'reception' then a.correo
               when u.rol = 'user' then c.correo
               when u.rol = 'trainer' then e.correo
               else ''
           end as correo
    from usuario u
    left join administrador a on u.id_usuario = a.id_usuario
    left join cliente c on u.id_usuario = c.id_usuario
    left join entrenador e on u.id_usuario = e.id_usuario;
end $$

create procedure sp_agregarcliente(
    in pnombre varchar(100),
    in pcedula varchar(20),
    in ptelefono varchar(20),
    in pcorreo varchar(100),
    in pfechanacimiento date,
    in pestado varchar(20)
)
begin
	if exists (select 1 from usuario where username = pcedula) then
        signal sqlstate '45000'
            set message_text = 'revisa el cliente, ya que se encuentra duplicado';
    else
		insert into usuario (username, contrasena, rol)
		values (pcedula, sha2(pcedula, 256), 'user');
		set @nuevousuarioid = last_insert_id();
		insert into cliente (id_usuario, nombre, cedula, telefono, correo, fecha_nacimiento, estado)
		values (@nuevousuarioid, pnombre, pcedula, ptelefono, pcorreo, pfechanacimiento, pestado);
	end if;
end $$

create procedure sp_obtenerclientesresumen()
begin
    select 
        c.*,
        cm.estado as estadomembresia,
        cm.fecha_fin as vencimiento
    from cliente c
    left join cliente_membresia cm on c.id_cliente = cm.id_cliente
    left join membresia m on cm.id_membresia = m.id_membresia;
end $$

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
    
end $$

create procedure sp_actualizarestadosmembresias()
begin
    -- actualizar a 'vencida' si la fecha_fin ya pasó y no está suspendida
set sql_safe_updates = 0;

update cliente_membresia
set estado = 'vencida'
where fecha_fin < curdate()
  and estado not in ('suspendida', 'vencida');

set sql_safe_updates = 1;

end $$

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
end $$

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
end $$

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
end $$

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

end $$

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
end $$

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
end $$

create procedure sp_obtenerejercicios()
begin
    select
        id_ejercicio   as idejercicio,
        nombre         as nombre,
        grupo_muscular as grupomuscular
    from ejercicio
    order by nombre asc;
end $$


delimiter ;
