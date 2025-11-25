// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (credited to the updates) 
// black.creepy.cat@gmail.com 

using UnityEngine;
using System;

/* Timer class.
Example usage:
public class Mover : MonoBehaviour {
	public Vector3 startPosition;
	public Vector3 endPosition;
	public EasyTimer timer = new Timer();
	void Start () {
		// Set timer so we move from startPosition to endPosition over 1 second
		timer.SetNewDuration(1f);
		// note: if you want to keep moving even if Unity's Time.timeScale is 0, set `timer.useUnscaledTime = true`;
	}
	void Update () {
		transform.position = Vector3.Lerp(startPosition, endPosition, timer.PercentageDone);
		if (timer.IsDone) {
			Debug.Log("Finished moving!");
		}
	}
}
*/

namespace creepycat.scifikitvol4
{
    // A small code found on the web, thanks github and the author... 
    // https://gist.github.com/ChrisNZL/6e3fd0f1ec8bd35ce53feb04b12c0fcc

    [System.Serializable]
    public class EasyTimer {

	    public bool useUnscaledTime;

	    [SerializeField] private float startTime;
	    [SerializeField] private float endTime;
	    [SerializeField] private float duration;

	    // Set a new duration, effectively resetting the timer
	    public void SetNewDuration (float newDuration) {

		    if (newDuration == 0f) {
			    Debug.LogWarning("WARNING: Timer's new duration is 0");
		    }

		    duration = newDuration;
		    startTime = this.Time;
		    endTime = startTime + duration;
	    }

	    // Get the percentage done, good for Lerp methods
	    public float PercentageDone {
		    get {
			    return Mathf.InverseLerp(startTime, endTime, this.Time);
		    }
	    }

	    // Get the percentage done with a SmoothStep applied
	    public float PercentageDoneSmoothStep {
		    get {
			    return Mathf.SmoothStep(0f, 1f, this.PercentageDone);
		    }
	    }
	
	    // See if the current time is greater than or equal to the end time
	    public bool IsDone {
		    get {
			    return this.Time >= endTime;
		    }
	    }

	    // Extend the timer if needed
	    public void ExtendDuration (float amount) {
		    startTime += amount;
		    endTime += amount;
	    }

	    private float Time {
		    get {
			    return useUnscaledTime ? UnityEngine.Time.unscaledTime : UnityEngine.Time.time;
		    }
	    }

    }

}