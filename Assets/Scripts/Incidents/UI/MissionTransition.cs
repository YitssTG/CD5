using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MissionTransition : MonoBehaviour
{
    private static MissionTransition instance;
    private CanvasGroup group;
    private TMP_Text label;
    private bool busy;
    private float shownAt;
    public static void Show(string message)
    {
        if(instance==null)
        {
            var go=new GameObject("Session transition");DontDestroyOnLoad(go);instance=go.AddComponent<MissionTransition>();
            var skin=MissionPresentationConfig.Load().adminSkin;if(skin==null)skin=IncidentUISkin.CreateDefault();
            var canvas=UIFactory.CreateCanvas("Transition",go.transform,800,new Vector2(1920,1080));
            UIFactory.CreatePanel("Backdrop",canvas.transform,Vector2.zero,Vector2.one,new Color(.01f,.025f,.04f,1),0,skin);
            instance.label=UIFactory.CreateText("Status",canvas.transform,message,32,new Vector2(.15f,.4f),new Vector2(.85f,.6f),TextAlignmentOptions.Center,skin.titleColor,skin);
            instance.group=canvas.gameObject.AddComponent<CanvasGroup>();instance.group.alpha=0;
        }
        instance.label.text=message;instance.busy=true;instance.shownAt=Time.unscaledTime;instance.group.blocksRaycasts=true;
    }
    public static void Finish(){if(instance!=null)instance.busy=false;}
    private void Update()
    {
        if(group==null)return;
        group.alpha=Mathf.MoveTowards(group.alpha,busy?1:0,Time.unscaledDeltaTime*5);
        if(!busy && group.alpha<=0){Destroy(gameObject);return;}
        if(busy && Time.unscaledTime-shownAt>15)label.text="La conexion esta tardando. Esperando respuesta de la sesion...";
    }
    private void OnDestroy(){if(instance==this)instance=null;}
}
