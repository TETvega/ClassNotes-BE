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

- 🔹 [Obtener todas las Tags de un Usuario](#dart-obtener-todas-las-tags)
    - 🔹 [Como Pedir Tags Correctamente](#chess_pawn-como-pedir-las-tags-correctamente)
- 🔹 [Crear una nueva Tag](#crear-una-nueva-actividad)
- 🔹 [Obtener una tag por ID](#obtener-una-actividad-por-id)
- 🔹 [Actualizar una tag por ID](#actualizar-una-actividad)
- 🔹 [Eliminar una Tag](#eliminar-una-actividad)

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



