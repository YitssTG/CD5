# Pruebas reproducibles de misión

Ejecutar desde PowerShell, con .NET 9 SDK ya instalado:

```powershell
./Tests/Mission/Run.ps1
```

Opcional: `-OutputDirectory <ruta>` conserva logs y ejecutable en una carpeta elegida. Sin ese argumento se utiliza el directorio temporal del proceso. El script no instala paquetes ni descarga dependencias. Devuelve error si falla compilación o cualquier aserción.

MissionTests.cs prueba los scripts reales del proyecto. EngineStubs.cs sustituye únicamente las interfaces de motor/red necesarias para ejecutar decisiones y RPC como llamadas locales. Está FUERA de Assets y no entra en el juego.

No es Unity Test Runner ni simulación de Photon: no cubre weaving, serialización real, pérdida de paquetes, físicas, cámara o UI. La aceptación de dos clientes está en TAREA03_SQL_INJECTION.md.

`-ExportSql` escribe los datos del flujo de respaldo en sql-steps.json dentro de la carpeta de resultados para inspección. No modifica el asset de Unity.
