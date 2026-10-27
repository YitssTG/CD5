using UnityEngine;
using UnityEngine.Assertions;

public class PlayerTagAssertion : MonoBehaviour
{
    public void ValidateTag()
    {
        string currentTag = gameObject.tag;
        Assert.AreEqual("Player", currentTag, $"E4 PlayerTagAssertion: expected tag 'Player', got '{currentTag}'.");
        Debug.Log($"E4 PASS: tag is {currentTag}.", this);
    }
}
