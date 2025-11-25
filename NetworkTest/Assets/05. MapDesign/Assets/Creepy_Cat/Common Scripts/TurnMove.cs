// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// Note : Thi swas my first unity script :) 
// I use this since 10 years to animate anything...
//
// IF you improve the code, do not hesitate to send me! (you'll be credited in the updates) 
// black.creepy.cat@gmail.com 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace creepycat.scifikitvol4 {

    public class TurnMove : MonoBehaviour {
		public float TurnX;
	    public float TurnY;
	    public float TurnZ;

	    public float MoveX;
	    public float MoveY;
	    public float MoveZ;

		public bool World;
		
		// Update is called once per frame
		void Update() {
			if (World == true) {
				transform.Rotate(TurnX * Time.deltaTime,TurnY * Time.deltaTime,TurnZ * Time.deltaTime, Space.World);
				transform.Translate(MoveX * Time.deltaTime, MoveY * Time.deltaTime, MoveZ * Time.deltaTime, Space.World);
			}else{
				transform.Rotate(TurnX * Time.deltaTime,TurnY * Time.deltaTime,TurnZ * Time.deltaTime, Space.Self);
				transform.Translate(MoveX * Time.deltaTime, MoveY * Time.deltaTime, MoveZ * Time.deltaTime, Space.Self);
			}
		}

	}

}

