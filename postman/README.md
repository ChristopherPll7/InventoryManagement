# Pruebas de aceptación con Postman

## Importar y autenticar

1. Inicia el proyecto con Docker Compose y abre `http://localhost:8080/swagger`.
2. En Swagger, selecciona **Authorize** e inicia sesión con `adminInventory`. La contraseña está en `INVENTORY_DEV_PASSWORD` de tu `.env` local.
3. Ejecuta `GET /api/products` en Swagger. Del curl generado copia solamente el token que aparece después de `Bearer `, sin comillas ni el prefijo. Debe ser un access token reciente.
4. En Postman importa `InventoryManagement.postman_collection.json` y `Local.postman_environment.json` desde esta carpeta.
5. Selecciona el entorno **Inventory Management - Local**. Guarda el token en el valor local de `accessToken`; deja `baseUrl=http://localhost:8080`. No compartas ni exportes el entorno con tokens reales.
6. Abre **Run collection**, selecciona las cinco carpetas en su orden original y ejecuta una iteración, sin demora entre solicitudes. El token puede caducar durante una revisión manual larga: obtén uno nuevo en Swagger si empiezas a recibir 401.

La colección usa Bearer heredado y no necesita credenciales de administrador de Keycloak. No envía tu contraseña ni habilita el flujo password grant. Postman también permite configurar OAuth2/PKCE directamente, pero requeriría registrar su callback en Keycloak; no es necesario para este recorrido. Referencia: [autorización de solicitudes en Postman](https://learning.postman.com/docs/use/send-requests/authorization/specifying-authorization-details).

## Qué se ejecuta

| Carpeta | Casos |
| --- | --- |
| 01 - Setup and categories | Salud, generación de identificadores únicos, creación/listado/consulta/actualización de categoría, duplicados y validación |
| 02 - Products | Creación/listado/consulta/actualización, stock inicial, SKU duplicado, precio inválido, recurso inexistente y categoría en uso |
| 03 - Inventory and history | Entrada de 10 y salida de 3, balance 7, rechazos sin cambios de stock/historial, tipos, fechas inclusivas, paginación y límites |
| 04 - Authentication | 401 sin token y 403 opcional con token válido sin products.read |
| 05 - Deactivation and preserved history | Desactivación del producto antes de la categoría, DELETE repetido, bloqueo de movimientos y conservación del historial |

Hay 40 solicitudes, incluidas las variantes de los 13 endpoints de negocio. Los scripts comprueban estados HTTP y, según el caso, códigos de error, contenido, referencias, balances, páginas e historial. El recorrido esperado realiza 39 solicitudes si no configuras la prueba opcional de 403. Un caso omitido no equivale a una prueba aprobada.

Para probar 403, obtén un access token de un usuario sin `products.read` y guárdalo localmente en `restrictedAccessToken`. Debe provenir del mismo realm, tener la audiencia inventory-api y estar vigente; un token inválido produciría 401. No uses el token de adminInventory para este caso. Si la variable está vacía, el script omite la solicitud y lo informa en la consola. Utiliza una versión actual de Postman que admita `pm.execution.skipRequest()`.

## Datos y resultados

- Cada ejecución crea una categoría y un producto con un sufijo único, además de dos movimientos. Las variables de colección `categoryId` y `productId` se capturan automáticamente; no definas variables homónimas en el entorno.
- Ejecuta el recorrido completo en orden. Las peticiones posteriores necesitan los registros creados al principio. Si falla la creación, corrige el problema y reinicia desde la primera petición.
- Al finalizar, el producto y la categoría quedan inactivos. Se conserva el stock 7 y los dos movimientos, conforme al borrado lógico de la API. No se eliminan físicamente registros ni se modifica la categoría General o los productos de ejemplo.
- Revisa las aserciones en los resultados del Runner. Conserva el resumen de estados y fallos; evita publicar encabezados Authorization o exportaciones que incluyan tokens.
- Esta colección no verifica concurrencia SQL ni reemplaza las pruebas de integración. La comparación del orden comprueba fechas descendentes y páginas sin repetición sobre datos sin cambios; los empates de fecha con el orden GUID de SQL Server se verifican en la suite SQL.

La colección se generó y se validó estructuralmente contra el código. Su ejecución en Postman con un token real queda pendiente; no se presenta como una ejecución aprobada de la API desplegada.
