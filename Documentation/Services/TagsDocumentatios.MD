## 🔗 Navegación  

⬅️ [Endpoints](./Services.index.md "Services")
📖 [Documentacion](../index.MD "Docuemntacion Home")  
🏠 [Home](../../README.MD "Home")  
📖 [Endpoints](./Services.index.md "Services")  
👥 [Contribution](../../contribution.MD "Contribution")  


# 📌 Tags - Endpoints

## 📖 Descripción


**Tag:**: Una tag es el identificador de categoria de una actividad(categoria de una actividad).
En este endpoint se permite Gestionar las Tags que un usuario puede crear, editar , obtener y eliminar. 

> [!IMPORTANT]  
> Cuando un usuario se **registra** automaticamente el __sistema__ le ***crea un conjunto de Tags Por Defecto***.     
> Una tag Creada por defecto para el y todos los usuarios, sin embargo es un registro distinto para cada usuario.

---

## 📌 Índice de Endpoints

- :small_blue_diamond: [Obtener todas las Tags de un Usuario](#dart-obtener-todas-las-tags)
    - :small_blue_diamond: [Como Pedir Tags Correctamente](#chess_pawn-como-pedir-las-tags-correctamente)
- :small_blue_diamond: [Manejo Correcto de Tags Implementación Con Frontend](#hammer_and_wrench-manejo-de-tags-junto-a-frontend)
- :small_blue_diamond: [Crear una nueva Tag](#bookmark-crear-una-nueva-tag)
- :small_blue_diamond: [Tags Creadas en el Registro de un Usuario.](#-tags-creadas-por-defecto-al-registrar-un-usuario)
- :small_blue_diamond: [Obtener una tag por ID](#rocket-obtener-una-tag-por-id)
- :small_blue_diamond: [Actualizar una tag por ID](#dart-actualizar-una-tag-por-id)
- :small_blue_diamond: [Eliminar una Tag](#wastebasket-eliminar-una-tag-por-id)

---

## :dart: Obtener todas las Tags

Al utilizar este endpoint realiza un llamado a la base de datos, y este busca todas las tags del usuario.   
Se encuentra paginado con `50` items por pagina por el momento.

> [!IMPORTANT]   
> No se espera una persona tenga mas de 50 Tags


> [!WARNING]   
> Este Metodo esta sujeto a cambios en el futuro con un limite de tags por ejemplo



### 🔗 Endpoint
```http
GET api/tags_activities/
```

> [!IMPORTANT]   
> Si bien este endopint soporta busqueda. No es recomendable Utilizarlo directamente.


### :chess_pawn: Como pedir las tags correctamente

Al utilizar las tags, se espera se tengan en metodo de carga en lazi loading en un **Store** o en el **LocalStore**.   
Por ejemplo: Cuando se solicitan las actividades , estas tienen el ID de la tag pero no cargan la infromacion de la tag dado el rendimiento de la aplicacion.
> Es necesario cargar las tags por separado en un store, luego comparar con un map o de alguna otra forma para cargar la informacion correcta de la tag.

Mas adelante se explica de manera correcta y con sintaxis de codigo.


### :package: Parámetros de consulta (Query Parameters)
	
| Parámetro    | Tipo             | Descripción |
|-------------|----------------|-------------|
| `searchTerm` | `string` | (Opcional) Permite buscar Tags por nombre o palabra clave. |
| `page` | `integer` | (Opcional) Indica el número de página para la paginación. Valor por defecto: `1`. |




📌 **Ejemplo de solicitud con parámetros:**  
```http
GET api/tags_activities/?searchTerm&page=1
```



### 📤 Respuesta
Si la solicitud es exitosa, el servidor responderá con un código `200 OK` y un JSON con la lista de Tags.

```json
{
  "data": {
    "hasNextPage": false,
    "hasPreviousPage": false,
    "currentPage": 1,
    "pageSize": 60,
    "totalItems": 2,
    "totalPages": 1,
    "items": [
      {
        "id": "6d4b3e97-09e6-4a95-a813-fee0d6bc2daf",
        "name": "Revisión",
        "colorHex": "808080",
        "icon": "rev",
        "teacherId": "41e958ea-a9e3-4deb-bccb-e17a987164c7"
      },
      {
        "id": "1ede90cb-d5e1-457d-8279-f45da35afecb",
        "name": "Debate",
        "colorHex": "A52A2A",
        "icon": "deb",
        "teacherId": "41e958ea-a9e3-4deb-bccb-e17a987164c7"
      },
    ]
  },
  "message": "TA-601: Etiquetas de actividad encontradas correctamente.",
  "statusCode": 200,
  "status": true
}
```
### 🔄 Explicación de los campos Retornados  

| Campo | Tipo | Descripción |
|--------|------|-------------|
| `data` | `object` | Contiene la información paginada de las etiquetas. |
| `data.hasNextPage` | `boolean` | Indica si hay una página siguiente en la paginación. |
| `data.hasPreviousPage` | `boolean` | Indica si hay una página anterior en la paginación. |
| `data.currentPage` | `integer` | Número de la página actual. |
| `data.pageSize` | `integer` | Cantidad de elementos por página. |
| `data.totalItems` | `integer` | Número total de elementos disponibles. |
| `data.totalPages` | `integer` | Número total de páginas disponibles. |
| `data.items` | `array` | Lista de etiquetas disponibles. |
| `data.items[].id` | `string (UUID)` | Identificador único de la etiqueta. |
| `data.items[].name` | `string` | Nombre de la etiqueta. |
| `data.items[].colorHex` | `string` | Código hexadecimal del color de la etiqueta. |
| `data.items[].icon` | `string` | Identificador del ícono asociado a la etiqueta. |
| `data.items[].teacherId` | `string (UUID)` | Identificador único del profesor asociado a la etiqueta. |
| `message` | `string` | Mensaje de respuesta del servidor. |
| `statusCode` | `integer` | Código de estado HTTP de la respuesta. |
| `status` | `boolean` | Indica si la respuesta fue exitosa (`true`) o no (`false`). |

---

> [!NOTA]   
> ***`data.items[].colorHex`*** : Esta propiedad devuele el color hexagesimal sin el uso del `#`.   
> ***`data.items[].icon`*** : Esta propiedad es parte de un diccionario, dado que el manejo viene por parte de **Frontend**, notese que este es un estring cualquiera no se guarda con `<>` entre el .



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


## :hammer_and_wrench: Manejo de Tags Junto a Frontend

Para este punto se tiene que comprender correctamente la estructura que se piensa manejar. No se guarda directamente el icono ya que se utilizara un diccionario en frontend el cual permite iterar sobre lo que se esta mandando.

Una actividad tiene tags, pero una **actividad manda el ID de tag** que tiene. Vease [Actividades](./activities.MD)

```json
{
    "id": "e5f6a7b8-9e2d-4c63-b56f-219803a9d0c5",
    "name": "Proyecto de Geografía",
    "isExtra": false,
    "maxScore": 90,
    "qualificationDate": "2025-06-01T00:00:00",
    "tagActivityId": "25bb94dc-b13b-4649-b562-f1af741aeaf3",
    "unitId": "01dd6027-2e73-4a83-9b43-5e752727e93c"
}
```

Cuando una Actividad es cargada se espera tener las Tags en un **store** pre-cargadas.
De esta manera podemos crear un componente que muestre el icono de tag que sea necesario segun el caso.

```jsx
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faStar, faMusic, faMicrophone, faTools } from '@fortawesome/free-solid-svg-icons';

const iconMap = {
    "fa-star": faStar,
    "fa-music": faMusic,
    "fa-microphone": faMicrophone,
    "fa-tools": faTools,
};

const CategoriaItem = ({ categoria }) => {
    return (
        <div>
            <FontAwesomeIcon icon={iconMap[categoria.iconName]} className={`#${categoria.colorHex}`} />
            <span>{categoria.nombre}</span>
        </div>
    );
};
```


## :bookmark: Crear una nueva Tag

Este endpoint permite la creación de una nueva Tag dentro del sistema.


### 🔗 Endpoint
```http
POST api/tags_activities/
```

📌 **Descripción:** Se envía un objeto JSON con los detalles de la Tag a crear. El sistema validará que: El color Hexadesimal este en un formato correcto.

### 📥 Request Body (application/json)
El cuerpo de la solicitud debe contener los siguientes campos:

```json
{
  "name": "TagPrueba",
  "colorHex": "fff000",
  "icon": "icon"
}
```
### 📖 Descripción de los campos  

| Campo | Tipo | Requerido | Descripción |
|--------|------|----------|-------------|
| `name` | `string` | ✅ | Nombre de la etiqueta (máximo 15 caracteres). |
| `colorHex` | `string` | ✅ | Código de color en formato hexadecimal (6 caracteres sin `#`). |
| `icon` | `string` | ✅ | Nombre del icono asociado a la etiqueta (máximo 20 caracteres). |



### 📤 Respuesta esperada
Si la solicitud es exitosa, el servidor responderá con un código `200 OK` y devolverá la Tag creada.

#### ✅ Ejemplo de respuesta (200 OK)
```json
{
  "data": {
    "id": "4eb9907f-d46f-46b1-acd3-08dd65899f63",
    "name": "TagPrueba",
    "colorHex": "fff000",
    "icon": "icon",
    "teacherId": "0f142536-4567-89ab-cdef-0123456789fe"
  },
  "message": "TA-605: Etiqueta de actividad creada exitosamente.",
  "statusCode": 201,
  "status": true
}
```

### ⚠️ Posibles errores

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Ocurre si los datos enviados son inválidos o faltan campos requeridos. |
| `404 Not Found` | Ocurre si la unidad (`unitId`) no existe en la base de datos. |
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
---

## 📖 Tags Creadas por Defecto al Registrar un Usuario  

Cuando un usuario se registra, se generan automáticamente las siguientes etiquetas (`tags`) para organizar sus actividades. Cada etiqueta tiene un nombre (`name`), un color en formato hexadecimal (`colorHex`) y un icono representativo (`icon`).  

### 📌 Lista de Tags por Defecto  

```json
[
  {
    "name": "Tasks",
    "colorHex": "#FF7E63",
    "icon": "tasks"
  },
  {
    "name": "Forums",
    "colorHex": "#84FF63",
    "icon": "forum"
  },
  {
    "name": "Exams",
    "colorHex": "#4EB234",
    "icon": "doc"
  },
  {
    "name": "ExtraPoints",
    "colorHex": "#57EFC6",
    "icon": "star"
  },
  {
    "name": "Excursion",
    "colorHex": "#57CAEF",
    "icon": "travel"
  },
  {
    "name": "Quiz",
    "colorHex": "#9569FD",
    "icon": "question"
  },
  {
    "name": "Presentation",
    "colorHex": "#E369FD",
    "icon": "presentation"
  },
  {
    "name": "Experiment",
    "colorHex": "#FD699D",
    "icon": "flask"
  }
]
```

### 🎯 Notas Importantes  
- Estas etiquetas ayudan a clasificar diferentes tipos de actividades en la plataforma.  
- Los colores están en formato hexadecimal y pueden personalizarse.  
- Los iconos representan visualmente cada tipo de actividad y pueden usarse en la interfaz de usuario.  

---

## :rocket: Obtener una tag por ID

Este endpoint permite obtener una etiqueta existente proporcionando su ID.

### 🔗 Endpoint
```http
GET /api/tags_activities/{id}
```

### :package: Parámetros de ruta

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `id` | `string (UUID)` | Identificador único de la etiqueta a actualizar. |

### 📤 Respuesta
Si la actualización es exitosa, el servidor responderá con un código `200 OK` y un JSON con la etiqueta actualizada.

```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "string",
    "colorHex": "dae4fb",
    "icon": "string",
    "teacherId": "string"
  },
  "message": "Etiqueta obtenida con exitosamente.",
  "statusCode": 200,
  "status": true
}
```

### ⚠️ Posibles errores

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Datos inválidos en la solicitud. |
| `404 Not Found` | La etiqueta con el ID proporcionado no existe. |
| `500 Internal Server Error` | Error interno del servidor. |
| `401 Unauthorized` | No se encuentra Autorizado a usar este metodo.  Para mas Informacion lea [Login Documentation](./AuthDocumentation.MD) |
---


## :dart: Actualizar una tag por ID

Este endpoint permite actualizar una etiqueta existente proporcionando su ID y los nuevos datos.

### 🔗 Endpoint
```http
PUT /api/tags_activities/{id}
```

### :package: Parámetros de ruta

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `id` | `string (UUID)` | Identificador único de la etiqueta a actualizar. |

### 📥 Cuerpo de la solicitud (Request Body)

```json
{
  "name": "string",
  "colorHex": "#dae4fb",
  "icon": "string"
}
```

### 📤 Respuesta
Si la actualización es exitosa, el servidor responderá con un código `200 OK` y un JSON con la etiqueta actualizada.

```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "string",
    "colorHex": "#dae4fb",
    "icon": "string",
    "teacherId": "string"
  },
  "message": "Etiqueta actualizada exitosamente.",
  "statusCode": 200,
  "status": true
}
```

### ⚠️ Posibles errores

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Datos inválidos en la solicitud. |
| `404 Not Found` | La etiqueta con el ID proporcionado no existe. |
| `500 Internal Server Error` | Error interno del servidor. |
| `401 Unauthorized` | No se encuentra Autorizado a usar este metodo.  Para mas Informacion lea [Login Documentation](./AuthDocumentation.MD) |


---

## :wastebasket: Eliminar una tag por ID

Este endpoint permite eliminar una etiqueta existente proporcionando su ID.

### 🔗 Endpoint
```http
DELETE /api/tags_activities/{id}
```

### :package: Parámetros de ruta

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `id` | `string (UUID)` | Identificador único de la etiqueta a eliminar. |

### 📤 Respuesta
Si la eliminación es exitosa, el servidor responderá con un código `200 OK` y un JSON confirmando la eliminación.

```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "string",
    "colorHex": "#dae4fb",
    "icon": "string",
    "teacherId": "string"
  },
  "message": "Etiqueta eliminada exitosamente.",
  "statusCode": 200,
  "status": true
}
```

### ⚠️ Posibles errores

| Código | Descripción |
|--------|-------------|
| `404 Not Found` | La etiqueta con el ID proporcionado no existe. |
| `500 Internal Server Error` | Error interno del servidor. |
| `401 Unauthorized` | No se encuentra Autorizado a usar este metodo.  Para mas Informacion lea [Login Documentation](./AuthDocumentation.MD) |
---

