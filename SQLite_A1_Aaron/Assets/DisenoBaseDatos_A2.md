# Diseno de la Base de Datos - Actividad 2: Inventario RPG

## Modelo Entidad-Relacion

### Entidades

#### Usuarios  (heredada de Actividad 1)
| Campo    | Tipo    | Restricciones                  |
|----------|---------|-------------------------------|
| id       | INTEGER | PK, AUTOINCREMENT              |
| usuario  | TEXT    | NOT NULL, UNIQUE               |
| password | TEXT    | NOT NULL, CHECK(length >= 8)   |

#### Objetos  (catalogo maestro)
| Campo       | Tipo    | Restricciones         |
|-------------|---------|----------------------|
| id          | INTEGER | PK, AUTOINCREMENT     |
| nombre      | TEXT    | NOT NULL, UNIQUE      |
| descripcion | TEXT    | NOT NULL, DEFAULT ''  |
| tipo        | TEXT    | NOT NULL (Arma, Armadura, Pocion, Material, Especial, Accesorio, Municion) |
| acumulable  | INTEGER | NOT NULL, 0=false 1=true |

#### Inventario  (slots del inventario de cada usuario)
| Campo      | Tipo    | Restricciones                           |
|------------|---------|----------------------------------------|
| id         | INTEGER | PK, AUTOINCREMENT                       |
| usuario_id | INTEGER | FK -> Usuarios(id) ON DELETE CASCADE    |
| objeto_id  | INTEGER | FK -> Objetos(id)  ON DELETE CASCADE    |
| cantidad   | INTEGER | NOT NULL DEFAULT 1, CHECK(cantidad>=0)  |
|            |         | UNIQUE(usuario_id, objeto_id)           |

#### HistorialUso  (AMPLIACION - registra cada uso de un objeto)
| Campo         | Tipo    | Restricciones                              |
|---------------|---------|-------------------------------------------|
| id            | INTEGER | PK, AUTOINCREMENT                          |
| inventario_id | INTEGER | FK -> Inventario(id) ON DELETE CASCADE     |
| fecha_uso     | TEXT    | NOT NULL (formato ISO: yyyy-MM-dd HH:mm:ss)|
| nota          | TEXT    | DEFAULT ''                                 |

---

## Relaciones

1. **Usuarios -- Inventario** (1:N)
   Un usuario tiene muchos slots de inventario.
   Cada slot pertenece exactamente a un usuario.

2. **Objetos -- Inventario** (1:N)
   Un objeto puede aparecer en los inventarios de muchos usuarios.
   Cada slot referencia exactamente un objeto.

   > Juntas, las relaciones 1 y 2 resuelven la relacion N:M entre Usuarios y Objetos
   > mediante la tabla intermedia Inventario.

3. **Inventario -- HistorialUso** (1:N) - AMPLIACION
   Un slot de inventario puede tener muchos registros de uso.
   Cada registro de uso pertenece exactamente a un slot de inventario.

---

## Diagrama simplificado

```
Usuarios (1) ---< Inventario >--- (N) Objetos
                      |
                      | (1)
                      |
                   HistorialUso (N)  [AMPLIACION]
```

---

## Normalizacion

### 1FN
Todos los atributos son atomicos.
No hay grupos repetitivos.

### 2FN
Todas las tablas tienen PK simple o compuesta.
En Inventario: cantidad depende de (usuario_id, objeto_id), no de parte de la clave.

### 3FN
No hay dependencias transitivas:
- En Inventario, los atributos del objeto (nombre, tipo...) estan en la tabla Objetos, no duplicados.
- En HistorialUso, los datos del inventario/objeto no se repiten.

---

## Justificacion de la Ampliacion (HistorialUso)

La tabla **HistorialUso** aporta informacion que no existe en ninguna otra tabla:
- Registro temporal de cada interaccion del usuario con un objeto.
- Permite auditar el uso de pociones, armas, etc.
- Es una relacion nueva (1:N con Inventario) que justifica una tabla adicional.
- Accesible desde Unity mediante InventarioDAL.RegistrarUso() y InventarioDAL.ObtenerHistorial().

---

## SQL de creacion (resumen)

```sql
-- Tabla heredada de Actividad 1
CREATE TABLE IF NOT EXISTS Usuarios (
    id       INTEGER PRIMARY KEY AUTOINCREMENT,
    usuario  TEXT    NOT NULL UNIQUE,
    password TEXT    NOT NULL CHECK (length(password) >= 8)
);

-- Catalogo maestro de objetos
CREATE TABLE IF NOT EXISTS Objetos (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre      TEXT    NOT NULL UNIQUE,
    descripcion TEXT    NOT NULL DEFAULT '',
    tipo        TEXT    NOT NULL DEFAULT 'General',
    acumulable  INTEGER NOT NULL DEFAULT 1
);

-- Inventario por usuario (N:M resuelta)
CREATE TABLE IF NOT EXISTS Inventario (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    usuario_id INTEGER NOT NULL,
    objeto_id  INTEGER NOT NULL,
    cantidad   INTEGER NOT NULL DEFAULT 1 CHECK(cantidad >= 0),
    FOREIGN KEY (usuario_id) REFERENCES Usuarios(id) ON DELETE CASCADE,
    FOREIGN KEY (objeto_id)  REFERENCES Objetos(id)  ON DELETE CASCADE,
    UNIQUE(usuario_id, objeto_id)
);

-- Historial de uso (AMPLIACION)
CREATE TABLE IF NOT EXISTS HistorialUso (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    inventario_id INTEGER NOT NULL,
    fecha_uso     TEXT    NOT NULL,
    nota          TEXT    DEFAULT '',
    FOREIGN KEY (inventario_id) REFERENCES Inventario(id) ON DELETE CASCADE
);
```

---

## Usuario predefinido para entrega

| Campo    | Valor      |
|----------|------------|
| usuario  | admin      |
| password | admin1234  |

Se inserta automaticamente al crear la BD si no existe:
```sql
INSERT OR IGNORE INTO Usuarios(usuario, password) VALUES(''admin'', ''admin1234'');
```
