using UnityEngine;

public class OnRig : MonoBehaviour
{
    public virtual bool isReturnableToOriginalState {get; set;}
	public virtual void ReturnToOriginalState() {}
	public virtual void Execute() {}
}
