using UnityEngine;
using UnityEngine.Assertions;

public class HealthAssertion : MonoBehaviour
{
    public int health = 100;

    public void ValidateHealth()
    {
        Assert.AreNotEqual(0, health, "E1 HealthAssertion: Player health must not be 0.");
        Debug.Log($"E1 PASS: health is {health} (not 0).", this);
    }
}
