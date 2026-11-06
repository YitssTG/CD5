# Cyber Defense: revisión de objetivos y pantalla

Revisión del código, no certificación de una partida real. No se cambiaron roles ni reglas de incidentes en esta intervención. Se conservaron cambios existentes y copias de los tres scripts modificados en la carpeta de recuperación externa clean-hud-20261106.

## Pantalla de juego

- HeatSessionUI oculta los paneles de demostración Room stats, Room Details, Inventory Toggel, Inventory UI Panel y Game Pause del GameCanvas de Game. Los sistemas no se destruyen. Si falla la carga del menú Heat, reaparecen los controles originales de sala para permitir salir.
- ChatSystem conserva recepción e historial mientras su vista está cerrada. F2 abre/cierra el chat; ESC cierra primero el chat. El HUD indica mensajes recibidos no leídos. No se replica el estado visual de otro jugador.
- F3 muestra/oculta la lista del equipo y sus controles de voz. El movimiento se bloquea al usarla. Se cierra al abrir tablet, PC o pausa.
- RoleTerminalUI muestra rol, tiempo, puntos y objetivo en una tarjeta compacta. La indicación E aparece solo junto a la estación correspondiente. Chat, tablet y pausa tienen accesos en el pie. El HUD se oculta al consultar la tablet o PC y durante la pausa.
- La pantalla se adapta a ambos roles con las mismas reglas; no se cambió el transporte de chat/voz ni se actualizaron paquetes.

## Correspondencia con los objetivos

| Objetivo | Evidencia en el proyecto | Límite / pendiente |
|---|---|---|
| Dos jugadores | FusionConnection, RoleManager y CooperativeIncident | Shared cooperativo; falta certificar recorrido completo con dos clientes actualizados. |
| Atacante y defensor / pentesting | RoleType mantiene Pentester y Admin. La investigación y la respuesta están separadas por permisos | No existe todavía ejecución activa de pruebas de ataque. Renombrar roles no cubriría este requisito. |
| Tutorial, pistas e interacción | RoleTerminalUI, TabletPresentation, RPC_Tutorial y RPC_Hint | Hay que comprobar teclado, raycasts, tamaño del cristal y sincronización en ejecución. |
| SQL Injection | SQLInjection.asset: siete pasos, evidencias, ticket, revisión, contención, reparación y verificación | Simulación guiada; no es un motor de consultas SQL ni un pentest interactivo libre. |
| Phishing | SqlInjectionFlow.BuildPhishing: cuatro decisiones, pistas y explicación final | Reporte por decisión, no utiliza el formulario completo de tickets. No simula envío de correo real. |
| Puntos y progresión | Score, Penalty, RPC_NextIncident y SqlCampaignScore | Ambos deben votar para avanzar. Se conserva el resultado SQL durante esa sesión; no hay campaña persistente entre ejecuciones. |
| Analizar dificultades | INSTRUMENTOS_TESIS.md contiene encuesta y entrevista propuestas | No hay respuestas recogidas, entrevistas realizadas ni revisión bibliográfica completada. |
| Evaluar comprensión | Documento con prueba inicial/final; MissionExitReport exporta el estado disponible | La evaluación no está incorporada en el juego. El reporte no sustituye una medición individual de aprendizaje. El historial tiene capacidad acotada. |
| Mejorar motivación y participación | Mecánicas cooperativas y cuestionario propuesto | Son objetivos por evaluar; no hay resultados de estudiantes que acrediten una mejora. |

Conclusión: cumplimiento parcial. No presentar el proyecto como terminado ni el impacto educativo como demostrado.

## Prueba de aceptación visual

1. Entrar con Editor y ejecutable nuevo. En ambos deben desaparecer los grandes paneles negros y los botones FPS, ping, inventario y cambio de escena.
2. Abrir F2, escribir y enviar un mensaje. Cerrarlo y recibir uno del compañero: comprobar indicador de no leídos, abrir y comprobar historial. Asegurar que el personaje no se mueve al escribir.
3. Abrir F3, comprobar lista y controles de voz; ESC debe cerrar primero la lista. La voz debe seguir funcionando con la lista cerrada.
4. Abrir tablet y PC: no deben superponerse chat/lista/HUD. Cerrar y comprobar que no vuelvan a aparecer los paneles de demostración.
5. Abrir pausa con ESC, continuar y salir al menú; crear otra sesión y repetir. Probar también ventana pequeña y pantalla completa.

La compilación de C# y las pruebas de lógica no verifican el aspecto final, las entradas de UI ni la red real.
