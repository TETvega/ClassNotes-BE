## 🔗 Navegación  

⬅️ [Endpoints](./Services.index.md "Services")
📖 [Documentacion](../index.MD "Docuemntacion Home")  
🏠 [Home](../../README.MD "Home")  
📖 [Endpoints](./Services.index.md "Services")  
👥 [Contribution](../../contribution.MD "Contribution")  



---

# 📌 **Endpoints de Autenticación**  


## 📖 Descripción
Este conjunto de endpoints permite gestionar la autentificacion dentro del sistema, incluye el resgistro, login y tambien un endpoint para refrescar el tokend del usuario.


## 📌 Índice de Endpoints

- 🔹 [🔐 **Iniciar sesión**](#iniciar-sesion)
- 🔹 [📝 **Registro de usuario**](#registro-de-usuario)
- 🔹 [🔄 **Refrescar token**](#refrescar-token)

---

## Iniciar sesion  

Endpoint mediante el cual un usuario inicia sesion en la aplicacion.
Cuando un usuario inicia sesion tiene acceso a los diferentes endpoint protegidos segun su Rol por Defecto.  

Si necesita saber mas de los roles de la aplicacion vease el siguiente enlace [Guia de Roles]()


### 🔗 Endpoint  
```http
POST /api/auth/login
```


### 📥 **Cuerpo de la solicitud**  

```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```


Cuando un usuario existe y su contraseña es la correcta podremos tenr el siguiente mensaje

### 📤 **Respuesta exitosa (200 OK)**  

```json
{
  "data": {
    "fullName": "Juan Pérez",
    "email": "user@example.com",
    "token": "eyJhbGciOiJIUzI1...",
    "tokenExpiration": "2025-03-16T03:14:28.643Z",
    "refreshToken": "123456789abcdef",
    "refreshTokenExpire": "2025-03-16T03:14:28.643Z"
  },
  "message": "Inicio de sesión exitoso.",
  "statusCode": 200,
  "status": true
}
```


### ⚠️ **Posibles errores**  

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Datos enviados incorrectos o faltantes. |
| `401 Unauthorized` | Credenciales incorrectas. |
| `403 Forbidden` | Cuenta deshabilitada o bloqueada. |
| `500 Internal Server Error` | Error interno en el servidor. |

#### **Ejemplo de respuesta en caso de error (401 Unauthorized)**  
```json
{
  "message": "Correo o contraseña incorrectos.",
  "statusCode": 401,
  "status": false
}
```

### Notas

Por el momento el numero de intentos para logearse es ilimitado.
Mensajes de error se mantienen en constantes

Proximamente se piensa manejar de una manera mas simple Mostrando al usuario en el apartado de login algo sutil

`***proximamente guia codigos de error***`


---

## Registro de usuario  

Metodo por el cual se crea una nueva cuenta 

> [!IMPORTANT]  
> Cuando una cuenta se crea, automaticamente se le designa un Rol [`USER`]  
> Una nueva cuenta cn el rol de Usuario se le crean Automaticamente Tags de Actividades por defecto **ver [Tags](./TagsDocumentatios.MD)**



### 🔗 Endpoint  
```http
POST /api/auth/register
```

### 📖 Descripción de los campos

| Campo            | Tipo                  | Requerido | Descripción | Consideraciones |
|-----------------|----------------------|----------|-------------|----------------|
| `firstName`     | `string`              | ✅        | Primer nombre del usuario. | Máximo 50 caracteres. |
| `lastName`      | `string`              | ✅        | Segundo nombre del usuario. | Máximo 50 caracteres. |
| `email`         | `string` (email)      | ✅        | Correo electrónico válido del usuario. | Debe ser único y seguir el formato de correo válido. |
| `password`      | `string`              | ✅        | Contraseña del usuario (mínimo 8 caracteres, incluyendo mayúsculas, minúsculas, números y caracteres especiales). | Máximo 100 caracteres. |
| `confirmPassword` | `string`              | ✅        | Confirmación de la contraseña (debe coincidir con `password`). | Debe ser igual a `password`. |

### 📥 **Cuerpo de la solicitud**
```json
{
  "firstName": "Juan",
  "lastName": "Pérez",
  "email": "user@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!"
}
```
### 🔹 **Respuestas**

| Código | Descripción |
|--------|-------------|
| `200`  | Registro exitoso. |

#### 📤 **Ejemplo de respuesta (200 - Success)**
```json
{
  "data": {
    "fullName": "Juan Pérez",
    "email": "user@example.com",
    "token": "string",
    "tokenExpiration": "2025-03-16T18:01:27.083Z",
    "refreshToken": "string",
    "refreshTokenExpire": "2025-03-16T18:01:27.083Z"
  },
  "message": "Registro exitoso",
  "statusCode": 200,
  "status": true
}
```
#### Parametro *`token`*

> Parametro mediante el cual se almacenan cierta infromacion util como los roles y claims Utilize la siguiente Url para ver parametros [JSTOOLSET JWT](https://www.jstoolset.com/jwt "Tokens").
> Se utiliza por medio de JWT , las autentificacion por BErear Token.
>```json
>{
>   "header":{
>     "alg":"HS256"
>     "typ":"JWT"
>   }
>   "payload":{
>     "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress":"sararuiz@example.com"
>     "jti":"f0fe883c-64c2-4f0d-b6a6-90a303b99c14"
>     "UserId":"0f142536-4567-89ab-cdef-0123456789fe"
>     "DefaultConfigCourse":""
>     "http://schemas.microsoft.com/ws/2008/06/identity/claims/role":"USER"
>     "exp":1742150942
>     "iss":"https://localhost:7047"
>     "aud":"http://localhost:"
>   }
>}

---


### 📤 **Respuesta exitosa (200 OK)**  
```json
{
  "data": {
    "fullName": "Juan Pérez",
    "email": "user@example.com",
    "token": "eyJhbGciOiJIUzI1...",
    "tokenExpiration": "2025-03-16T03:14:28.636Z",
    "refreshToken": "987654321abc",
    "refreshTokenExpire": "2025-03-16T03:14:28.636Z"
  },
  "message": "Usuario registrado correctamente.",
  "statusCode": 200,
  "status": true
}
```

### ⚠️ **Posibles errores**  

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Datos inválidos o faltantes. |
| `409 Conflict` | El correo electrónico ya está registrado. |
| `500 Internal Server Error` | Error interno en el servidor. |

#### **Ejemplo de respuesta en caso de error (409 Conflict - Email ya registrado)**  
```json
{
  "message": "El correo electrónico ya está en uso.",
  "statusCode": 409,
  "status": false
}
```

---

## Refrescar token  

Endpoint para refrescar el token y el usuario tenga acceso a a los diferentes metodos o serviciones solicitados.


### 🔗 Endpoint  
```http
POST /api/auth/refresh-token
```

### 📥 **Cuerpo de la solicitud**  
```json
{
  "token": "eyJhbGciOiJIUzI1...",
  "refreshToken": "987654321abc"
}
```

### 📤 **Respuesta exitosa (200 OK)**  
```json
{
  "data": {
    "fullName": "Juan Pérez",
    "email": "user@example.com",
    "token": "eyJhbGciOiJIUzI1...",
    "tokenExpiration": "2025-03-16T03:14:28.637Z",
    "refreshToken": "654321fedcba",
    "refreshTokenExpire": "2025-03-16T03:14:28.637Z"
  },
  "message": "Token renovado correctamente.",
  "statusCode": 200,
  "status": true
}
```

### ⚠️ **Posibles errores**  

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Token inválido o faltante. |
| `401 Unauthorized` | Token expirado o inválido. |
| `500 Internal Server Error` | Error interno en el servidor. |

#### **Ejemplo de respuesta en caso de error (401 Unauthorized - Token expirado)**  
```json
{
  "message": "El token de sesión ha expirado. Inicie sesión nuevamente.",
  "statusCode": 401,
  "status": false
}
```

---

## 📌 **Notas adicionales**  
- Los tokens son generados usando **JWT (JSON Web Tokens)**.  
- Se recomienda enviar el `Authorization: Bearer <token>` en los encabezados de las solicitudes protegidas.  
- El `refreshToken` se usa para generar un nuevo `token` sin necesidad de volver a ingresar las credenciales.  
- Los tokens tienen una duración definida (`tokenExpiration`). Cuando expiran, se debe usar el `refreshToken`.  

---
