
## 🔗 Navegación

⬅️ [Endpoints](./Services.index.md "Services")  
📖 [Documentacion](../index.MD "Docuemntacion Home")    
🏠 [Home](../../README.MD "Home")  
📖 [Endpoints](./Services.index.md "Services")  
👥 [Contribution](../../contribution.MD "Contribution")  


# 📂 Activities - Endpoints

## 📖 Descripción
Este conjunto de endpoints permite gestionar las actividades dentro del sistema, incluyendo la creación, obtención, actualización y eliminación de actividades.

> [!NOTE]  
> Una consulta Protegida.  
>Los roles que tienen acceso son los siguientes **[ USER ]**. 

---

## 📌 Índice de Endpoints

- 🔹 [Obtener todas las actividades](#obtener-todas-las-actividades)
- 🔹 [Crear una nueva actividad](#crear-una-nueva-actividad)
- 🔹 [Obtener una actividad por ID](#obtener-una-actividad-por-id)
- 🔹 [Actualizar una actividad](#actualizar-una-actividad)
- 🔹 [Eliminar una actividad](#eliminar-una-actividad)

---

## Obtener todas las actividades

Muestra todas las actividades de la persona que lo esta pidiendo.
Muestra las actividades de los Centros Activos y Clases Activas 

Una Clase se Considera Activa siempre que su centro no este Archivado o su estado marque lo contrario.
Una clase se considera activa si su fecha limite aun no a terminado.

### 🔗 Endpoint
```http
GET /api/activities
```

### 📥 Parámetros de consulta (Query Parameters)

| Parámetro    | Tipo             | Descripción |
|-------------|----------------|-------------|
| `searchTerm` | `string` | (Opcional) Permite buscar actividades por nombre o palabra clave. (Busca en Nombres de clase tambien) |
| `page` | `integer` | (Opcional) Indica el número de página para la paginación. Valor por defecto: `1`. |
| `pageSize` | `integer` | (opcional) Indica la cantidad de Items por pagina. Valor por defecto en `10`. |
| `center` | `string (UUID)` | (opcional) Indica el centro del cual muestra Actividades. |
| `tag` | `string (UUID)` | (opcional) Indica el Tag del cual muestra Actividades. |
| `order` | `string [ (near), (all) , (pending) , (completed) ]` | (opcional) Indica en que orden traer las actividades. Valor por defecto en `near`


#### Parametro *`order`*

>**Ordenamiento de Actividades**  
>
> ***near*** : Actividades Proximas,  Toda actividad cuya fecha no ha vencido se encuentra en Proximos, independientemente de su estado (completado o incompleto) 
> 
> ***all*** : Trae Todas las Actividades independientemente de su fecha o estado.
>
> ***pending*** : Trea todas las actividades con estado Pendiente de Revision. Una actividad pasa a un estado de pendiente cuando las suma de alumnos calificados **(exeptuando los alumnos no activos)** no es igual a la suma de de los Alumnos activos en la clase y su fecha de calificacion es menor a la fecha actual
>
> ***completed*** : Son Todas las Actividades donde la suma de los AlumnosActivos es igual a la Suma de Alumnos Calificados **(Exeptuando los alumnos no activos)**  
>

📌 **Ejemplo de solicitud con parámetros:**  
```http
GET /api/activities?searchTerm=tarea1&page=2&center=10219jj9192h&tag=i91293i3&order=near
```

### 📤 Respuesta
Si la solicitud es exitosa, el servidor responderá con un código `200 OK` y un JSON con la lista de actividades.

#### ✅ Ejemplo de respuesta (200 OK)
```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Examen Final",
      "qualificationDate": "2025-03-15T06:22:42.187Z",
      "tagActivityId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "class": {
        "id": "7e1a4a2b-8923-4b3d-b8e9-4eaa23fbe5b2",
        "name": "Matemáticas Avanzadas"
      },
      "centerId" "7e1a4a2b-8923-4b3d-b8e9-4eaa23fbe5b2"
    }
  ],
  "message": "Lista de actividades obtenida correctamente.",
  "statusCode": 200,
  "status": true
}
```

### 🔄 Explicación de los campos Retornados

| Campo | Tipo | Descripción |
|--------|------|-------------|
| `id` | `string (UUID)` | Identificador único de la actividad. |
| `name` | `string` | Nombre de la actividad. |
| `qualificationDate` | `string (ISO 8601)` | Fecha en la que se calificará la actividad. |
| `tagActivityId` | `string (UUID)` | Identificador de la etiqueta asociada a la actividad. |
| `unitId` | `string (UUID)` | Identificador de la unidad a la que pertenece la actividad. |
| `class.id` | `string (UUID)` | Identificador unico de la Clase a la cual pertenece |
| `class.name` | `string (UUID)` | Nombre de la clase a la cual pertenece
| `centerId` | `string (UUID)` | Identificador unico del centro al cual pertenece esta actividad. | 

---

## ⚠️ Posibles errores

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

## 📝 Notas adicionales

- La paginación ayuda a optimizar el rendimiento en consultas con muchas actividades.
- Se recomienda utilizar el parámetro `order` para filtrar actividades de manera eficiente.
- El campo `isExtra` permite distinguir entre actividades regulares y complementarias.

---
<!-- Creacion de una Actividad -->
## Crear una nueva actividad

Este endpoint permite la creación de una nueva actividad dentro del sistema. Una actividad pertenece a una unidad específica dentro de un curso y esta categorizada con una etiqueta (`tagActivityId`).

---

### 🔗 Endpoint
```http
POST /api/activities
```

📌 **Descripción:** Se envía un objeto JSON con los detalles de la actividad a crear. El sistema validará que: la unidad (`unitId`) exista antes de procesar la solicitud, la actividad
(`tagActivityId`) exista antes de procesar la solicitud

---

### 📥 Request Body (application/json)
El cuerpo de la solicitud debe contener los siguientes campos:

```json
{
  "name": "string",
  "isExtra": true,
  "maxScore": 100,
  "qualificationDate": "2025-03-15T07:23:44.842Z",
  "tagActivityId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "unitId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### 📖 Descripción de los campos
| Campo | Tipo | Requerido | Descripción |
|--------|------|----------|-------------|
| `name` | `string` | ✅ | Nombre de la actividad. |
| `isExtra` | `boolean` | ✅ | Indica si la actividad es complementaria (extra puntos fuera del 100). |
| `maxScore` | `integer` | ✅ | Puntuación máxima asignada a la actividad. (Diferente del Maximo total qu puede obtener en un curso) |
| `qualificationDate` | `string (ISO 8601)` | ✅ | Fecha de calificación de la actividad. |
| `tagActivityId` | `string (UUID)` | ✅ | ID de la etiqueta asociada a la actividad. |
| `unitId` | `string (UUID)` | ✅ | ID de la unidad a la que pertenece la actividad. |

---

### 📤 Respuesta esperada
Si la solicitud es exitosa, el servidor responderá con un código `200 OK` y devolverá la actividad creada.

#### ✅ Ejemplo de respuesta (200 OK)
```json
{
  "data": {
    "hasNextPage": false,
    "hasPreviousPage": false,
    "currentPage": 1,
    "pageSize": 10,
    "totalItems": 6,
    "totalPages": 1,
    "items": [
      {
        "id": "e5f6a7b8-9e2d-4c63-b56f-219803a9d0c5",
        "name": "Proyecto de Geografía",
        "isExtra": false,
        "maxScore": 90,
        "qualificationDate": "2025-06-01T00:00:00",
        "tagActivityId": "25bb94dc-b13b-4649-b562-f1af741aeaf3",
        "unitId": "01dd6027-2e73-4a83-9b43-5e752727e93c"
      }
    ]
  },
  "message": "ACT-8085: Actividad encontrada correctamente.",
  "statusCode": 200,
  "status": true
}
```
### 🔄 Explicación de los campos Retornados

| Campo | Tipo | Descripción |
|--------|------|-------------|
| `data.items[].id` | `string (UUID)` | Identificador único de la actividad. |
| `data.items[].name` | `string` | Nombre de la actividad. |
| `data.items[].isExtra` | `boolean` | Identificador de una actividad es Extra o no.
| `data.items[].qualificationDate` | `string (ISO 8601)` | Fecha en la que se calificará la actividad. |
| `data.items[].tagActivityId` | `string (UUID)` | Identificador de la etiqueta asociada a la actividad. |
| `data.items[].unitId` | `string (UUID)` | Identificador de la unidad a la que pertenece la actividad. |
| `data.items[].class.id` | `string (UUID)` | Identificador unico de la Clase a la cual pertenece |



### Notas 

> ***isExtra*** = `False`esta es una actividad que pertenece al total de puntos a evaluar dentro de la calificacion maxima que puede sacar el alumno.
>
> ***isExtra*** = `True` esta es una actividad fuera del puntaje normal. Una actividad con puntaje extraordinario. Su puntaje se califica sobre el maximo puntaje calificado siempre considerando su respectiva ponderacion por unidad o cenvercion por parcial.


---

### ⚠️ Posibles errores

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Ocurre si los datos enviados son inválidos o faltan campos requeridos. |
| `404 Not Found` | Ocurre si la unidad (`unitId`) no existe en la base de datos. |
| `500 Internal Server Error` | Error interno del servidor al procesar la solicitud. |
| `401 Unauthorized` | No se encuentra Autorizado a usar este metodo.  Para mas Informacion lea [Login Documentation](./AuthDocumentation.MD) |

📌 **Ejemplo de error si la unidad no existe:**
```json
{
  "message": "Unidad inválida. No se encontró la unidad especificada.",
  "statusCode": 404,
  "status": false
}
```

## Obtener una actividad por ID

### 🔗 Endpoint  
```http
GET /api/activities/{id}
```

### 📄 Descripción  

Este endpoint permite obtener la información detallada de una actividad específica a partir de su ID.


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
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Actividad 1",
    "isExtra": true,
    "maxScore": 10,
    "qualificationDate": "2025-03-15T16:28:11.697Z",
    "tagActivityId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "unit": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "number": 3
    },
    "class": {
      "id": "7e1a4a2b-8923-4b3d-b8e9-4eaa23fbe5b2",
      "name": "Matemáticas Avanzadas"
    },
  },
  "message": "Actividad obtenida exitosamente",
  "statusCode": 200,
  "status": true
}
``` 

| Campo | Tipo | Descripción |
|--------|------|-------------|
| `id` | `string (UUID)` | Identificador único de la actividad. |
| `name` | `string` | Nombre de la actividad. |
| `isExtra` | `boolean` | Identificador de una actividad es Extra o no.|
| `maxScore` | `number` | Puntaje maximo que podra obtener un alumno en esta actividad.|
| `qualificationDate` | `string (ISO 8601)` | Fecha en la que se calificará la actividad. |
| `tagActivityId` | `string (UUID)` | Identificador de la etiqueta asociada a la actividad. |
| `unit.id` | `string (UUID)` | Identificador de la unidad a la que pertenece la actividad. |
| `unit.number` | `integer` | Numero de la Unidad a la cual esta asociada esta actividad |
| `class.id` | `string (UUID)` | Identificador unico de la Clase a la cual pertenece |
| `class.name` | `string (UUID)` | Nombre de la clase a la cual pertenece |



---

## ⚠️ Posibles errores

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Ocurre si los parámetros enviados son inválidos. |
| `500 Internal Server Error` | Error interno en el servidor. |
| `404 Not Found` | No se ha encontrado el registro {`id`}|
| `401 Unauthorized` | No se encuentra Autorizado a usar este metodo.  Para mas Informacion lea [Login Documentation](./AuthDocumentation.MD) |
---

### 📌 Notas  
- 🆔 **El ID de la actividad debe ser un UUID válido**.  




## Actualizar una actividad

### 🔗 Endpoint
```http
PUT /api/activities/{id}
```

### 📝 Descripción
Este endpoint permite actualizar una actividad existente mediante su `id`. Se debe enviar un cuerpo de solicitud con los nuevos valores de la actividad.

### 📥 Parámetros

en la consulta del metodo es necesario el parametro siguiente: 

| Nombre | Tipo | Ubicación | Descripción |
|--------|------|-----------|-------------|
| `id` | `string (UUID)` | `path` | Identificador único de la actividad a actualizar. |


### 📖 Descripción de los campos
| Campo | Tipo | Requerido | Descripción |
|--------|------|----------|-------------|
| `name` | `string` | ✅ | Nombre de la actividad. |
| `isExtra` | `boolean` | ✅ | Indica si la actividad es complementaria (extra puntos fuera del 100). |
| `maxScore` | `integer` | ✅ | Puntuación máxima asignada a la actividad. (Diferente del Maximo total qu puede obtener en un curso) |
| `qualificationDate` | `string (ISO 8601)` | ✅ | Fecha de calificación de la actividad. |
| `tagActivityId` | `string (UUID)` | ✅ | ID de la etiqueta asociada a la actividad. |
| `unitId` | `string (UUID)` | ✅ | ID de la unidad a la que pertenece la actividad. |


### 📩 Cuerpo de la Solicitud (`application/json`)
```json
{
  "name": "string",
  "isExtra": true,
  "maxScore": 100,
  "qualificationDate": "2025-03-15T22:33:36.002Z",
  "tagActivityId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "unitId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### 📤 Respuesta Exitosa (`200 OK`)

Cuando los Cambios se Han aplicado Correctamente
```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "string",
    "isExtra": true,
    "maxScore": 0,
    "qualificationDate": "2025-03-15T22:33:36.003Z",
    "tagActivityId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "unitId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  },
  "message": "Actividad actualizada exitosamente",
  "statusCode": 200,
  "status": true
}
```

### ⚠️ Posibles Errores
| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Datos inválidos en la solicitud. |
| `404 Not Found` | La actividad con el {`ID`} especificado no existe. |
| `500 Internal Server Error` | Error en el servidor al procesar la solicitud. |
| `401 Unauthorized` | No se encuentra Autorizado a usar este metodo.  Para mas Informacion lea [Login Documentation](./AuthDocumentation.MD) |

### 🔍 Notas Adicionales
- **El campo `id` es obligatorio** y debe enviarse en la URL.
- **El `maxScore` no puede ser menor a 0.**
- **El `qualificationDate` debe estar en formato ISO 8601.**
- Asegurarse de que los `tagActivityId` y `unitId` existan antes de enviar la solicitud.

---
✅ **Ejemplo de Uso**:
```http
PUT /api/activities/3fa85f64-5717-4562-b3fc-2c963f66afa6
```



## Eliminar una actividad


### 🔗 Endpoint
```http
DELETE /api/activities/{id}
```

### 📝 Descripción
Este endpoint permite eliminar una actividad existente mediante su `id`. Al eliminar la actividad, **se eliminan también todas las calificaciones de alumnos asociadas**, independientemente de su estado (activo o inactivo).

### 📥 Parámetros
| Nombre | Tipo | Ubicación | Descripción |
|--------|------|-----------|-------------|
| `id` | `string (UUID)` | `path` | Identificador único de la actividad a eliminar. |

### 📤 Respuesta Exitosa (`200 OK`)
```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "string",
    "isExtra": true,
    "maxScore": 0,
    "qualificationDate": "2025-03-15T22:36:57.651Z",
    "tagActivityId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "unitId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  },
  "message": "Actividad eliminada exitosamente",
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
- **Todas las calificaciones asociadas a la actividad serán eliminadas automáticamente.**
- **Esta acción es irreversible**, asegúrese antes de eliminar una actividad.

---
✅ **Ejemplo de Uso**:
```http
DELETE /api/activities/3fa85f64-5717-4562-b3fc-2c963f66afa6
```
