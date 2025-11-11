using System;
using Nolvus.Browser;
using Nolvus.Core.Enums;
using Nolvus.Core.Events;
using Nolvus.Core.Services;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;

namespace Nolvus.Browser.Core;

public class ChromiumDownloader {
	private WebSite WebSite;
	private string File;
	private string ModId;
	private string _Url;
    public string Url { get { return _Url; } }
	private AvaloniaCefBrowser _browser;
	public AvaloniaCefBrowser Browser => _browser;
	private TaskCompletionSource<object> TaskCompletionDownload = new TaskCompletionSource<object>();
	private TaskCompletionSource<string> TaskCompletionDownloadLink = new TaskCompletionSource<string>();
    private TaskCompletionSource<object> TaskCompletionNexusSSO = new TaskCompletionSource<object>();
    private ChromeDownloaderHandler DownloadHandler;
	public event OnFileDownloadRequestedHandler? OnFileDownloadRequest;
	public event Action<string>? PageInfoChanged;
	public event Action? HideLoadingRequested;
	public event Action<string>? NavigationRequested;

	public ChromiumDownloader(string address, bool LinkOnly, DownloadProgressChangedHandler OnProgress) {
		_Url = address;

		if (_Url.Contains("www.nexusmods.com/sso")) {
			WebSite = WebSite.NexusSSO;
		}
		else if (_Url.Contains("nexusmods.com")) {
			WebSite = WebSite.Nexus;
		}
		else if (_Url.Contains("enbdev.com")) {
			WebSite = WebSite.EnbDev;
		}
		else {
			WebSite = WebSite.Other;
		}

		DownloadHandler = new ChromeDownloaderHandler(LinkOnly, OnProgress);
		DownloadHandler.OnFileDownloadRequest += DownloadRequested;
		DownloadHandler.OnFileDownloadCompleted += DownloadCompleted;
	}

	public void CreateBrowser()
	{
		_browser = new AvaloniaCefBrowser(() => CefRequestContext.GetGlobalContext())
		{
			Address = _Url
		};
		_browser.DownloadHandler = DownloadHandler;
		//idk about these
		RegisterFrameLoadEnd();
		RegisterLoadingStateEvent();
	}

	private void Browser_LoadEnd(object sender, LoadEndEventArgs e)
	{
		if (e.Frame.IsMain)
		{
			var Url = _browser.Address;
			switch (WebSite)
			{
				case WebSite.Nexus:
					HandleNexusLoadEnd(Url);
					break;

				case WebSite.NexusSSO:
					HandleNexusSSOLoadEnd(Url);
					break;
			}
		}
	}

	private void Browser_LoadingStateChanged(object sender, LoadingStateChangeEventArgs e)
	{
    	if (!e.IsLoading)
    	{
        	UnRegisterLoadingStateEvent();

        	switch (WebSite)
        	{
            	case WebSite.Nexus:
                	HandleNexusLoadState();
                	break;

            	case WebSite.EnbDev:
                	HandleEnbDev();
                	break;

            	case WebSite.Other:
                	HandleOthers();
                	break;
        	}
    	}
	}
	
	private void RegisterFrameLoadEnd()
	{
    	_browser.LoadEnd += Browser_LoadEnd;
	}

	private void UnRegisterFrameLoadEnd()
	{
    	_browser.LoadEnd -= Browser_LoadEnd;
	}

	private void RegisterLoadingStateEvent()
	{
    	_browser.LoadingStateChange += Browser_LoadingStateChanged;
	}

	private void UnRegisterLoadingStateEvent()
	{
    	_browser.LoadingStateChange -= Browser_LoadingStateChanged;
	}


	private void DownloadRequested(object? sender, FileDownloadRequestEvent EventArgs)
	{
		TaskCompletionDownloadLink.TrySetResult(EventArgs.DownloadUrl);
		OnFileDownloadRequest?.Invoke(this, EventArgs);
	}

	private void DownloadCompleted(object? sender, FileDownloadRequestEvent EventArgs)
	{
		TaskCompletionDownloadLink.TrySetResult(EventArgs.DownloadUrl);
	}

	public bool IsDownloadComplete => DownloadHandler.IsDownloadComplete;

	public Task AwaitDownload(string FileName)
	{
		File = FileName;
		return TaskCompletionDownload.Task;
	}

	public Task<string> AwaitDownloadLink(string NexusModId)
	{
		ModId = NexusModId;
		return TaskCompletionDownloadLink.Task;
	}

	public Task AwaitNexusSSOAuthentication()
	{
		return TaskCompletionNexusSSO.Task;
	}

	#region Scripts

	private async Task<bool> EvaluateScriptWithResponse(string Script)
    {
		try
		{
			int result = await _browser.EvaluateJavaScript<int>(Script);
			return result == 1;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.StackTrace);
			return false;
        }
    }

	private async Task EvaluateScript(string Script)
	{
		try
        {
			await _browser.EvaluateJavaScript<object>(Script);
        }
		catch (Exception ex)
        {
			Console.WriteLine(ex.StackTrace);
        }
	}

	private void ExecuteScript(string Script)
	{
		_browser.ExecuteJavaScript(Script);
	}

	#endregion

	#region Nexus

	private async Task<bool> IsLoginNeeded()
	{
		return await EvaluateScriptWithResponse(ScriptManager.GetIsLoginNeeded());
	}

	private void RedirectToLogin()
	{
		ExecuteScript(ScriptManager.GetRedirectToLogin());
	}

	private async Task<bool> IsModNotFound()
	{
		return await EvaluateScriptWithResponse(ScriptManager.GetIsModNotFound());
	}

	private async Task<bool> IsDownloadAvailable()
	{
		return await EvaluateScriptWithResponse(ScriptManager.GetIsDownloadAvailable());
	}

	private async void InitializeNexusManualDownload()
	{
		await EvaluateScript(ScriptManager.GetNexusManualDownloadInit());

        await Task.Delay(100).ContinueWith(T =>
        {
            ExecuteScript(ScriptManager.GetNexusManualDownload());
        });
	}

	private async void HandleNexusLoadState()
	{
        if (await IsLoginNeeded())
        {
            RedirectToLogin();
        }            
        else if (await IsDownloadAvailable())
        {
            InitializeNexusManualDownload();
        }
	}

	private void HandleNexusLoadEnd(string Url)
	{
		PageInfoChanged?.Invoke(Url);

    	if (Url == "https://users.nexusmods.com/auth/sign_in")
    	{
        	HideLoadingRequested?.Invoke();
    	}
    	else if (Url.Contains("https://users.nexusmods.com/auth/continue?"))
    	{
        	HideLoadingRequested?.Invoke();
    	}
    	else if (Url == $"https://www.nexusmods.com/skyrimspecialedition/mods/{ModId}?tab=files")
    	{
        	RegisterLoadingStateEvent();
        	NavigationRequested?.Invoke(_Url);
    	}
    	else
    	{
        	HideLoadingRequested?.Invoke();
    	}
	}

	#endregion

	#region Nexus SSO

	private void HandleNexusSSOLoadEnd(string Url)
	{

	}

	#endregion

	#region ENB

	private void HandleEnbDev()
	{
        var Script = ScriptManager.GetHandleENBDev(File);
        ExecuteScript(Script);
	}

	#endregion

	#region  Others

	private void HandleOthers()
	{
		
	}

	#endregion
	
}