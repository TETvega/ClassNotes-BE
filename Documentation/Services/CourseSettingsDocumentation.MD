## 🔗 Navegación  

⬅️ [Endpoints](./Services.index.md "Services")
📖 [Documentacion](../index.MD "Docuemntacion Home")  
🏠 [Home](../../README.MD "Home")  
📖 [Endpoints](./Services.index.md "Services")  
👥 [Contribution](../../contribution.MD "Contribution)

# 📌 CourseSettings - Endpoints

## 📖 Descripción

**CourseSetting:**: Una configuración de curso se trata de una configuración o ajuste que se aplica a un curso en especifico
En este endpoint se permite gestionar las configuraciones de cursos que un usuario puede crear, editar, obtener y eliminar.
Las configuraciones de cursos se crean como originales (siempre y cuando no existan en la base de datos), estos registros originales nunca se asocian a ninguna tabla, por cada confiuración original que se quiera aplicar a un curso se creará una copia (o un duplicado) copiando sus campos, cambiando sus propiedades de Id y de IsOriginal. 

> [!NOTE]  
> Una consulta Protegida.  
> Los roles que tienen acceso son los siguientes **[ USER ]**. 

> La configuración por defecto que puede tener un usuario siempre es una original. 
---

## 📌 Índice de Endpoints

- :small_blue_diamond: [Obtener todos las Courses Setting de un Usuario](#️-obtener-todos-los-course-settings)
- :small_blue_diamond: [Crear un Course Setting](#️---crear-un-nuevo-course-setting)
- :small_blue_diamond: [Obtener un Course Setting por ID](#--️-obtener-un-course-setting-por-id)
- :small_blue_diamond: [Actualizar un Course Setting por ID](#--️-actualizar-un-course-setting-por-id)
- :small_blue_diamond: [Eliminar un Course Setting por ID](#️--️-eliminar-un-course-setting-por-id)

---

## :gear: Obtener todos los Course Settings

Al utilizar este endpoint realiza un llamado a la base de datos, y este busca todas las configuraciones de curso que sean originales del usuario.   
Se encuentra paginado con `10` items por pagina por el momento.

> [!IMPORTANT]   
> Solo se retornaran aquellas configuraciones que sean `Originales`, las copias asignadas a cada curso no seran retornadas en el listado.

### 🔗 Endpoint
```http
GET /api/courses_settings
```

### :package: Parámetros de consulta (Query Parameters)
| Parámetro    | Tipo             | Descripción | Valores Aceptados |
|-------------|----------------|-------------|-------------|
| `searchTerm` | `string` | (Opcional) Permite buscar Configuraciones por el tipo de puntación. Valor por defecto: "" | `weighted_pts`, `gold_pts`, "" |
| `page` | `integer` | (Opcional) Indica el número de página para la paginación. Valor por defecto: `1`. | Cualquier entero mayor a 0 |

> [!NOTE]  
> weighted_pts se refiere a PONDERADO. 

> gold_pts se refiere a PUNTOS ORO.

> POSIBLE IMPLEMENTACIÓN DE averaged_pts (no es seguro).

📌 **Ejemplo de solicitud con parámetros:**  
```http
GET api/courses_settings?searchTerm=gold_pts&page=1
```

### 📤 Respuesta
Si la solicitud es exitosa, el servidor responderá con un código `200 OK` y un JSON con la lista de configuraciones.

#### ✅ Ejemplo de respuesta (200 OK)

```json
{
  "data": {
    "hasNextPage": false,
    "hasPreviousPage": false,
    "currentPage": 1,
    "pageSize": 10,
    "totalItems": 3,
    "totalPages": 1,
    "items": [
      {
        "id": "c494dba0-4a0a-4a67-4fe5-08dd6ce23013",
        "name": "Configuración 1",
        "scoreType": "gold_pts",
        "startDate": "2025-01-26T04:28:54.137",
        "endDate": "2025-03-26T04:28:54.137",
        "minimumGrade": 70,
        "maximumGrade": 100,
        "minimumAttendanceTime": 10,
        "isOriginal": true
      },
      {
        "id": "e2218f1f-3c76-4ea2-d4da-08dd6cb60694",
        "name": "Config 6",
        "scoreType": "gold_pts",
        "startDate": "2025-04-26T04:28:54.137",
        "endDate": "2025-05-26T04:28:54.137",
        "minimumGrade": 50,
        "maximumGrade": 100,
        "minimumAttendanceTime": 10,
        "isOriginal": true
      },
      {
        "id": "aa48af7c-783d-496c-a16c-59be000b2774",
        "name": "Configuración 3",
        "scoreType": "gold_pts",
        "startDate": "2025-02-01T00:00:00",
        "endDate": "0001-01-01T00:00:00",
        "minimumGrade": 70,
        "maximumGrade": 100,
        "minimumAttendanceTime": 80,
        "isOriginal": true
      }
    ]
  },
  "message": "CP-244: La configuración de curso fue encontrada.",
  "statusCode": 200,
  "status": true
}
```

### 🔄 Explicación de los campos Retornados  

| Campo | Tipo | Descripción | 
|--------|------|-------------|
| `data` | `object` | Contiene la información paginada de las congifuraciones de los cursos. |
| `data.hasNextPage` | `boolean` | Indica si hay una página siguiente en la paginación. |
| `data.hasPreviousPage` | `boolean` | Indica si hay una página anterior en la paginación. |
| `data.currentPage` | `integer` | Número de la página actual. |
| `data.pageSize` | `integer` | Cantidad de elementos por página. |
| `data.totalItems` | `integer` | Número total de elementos disponibles. |
| `data.totalPages` | `integer` | Número total de páginas disponibles. |
| `data.items` | `array` | Lista de configuraciones de cursos disponibles. |
| `data.items[n].id` | `string (UUID)` | Identificador único de la configuración. |
| `data.items[n].name` | `string` | Nombre de la configuración. |
| `data.items[n].scoreType` | `string` | Modo/Tipo de puntaje de la clase. |
| `data.items[n].startDate` | `string (ISO 8601)` | Fecha de inicio de la configuración del curso |
| `data.items[n].endDate` | `string (ISO 8601)` | Fecha de fin de la configuración del curso. |
| `data.items[n].minimumGrade` | `float` | Puntaje mínimo que tiene la configuración. |
| `data.items[n].maximumGrade` | `float` | Puntaje máximo que tiene la configuración. |
| `data.items[n].minimumAttendanceTime` | `integer` | Tiempo mínimo (en minutos) para que un estudiante pueda marcar asistencia. |
| `data.items[n].isOriginal` | `bool` | Indica si la configuración del curso es original o copia. |
| `message` | `string` | Mensaje de respuesta del servidor. |
| `statusCode` | `integer` | Código de estado HTTP de la respuesta. |
| `status` | `boolean` | Indica si la respuesta fue exitosa (`true`) o no (`false`). |

---

### ⚠️ Posibles errores

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Ocurre si los parámetros enviados son inválidos. |
| `500 Internal Server Error` | Error interno en el servidor. |
| `401 Unauthorized` | No se encuentra Autorizado a usar este metodo.  Para mas Informacion lea [Login Documentation](./AuthDocumentation.MD) |


Ejemplo de respuesta en caso de error:
```json
{
  "message": "Error interno del servidor.",
  "statusCode": 500,
  "status": false
}
```

---

## :hammer_and_wrench: Manejo de Course_Settings Junto a Frontend

Esta data se tiene pensado que estará disponible en un componente Select en el cual se pueda escoger cualquiera de las configuraciones (ya sea para aplicar configuración a un nuevo curso o cuando se quiera seleccionar alguna configuración original para editarla o eliminarla).

```jsx
import React, { useState } from 'react';

const ConfigurationSelector = ({ data }) => {
  const [selectedConfig, setSelectedConfig] = useState('');

  const handleChange = (event) => {
    setSelectedConfig(event.target.value);
    // Aquí podrías llamar a una función prop para pasar la configuración seleccionada al componente padre
    // Ejemplo: onConfigSelect(event.target.value);
  };

  return (
    <div className="config-selector">
      <label htmlFor="config-select">Selecciona una configuración:</label>
      <select 
        id="config-select"
        value={selectedConfig}
        onChange={handleChange}
        className="form-select"
      >
        <option value="">-- Seleccione --</option>
        {data.items.map((config) => (
          <option key={config.id} value={config.id}>
            {config.name}
          </option>
        ))}
      </select>
    </div>
  );
};

export default ConfigurationSelector;
```

Una vez seleccionada una opción, se debera renderizar su contenido en otra vista

### 📝 Notas adicionales

- La paginación ayuda a optimizar el rendimiento en consultas con muchas configuraciones, se recomienda utilzar un select que permita filtrar los resultados ya sea por el tipo de puntaje,.
- La paginación si se considera muy pequeña podría llegar a aumentarse, debido a que este input al ser un scroll con poca información se estarían realizando llamadas al API constantemente
- La paginación de este endpoint por parte de FE se manejaría de forma diferente, ya que no se utilizarían botones en los que se muestre la página actual o cuantas páginas hay en total. Si no que se tendría que implementar algún algoritmo que detecte cuando se esta cerca de acabar los items de la pagina actual y haya siguiente pagina, realicé la petición al BE con el número de pagina que se va a cambiar
- Cuando se tenga que renderizar los datos de una configuración seleccionada se debe realizar un ternario para evaluar el valor de la prop `scoreType` de cada objeto

---

## :gear: + :heavy_plus_sign: Crear un nuevo Course Setting

Este endpoint permite la creación de un nuevo Course Setting dentro del sistema. Este Course Setting no tendrá relación con ningún otro registro de cualquier tabla, si quisiese aplicar esta configuración a un curso se crea una copia que se relaciona, unicamente cambiando su Id y su propiedad IsOriginal.

Ver más información sobre como aplicar un `Course Setting` en la documentación de [CoursesDocumentation](./CoursesDocumentation.MD) 

### 🔗 Endpoint
```http
POST api/courses_settings
```

📌 **Descripción:** Se envía un objeto JSON con los detalles del Course Setting a crear. El sistema validará que: No haya inconvenientes en las fechas, no haya inconvenientes en la cantidad de puntos, o existencia de la misma configuración

---

### 📥 Request Body (application/json)
El cuerpo de la solicitud debe contener los siguientes campos:

```json
{
  "name": "string",
  "scoreType": "string",
  "startDate": "2025-03-27T17:31:53.723Z",
  "endDate": "2025-03-27T17:31:53.723Z",
  "minimumGrade": 100,
  "maximumGrade": 100,
  "minimumAttendanceTime": 59
}
```

### 📖 Descripción de los campos
| Campo | Tipo | Requerido | Descripción |
|--------|------|----------|-------------|
| `name` | `string` | ✅ | Nombre de la configuración. |
| `scoreType` | `string` | ✅ | Indica el tipo de puntaje que tiene la configuración. |
| `startDate` | `string (ISO 8601)` | ✅ | Fecha de inicio en la es aplicable la configuración. |
| `endDate` | `string (ISO 8601)` | ✅ | Fecha de fin en la es aplicable la configuración . |
| `minimumGrade` | `float` | ✅ | Puntaje mínimo que tendrá la configuración. |
| `maximumGrade` | `float` | ✅ | Puntaje màximo que tendrá la configuración. |
| `minimumAttendanceTime` | `integer` | ✅ | Tiempo (en minutos) que tendrá un estudiante para marcar asistencia. |

---

### 📤 Respuesta esperada
Si la solicitud es exitosa, el servidor responderá con un código `200 OK` y devolverá el Course Setting creado.

#### ✅ Ejemplo de respuesta (200 OK)
```json
{
  "data": {
    "id": "1445647a-d5f0-4566-e4ee-08dd6d54587a",
    "name": "Configuración 5 Prueba",
    "scoreType": "weighted_pts",
    "startDate": "2025-01-03T03:03:27.528Z",
    "endDate": "2025-04-03T03:03:27.528Z",
    "minimumGrade": 70,
    "maximumGrade": 100,
    "minimumAttendanceTime": 10,
    "isOriginal": true
  },
  "message": "CP-231: Configuración de curso creada exitosamente.",
  "statusCode": 201,
  "status": true
}
```

### 🔄 Explicación de los campos Retornados

| Campo | Tipo | Descripción |
|--------|------|-------------|
| `data.id` | `string (UUID)` |  Nombre de la configuración. |
| `data.name` | `string` |  Nombre de la configuración. |
| `data.scoreType` | `string` |  Indica el tipo de puntaje que tiene la configuración. |
| `data.startDate` | `string (ISO 8601)` |  Fecha de inicio en la es aplicable la configuración. |
| `data.endDate` | `string (ISO 8601)` |  Fecha de fin en la es aplicable la configuración . |
| `data.minimumGrade` | `float` |  Puntaje mínimo que tendrá la configuración. |
| `data.maximumGrade` | `float` |  Puntaje màximo que tendrá la configuración. |
| `data.minimumAttendanceTime` | `integer` |  Tiempo (en minutos) que tendrá un estudiante para marcar asistencia.
| `message` | `string` | Mensaje de respuesta del servidor. |
| `statusCode` | `integer` | Código de estado HTTP de la respuesta. |
| `status` | `boolean` | Indica si la respuesta fue exitosa (`true`) o no (`false`). |

---

### ⚠️ Posibles errores

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Ocurre si los datos enviados son inválidos o faltan campos requeridos. |
| `500 Internal Server Error` | Error interno del servidor al procesar la solicitud. |
| `401 Unauthorized` | No se encuentra Autorizado a usar este metodo.  Para mas Informacion lea [Login Documentation](./AuthDocumentation.MD) |

📌 **Ejemplo de error si no esta Logeado:**
```json
{
  "message": "No Autorizado",
  "statusCode": 401,
  "status": false
}
```

📌 **Ejemplo de error si hay error de validaciones de fechas:**
```json
{
  "message": "Hay inconsistencias en las fechas ingresadas",
  "statusCode": 400,
  "status": false
}
```

> [!NOTE]  
> La vista de creación de un Course Setting (sin aplicarla directamente a un Course, mientras se esta creando el Course), deberá a aplicarse en una vista apartado externo de un dashboard de un curso. 

> Las Propiedades startDate y endDate se aplican a la configuración, porque al finalizar el lapso de tiempo de las clases en que la configuración original vaya a ser aplicada, cuando se creen nuevas clases en un lapso de tiempo diferente o posterior, se modifiquen estas fechas de la original una vez y aplicar la configuración editada a los cursos/clases que se requieran aplicar. 
---

## :mag: + :gear: Obtener un Course Setting por ID

### 🔗 Endpoint
```http
GET /api/courses_settings/{id}
```

### 📄 Descripción  

Este endpoint permite obtener la información detallada de una configuración específica a partir de su ID.

### 🔍 Parámetros  

| **Nombre** | **Tipo**         | **Ubicación** | **Descripción** |
|-----------|----------------|--------------|----------------|
| `id`      | `string (UUID)` | `path`       | Identificador único de la actividad a consultar. |

### ✅ Respuestas 

#### 🟢 **200 - Success**  
> La solicitud se completó correctamente y devuelve los detalles de la actividad.

```json
{
  "data": {
    "id": "1445647a-d5f0-4566-e4ee-08dd6d54587a",
    "name": "Configuración 5 Prueba",
    "scoreType": "weighted_pts",
    "startDate": "2025-01-03T03:03:27.528",
    "endDate": "2025-04-03T03:03:27.528",
    "minimumGrade": 70,
    "maximumGrade": 100,
    "minimumAttendanceTime": 10,
    "isOriginal": true
  },
  "message": "CP-244: La configuración de curso fue encontrada.",
  "statusCode": 200,
  "status": true
}
```

### 🔄 Explicación de los campos Retornados

| Campo | Tipo | Descripción |
|--------|------|-------------|
| `data.id` | `string (UUID)` |  Nombre de la configuración. |
| `data.name` | `string` |  Nombre de la configuración. |
| `data.scoreType` | `string` |  Indica el tipo de puntaje que tiene la configuración. |
| `data.startDate` | `string (ISO 8601)` |  Fecha de inicio en la es aplicable la configuración. |
| `data.endDate` | `string (ISO 8601)` |  Fecha de fin en la es aplicable la configuración . |
| `data.minimumGrade` | `float` |  Puntaje mínimo que tendrá la configuración. |
| `data.maximumGrade` | `float` |  Puntaje màximo que tendrá la configuración. |
| `data.minimumAttendanceTime` | `integer` |  Tiempo (en minutos) que tendrá un estudiante para marcar asistencia.
| `message` | `string` | Mensaje de respuesta del servidor. |
| `statusCode` | `integer` | Código de estado HTTP de la respuesta. |
| `status` | `boolean` | Indica si la respuesta fue exitosa (`true`) o no (`false`). |

### ⚠️ Posibles errores

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Datos inválidos en la solicitud. |
| `404 Not Found` | La etiqueta con el ID proporcionado no existe. |
| `500 Internal Server Error` | Error interno del servidor. |
| `401 Unauthorized` | No se encuentra Autorizado a usar este metodo.  Para mas Informacion lea [Login Documentation](./AuthDocumentation.MD) |
---

### 📌 Notas  
- 🆔 **El ID de la configuración debe ser un string (UUID) válido**.

---

## :arrows_counterclockwise: + :gear: Actualizar un Course Setting por ID

### 🔗 Endpoint
```http
PUT /api/courses_settings/{id}
```

### 📝 Descripción
Este endpoint permite actualizar una configuración existente mediante su `id`. Se debe enviar un cuerpo de solicitud con los nuevos valores de la configuración

### :package: Parámetros de ruta

| Nombre | Tipo | Ubicación | Descripción |
|--------|------|-----------|-------------|
| `id` | `string (UUID)` | `path` | Identificador único de la configuración a actualizar. |

### 📥 Cuerpo de la solicitud (Request Body)

```json
{
  "name": "string",
  "scoreType": "string",
  "startDate": "2025-03-27T18:49:55.642Z",
  "endDate": "2025-03-27T18:49:55.642Z",
  "minimumGrade": 100,
  "maximumGrade": 100,
  "minimumAttendanceTime": 59
}
```

### 📖 Descripción de los campos
| Campo | Tipo | Requerido | Descripción |
|--------|------|----------|-------------|
| `name` | `string` | ✅ | Nombre de la configuración. |
| `scoreType` | `string` | ✅ | Indica el tipo de puntaje que tiene la configuración. |
| `startDate` | `string (ISO 8601)` | ✅ | Fecha de inicio en la es aplicable la configuración. |
| `endDate` | `string (ISO 8601)` | ✅ | Fecha de fin en la es aplicable la configuración . |
| `minimumGrade` | `float` | ✅ | Puntaje mínimo que tendrá la configuración. |
| `maximumGrade` | `float` | ✅ | Puntaje màximo que tendrá la configuración. |
| `minimumAttendanceTime` | `integer` | ✅ | Tiempo (en minutos) que tendrá un estudiante para marcar asistencia. |

### 📤 Respuesta
Si la actualización es exitosa, el servidor responderá con un código `200 OK` y un JSON con la etiqueta actualizada.

```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "string",
    "scoreType": "gold_pts",
    "startDate": "2025-01-27T18:49:55.644Z",
    "endDate": "2025-04-27T18:49:55.644Z",
    "minimumGrade": 50,
    "maximumGrade": 100,
    "minimumAttendanceTime": 5,
    "isOriginal": true
  },
  "message": "string",
  "statusCode": 200,
  "status": true
}
```

### ⚠️ Posibles Errores
| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Datos inválidos en la solicitud. |
| `404 Not Found` | La configuración con el {`ID`} especificado no existe. |
| `500 Internal Server Error` | Error en el servidor al procesar la solicitud. |
| `401 Unauthorized` | No se encuentra Autorizado a usar este metodo.  Para mas Informacion lea [Login Documentation](./AuthDocumentation.MD) |

### 🔍 Notas Adicionales
- **El campo `id` es obligatorio** y debe enviarse en la URL.
- En la edición de un configuración de curso, aunque solo se **modifique un campo**, el resto de datos deberá ser igual al registro original que se esta editando, o sea, enviar el DTO con los **datos originales combinado con los datos que se editaron**.
- **El `minimumGrade`, `maximumGrade` y `minimumAttendanceTime` no puede ser menor a 0.**
- **El `minimumGrade`, `maximumGrade` debe tener sentido, es decir que el maximo no sea menor que el minimo.**
- **El `startDate` y `endDate` deben estar en formato ISO 8601.**

---
✅ **Ejemplo de Uso**:
```http
PUT /api/courses_settings/1445647a-d5f0-4566-e4ee-08dd6d54587a
```

---

> [!NOTE]  
> Se puede editar tanto configuraciones originales como configuraciones copias.

> Las configuraciones originales se editan en un apartado o vista externo a cualquier vista relacionado a un curso.

> Las configuraciones copias (relacionadas a un curso) se editan en un apartado o vista dentro del dashboard de un curso.

> Las Propiedades startDate y endDate se aplican a la configuración, porque al finalizar el lapso de tiempo de las clases en que la configuración original vaya a ser aplicada, cuando se creen nuevas clases en un lapso de tiempo diferente o posterior, se modifiquen estas fechas de la original una vez y aplicar la configuración editada a los cursos/clases que se requieran aplicar. 

---

## :wastebasket: + :gear: Eliminar un Course Setting por ID

### 🔗 Endpoint
```http
DELETE /api/courses_settings/{id}
```

### 📝 Descripción
Este endpoint permite eliminar una configuración original existente mediante su `id`. Si se intenta eliminar una configuración copia este no permitirá y lanzará un error

### 📥 Parámetros
| Nombre | Tipo | Ubicación | Descripción |
|--------|------|-----------|-------------|
| `id` | `string (UUID)` | `path` | Identificador único de la configuración a eliminar. |

### 📤 Respuesta
Si la eliminación es exitosa, el servidor responderá con un código `200 OK` y un JSON confirmando la eliminación.

```json
{
  "data": null,
  "message": "CNS-114: Nota de curso eliminada exitosamente.",
  "statusCode": 200,
  "status": true
}
```

### ⚠️ Posibles Errores
| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Datos inválidos en la solicitud. |
| `404 Not Found` | La actividad con el ID especificado no existe. |
| `500 Internal Server Error` | Error en el servidor al procesar la solicitud. |
| `401 Unauthorized` | No se encuentra Autorizado a usar este metodo.  Para mas Informacion lea [Login Documentation](./AuthDocumentation.MD) |


### 🔍 Notas Adicionales
- **El campo `id` es obligatorio** y debe enviarse en la URL.
- **Esta acción es irreversible**, asegúrese antes de eliminar una configuración.

---
✅ **Ejemplo de Uso**:
```http
DELETE /api/courses/e1e663a2-5eee-4d28-aa29-08dd6d803382
```
