#if CMPSETUP_COMPLETE
using AvocadoShark;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    [SerializeField] private  TextMeshProUGUI content;

    public void ShowPopup(string text)
    {
        content.text = text;
        gameObject.SetActive(true);
    }

    public void DisablePopup()
    {
        if (FusionConnection.Instance != null) FusionConnection.Instance.ReturnToMenu();
    }
}
#endif
