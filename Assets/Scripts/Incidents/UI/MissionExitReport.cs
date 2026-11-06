using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;

// Last observed snapshot survives Runner shutdown. No persisted account data or invented results.
public static class MissionExitReport
{
    private static string snapshot;
    private static bool dismissed;
    private static float capturedAt;
    private static int lastRecord = -1;
    public static void Capture(CooperativeIncident mission)
    {
        if (!mission.IsOnline || mission.Phase == MissionPhase.Waiting || dismissed) return;
        if (lastRecord == mission.RecordCount && Time.unscaledTime - capturedAt < .5f) return;
        snapshot = mission.Summary() + "\n\nULTIMO ESTADO RECIBIDO\n" + mission.Journal();
        capturedAt = Time.unscaledTime; lastRecord = mission.RecordCount;
    }
    public static void Dismiss() { snapshot = null; dismissed = true; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        snapshot = null; dismissed = false; lastRecord = -1;
        SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded;
    }
    private static void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game") { dismissed = false; snapshot = null; lastRecord = -1; }
        if (scene.name != "Menu" || string.IsNullOrEmpty(snapshot)) return;
        var root = new GameObject("Last session report"); SceneManager.MoveGameObjectToScene(root, scene);
        var skin = MissionPresentationConfig.Load().adminSkin;
        if (skin == null) skin = IncidentUISkin.CreateDefault();
        var canvas = UIFactory.CreateCanvas("Report",root.transform,900,new Vector2(1920,1080));
        UIFactory.CreatePanel("Background",canvas.transform,Vector2.zero,Vector2.one,new Color(.01f,.025f,.04f,.98f),0,skin);
        var scrollGo=new GameObject("Scroll",typeof(RectTransform),typeof(ScrollRect),typeof(RectMask2D),typeof(Image));scrollGo.transform.SetParent(canvas.transform,false);
        var rect=scrollGo.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.1f,.16f);rect.anchorMax=new Vector2(.9f,.9f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        scrollGo.GetComponent<Image>().color=new Color(.02f,.04f,.06f);
        var content=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI),typeof(ContentSizeFitter));content.transform.SetParent(scrollGo.transform,false);
        var cr=content.GetComponent<RectTransform>();cr.anchorMin=new Vector2(0,1);cr.anchorMax=Vector2.one;cr.pivot=new Vector2(.5f,1);cr.offsetMin=cr.offsetMax=Vector2.zero;
        var text=content.GetComponent<TextMeshProUGUI>();text.text=snapshot;text.fontSize=24;text.richText=false;text.color=Color.white;
        content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=scrollGo.GetComponent<ScrollRect>();scroll.viewport=rect;scroll.content=cr;scroll.horizontal=false;scroll.scrollSensitivity=35;
        var notice=UIFactory.CreateText("Export status",canvas.transform,"",18,new Vector2(.1f,.125f),new Vector2(.9f,.16f),TextAlignmentOptions.Center,Color.white,skin);
        notice.raycastTarget=false;
        var export=UIFactory.CreateButton("Export",canvas.transform,"Guardar reporte local",new Vector2(.18f,.05f),new Vector2(.48f,.12f),skin.buttonPrimary,skin);
        export.onClick.AddListener(()=>{
            try {
                string dir=Path.Combine(Application.persistentDataPath,"Reportes");
                Directory.CreateDirectory(dir);
                string file=Path.Combine(dir,"CyberDefense-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8)+".txt");
                File.WriteAllText(file,"Reporte de partida simulado; no mide por si solo el aprendizaje.\n\n"+snapshot);
                notice.text="Guardado en: "+file;
            } catch (Exception ex) { notice.text="No se pudo guardar el reporte: "+ex.Message; }
        });
        var close=UIFactory.CreateButton("Close",canvas.transform,"Continuar al menu",new Vector2(.52f,.05f),new Vector2(.82f,.12f),skin.buttonPrimary,skin);
        close.onClick.AddListener(()=>{Dismiss();UnityEngine.Object.Destroy(root);});
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
}
