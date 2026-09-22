# Reporte de desarrollo dirigido con IA

Fecha de elaboración: 22 de septiembre de 2026. Proyecto: Inventory Management API.

Este reporte resume los prompts, las decisiones y la supervisión observables en la conversación de desarrollo y los archivos del proyecto. No es una transcripción completa ni atribuye al desarrollador aprobaciones explícitas que no constan en el historial. Las decisiones descritas como «incorporadas» aparecen en el código y los ADR; las solicitudes o correcciones expresas del desarrollador se identifican por separado.

## 1. Prompts principales y criterio de dirección

Los siguientes extractos proceden de las solicitudes del desarrollador. Las explicaciones de su propósito son un resumen razonado.

| Etapa | Prompt o extracto del pedido | Criterio de supervisión y resultado |
| --- | --- | --- |
| Diagnóstico previo | «Read the root AGENTS.md, README.md, docs/specs/001-inventory-domain-spec.md, docs/architecture-decisions.md and docs/current-status.md». «Do not modify any files yet». | Separar revisión de implementación. Exigir lectura de restricciones y evidencia antes de permitir cambios. |
| Contraste con el código | «Verify claims against the actual source code. Do not assume that docs/current-status.md is accurate when the implementation says otherwise». | No aceptar la documentación de la IA como prueba suficiente. Solicitar arquitectura, faltantes, violaciones, build, pruebas y plan priorizado. |
| Recuperación de la base | «Restaurar la línea base de pruebas y Corregir los defectos existentes en los contratos». | Priorizar confiabilidad y contratos antes de ampliar funcionalidades. |
| Implementación incremental | «Continuemos ahora con los puntos 3, 4»; después, completar CRUD de categorías y actualización/desactivación de productos. | Trabajar por bloques del plan. El historial disponible no reproduce íntegramente la respuesta inicial que numeró los puntos; no se reconstruye aquí como si fuera una cita. Los resultados documentan transacciones, concurrencia y ciclo de vida del catálogo. |
| Seguimiento de pendientes | «Continua con las tareas de los puntos 5 y 6 que no se lograron completar». | No equiparar una entrega parcial con una tarea terminada. Volver sobre lo pendiente. |
| Consultas | «Implementar las consultas de balance de inventario e historial de movimientos, Agregar filtros, ordenamiento determinista del más reciente al más antiguo y paginación limitada». | Expresar comportamiento observable y límites antes de implementar el bloque. |
| Autorización y entrega | «Definir permisos, probar el comportamiento de 401/403, agregar despliegue versionado del esquema, completar los ADR, separar archivos que contienen múltiples tipos». | Incluir autorización, mantenibilidad, entrega y pruebas en el alcance; no limitar el resultado a endpoints que compilen. |
| Corrección del entorno SQL | «No quiero que ejecutes la suite SQL en bases temporales aisladas». Después: «Utiliza la base existente… cuidado en no borrar la DB». | Rechazo explícito de la estrategia inicial de pruebas. Autorizar cambios de datos para este proyecto, conservando la base. |
| Error real de Keycloak | «Corrige la parte del campo no reconocido \"clients\"», adjuntando el error de importación. | Contrastar configuración generada con el servicio real. Se corrigió `roles.clients` a `roles.client`. |
| Datos iniciales | Pregunta sobre la categoría General y petición de tres registros relacionados por tabla. | Cuestionar una decisión implícita de la IA y concretar el alcance de los datos de ejemplo. Se incorporó V003 sin editar migraciones aplicadas. |
| Diagnóstico de permisos | Presentación del error `FORBIDDEN` al usar Swagger estando autenticado. | Aportar evidencia de ejecución. Se distinguió autenticación de autorización y se identificó la ausencia de `products.read` en el token compartido. El token no se reproduce en este reporte. |
| Cuenta de desarrollo | «Crea un usuario llamado adminInventory… desde un inicio del desarrollo, genera las credenciales pertinentes». | Convertir la necesidad de acceso inicial en una cuenta explícita con permisos, conservando las políticas para otros usuarios. |
| Pruebas de aceptación | Solicitud de opciones y posterior elección de una colección Postman «dada la premura». | Elegir una herramienta repetible acorde al tiempo disponible. Se generaron colección, entorno y guía; no se confundió generación con ejecución aprobada. |

La interacción muestra dirección mediante restricciones, priorización y evidencia de fallos. La IA realizó inspección, propuestas, implementación y verificaciones técnicas; el desarrollador fijó el alcance y corrigió decisiones operativas concretas.

## 2. Evidencia de Spec-Driven Development

### Documento utilizado antes de las iteraciones

Se adjunta por referencia el documento completo incluido en la entrega: [001-inventory-domain-spec.md](specs/001-inventory-domain-spec.md). Debe entregarse junto con este reporte, conservando la carpeta `docs/specs`. No se creó una copia retrospectiva con apariencia de documento original.

**Sí existía una especificación de comportamiento y contratos antes de las ampliaciones dirigidas en este historial.** La primera solicitud disponible exige leerla y comparar la implementación existente con ella antes de modificar archivos. Esto respalda el uso de una especificación previa para esas iteraciones. Sin embargo, al inicio ya había código: el historial disponible y la ausencia de un repositorio Git verificable no permiten demostrar que el documento precediera a la primera línea de todo el proyecto, ni reconstruir su versión exacta inicial.

El spec define entidades, invariantes, doce casos de uso, rutas REST, errores, autenticación, restricciones CQRS, transacciones, concurrencia, pruebas y Definition of Done. [AGENTS.md](../AGENTS.md) convierte varias restricciones en instrucciones de trabajo para la IA; no sustituye el contrato de dominio.

### Trazabilidad de requisitos a implementación y pruebas

| Comportamiento especificado | Decisión e implementación | Evidencia de prueba |
| --- | --- | --- |
| Cantidad positiva, stock no negativo, movimiento y balance atómicos; secciones 8, 18, 19, 28 y 29 | [InventoryCommandStore](../src/InventoryManagement.Infrastructure/Persistence/InventoryCommandStore.cs): validación de dominio, lectura bloqueada y dos escrituras en una transacción | [InventoryMovementTests](../tests/InventoryManagement.Domain.Tests/InventoryMovementTests.cs) y [PersistenceContractTests](../tests/InventoryManagement.Infrastructure.Tests/PersistenceContractTests.cs): salidas concurrentes y fallos inyectados |
| No eliminar una categoría con productos activos; UC-004 | Borrado lógico y comprobación transaccional; ADR-008/009 | [CatalogPersistenceTests](../tests/InventoryManagement.Infrastructure.Tests/CatalogPersistenceTests.cs): reglas y carreras con creación/reasignación |
| Conservar historia al desactivar productos; UC-008 | Deactivación sin modificar stock ni borrar movimientos | CatalogPersistenceTests y [CatalogHttpTests](../tests/InventoryManagement.Api.Tests/CatalogHttpTests.cs) |
| Balance e historial con filtros y paginación; UC-011/012 | [InventoryQueries](../src/InventoryManagement.Infrastructure/Persistence/InventoryQueries.cs), límites en Application y orden `CreatedAt DESC, Id DESC` | [InventoryReadPersistenceTests](../tests/InventoryManagement.Infrastructure.Tests/InventoryReadPersistenceTests.cs) y [InventoryReadHttpTests](../tests/InventoryManagement.Api.Tests/InventoryReadHttpTests.cs) |
| EF para lecturas, Dapper para escrituras; secciones 26/27 | Puertos en Application e implementaciones en Infrastructure; EF participa en la transacción de escritura sin realizar mutaciones | [ReadOnlyContextTests](../tests/InventoryManagement.Infrastructure.Tests/ReadOnlyContextTests.cs), revisión de fuente y pruebas SQL |
| 401 sin autenticación válida y 403 sin permiso; sección 25 | Políticas explícitas y validación JWT en API | [JwtAuthorizationTests](../tests/InventoryManagement.Api.Tests/JwtAuthorizationTests.cs): 13 rutas y siete condiciones de token inválido |

El spec no resuelve todos los detalles. Los tamaños de página, el desempate por ID, los permisos concretos y el mecanismo de migración se precisaron en [architecture-decisions.md](architecture-decisions.md). Los datos de ejemplo y la cuenta `adminInventory` son solicitudes posteriores de desarrollo, no requisitos originales del dominio.

El documento prescribe TDD, pero no hay evidencia histórica completa para certificar un ciclo rojo-verde-refactor en cada funcionalidad. La existencia de pruebas actuales no demuestra por sí sola que todas se escribieran antes que el código.

## 3. Arquitectura resultante y decisiones de la colaboración

Las dependencias de proyecto se organizan así: **Application → Domain; Infrastructure → Application/Domain; API → Application/Infrastructure**. Domain contiene entidades e invariantes; Application define comandos, consultas, handlers y puertos; Infrastructure implementa persistencia; API compone dependencias, autenticación y contratos HTTP. La referencia de API a Infrastructure permite registrar implementaciones en la raíz de composición.

Una consulta recorre controlador → handler → puerto de lectura → EF Core. Un comando recorre controlador → handler → puerto de escritura; Infrastructure coordina la lectura de validación con EF y las mutaciones con Dapper. CQRS separa responsabilidades y contratos, pero no requiere dos bases de datos en esta solución.

### Propuestas de IA incorporadas

La columna de decisión refleja el diseño documentado e implementado, no una aprobación verbal individual que no conste en la conversación. Los motivos proceden de los ADR.

| Propuesta de IA | Decisión incorporada y motivo | Coste o límite reconocido |
| --- | --- | --- |
| Persistir el saldo, ADR-001 | `Products.CurrentStock`, actualizado junto al movimiento, para lecturas eficientes | Duplica información derivable y exige consistencia transaccional |
| Transacción serializable y bloqueos, ADR-002/006 | `UPDLOCK, HOLDLOCK`, EF y Dapper sobre la misma conexión/transacción; restricción SQL contra stock negativo | Las operaciones sobre el mismo producto esperan entre sí |
| Interfaces CQRS propias, ADR-003 | Handlers explícitos en lugar de agregar MediatR para este alcance | No hay pipeline transversal provisto por esa biblioteca |
| Borrado lógico, ADR-004/008 | Conservar referencias e historial; impedir desactivar categorías con productos activos | Los registros inactivos siguen visibles y reservan nombres/SKU |
| Puertos específicos, ADR-014 | Stores y consultas por responsabilidad, sin repositorio genérico | Más interfaces pequeñas; algunas validaciones se repiten bajo bloqueo para evitar carreras |
| Keycloak y autoridad coherente, ADR-005/013 | OAuth2/OIDC con PKCE y mismo hostname para navegador/contenedores | Configuración HTTP local de desarrollo, no configuración final de producción |
| Permisos de lectura/escritura, ADR-011 | Seis permisos independientes y claim `permissions` | Iniciar sesión no concede permisos automáticamente; requiere configurar roles/mappers |
| Migraciones SQL versionadas, ADR-012 | Scripts embebidos, checksum, journal, transacción y servicio previo al API | Solo avance; adoptar V001 no equivale a detectar cualquier desviación manual de esquema |
| Paginación limitada, ADR-010 | Tamaño predeterminado 20, máximo 100 y página máxima 10000, con desempate único | Conteo y página no comparten snapshot; las inserciones pueden desplazar resultados |

### Decisiones corregidas, modificadas o acotadas

1. **Pruebas en bases temporales → base existente.** El desarrollador rechazó explícitamente la estrategia inicial y autorizó usar la base del proyecto. La fixture conserva una instantánea de IDs y elimina filas nuevas al terminar, sin eliminar la base. La concesión es específica del entorno autorizado: presupone que no hay escritores externos concurrentes y no constituye una recomendación para producción.
2. **Acceso general por autenticación → usuario inicial específico.** Ante la consulta sobre dar todos los permisos por defecto, la IA propuso una cuenta explícita. El desarrollador pidió `adminInventory`; se implementó un inicializador que asigna los seis roles a esa cuenta. Se mantuvieron las políticas y no se concedió acceso completo a cualquier usuario autenticado.
3. **Modificar la migración inicial → añadir V003.** El desarrollador pidió ampliar los datos iniciales. La IA propuso una migración nueva para respetar los checksums ya registrados. Se conservó General y se añadieron categorías, productos y movimientos relacionados en orden. Se implementó esa alternativa; no consta una aprobación verbal separada de ese mecanismo.
4. **Validación de aceptación → colección Postman.** El desarrollador eligió Postman por el tiempo disponible después de solicitar alternativas. El resultado es un recorrido ordenado con aserciones, sin afirmar que reemplaza pruebas SQL de concurrencia o certifica el login de navegador.

## 4. Errores de la IA y cómo se supervisaron

**Configuración de Keycloak incorrecta.** El JSON generado usaba `roles.clients`. El servicio rechazó la importación; el desarrollador aportó el mensaje y solicitó corregirlo. Se cambió a `roles.client`, manteniendo `clients` en la raíz. Un JSON sintácticamente válido no garantiza compatibilidad con el esquema de Keycloak; el build .NET tampoco valida ese contrato externo.

**Consulta EF que no se traducía a SQL.** Las primeras ejecuciones SQL autorizadas detectaron cinco fallos de catálogo. La proyección a `CategoryDto` precedía al filtro u ordenamiento. [CategoryQueries](../src/InventoryManagement.Infrastructure/Persistence/CategoryQueries.cs) aplica ahora esas operaciones sobre entidades antes de proyectar. Las pruebas HTTP con dobles no habían demostrado compatibilidad con el proveedor SQL real.

**Dato inicial no exigido por el spec.** El desarrollador preguntó por la categoría General. La IA reconoció que era una decisión de implementación, no una exigencia del dominio, y no pudo atribuir su inserción a una ejecución concreta. La ampliación posterior del seed sí respondió a una solicitud explícita.

**Autenticado no significa autorizado.** El desarrollador aportó un 403 real desde Swagger. Se compararon el token y la política del endpoint; faltaba `products.read`. La solución conservó la validación y configuró el acceso, en vez de retirar las políticas para ocultar el problema.

Estos casos muestran por qué las salidas de IA se trataron como propuestas verificables: el desarrollador aportó restricciones y evidencia operativa, y las pruebas descubrieron defectos que la compilación y los dobles de persistencia no detectaban.

## 5. Evidencia de validación y límites de la entrega

Los resultados siguientes corresponden a ejecuciones previas observadas en el historial y registradas en [current-status.md](current-status.md); **no se han vuelto a ejecutar para redactar este reporte**.

| Evidencia | Resultado y alcance |
| --- | --- |
| Suite con conexión a InventoryManagement | 206 aprobadas, ninguna omitida: 36 Domain, 33 Application, 102 API y 35 Infrastructure; estas últimas incluyen 33 SQL y dos guardas del contexto |
| Ejecuciones posteriores sin conexión SQL configurada | 173 aprobadas y 33 omitidas; no equivalen a una nueva certificación SQL |
| Build, publicación Release y configuración Compose | Verificados en los hitos documentados; no prueban por sí solos un despliegue completo |
| Cuenta de desarrollo | Inicializador ejecutado dos veces contra Keycloak: creación y posterior conservación del usuario, con seis roles comprobados |
| Postman | [Colección](../postman/InventoryManagement.postman_collection.json), [entorno](../postman/Local.postman_environment.json) y [guía](../postman/README.md): 40 solicitudes, JSON y scripts validados; ejecución con token real pendiente. El caso 403 requiere un token restringido y se omite si falta |

No se aporta un porcentaje de cobertura ni se afirma que todos los criterios estén comprobados de extremo a extremo. Las pruebas JWT locales no prueban el flujo interactivo de Keycloak; una colección generada no es evidencia de ejecución; una prueba omitida no es una prueba aprobada. Los resultados históricos de despliegue no certifican automáticamente cada cambio posterior.

La cronología completa y las aprobaciones individuales no pueden reconstruirse mediante commits porque el entorno revisado no tenía un repositorio Git disponible. Este reporte conserva esa limitación en lugar de inventar fechas de commits, capturas o actas de aprobación.

## 6. Documentos que acompañan el reporte

- [Especificación de dominio y contratos](specs/001-inventory-domain-spec.md): documento previo utilizado para dirigir las iteraciones observables.
- [Decisiones de arquitectura](architecture-decisions.md): alternativas, recomendaciones, decisiones, motivos y costes.
- [Estado y verificaciones](current-status.md): resultados por hito y limitaciones.
- [Instrucciones para la IA](../AGENTS.md): límites de arquitectura, persistencia, secretos y comprobaciones.
- [Guía general](../README.md) y [pruebas Postman](../postman/README.md): reproducción del entorno y recorrido de aceptación.

La evidencia más clara del criterio del desarrollador es la exigencia de comparar el spec con el código, la ejecución por prioridades, el rechazo explícito de una estrategia SQL no deseada, la aportación de fallos reales y la elección de una cuenta con permisos concretos. La contribución de la IA fue acelerar el análisis y la implementación, con decisiones sujetas a esas restricciones y a verificación técnica.
