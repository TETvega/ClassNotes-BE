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
| `pageSize` | `integer` | (Opcional) Indica el tamaño de número de página de los elementos que se retornarán por página. Valor por defecto: `10`. |

📌 **Ejemplo de solicitud con parámetros:**  
```http
GET api/courses?pageSize=7&page=1&searchTerm
```

### 📤 Respuesta
Si la solicitud es exitosa, el servidor responderá con un código `200 OK` y un JSON con la lista de Cursos.

#### ✅ Ejemplo de respuesta (200 OK)
```json
{
	"data": {
		"hasNextPage": true,
		"hasPreviousPage": false,
		"currentPage": 1,
		"pageSize": 3,
		"totalItems": 15,
		"totalPages": 5,
		"items": [
			{
				"course": {
				  "id": "9cab2766-cb36-4b75-ebec-08dd6f92e2f9",
				  "name": "PruebaEdit",
				  "section": "A",
				  "startTime": "10:00:00",
				  "finishTime": "11:00:00",
				  "code": null,
				  "isActive": true,
				  "centerId": "42ac6409-53d6-429b-9414-ad4aaf153fcf",
				  "settingId": "d58d9c3f-0769-4ce5-a317-08dd6f92e2df"
				},
				"courseSetting": {
				  "id": "d58d9c3f-0769-4ce5-a317-08dd6f92e2df",
				  "name": "Configuración Prueba",
				  "scoreType": "weight_pts",
				  "startDate": "2025-01-01T00:00:00",
				  "endDate": "2025-02-02T00:00:00",
				  "minimumGrade": 60,
				  "maximumGrade": 100,
				  "minimumAttendanceTime": 15,
				  "isOriginal": false
				}
			},
			{
				"course": {
				  "id": "a2b3c4d5-6e7f-3a8b-9c0d-1e2f3a4b5c6d",
				  "name": "Derecho Civil",
				  "section": "2200",
				  "startTime": "10:00:00",
				  "finishTime": "11:00:00",
				  "code": "DC-220",
				  "isActive": false,
				  "centerId": "550e8400-e29b-41d4-a716-446655440000",
				  "settingId": "aa48af7c-783d-496c-a16c-59be000b2784"
				},
				"courseSetting": {
				  "id": "aa48af7c-783d-496c-a16c-59be000b2784",
				  "name": "Configuración 13",
				  "scoreType": "Oro",
				  "startDate": "2025-02-01T00:00:00",
				  "endDate": "2025-11-30T00:00:00",
				  "minimumGrade": 70,
				  "maximumGrade": 0,
				  "minimumAttendanceTime": 80,
				  "isOriginal": true
				}
			},
		]
	},
	"message": "CRS-9012: Los cursos fueron encontrados exitosamente",
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
| `data.items[n].course.id` | `string (UUID)` | Identificador único del curso. |
| `data.items[n].course.name` | `string` | Nombre del curso. |
| `data.items[n].course.section` | `string` | Sección del curso. |
| `data.items[n].course.startTime` | `string (HH:mm:ss)` | Hora de inicio de la clase. |
| `data.items[n].course.finishTime` | `string (HH:mm:ss)` | Hora de fin de la clase. |
| `data.items[n].course.code` | `string` | Código del curso. |
| `data.items[n].course.isActive` | `bool` | Estado del curso. |
| `data.items[n].course.centerId` | `string (UUID)` | Identificador único del centro al que pertenece la clase. |
| `data.items[n].course.settingId` | `string (UUID)` | Identificador único de la configuración del curso. |
| `data.items[n].courseSetting.id` | `string (UUID)` | Identificador único de la configuración del curso. |
| `data.items[n].courseSetting.name` | `string` | Nombre de la configuración del curso. |
| `data.items[n].courseSetting.scoreType` | `string` | Modo/Tipo de puntaje de la clase. |
| `data.items[n].courseSetting.startDate` | `string (ISO 8601)` | Fecha de inicio del curso. |
| `data.items[n].courseSetting.endDate` | `string (ISO 8601)` | Fecha de fin del curso. |
| `data.items[n].courseSetting.minimumGrade` | `float` | Puntaje mínimo que tiene el curso. |
| `data.items[n].courseSetting.maximumGrade` | `float` | Puntaje máximo que tiene el curso. |
| `data.items[n].courseSetting.minimumAttendanceTime` | `integer` | Tiempo mínimo (en minutos) para que un estudiante pueda marcar asistencia. |
| `data.items[n].courseSetting.isOriginal` | `bool` | Indica si la configuración del curso es original o copia. |
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
  "course": {  
    "name": "string",
    "section": "string",
    "startTime": "string",
    "finishTime": "string",
    "code": "string",
    "centerId": "string",
    "settingId": "string"
  },
  "courseSetting": {
    "name": "string",
    "scoreType": "string",
    "startDate": "2025-02-28T00:00:00Z",
    "endDate": "2025-03-28T00:00:00Z",
    "minimumGrade": 70,
    "maximumGrade": 100,
    "minimumAttendanceTime": 10
  },
}
```

### 📖 Descripción de los campos
| Campo | Tipo | Requerido | Descripción |
|--------|------|----------|-------------|
| `course.name` | `string` | ✅ | Nombre del curso. |
| `course.section` | `string` | ❌ | Nombre de la sección (máximo 4 carácteres). |
| `course.startTime` | `string (HH:mm:ss)` | ✅ | Hora de inicio del curso. |
| `course.finishTime` | `string (HH:mm:ss)` | ❌ | Hora de fin del curso. |
| `course.code` | `string` | ❌ | Codigo de la clase (máximo 15 carácteres). |
| `course.centerId` | `string (UUID)` | ✅ | Identificador del curso al que pertenece el curso. |
| `course.settingId` | `string (UUID)` | ❌ | Identificador de la configuración del curso que se le aplicará (si no se seleccionó una configuración existente, enviar el valor como `null`). |
| `courseSetting.name` | `string` | ✅ | Nombre de la configuración del curso. |
| `courseSetting.scoreType` | `string` | ✅ | Indica el tipo de puntaje que tiene la configuración del curso. |
| `courseSetting.startDate` | `string (ISO 8601)` | ✅ | Fecha de inicio en la es aplicable la configuración del curso. |
| `courseSetting.endDate` | `string (ISO 8601)` | ❌ | Fecha de fin en la es aplicable la configuración del curso. |
| `courseSetting.minimumGrade` | `float` | ✅ | Puntaje mínimo que tendrá la configuración del curso. |
| `courseSetting.maximumGrade` | `float` | ✅ | Puntaje màximo que tendrá la configuración del curso. |
| `courseSetting.minimumAttendanceTime` | `integer` | ✅ | Tiempo (en minutos) que tendrá un estudiante para marcar asistencia. |

---

### 📤 Respuesta esperada
Si la solicitud es exitosa, el servidor responderá con un código `200 OK` y devolverá el Course creada.

#### ✅ Ejemplo de respuesta (200 OK)
```json
{
  "data": {
    "course": {
      "id": "9cab2766-cb36-4b75-ebec-08dd6f92e2f9",
      "name": "Prueba",
      "section": null,
      "startTime": "10:00:00",
      "finishTime": null,
      "code": null,
      "isActive": true,
      "centerId": "42ac6409-53d6-429b-9414-ad4aaf153fcf",
      "settingId": "d58d9c3f-0769-4ce5-a317-08dd6f92e2df"
    },
    "courseSetting": {
      "id": "d58d9c3f-0769-4ce5-a317-08dd6f92e2df",
      "name": "Configuración Prueba",
      "scoreType": "weight_pts",
      "startDate": "2025-01-01T00:00:00",
      "endDate": "2025-02-02T00:00:00",
      "minimumGrade": 60,
      "maximumGrade": 100,
      "minimumAttendanceTime": 15,
      "isOriginal": false
    }
  },
  "message": "CRS-9052: Curso creado exitosamente.",
  "statusCode": 201,
  "status": true
}
```

### 🔄 Explicación de los campos Retornados
| Campo | Tipo | Descripción |
|--------|------|-------------|
| `data.course.id` | `string (UUID)` |  Identificador del curso. |
| `data.course.name` | `string` |  Nombre del curso. |
| `data.course.section` | `string` |  Sección del curso. |
| `data.course.startTime` | `string (HH:mm:ss)` |  Hora de inicio del curso. |
| `data.course.finishTime` | `string (HH:mm:ss)` |  Hora de fin del curso. |
| `data.course.code` | `string` |  Código del curso. |
| `data.course.isActive` | `bool` |  Estado del curso. |
| `data.course.centerId` | `string (UUID)` |  Identificador del centro al que pertenece el curso.
| `data.course.settingId` | `string (UUID)` |  Identificador de la configuración del curso.
| `data.courseSetting.id` | `string (UUID)` |  Identificador de la configuración del curso.
| `data.courseSetting.name` | `string` |  Nombre de la configuración del curso. |
| `data.courseSetting.scoreType` | `string` |  Indica el tipo de puntaje que tiene la configuración del curso. |
| `data.courseSetting.startDate` | `string (ISO 8601)` |  Fecha de inicio en la es aplicable la configuración del curso. |
| `data.courseSetting.endDate` | `string (ISO 8601)` |  Fecha de fin en la es aplicable la configuración del curso. |
| `data.courseSetting.minimumGrade` | `float` |  Puntaje mínimo que tendrá la configuración del curso. |
| `data.courseSetting.maximumGrade` | `float` |  Puntaje màximo que tendrá la configuración del curso. |
| `data.courseSetting.minimumAttendanceTime` | `integer` |  Tiempo (en minutos) que tendrá un estudiante para marcar asistencia.
| `data.courseSetting.isOriginal` | `integer` |  Configuración original (deberá retornar false).
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

> Si se seleccionó un settingId la data de la configuración del curso se tiene que copiar tal cual para que se cree el duplicado de forma correcta. O sea la previsualización debe contener los datos de la configuración original seleccionada

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
    "course": {
      "id": "9cab2766-cb36-4b75-ebec-08dd6f92e2f9",
      "name": "PruebaEdit",
      "section": "A",
      "startTime": "10:00:00",
      "finishTime": "11:00:00",
      "code": null,
      "isActive": true,
      "centerId": "42ac6409-53d6-429b-9414-ad4aaf153fcf",
      "settingId": "d58d9c3f-0769-4ce5-a317-08dd6f92e2df"
    },
    "courseSetting": {
      "id": "d58d9c3f-0769-4ce5-a317-08dd6f92e2df",
      "name": "Configuración Prueba",
      "scoreType": "weight_pts",
      "startDate": "2025-01-01T00:00:00",
      "endDate": "2025-02-02T00:00:00",
      "minimumGrade": 60,
      "maximumGrade": 100,
      "minimumAttendanceTime": 15,
      "isOriginal": false
    }
  },
  "message": "CRS-9011: El curso fue encontrado exitosamente.",
  "statusCode": 200,
  "status": true
}
```

### 🔄 Explicación de los campos Retornados  

| Campo | Tipo | Descripción | 
|--------|------|-------------|
| `data.course.id` | `string (UUID)` | Identificador único del curso. |
| `data.course.name` | `string` | Nombre del curso. |
| `data.course.section` | `string` | Sección del curso. |
| `data.course.startTime` | `string (HH:mm:ss)` | Hora de inicio de la clase. |
| `data.course.finishTime` | `string (HH:mm:ss)` | Hora de fin de la clase. |
| `data.course.code` | `string` | Código del curso. |
| `data.course.isActive` | `bool` | Estado del curso. |
| `data.course.centerId` | `string (UUID)` | Identificador único del centro al que pertenece la clase. |
| `data.course.settingId` | `string (UUID)` | Identificador único de la configuración del curso. |
| `data.courseSetting.id` | `string (UUID)` | Identificador único de la configuración del curso. |
| `data.courseSetting.name` | `string` | Nombre de la configuración del curso. |
| `data.courseSetting.scoreType` | `string` | Modo/Tipo de calificación de la clase. |
| `data.courseSetting.StartDate` | `string (ISO 8601)` | Fecha de inicio del curso. |
| `data.courseSetting.EndDate` | `string (ISO 8601)` | Fecha de fin del curso. |
| `data.courseSetting.minimumGrade` | `float` | Puntaje mínimo que tiene el curso. |
| `data.courseSetting.maximumGrade` | `float` | Puntaje máximo que tiene el curso. |
| `data.courseSetting.minimumAttendanceTime` | `integer` | Tiempo mínimo (en minutos) para que un estudiante pueda marcar asistencia. |
| `data.courseSetting.isOriginal` | `bool` | Indica si la configuración del curso es original o copia. |
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
  "isActive": true
}
```

### 📖 Descripción de los campos
| Campo | Tipo | Descripción |
|--------|------|-------------|
| `name` | `string` |  Nombre del curso. |
| `section` | `string` |  Sección del curso. |
| `startTime` | `string (HH:mm:ss)` |  Hora de inició del curso. |
| `finishTime` | `string (HH:mm:ss)` | Hora de fin del curso. |
| `code` | `string` |  Codigo del curso. |
| `isActive` | `bool` |  Estado del curso. |

### 📤 Respuesta esperada
Si la solicitud es exitosa, el servidor responderá con un código `200 OK` y devolverá el Course editado.

```json
{
  "data": {
    "id": "9cab2766-cb36-4b75-ebec-08dd6f92e2f9",
    "name": "PruebaEdit",
    "section": "V0",
    "startTime": "10:00:00",
    "finishTime": "11:00:00",
    "code": null,
    "isActive": true,
    "centerId": "42ac6409-53d6-429b-9414-ad4aaf153fcf",
    "settingId": "d58d9c3f-0769-4ce5-a317-08dd6f92e2df"
  },
  "message": "CRS-9053: Curso actualizado exitosamente.",
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