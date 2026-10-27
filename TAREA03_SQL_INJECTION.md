# Tarea 03 — SQL Injection cooperativo

Estado: implementación y comprobaciones de código realizadas. Falta validar importación/Fusion IL weaving, presentación y recorrido completo en dos clientes reales. No se declara completada la Tarea 03.

## Configuración

- Abrir el proyecto y dejar que Unity importe los nuevos scripts. Fuera de Play, recargar Game y Environment 1 si estaban abiertas con una versión anterior en memoria.
- Game / RoleSystem: CooperativeIncident añadido al NetworkObject existente, registrado en NetworkedBehaviours y vinculado a Assets/Scripts/Roles/SQLInjection.asset.
- Environment 1: se reutilizan el único ServidorRed y su ConsolaControl asociada. El servidor sigue al Shared master para conservarse si sale su propietario. No se añadieron objetos de red ni prefabs de terminal.
- SQLInjection.asset: penalización por reporte incorrecto = 10 puntos, espera = 5 segundos, radio de interacción = 4 metros. Evidencias y explicación editables. La puntuación es una penalización del ejercicio, no monedas ni tiempo total.
- Configurar el mismo asset y generar un NUEVO build para ambos clientes. Un ejecutable anterior no contiene el nuevo NetworkBehaviour y no debe conectarse a esta versión.
- Shared se conserva; no hay paquetes nuevos. La misión utiliza RoleTerminalUI; Heat continúa únicamente en menú/pausa.

## Recorrido de aceptación (Editor + build nuevo o dos PCs)

1. Menu → crear/unirse a la misma sala → Game + Environment 1. Seleccionar roles diferentes y marcar ambos LISTO. Ver alerta SQLI-001/1 en ambos.
2. Pentester: acercarse al servidor. El indicador inferior muestra distancia. A 4 m o menos, E o TAB abre la terminal. Leer evidencias, elegir Fuerza bruta, confirmar diagnóstico y enviar reporte. Comprobar penalización de 10 puntos, espera de 5 s y retorno al análisis en ambos clientes. Doble clic no debe duplicar el cobro.
3. Pentester: elegir SQL Injection, confirmar y enviar. Administrador: comprobar notificación de reporte recibido en el indicador inferior y en su consola.
4. Administrador: acercarse a la consola asociada, E/TAB. Probar Servicio de correo: feedback sin avance. Elegir Portal de matriculas y contener. El servidor cambia a amarillo; el acceso se aísla solo en la simulación.
5. Administrador: probar Reiniciar servidor: feedback sin resolución. Elegir Consultas parametrizadas. Queda pendiente la verificación del Pentester; el Administrador no dispone de ese botón.
6. Pentester: volver a la terminal y verificar. Ambas pantallas deben mostrar incidente resuelto, servicio restaurado, explicación educativa y la misma penalización. Servidor verde. El resultado de verificación está guionizado; no se conecta a una base de datos ni ejecuta una consulta real.
7. Abrir/cerrar terminal con TAB, ESC y botón Cerrar; abrir/cerrar pausa; escribir en chat. Confirmar recuperación de movimiento, salto y cámara de ambos. Alejarse de la terminal cierra su interfaz y bloquea solicitudes remotas fuera de rango.
8. Repetir invirtiendo quién crea la sala y quién toma cada rol. Probar desconexión en investigación, reporte y verificación. Las acciones se suspenden sin compañero; cuando una nueva pareja marca LISTO se inicia un nuevo número de incidente y se limpia la penalización. No se reanuda como si el reemplazo hubiera aportado el reporte anterior.
9. Salir → Menu → crear/unirse a otra sesión. El incidente debe comenzar limpio. No cambiar a Environment 2 para esta misión: allí no existen las dos estaciones.

## Archivos de esta etapa

Nuevos (con sus .meta):
- Assets/Scripts/Roles/IncidentRules.cs: transiciones independientes de Unity/Fusion.
- Assets/Scripts/Roles/IncidentDefinition.cs y SQLInjection.asset: datos y configuración educativa.
- Assets/Scripts/Roles/CooperativeIncident.cs: estado replicado, RPC, identidad, proximidad y control de duplicados por número/revisión.

Modificados:
- Assets/Scripts/Roles/RoleTerminalUI.cs: interfaz funcional de los dos roles y bloqueo de controles reutilizado.
- Assets/Clean Multiplayer Pro/Scripts/ScripsNew/PlayerRole.cs: interacción abre la misión cuando está presente; conserva prototipo para otras escenas.
- Assets/Clean Multiplayer Pro/Scripts/ScripsNew/ServidorRed.cs: bloquea RPC antiguos durante la misión y refleja estado visual.
- Assets/Clean Multiplayer Pro/Scenes/Game.unity: un componente en RoleSystem y su registro de red.
- Assets/Clean Multiplayer Pro/Scenes/Environment 1.unity: solo Flags del NetworkObject del servidor, de DestroyWhenStateAuthorityLeaves a MasterClientObject.

## Diseño y límites

La autoridad valida al emisor RPC usando el registro de RoleManager (no el MyRole declarado por un avatar). Exige dos jugadores activos distintos, pareja coincidente con la del incidente, distancia a la estación y revisión actual. Los dobles envíos de una revisión ya procesada se ignoran. Las acciones fuera de fase no progresan. Un cambio de master con la misma pareja conserva propiedades Networked; un reemplazo de participante inicia otro ejercicio. Shared no es un servidor dedicado antitrampas: un cliente que modifique su propio proceso no está cubierto por estas comprobaciones.

Datos, reglas, networking y UI están separados. Agregar otro incidente requerirá su definición y reglas/presentación específicas; no se han implementado otros ataques. Los estados de contención y reparación son simulados y no ejecutan tráfico, SQL ni comandos del sistema.

## Verificación realizada

- Compilación C# con referencias locales de Unity/Fusion para Editor y Player: exit 0. Advertencias anteriores de BugSimulator y SetupWizard.
- 24 comprobaciones sobre los scripts reales RoleManager, CooperativeIncident e IncidentRules con sustitutos de Unity/Fusion: pasan. Cubren permisos, proximidad, estados, opciones inválidas, penalización, cooldown, duplicados, desconexión, reemplazo y secuencia completa.
- Estos sustitutos NO verifican transporte Photon, IL weaving, orden de callbacks, físicas ni interfaz renderizada. Esa validación requiere la prueba anterior.
- Respaldos previos, código de pruebas y logs están en las carpetas externas indicadas en la entrega. Sin commits automáticos ni cambios de paquetes.
