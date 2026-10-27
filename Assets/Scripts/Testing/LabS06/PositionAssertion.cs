using UnityEngine;
using UnityEngine.Assertions;

public class PositionAssertion : MonoBehaviour
{
    public void ValidatePosition()
    {
        Vector3 position = transform.position;
        Assert.AreApproximatelyEqual(0f, position.x, 0.001f, $"E3 PositionAssertion: X expected 0, got {position.x}.");
        Assert.AreApproximatelyEqual(0f, position.y, 0.001f, $"E3 PositionAssertion: Y expected 0, got {position.y}.");
        Assert.AreApproximatelyEqual(0f, position.z, 0.001f, $"E3 PositionAssertion: Z expected 0, got {position.z}.");
        Debug.Log($"E3 PASS: position {position} is approximately zero.", this);
    }
}
