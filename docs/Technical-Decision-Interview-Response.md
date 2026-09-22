**1. Describe una decisión técnica reciente que hayas tomado con información incompleta. ¿Qué te faltaba y cómo decidiste avanzar?**

En un proyecto reciente, durante una reunión con el cliente se definió que el acceso a sus datos se realizaría mediante un servicio OData. Inicialmente planteé utilizar Power Automate para consumir la información y procesarla mediante un flujo de integración.

Sin embargo, en ese momento el cliente todavía no contaba con el endpoint definitivo, por lo que no disponíamos de información completa sobre la autenticación, la estructura de los datos ni la configuración final de la conexión.

Para evitar que esta dependencia detuviera el desarrollo, decidimos crear un endpoint de prueba que simulara el comportamiento esperado del servicio del cliente. Esto nos permitió validar la comunicación, desarrollar la lógica inicial del flujo y avanzar con las siguientes etapas del proyecto mientras se completaba la integración definitiva.

---

**2. Cuando dos enfoques técnicos parecen igual de válidos, ¿qué criterios usas para elegir uno?**

Cuando dos soluciones técnicas son igualmente viables, evalúo principalmente las necesidades y restricciones del proyecto. Considero aspectos como el tiempo de desarrollo, el costo de implementación y mantenimiento, la complejidad de la solución, la escalabilidad y la facilidad de soporte a futuro.

Por ejemplo, si existe una fecha de entrega ajustada, puedo priorizar la alternativa que permita implementar la solución de manera más rápida sin comprometer su calidad. Si el presupuesto es una restricción importante, analizo cuál de las opciones representa un menor costo de infraestructura, licenciamiento o mantenimiento.

En general, busco elegir la alternativa que ofrezca el mejor equilibrio entre tiempo, costo, mantenibilidad y necesidades reales del negocio.

---

**3. Cuenta un caso en el que hayas tenido que revertir o cambiar una decisión técnica propia. ¿Qué aprendiste de esa experiencia?**

En el mismo proyecto de integración mediante OData, inicialmente decidimos utilizar Power Automate para procesar la información, ya que con los requerimientos disponibles parecía una solución adecuada.

Cuando finalmente recibimos el acceso al servicio del cliente, descubrimos que únicamente exponía tablas completas de manera independiente. Las operaciones necesarias para el proyecto requerían realizar cruces entre distintas tablas, aplicar filtros y ejecutar lógica de transformación que no se había especificado claramente durante el levantamiento inicial.

Aunque técnicamente era posible intentar resolver parte de esta lógica mediante Power Automate, la solución se volvía considerablemente más compleja y difícil de mantener. Por este motivo, decidimos cambiar el enfoque y desarrollar la integración mediante código, donde teníamos mayor control sobre las consultas, transformaciones y procesamiento de la información.

Esta experiencia me enseñó la importancia de definir con mayor detalle los contratos de integración antes de seleccionar una tecnología: estructura de los datos, filtros disponibles, relaciones entre entidades, autenticación y volumen de información. También reforzó la importancia de conocer los límites de cada herramienta y estar dispuesto a cambiar una decisión técnica cuando aparecen nuevas condiciones que modifican el escenario original.
