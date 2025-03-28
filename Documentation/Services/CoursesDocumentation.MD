## 🔗 Navegación  

⬅️ [Endpoints](./Services.index.md "Services")
📖 [Documentacion](../index.MD "Docuemntacion Home")  
🏠 [Home](../../README.MD "Home")  
📖 [Endpoints](./Services.index.md "Services")  
👥 [Contribution](../../contribution.MD "Contribution")  


# 📌 Courses - Endpoints

## 📖 Descripción

**Course:**: Un curso se trata de una clase la cual imparte un docente en un centro.
En este endpoint se permite gestionar los cursos que un usuario puede crear, editar , obtener y eliminar.
Existe parte de codigo relacionado a la configuracion de un curso "CourseSetting" cuando se crea un curso

> [!NOTE]  
> Una consulta Protegida.  
> Los roles que tienen acceso son los siguientes **[ USER ]**. 

---

## 📌 Índice de Endpoints

- :small_blue_diamond: [Obtener todos las Courses de un Usuario](#-obtener-todos-los-courses)
- :small_blue_diamond: [Crear un Course](#-crear-un-nuevo-course)
- :small_blue_diamond: [Obtener un Course por ID](#-obtener-un-course-por-id)
- :small_blue_diamond: [Actualizar un Course por ID](#-actualizar-un-course-por-id)
- :small_blue_diamond: [Eliminar un Course por ID](#️-eliminar-un-course-por-id)

---


## :books: Obtener todos los Courses

Al utilizar este endpoint realiza un llamado a la base de datos, y este busca todas los courses del usuario.   
Se encuentra paginado con `10` items por pagina por el momento.

> [!WARNING]   
> Este Metodo retorna tanto cursos que esten activos, como cursos que no estan activos, manejar con discreción por parte de FE.

Una Clase se Considera Activa siempre que su centro no este Archivado o su estado marque lo contrario.
Una clase se considera activa si su fecha limite aun no ha terminado.

### 🔗 Endpoint
```http
GET /api/courses
```

### :package: Parámetros de consulta (Query Parameters)
| Parámetro    | Tipo             | Descripción |
|-------------|----------------|-------------|
| `searchTerm` | `string` | (Opcional) Permite buscar Courses por nombre o codigo del mismo. Valor por defecto: "" |
| `page` | `integer` | (Opcional) Indica el número de página para la paginación. Valor por defecto: `1`. |

📌 **Ejemplo de solicitud con parámetros:**  
```http
GET api/courses?searchTerm&page=1
```

### 📤 Respuesta
Si la solicitud es exitosa, el servidor responderá con un código `200 OK` y un JSON con la lista de Cursos.

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
        "id": "4b28cfd7-c7e0-4e6a-6fab-08dd6cb606a8",
        "name": "Alemane",
        "section": "C",
        "startTime": "10:00:00",
        "finishTime": "11:00:00",
        "code": "AL-102",
        "isActive": true,
        "centerId": "42ac6409-53d6-429b-9414-ad4aaf153fcf",
        "settingId": "e1dcb64b-63c9-4d9a-d4db-08dd6cb60694",
        "settingName": "Config 6",
        "scoreType": "gold_pts",
        "startDate": "2025-04-26T04:28:54.137",
        "endDate": "2025-05-26T04:28:54.137",
        "minimumGrade": 50,
        "maximumGrade": 100,
        "minimumAttendanceTime": 10,
        "isOriginal": false
      },
      {
        "id": "a2b3c4d5-6e7f-3a8b-9c0d-1e2f3a4b5c6d",
        "name": "Derecho Civil",
        "section": "2200",
        "startTime": "00:00:00",
        "finishTime": "00:00:00",
        "code": "DC-220",
        "isActive": true,
        "centerId": "550e8400-e29b-41d4-a716-446655440000",
        "settingId": "f47c3c99-d52d-4f33-95f1-63e4cd07ab39",
        "settingName": "Configuración 1",
        "scoreType": "weighted_pts",
        "startDate": "2025-01-01T00:00:00",
        "endDate": "0001-01-01T00:00:00",
        "minimumGrade": 65,
        "maximumGrade": 0,
        "minimumAttendanceTime": 80,
        "isOriginal": true
      },
      {
        "id": "e0f1a2b3-4c5d-1e6f-7a8b-9c0d1e2f3a4b",
        "name": "Filosofía",
        "section": "2000",
        "startTime": "00:00:00",
        "finishTime": "00:00:00",
        "code": "FL-200",
        "isActive": false,
        "centerId": "550e8400-e29b-41d4-a716-446655440000",
        "settingId": "00000000-0000-0000-0000-000000000000",
        "settingName": null,
        "scoreType": null,
        "startDate": "0001-01-01T00:00:00",
        "endDate": "0001-01-01T00:00:00",
        "minimumGrade": 0,
        "maximumGrade": 0,
        "minimumAttendanceTime": 0,
        "isOriginal": false
      },
    ]
  },
  "message": "Cursos encontrados correctamente",
  "statusCode": 200,
  "status": true
}
```

### 🔄 Explicación de los campos Retornados  

| Campo | Tipo | Descripción | 
|--------|------|-------------|
| `data` | `object` | Contiene la información paginada de los cursos. |
| `data.hasNextPage` | `boolean` | Indica si hay una página siguiente en la paginación. |
| `data.hasPreviousPage` | `boolean` | Indica si hay una página anterior en la paginación. |
| `data.currentPage` | `integer` | Número de la página actual. |
| `data.pageSize` | `integer` | Cantidad de elementos por página. |
| `data.totalItems` | `integer` | Número total de elementos disponibles. |
| `data.totalPages` | `integer` | Número total de páginas disponibles. |
| `data.items` | `array` | Lista de cursos disponibles. |
| `data.items[n].id` | `string (UUID)` | Identificador único del curso. |
| `data.items[n].name` | `string` | Nombre del curso. |
| `data.items[n].section` | `string` | Sección del curso. |
| `data.items[n].startTime` | `string (HH:mm:ss)` | Hora de inicio de la clase. |
| `data.items[n].finishTime` | `string (HH:mm:ss)` | Hora de fin de la clase. |
| `data.items[n].code` | `string` | Código del curso. |
| `data.items[n].isActive` | `bool` | Estado del curso. |
| `data.items[n].centerId` | `string (UUID)` | Identificador único del centro al que pertenece la clase. |
| `data.items[n].settingId` | `string (UUID)` | Identificador único de la configuración del curso. |
| `data.items[n].settingName` | `string` | Nombre de la configuración del curso. |
| `data.items[n].scoreType` | `string` | Modo/Tipo de puntaje de la clase. |
| `data.items[n].startDate` | `string (ISO 8601)` | Fecha de inicio del curso. |
| `data.items[n].endDate` | `string (ISO 8601)` | Fecha de fin del curso. |
| `data.items[n].minimumGrade` | `float` | Puntaje mínimo que tiene el curso. |
| `data.items[n].maximumGrade` | `float` | Puntaje máximo que tiene el curso. |
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


## :heavy_plus_sign: Crear un nuevo course

Este endpoint permite la creación de un nuevo Course dentro del sistema.

### 🔗 Endpoint
```http
POST api/courses
```

📌 **Descripción:** Se envía un objeto JSON con los detalles del Course a crear. El sistema validará que: No haya inconvenientes en las fechas y horas, no haya inconvenientes en la cantidad de puntos, o que los datos de la configuración ingresada ya exista

---

### 📥 Request Body (application/json)
El cuerpo de la solicitud debe contener los siguientes campos:

```json
{
  "name": "string",
  "section": "string",
  "startTime": "string",
  "finishTime": "string",
  "code": "string",
  "isActive": true,
  "centerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "settingId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "settingName": "string",
  "scoreType": "string",
  "startDate": "2025-03-27T22:31:58.615Z",
  "endDate": "2025-03-27T22:31:58.615Z",
  "minimumGrade": 100,
  "maximumGrade": 100,
  "minimumAttendanceTime": 59
}
```

### 📖 Descripción de los campos
| Campo | Tipo | Requerido | Descripción |
|--------|------|----------|-------------|
| `name` | `string` | ✅ | Nombre del curso. |
| `section` | `string` | ❌ | Nombre de la sección (máximo 4 carácteres). |
| `startTime` | `string (HH:mm:ss)` | ✅ | Hora de inicio del curso. |
| `finishTime` | `string (HH:mm:ss)` | ✅ | Hora de fin del curso. |
| `code` | `string` | ❌ | Codigo de la clase (máximo 15 carácteres). |
| `isActive` | `bool` | ✅ | Estado del curso. |
| `centerId` | `string (UUID)` | ✅ | Identificador del curso al que pertenece el curso. |
| `SettingId` | `string (UUID)` | ❌ | Identificador de la configuración del curso que se le aplicará (si no se seleccionó una configuración existente, enviar el valor como `null`). |
| `settingName` | `string` | ✅ | Nombre de la configuración del curso. |
| `scoreType` | `string` | ✅ | Indica el tipo de puntaje que tiene la configuración del curso. |
| `startDate` | `string (ISO 8601)` | ✅ | Fecha de inicio en la es aplicable la configuración del curso. |
| `endDate` | `string (ISO 8601)` | ✅ | Fecha de fin en la es aplicable la configuración del curso. |
| `minimumGrade` | `float` | ✅ | Puntaje mínimo que tendrá la configuración del curso. |
| `maximumGrade` | `float` | ✅ | Puntaje màximo que tendrá la configuración del curso. |
| `minimumAttendanceTime` | `integer` | ✅ | Tiempo (en minutos) que tendrá un estudiante para marcar asistencia. |

---

### 📤 Respuesta esperada
Si la solicitud es exitosa, el servidor responderá con un código `200 OK` y devolverá el Course creada.

#### ✅ Ejemplo de respuesta (200 OK)
```json
{
  "data": {
    "id": "e1e663a2-5eee-4d28-aa29-08dd6d803382",
    "name": "Curso Prueba",
    "section": "2D",
    "startTime": "13:00:00",
    "finishTime": "14:00:00",
    "code": "DH-AS",
    "isActive": true,
    "centerId": "42ac6409-53d6-429b-9414-ad4aaf153fcf",
    "settingId": "4e542f6b-2dc1-42e9-5b81-08dd6d803376",
    "settingName": "Config Prueba",
    "scoreType": "gold_pts",
    "startDate": "2025-01-26T04:28:54.137Z",
    "endDate": "2025-03-26T04:28:54.137Z",
    "minimumGrade": 70,
    "maximumGrade": 100,
    "minimumAttendanceTime": 10,
    "isOriginal": false
  },
  "message": "CNS-112: Nota de curso creada exitosamente.",
  "statusCode": 201,
  "status": true
}
```

### 🔄 Explicación de los campos Retornados
| Campo | Tipo | Descripción |
|--------|------|-------------|
| `data.id` | `string (UUID)` |  Identificador del curso. |
| `data.name` | `string` |  Nombre del curso. |
| `data.section` | `string` |  Sección del curso. |
| `data.startTime` | `string (HH:mm:ss)` |  Hora de inicio del curso. |
| `data.finishTime` | `string (HH:mm:ss)` |  Hora de fin del curso. |
| `data.code` | `string` |  Código del curso. |
| `data.isActive` | `bool` |  Estado del curso. |
| `data.centerId` | `string (UUID)` |  Identificador del centro al que pertenece el curso.
| `data.settingId` | `string (UUID)` |  Identificador de la configuración del curso.
| `data.settingName` | `string` |  Nombre de la configuración del curso. |
| `data.scoreType` | `string` |  Indica el tipo de puntaje que tiene la configuración del curso. |
| `data.startDate` | `string (ISO 8601)` |  Fecha de inicio en la es aplicable la configuración del curso. |
| `data.endDate` | `string (ISO 8601)` |  Fecha de fin en la es aplicable la configuración del curso. |
| `data.minimumGrade` | `float` |  Puntaje mínimo que tendrá la configuración del curso. |
| `data.maximumGrade` | `float` |  Puntaje màximo que tendrá la configuración del curso. |
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

> Antes de crear el curso se crea una configuración que sera unica para el curso (ya que sea que se envie una seleccionada de las originales o que se genere según los datos que se envíen del apartado de configuración cuando se cree el curso)

> De parte de FE considerar los siguientes casos para el settingId: si el usuario seleccionó un settingId a aplicar, los campos de la configuración deberan ser bloqueados ya que se seleccionó una configuración existente, y solo estaran habilidados cuando no se haya seleccionado ningun settingId, es decir este valor sera null a la hora de enviar la petición


---


## :mag: Obtener un course por ID

### 🔗 Endpoint
```http
GET /api/courses/{id}
```

### 📄 Descripción  

Este endpoint permite obtener la información detallada de un curso específico y su configuración a partir de su ID.

### :package: Parámetros de ruta

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `id` | `string (UUID)` | Identificador único del curso a buscar. |

### 📤 Respuesta
Si la actualización es exitosa, el servidor responderá con un código `200 OK` y un JSON con la etiqueta actualizada.

```json
{
  "data": {
    "id": "4b28cfd7-c7e0-4e6a-6fab-08dd6cb606a8",
    "name": "Curso",
    "section": "C",
    "startTime": "10:00:00",
    "finishTime": "11:00:00",
    "code": "AL102",
    "isActive": true,
    "centerId": "42ac6409-53d6-429b-9414-ad4aaf153fcf",
    "settingId": "e1dcb64b-63c9-4d9a-d4db-08dd6cb60694",
    "settingName": "Configuracion",
    "scoreType": "weighted_pts",
    "startDate": "2025-04-26T04:28:54.137",
    "endDate": "2025-05-26T04:28:54.137",
    "minimumGrade": 50,
    "maximumGrade": 100,
    "minimumAttendanceTime": 10,
    "isOriginal": false
  },
  "message": "CNS-125: Se encontraron las notas de curso.",
  "statusCode": 200,
  "status": true
}
```

### 🔄 Explicación de los campos Retornados  

| Campo | Tipo | Descripción | 
|--------|------|-------------|
| `data.id` | `string (UUID)` | Identificador único del curso. |
| `data.name` | `string` | Nombre del curso. |
| `data.section` | `string` | Sección del curso. |
| `data.startTime` | `string (HH:mm:ss)` | Hora de inicio de la clase. |
| `data.finishTime` | `string (HH:mm:ss)` | Hora de fin de la clase. |
| `data.code` | `string` | Código del curso. |
| `data.isActive` | `bool` | Estado del curso. |
| `data.centerId` | `string (UUID)` | Identificador único del centro al que pertenece la clase. |
| `data.settingId` | `string (UUID)` | Identificador único de la configuración del curso. |
| `data.settingName` | `string` | Nombre de la configuración del curso. |
| `data.scoreType` | `string` | Modo/Tipo de calificación de la clase. |
| `data.StartDate` | `string (ISO 8601)` | Fecha de inicio del curso. |
| `data.EndDate` | `string (ISO 8601)` | Fecha de fin del curso. |
| `data.minimumGrade` | `float` | Puntaje mínimo que tiene el curso. |
| `data.maximumGrade` | `float` | Puntaje máximo que tiene el curso. |
| `data.minimumAttendanceTime` | `integer` | Tiempo mínimo (en minutos) para que un estudiante pueda marcar asistencia. |
| `data.isOriginal` | `bool` | Indica si la configuración del curso es original o copia. |
| `message` | `string` | Mensaje de respuesta del servidor. |
| `statusCode` | `integer` | Código de estado HTTP de la respuesta. |
| `status` | `boolean` | Indica si la respuesta fue exitosa (`true`) o no (`false`). |

---

### ⚠️ Posibles errores

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Datos inválidos en la solicitud. |
| `404 Not Found` | El curso con el ID proporcionado no existe. |
| `500 Internal Server Error` | Error interno del servidor. |
| `401 Unauthorized` | No se encuentra Autorizado a usar este metodo.  Para mas Informacion lea [Login Documentation](./AuthDocumentation.MD) |
---

### 📌 Notas  
- 🆔 **El ID del curso debe ser un string (UUID) válido**.

---

## :arrows_counterclockwise: Actualizar un course por ID

### 🔗 Endpoint
```http
PUT /api/courses/{id}
```

### 📝 Descripción
Este endpoint permite actualizar un curso existente mediante su `id`. Se debe enviar un cuerpo de solicitud con los nuevos valores del curso

### :package: Parámetros de ruta

| Nombre | Tipo | Ubicación | Descripción |
|--------|------|-----------|-------------|
| `id` | `string (UUID)` | `path` | Identificador único del curso a actualizar. |

### 📥 Cuerpo de la solicitud (Request Body)

```json
{
  "name": "string",
  "section": "string",
  "startTime": "string",
  "finishTime": "string",
  "code": "string",
  "isActive": true,
}
```

### 📖 Descripción de los campos
| Campo | Tipo | Descripción |
|--------|------|-------------|
| `data.id` | `string (UUID)` |  Identificador único del curso. |
| `data.name` | `string` |  Nombre del curso. |
| `data.section` | `string` |  Sección del curso. |
| `data.startTime` | `string (HH:mm:ss)` |  Hora de inició del curso. |
| `data.finishTime` | `string (HH:mm:ss)` | Hora de fin del curso. |
| `data.code` | `string` |  Codigo del curso. |
| `data.isActive` | `bool` |  Estado del curso. |

### 📤 Respuesta esperada
Si la solicitud es exitosa, el servidor responderá con un código `200 OK` y devolverá el Course editado.

```json
{
  "data": {
    "id": "e1e663a2-5eee-4d28-aa29-08dd6d803382",
    "name": "Prueba2Edit",
    "section": "V0",
    "startTime": "12:00:00",
    "finishTime": "13:00:00",
    "code": null,
    "isActive": false,
    "centerId": "42ac6409-53d6-429b-9414-ad4aaf153fcf",
    "settingId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "settingName": "string",
    "scoreType": "string",
    "startDate": "2025-03-27T23:20:31.432Z",
    "endDate": "2025-03-27T23:20:31.432Z",
    "minimumGrade": 60,
    "maximumGrade": 100,
    "minimumAttendanceTime": 10,
    "isOriginal": false
  },
  "message": "CNS-113: Nota de curso actualizada exitosamente.",
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
- En la edición de un curso, aunque solo se **modifique un campo**, el resto de datos deberá ser igual al registro original que se esta editando, o sea, enviar el DTO con los **datos originales combinado con los datos que se editaron**.
- **El `startTime` y `finishTime` deben estar en formato HH:mm:ss.**
- **El `startTime` y `finishTime` deben tener sentido coherente, es decir `startTime` debe ser menor que `finishTime`**

---
✅ **Ejemplo de Uso**:
```http
PUT /api/courses/e1e663a2-5eee-4d28-aa29-08dd6d803382
```

---



## :wastebasket: Eliminar un course por ID

### 🔗 Endpoint
```http
DELETE /api/courses/{id}
```

### 📝 Descripción
Este endpoint permite eliminar un curso existente mediante su `id`. Al eliminar el curso, **se eliminan también todos los registros asociados al courseId (asistencias, unidades, notas, etc...)**.

### 📥 Parámetros
| Nombre | Tipo | Ubicación | Descripción |
|--------|------|-----------|-------------|
| `id` | `string (UUID)` | `path` | Identificador único del curso a eliminar. |

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
- **Esta acción es irreversible**, asegúrese antes de eliminar un curso.
- **Todas las notas, unidades, asistencias, estudiantes (activos y no activos del curso), registros intermediarios asociados al curso serán eliminadas automáticamente, asi como la configuración de curso asociado será también eliminado**

---
✅ **Ejemplo de Uso**:
```http
DELETE /api/courses/e1e663a2-5eee-4d28-aa29-08dd6d803382
```