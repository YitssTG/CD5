using UnityEngine;
using UnityEngine.Assertions;

public class ActiveObjectAssertion : MonoBehaviour
{
    public GameObject target;

    public void ValidateActive()
    {
        Assert.IsNotNull(target, "E2 ActiveObjectAssertion: target reference is missing.");
        Assert.IsTrue(target.activeSelf, $"E2 ActiveObjectAssertion: '{target.name}' must be active.");
        Debug.Log($"E2 PASS: {target.name} is active.", this);
    }
}
