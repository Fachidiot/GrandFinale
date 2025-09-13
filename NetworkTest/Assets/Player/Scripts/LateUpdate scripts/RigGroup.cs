using System;
using System.Collections.Generic;
using UnityEngine;

public class RigGroup : MonoBehaviour
{
	[Serializable]
	public class OnRigComponent
	{
		public OnRig onRigComponent;
		public bool mActive = true;
	}

	public List<OnRigComponent> jobsObjects = new List<OnRigComponent>();

	public List<OnRig> GetOnRigsList()
	{
		List<OnRig> returnedList = new List<OnRig>();

		foreach (var r in jobsObjects)
		{
			if (!r.mActive | r.onRigComponent == null) continue;

			returnedList.Add(r.onRigComponent);
		}
		return returnedList;
	}
}