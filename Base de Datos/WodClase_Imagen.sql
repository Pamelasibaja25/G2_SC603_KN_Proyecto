USE DB_Orion_Fit;
/* imagen del tipo de entrenamiento */
alter table rutina add column imagen varchar(255) null;

/* vinculo opcional entre clase y wod */
alter table clase add column id_rutina int null;

alter table clase
    add constraint fk_clase_rutina foreign key (id_rutina) references rutina(id_rutina);

drop procedure if exists sp_obtenerwods;
drop procedure if exists sp_agregarwod;

delimiter $$

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
end$$

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
end$$

delimiter ;
