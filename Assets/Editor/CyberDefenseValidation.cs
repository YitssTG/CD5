#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using Fusion;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CyberDefenseValidation
{
    [MenuItem("CyberDefense/Validar configuracion de la mision")]
    public static void Validate()
    {
        ValidateMissionNetworkBudget();
        var d=AssetDatabase.LoadAssetAtPath<IncidentDefinition>("Assets/Scripts/Roles/SQLInjection.asset");
        if(d==null || !d.HasSteps || d.steps.Length!=7)throw new InvalidOperationException("SQLInjection debe tener los siete pasos configurados.");
        string[] ids={"scan","evidence","ticket","review","contain","repair","verify"};
        for(int i=0;i<ids.Length;i++)
        {
            var step=d.steps[i];if(step==null || step.stepId!=ids[i])throw new InvalidOperationException("Orden de pasos incompatible con el flujo de tickets.");
            if(step.hints!=null && step.hints.Length>30)throw new InvalidOperationException("Maximo 30 pistas por paso.");
            if(step.HasMinigame && (step.minigame.options.Length>4 || step.minigame.options.Length==0))throw new InvalidOperationException("La UI admite 1 a 4 opciones por paso.");
        }
        var config=AssetDatabase.LoadAssetAtPath<MissionPresentationConfig>("Assets/Resources/MissionPresentation.asset");
        if(config==null || config.tabletPrefab==null || config.pentesterSkin==null || config.adminSkin==null || config.notification==null || config.click==null)
            throw new InvalidOperationException("Referencias de presentacion incompletas.");
        foreach(var scene in EditorBuildSettings.scenes)if(scene.enabled && !File.Exists(scene.path))throw new InvalidOperationException("Escena ausente: "+scene.path);
        Debug.Log("[CyberDefense] Configuracion, pasos, tablet, temas y audio validos. Falta la prueba de dos clientes.");
    }
    public static void ValidateMissionNetworkBudget()
    {
        // Read Fusion's actual woven layout, not estimates based on C# field counts.
        int bytes = 256; // Conservative allowance for the scene object's header.
        foreach (var type in new[] { typeof(RoleManager), typeof(CooperativeIncident) })
        {
            var layout = (NetworkBehaviourWeavedAttribute)Attribute.GetCustomAttribute(type, typeof(NetworkBehaviourWeavedAttribute));
            if (layout == null) throw new BuildFailedException("Fusion aun no ha procesado " + type.Name + ". Espera a que Unity termine de compilar.");
            bytes += layout.WordCount * 4;
        }
        if (bytes > 32768) throw new BuildFailedException("RoleSystem excede el limite de Fusion: " + bytes + " bytes (incluye margen de cabecera), maximo 32768.");
        Debug.Log("[CyberDefense] RoleSystem: " + bytes + " bytes incluyendo margen de cabecera; limite 32768.");
    }
    [MenuItem("CyberDefense/Crear build Windows en carpeta nueva")]
    public static void BuildWindows()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Deten Play antes del build.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)
            throw new InvalidOperationException("Hay escenas sin guardar. Revisalas y guardalas manualmente antes del build; no se descartaron cambios.");
        Validate();
        string dir="Builds/CooperativeMission-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        if(Directory.Exists(dir))throw new InvalidOperationException("La carpeta de build ya existe.");
        Directory.CreateDirectory(dir);
        var scenes=Array.FindAll(EditorBuildSettings.scenes,s=>s.enabled);
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=Array.ConvertAll(scenes,s=>s.path),
            target=BuildTarget.StandaloneWindows64,locationPathName=dir+"/CyberDefenseCoop.exe",options=BuildOptions.Development });
        if(result.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Build fallido: consulta la consola de Unity.");
        Debug.Log("[CyberDefense] Build nuevo: "+Path.GetFullPath(dir));
    }
}

// Also guard normal Unity builds, even when the custom build menu is not used.
public sealed class CyberDefenseNetworkBuildGuard : IPreprocessBuildWithReport, IProcessSceneWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) => CyberDefenseValidation.ValidateMissionNetworkBudget();
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        foreach (var root in scene.GetRootGameObjects())
        foreach (var obj in root.GetComponentsInChildren<NetworkObject>(true))
        {
            int bytes = NetworkObject.GetWordCount(obj) * 4;
            if (bytes > 32768) throw new BuildFailedException(scene.path + "/" + obj.name + ": estado Fusion de " + bytes + " bytes, maximo 32768.");
        }
    }
}
#endif
