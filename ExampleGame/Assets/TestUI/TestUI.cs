/*******************************************************************************
 *
 * Author: Christoph Wendt
 * 
 * Version: 1.0.6
 * 
 * Copyright (c) 2013-2015 HockeyApp, Bit Stadium GmbH.
 * All rights reserved.
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 * 
 ******************************************************************************/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class TestUI : MonoBehaviour{

	public GUISkin customUISkin;
	private int controlHeight = 64;
	private int horizontalMargin = 20;
	private int space = 20;

	#if (UNITY_ANDROID && !UNITY_EDITOR)
	private string appID = "b90dca1145f290bf8031784c196b34df";
	private string serverURL = "    https://rink.hockeyapp.net/     ";
	#endif

	void OnGUI(){	

		AutoResize (640, 1136);
		GUI.skin = customUISkin;

		GUI.Label(GetControlRect(1), "Choose an exception type");

		if(GUI.Button(GetControlRect(2), "Divide By Zero"))
		{

			int i = 0;
			i = 5 / i;
		}

		if(GUI.Button(GetControlRect(3), "Native Code Crash"))
		{	
			ForceAppCrash();	
		}

		if(GUI.Button(GetControlRect(4), "Index Out Of Range"))
		{
			string[] arr	= new string[3];
			arr[4]	= "Out of Range";
		}

		if(GUI.Button(GetControlRect(5), "Custom Exception"))
		{	
			throw new System.Exception("My Custom Exception");	
		}

		if(GUI.Button(GetControlRect(6), "Custom Coroutine Exception"))
		{	
			StartCoroutine(CorutineCrash());	
		}

		if(GUI.Button(GetControlRect(7), "Handled Null Pointer Exception"))
		{	
			try {
				NullReferenceException();
			} catch (Exception e) {
				throw new Exception("Null Pointer Exception");
			}	
		}

		if(GUI.Button(GetControlRect(8), "Null Pointer Exception"))
		{
			NullReferenceException();
		}

		if(GUI.Button(GetControlRect(9), "Coroutine Null Exception"))
		{	
			StartCoroutine(CorutineNullCrash());	
		}

		GUI.Label(GetControlRect(10), "Features");

		if(GUI.Button(GetControlRect(11), "Show Feedback Form"))
		{	
			ShowFeedbackForm();
		}
	}

	private Rect GetControlRect(int controlIndex){

		return new Rect (horizontalMargin,
		                controlIndex * (controlHeight + space),
		                640 - (2 * horizontalMargin),
		                controlHeight);
	}

	public void AutoResize(int screenWidth, int screenHeight){

		Vector2 resizeRatio = new Vector2((float)Screen.width / screenWidth, (float)Screen.height / screenHeight);
		GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(resizeRatio.x, resizeRatio.y, 1.0f));
	}

	System.Collections.IEnumerator CorutineNullCrash(){

		string crash = null;
		crash	= crash.ToLower();
		yield break;
	}
	
	System.Collections.IEnumerator CorutineCrash(){	

		throw new System.Exception("Custom Coroutine Exception");
	}

	public void NullReferenceException(){
		object testObject = null;
		testObject.GetHashCode();
	}
	
	public void ForceAppCrash(){

		#if (UNITY_ANDROID && !UNITY_EDITOR)
		AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
		AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"); 
		AndroidJavaObject exampleClass = new AndroidJavaObject("net.hockeyapp.exampleunityplugin.ExampleClass"); 
		exampleClass.Call("forceAppCrash", activity);
		#endif
	}

	public void ShowFeedbackForm(){
		
		#if (UNITY_ANDROID && !UNITY_EDITOR)
		AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
		AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"); 
		AndroidJavaClass pluginClass = new AndroidJavaClass("net.hockeyapp.unity.HockeyUnityPlugin"); 
		pluginClass.CallStatic("startFeedbackForm", currentActivity);
		#endif
	}
}