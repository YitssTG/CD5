using UnityEngine;
using UnityEngine.Assertions;

public class ReferenceAssertion : MonoBehaviour
{
    private MyClass myClassReference;

    public void ValidateReferenceIsNull()
    {
        Assert.IsNull(myClassReference, "E5 ReferenceAssertion: myClassReference is expected to be null.");
        Debug.Log("E5 PASS: myClassReference is null.", this);
    }

    public void CreateReference()
    {
        myClassReference = new MyClass();
        Debug.Log("E5: CreateReference() assigned a MyClass instance.", this);
    }

    public void ValidateReferenceIsNotNull()
    {
        Assert.IsNotNull(myClassReference, "E5 ReferenceAssertion: myClassReference is expected to be non-null after CreateReference().");
        Debug.Log("E5 PASS: myClassReference is not null.", this);
    }
}

public class MyClass { }
