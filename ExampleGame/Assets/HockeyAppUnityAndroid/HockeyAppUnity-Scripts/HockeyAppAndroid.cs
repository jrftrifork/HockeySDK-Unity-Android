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

public class HockeyAppAndroidEngine {
	
	protected const string HOCKEYAPP_BASEURL = "https://rink.hockeyapp.net/";
	protected const string HOCKEYAPP_CRASHESPATH = "api/2/apps/[APPID]/crashes/upload";

	protected const int MAX_CHARS = 199800;
	protected const string LOG_FILE_DIR = "/logs/";
	private readonly string appID = "your-hockey-app-id";
	private readonly string packageID = "your-package-identifier";
	private readonly string serverURL = "your-custom-server-url";
	private readonly bool exceptionLogging = false;
    private HockeyAppCrashManagerListener listener;

    public HockeyAppAndroidEngine(MonoBehaviour monoBehaviour, string appID, string packageID, bool autoUpload, bool exceptionLogging, bool updateManager, string serverURL=null, HockeyAppCrashManagerListener listener = null) {
		this.appID = appID;
		this.packageID = packageID;
		this.exceptionLogging = exceptionLogging;
		this.serverURL = (serverURL != null) ? serverURL : "";
        this.listener = listener;

        if(exceptionLogging == true  && IsConnected() == true)
		{
			List<string> logFileDirs = GetLogFiles();
			if(logFileDirs.Count > 0)
			{
				monoBehaviour.StartCoroutine(SendLogs(logFileDirs));
			}
		}
		string urlString = GetBaseURL();
		StartCrashManager(urlString, appID, updateManager, autoUpload);
	}
	
	public void RegisterListeners(){
        if(exceptionLogging == true)
		{
			System.AppDomain.CurrentDomain.UnhandledException += OnHandleUnresolvedException;
			Application.logMessageReceived += OnHandleLogCallback;
		}
	}
	
	public void UnRegisterListeners(){
		if(exceptionLogging == true)
		{
			System.AppDomain.CurrentDomain.UnhandledException -= OnHandleUnresolvedException;
			Application.logMessageReceived -= OnHandleLogCallback;
		}
	}

	/// <summary>
	/// Start HockeyApp for Unity.
	/// </summary>
	/// <param name="appID">The app specific Identifier provided by HockeyApp</param>
	protected void StartCrashManager(string urlString, string appID, bool updateManagerEnabled, bool autoSendEnabled) {
		AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
		AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"); 
		AndroidJavaClass pluginClass = new AndroidJavaClass("net.hockeyapp.unity.HockeyUnityPlugin"); 
		pluginClass.CallStatic("startHockeyAppManager", currentActivity, urlString, appID, updateManagerEnabled, autoSendEnabled);
	}

	/// <summary>
	/// Get the version code of the app.
	/// </summary>
	/// <returns>The version code of the Android app.</returns>
	protected String GetVersionCode(){
		AndroidJavaClass jc = new AndroidJavaClass("net.hockeyapp.unity.HockeyUnityPlugin"); 
		string versionCode =  jc.CallStatic<string>("getVersionCode");

		return versionCode;
	}

	/// <summary>
	/// Get the version name of the app.
	/// </summary>
	/// <returns>The version name of the Android app.</returns>
	protected String GetVersionName(){
		AndroidJavaClass jc = new AndroidJavaClass("net.hockeyapp.unity.HockeyUnityPlugin"); 
		string versionName =  jc.CallStatic<string>("getVersionName");
		
		return versionName;
	}

	/// <summary>
	/// Get the SDK version.
	/// </summary>
	/// <returns>The SDK version.</returns>
	protected String GetSdkVersion(){
		AndroidJavaClass jc = new AndroidJavaClass("net.hockeyapp.unity.HockeyUnityPlugin"); 
		string sdkVersion =  jc.CallStatic<string>("getSdkVersion");
		
		return sdkVersion;
	}

	/// <summary>
	/// Get the name of the SDK.
	/// </summary>
	/// <returns>The name of the SDK.</returns>
	protected String GetSdkName(){
		AndroidJavaClass jc = new AndroidJavaClass("net.hockeyapp.unity.HockeyUnityPlugin"); 
		string sdkName =  jc.CallStatic<string>("getSdkName");
		
		return sdkName;
	}


	/// <summary>
	/// Collect all header fields for the custom exception report.
	/// </summary>
	/// <returns>A list which contains the header fields for a log file.</returns>
	protected virtual List<string> GetLogHeaders() {
		List<string> list = new List<string>();

		list.Add("Package: " + packageID);

		string versionCode = GetVersionCode();
		list.Add("Version Code: " + versionCode);

		string versionName = GetVersionName();
		list.Add("Version Name: " + versionName);

		string[] versionComponents = SystemInfo.operatingSystem.Split('/');
		string osVersion = "Android: " + versionComponents[0].Replace("Android OS ", "");
		list.Add (osVersion);
		
		list.Add("Model: " + SystemInfo.deviceModel);

		list.Add("Date: " + DateTime.UtcNow.ToString("ddd MMM dd HH:mm:ss {}zzzz yyyy").Replace("{}", "GMT"));

		return list;
	}

	/// <summary>
	/// Create the form data for a single exception report.
	/// </summary>
	/// <param name="log">A string that contains information about the exception.</param>
	/// <returns>The form data for the current crash report.</returns>
	protected virtual WWWForm CreateForm(string log){
		WWWForm form = new WWWForm();

		byte[] bytes = null;
		using(FileStream fs = File.OpenRead(log)){
			
			if (fs.Length > MAX_CHARS)
			{
				string resizedLog = null;
				
				using(StreamReader reader = new StreamReader(fs)){
					
					reader.BaseStream.Seek( fs.Length - MAX_CHARS, SeekOrigin.Begin );
					resizedLog = reader.ReadToEnd();
				}
				
				List<string> logHeaders = GetLogHeaders();
				string logHeader = "";
				
				foreach (string header in logHeaders)
				{
					logHeader += header + "\n";
				}
				
				resizedLog = logHeader + "\n" + "[...]" + resizedLog;
				
				try
				{
					bytes = System.Text.Encoding.Default.GetBytes(resizedLog);
				}
				catch(ArgumentException ae)
				{
					if (Debug.isDebugBuild) 
					{
						Debug.Log("Failed to read bytes of log file: " + ae);
					}
				}
			}
			else
			{
				try
				{
					bytes = File.ReadAllBytes(log);
				}
				catch(SystemException se)
				{
					if (Debug.isDebugBuild) 
					{
						Debug.Log("Failed to read bytes of log file: " + se);
					}
				}
			}
		}
		
		if(bytes != null)
		{
			form.AddBinaryData("log", bytes, log, "text/plain");
    		if (listener != null && listener.GetUserID() != null)
    		{
                form.AddField("userID", listener.GetUserID());
            }
        }
		
		return form;
	}

	/// <summary>
	/// Get a list of all existing exception reports.
	/// </summary>
	/// <returns>A list which contains the filenames of the log files.</returns>
	protected virtual List<string> GetLogFiles() {
		
		List<string> logs = new List<string>();
		
		string logsDirectoryPath = Application.persistentDataPath + LOG_FILE_DIR;
		
		try
		{
			if (Directory.Exists(logsDirectoryPath) == false)
			{
				Directory.CreateDirectory(logsDirectoryPath);
			}
			
			DirectoryInfo info = new DirectoryInfo(logsDirectoryPath);
			FileInfo[] files = info.GetFiles();
			
			if (files.Length > 0)
			{
				foreach (FileInfo file in files)
				{
					if (file.Extension == ".log")
					{
						logs.Add(file.FullName);
					}
					else
					{
						File.Delete(file.FullName);
					}
				}
			}
		}
		catch(Exception e)
		{
			if (Debug.isDebugBuild) 
			{
				Debug.Log("Failed to write exception log to file: " + e);
			}
		}
		
		return logs;
	}

	/// <summary>
	/// Upload existing reports to HockeyApp and delete delete them locally.
	/// </summary>
	protected virtual IEnumerator SendLogs(List<string> logs){

		string crashPath = HOCKEYAPP_CRASHESPATH;
		string url = GetBaseURL() + crashPath.Replace("[APPID]", appID);

		string sdkVersion = GetSdkVersion ();
		string sdkName = GetSdkName ();
		if (sdkName != null && sdkVersion != null) {
			url+= "?sdk=" + WWW.EscapeURL(sdkName) + "&sdk_version=" + sdkVersion;
		}

		foreach (string log in logs)
		{		
			WWWForm postForm = CreateForm(log);
			string lContent = postForm.headers["Content-Type"].ToString();
			lContent = lContent.Replace("\"", "");
			Dictionary<string,string> headers = new Dictionary<string,string>();
			headers.Add("Content-Type", lContent);
			WWW www = new WWW(url, postForm.data, headers);
			yield return www;

			if (String.IsNullOrEmpty (www.error)) 
			{
				try 
				{
					File.Delete (log);
				} 
				catch (Exception e) 
				{
					if (Debug.isDebugBuild) Debug.Log ("Failed to delete exception log: " + e);
				}
			}
		}
	}

	/// <summary>
	/// Write a single exception report to disk.
	/// </summary>
	/// <param name="logString">A string that contains the reason for the exception.</param>
	/// <param name="stackTrace">The stacktrace for the exception.</param>
	protected virtual void WriteLogToDisk(string logString, string stackTrace){

		string logSession = DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss_fff");
		string log = logString.Replace("\n", " ");
		string[]stacktraceLines = stackTrace.Split('\n');
		
		log = "\n" + log + "\n";
		foreach (string line in stacktraceLines)
		{
			if(line.Length > 0)
			{
				log +="  at " + line + "\n";
			}
		}
		
		List<string> logHeaders = GetLogHeaders();
		using (StreamWriter file = new StreamWriter(Application.persistentDataPath + LOG_FILE_DIR + "LogFile_" + logSession + ".log", true))
		{
			foreach (string header in logHeaders)
			{
				file.WriteLine(header);
			}
			file.WriteLine(log);
		}
	}

	/// <summary>
	/// Get the base url used for custom exception reports.
	/// </summary>
	/// <returns>A formatted base url.</returns>
	protected virtual string GetBaseURL() {
		
		string baseURL ="";
		
		string urlString = serverURL.Trim();
		if(urlString.Length > 0)
		{
			baseURL = urlString;
			
			if(baseURL[baseURL.Length -1].Equals("/") != true){
				baseURL += "/";
			}
		}
		else
		{
			baseURL = HOCKEYAPP_BASEURL;
		}
		
		return baseURL;
	}
	
	/// <summary>
	/// Checks whether internet is reachable
	/// </summary>
	protected virtual bool IsConnected()
	{		
		bool connected = false;
		if  (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork || 
		     (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork))
		{
			connected = true;
		}
		
		return connected;
	}

	/// <summary>
	/// Handle a single exception. By default the exception and its stacktrace gets written to disk.
	/// </summary>
	/// <param name="logString">A string that contains the reason for the exception.</param>
	/// <param name="stackTrace">The stacktrace for the exception.</param>
	protected virtual void HandleException(string logString, string stackTrace){
		WriteLogToDisk(logString, stackTrace);
	}

	/// <summary>
	/// Callback for handling log messages.
	/// </summary>
	/// <param name="logString">A string that contains the reason for the exception.</param>
	/// <param name="stackTrace">The stacktrace for the exception.</param>
	/// <param name="type">The type of the log message.</param>
	public void OnHandleLogCallback(string logString, string stackTrace, LogType type){
		if(LogType.Assert == type || LogType.Exception == type || LogType.Error == type)	
		{	
			HandleException(logString, stackTrace);
		}	
	}
	
	public void OnHandleUnresolvedException(object sender, System.UnhandledExceptionEventArgs args){
		if(args == null || args.ExceptionObject == null)
		{	
			return;	
		}

		if(args.ExceptionObject.GetType() == typeof(System.Exception))
		{	
			System.Exception e	= (System.Exception)args.ExceptionObject;
			HandleException(e.Source, e.StackTrace);
		}
	}
}

public interface HockeyAppCrashManagerListener
{
    string GetUserID();
}
