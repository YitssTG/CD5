using System;

// Same seven-step contract as the configured SQLInjection asset. Used only for legacy empty definitions.
public static class SqlInjectionFlow
{
    public static IncidentStep[] BuildPhishing()
    {
        var steps = new[] {
            Make("phish_evidence", IncidentStage.Investigating, RoleType.Pentester, "Analiza el correo simulado", "Correo de entrenamiento: remitente soporte@centro-ayuda.example; pide acceder urgentemente a https://acceso-verificacion.example. El dominio no coincide con el canal institucional. Identifica las senales; no abras enlaces reales.", 10),
            Make("phish_report", IncidentStage.Diagnosed, RoleType.Pentester, "Comunica el diagnostico al defensor", "El informe debe distinguir los datos observados de las suposiciones.", 10),
            Make("phish_contain", IncidentStage.Reported, RoleType.Admin, "Contiene el riesgo para los usuarios", "Se sospecha que un usuario introdujo credenciales. Elige una respuesta proporcionada.", 15),
            Make("phish_verify", IncidentStage.Verification, RoleType.Pentester, "Verifica la respuesta", "Comprueba que las sesiones sospechosas fueron revocadas y que el acceso legitimo funciona con credenciales restablecidas.", 15)
        };
        steps[0].minigame = new MinigameDefinition { kind=MinigameKind.RequestAnalysis, prompt="Marca las tres senales observadas", options=new[] {
            Option("sender","Remitente con dominio diferente",true), Option("urgency","Solicitud urgente de acceso",true),
            Option("link","Enlace con dominio no institucional",true), Option("hardware","Fallo fisico del servidor",false) } };
        steps[1].minigame = new MinigameDefinition { kind=MinigameKind.SimpleChoice, prompt="Selecciona el reporte basado en evidencias", options=new[] {
            Option("correct","Posible phishing: verificar por canal oficial",true), Option("wrong","Es un fallo fisico confirmado",false) } };
        steps[2].minigame = new MinigameDefinition { kind=MinigameKind.SimpleChoice, prompt="Elige la respuesta inicial", options=new[] {
            Option("revoke","Revocar sesiones y restablecer acceso por canal verificado",true), Option("forward","Reenviar el enlace a todos los usuarios",false), Option("ignore","Ignorar el reporte",false) } };
        steps[3].minigame = new MinigameDefinition { kind=MinigameKind.SimpleChoice, prompt="Evalua ambas comprobaciones", options=new[] {
            Option("both","Sesiones revocadas y acceso legitimo recuperado",true), Option("one","Basta con reiniciar el monitor",false) } };
        steps[0].hints=new[]{"Compara el dominio con el canal institucional y observa la urgencia."};
        steps[1].hints=new[]{"Reporta hechos observados y no confirmes una causa sin evidencia."};
        steps[2].hints=new[]{"Protege las cuentas y usa un canal de comunicacion verificado."};
        steps[3].hints=new[]{"Comprueba tanto la seguridad como el acceso legitimo."};
        foreach(var step in steps) { step.penaltyPoints=10; step.failureFeedback="La respuesta no corresponde a la evidencia. Consulta la pista y vuelve a intentarlo."; }
        steps[3].completesIncident=true;
        return steps;
    }
    public static IncidentStep[] Build(IncidentDefinition d)
    {
        var steps = new[] {
            Make("scan", IncidentStage.Alert, RoleType.Pentester, "Identifica SRV-01 e inicia el analisis", "El portal registra accesos sin sesion valida. Analiza el servidor para obtener evidencias.", 5),
            Make("evidence", IncidentStage.Investigating, RoleType.Pentester, "Selecciona las evidencias relevantes en el servidor", d.evidence, 10),
            Make("ticket", IncidentStage.Diagnosed, RoleType.Pentester, "Redacta y envia el ticket desde tu tablet", "Incluye servicio, diagnostico, evidencias, gravedad y recomendacion. El Administrador revisara tu propuesta.", 0),
            Make("review", IncidentStage.Reported, RoleType.Admin, "Introduce el codigo del ticket en tu PC y revisalo", "Contrasta el diagnostico con las evidencias. Puedes aceptar o solicitar correccion.", 0),
            Make("contain", IncidentStage.Reported, RoleType.Admin, "Selecciona el servicio afectado y contienelo", "Aisla temporalmente el acceso al servicio identificado en el reporte. La contencion no elimina la causa.", 10),
            Make("repair", IncidentStage.Contained, RoleType.Admin, "Ordena la reparacion del servicio", "El servicio esta aislado. Ordena las tareas para separar instrucciones y datos antes de desplegar la correccion.", 20),
            Make("verify", IncidentStage.Verification, RoleType.Pentester, "Verifica la reparacion antes de restaurar", "Pruebas simuladas: la entrada sospechosa se trata como texto y no altera SQL; el acceso legitimo sigue funcionando. Evalua ambos resultados.", 10)
        };
        steps[1].minigame = new MinigameDefinition { kind = MinigameKind.RequestAnalysis, prompt = "Marca todas las evidencias relevantes", options = new[] {
            Option("concat", "Entrada concatenada en SQL", true), Option("logic", "Cambio de la condicion de consulta", true),
            Option("access", "Acceso sin sesion valida", true), Option("email", "Correo de phishing no observado", false) } };
        steps[4].minigame = new MinigameDefinition { kind = MinigameKind.SimpleChoice, options = new[] { Option("portal", "Portal de matriculas", true), Option("mail", "Servicio de correo", false) } };
        steps[5].minigame = new MinigameDefinition { kind = MinigameKind.ServerRepair, prompt = "Selecciona en orden (clic de nuevo para retirar)", requiredOrder = new[] {0,1,2}, options = new[] {
            Option("params", "1. Sustituir concatenacion por parametros", true), Option("test", "2. Probar entrada y acceso legitimo", true),
            Option("deploy", "3. Desplegar correccion en servicio aislado", true), Option("restart", "Reiniciar sin corregir la consulta", false) } };
        // Do not reveal the sequence numbers in presentation; the rules retain the order.
        steps[5].minigame.options[0].label = "Sustituir concatenacion por parametros";
        steps[5].minigame.options[1].label = "Probar entrada y acceso legitimo";
        steps[5].minigame.options[2].label = "Desplegar correccion en servicio aislado";
        steps[6].minigame = new MinigameDefinition { kind = MinigameKind.SimpleChoice, options = new[] {
            Option("both", "Ambas pruebas cumplen: restaurar servicio", true), Option("one", "Basta con que el servidor encienda", false) } };
        steps[6].completesIncident = true;
        foreach (var s in steps) { s.penaltyPoints = d.wrongReportPenalty; s.penaltyDelay = d.wrongReportDelay; }
        steps[1].hints = new[] { "Distingue lo observado de las explicaciones sin evidencia.", "Busca la frontera perdida entre datos e instrucciones." };
        steps[2].hints = new[] { "Un acceso no autorizado a cuentas tiene impacto alto.", "La recomendacion debe corregir la construccion de consultas." };
        steps[3].hints = new[] { "Compara servicio, evidencias y recomendacion antes de aceptar." };
        steps[4].hints = new[] { "El reporte identifica el portal, no el correo." };
        steps[5].hints = new[] { "Primero corrige, despues prueba y finalmente despliega." };
        steps[6].hints = new[] { "Seguridad y funcionamiento legitimo deben cumplirse juntos." };
        return steps;
    }
    private static IncidentStep Make(string id, IncidentStage stage, RoleType role, string objective, string briefing, int points) =>
        new IncidentStep { stepId = id, stage = stage, authorizedRole = role, objective = objective, briefing = briefing,
            successPoints = points, minigame = new MinigameDefinition { kind = MinigameKind.None },
            successFeedback = "Completado: " + objective, failureFeedback = "La decision no corresponde a las evidencias o al orden requerido. Revisa la explicacion y reintenta." };
    private static MinigameOption Option(string id, string label, bool correct) => new MinigameOption { id = id, label = label, isCorrect = correct, isDistractor = !correct };
}
