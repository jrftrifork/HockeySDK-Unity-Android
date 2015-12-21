﻿/*******************************************************************************
 *
 * Author: Christoph Wendt
 * 
 * Version: 1.0.8
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
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class HockeyAppAndroid : MonoBehaviour {
	private HockeyAppAndroidEngine engine;
	public string appID = "your-hockey-app-id";
	public string packageID = "your-package-identifier";
	public string serverURL = "your-custom-server-url";
	public bool autoUpload = false;
	public bool exceptionLogging = false;
	public bool updateManager = false;

	void Awake(){
		#if (UNITY_ANDROID && !UNITY_EDITOR)
		DontDestroyOnLoad(gameObject);
		engine = new HockeyAppAndroidEngine(this, appID, packageID, autoUpload, exceptionLogging, updateManager, serverURL);
		#endif
	}
	
	void OnEnable(){		
		#if (UNITY_ANDROID && !UNITY_EDITOR)
		if(exceptionLogging == true)
		{
			System.AppDomain.CurrentDomain.UnhandledException += engine.OnHandleUnresolvedException;
			Application.logMessageReceived += engine.OnHandleLogCallback;
		}
		#endif
	}
	
	void OnDisable(){
		#if (UNITY_ANDROID && !UNITY_EDITOR)
		if(exceptionLogging == true)
		{
			System.AppDomain.CurrentDomain.UnhandledException -= engine.OnHandleUnresolvedException;
			Application.logMessageReceived -= engine.OnHandleLogCallback;
		}
		#endif
	}
}
