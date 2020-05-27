﻿using Newtonsoft.Json;
using SAPFEWSELib;
using SapROTWr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAPBridge
{
    public class SAPHook
    {
        private static SAPHook _instance = null;
        public static SAPHook Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SAPHook();
                    _instance.init();
                }
                return _instance;
            }
        }
        private GuiApplication _app = null;
        public GuiApplication app { 
            get
            {
                if(_app != null)
                {
                    try
                    {
                        _ = _app.Children.Count;
                    }
                    catch (Exception)
                    {
                        _app = null;
                    }
                }
                if(_app == null) _app = GetSAPGuiApp();
                return _app;
            }
        }
        public void init()
        {
            _ = app;
        }
        public SAPSession[] Sessions { get; private set; }
        public SAPConnection[] Connections { get; private set; }
        public void RefreshSessions()
        {
            var connections = new List<SAPConnection>();
            var sessions = new List<SAPSession>();
            for (int i = 0; i < app.Children.Count; i++)
            {
                var con = app.Children.ElementAt(i) as GuiConnection;
                if (con.Sessions.Count == 0) continue;
                var sapconnection = new SAPConnection();
                sapconnection.ConnectionString = con.ConnectionString;
                sapconnection.Description = con.Description;
                sapconnection.DisabledByServer = con.DisabledByServer;
                sapconnection.Id = con.Id;
                sapconnection.Name = con.Name;

                var sapsessions = new List<SAPSession>();
                for (int j = 0; j < con.Sessions.Count; j++)
                {
                    var session = con.Children.ElementAt(j) as GuiSession;
                    var sapsession = new SAPSession();
                    sapsession.Busy = session.Busy;
                    sapsession.Id = session.Id;
                    sapsession.Info = new SAPSessionInfo();
                    sapsession.Info.ApplicationServer = session.Info.ApplicationServer;
                    sapsession.Info.Client = session.Info.Client;
                    sapsession.Info.Codepage = session.Info.Codepage;
                    sapsession.Info.Flushes = session.Info.Flushes;
                    sapsession.Info.Group = session.Info.Group;
                    sapsession.Info.GuiCodepage = session.Info.GuiCodepage;
                    sapsession.Info.I18NMode = session.Info.I18NMode;
                    sapsession.Info.InterpretationTime = session.Info.InterpretationTime;
                    sapsession.Info.IsLowSpeedConnection = session.Info.IsLowSpeedConnection;
                    sapsession.Info.Language = session.Info.Language;
                    sapsession.Info.MessageServer = session.Info.MessageServer;
                    sapsession.Info.Program = session.Info.Program;
                    sapsession.Info.ResponseTime = session.Info.ResponseTime;
                    sapsession.Info.RoundTrips = session.Info.RoundTrips;
                    sapsession.Info.ScreenNumber = session.Info.ScreenNumber;
                    sapsession.Info.ScriptingModeReadOnly = session.Info.ScriptingModeReadOnly;
                    sapsession.Info.ScriptingModeRecordingDisabled = session.Info.ScriptingModeRecordingDisabled;
                    sapsession.Info.SessionNumber = session.Info.SessionNumber;
                    sapsession.Info.SystemName = session.Info.SystemName;
                    sapsession.Info.SystemNumber = session.Info.SystemNumber;
                    sapsession.Info.SystemSessionId = session.Info.SystemSessionId;
                    sapsession.Info.Transaction = session.Info.Transaction;
                    sapsession.Info.User = session.Info.User;
                    sapsession.IsActive = session.IsActive;
                    sapsession.IsListBoxActive = session.IsListBoxActive;
                    sapsession.Name = session.Name;
                    sapsession.ProgressPercent = session.ProgressPercent;
                    sapsession.ProgressText = session.ProgressText;
                    sapsession.Record = session.Record;
                    sapsession.RecordFile = session.RecordFile;
                    sapsession.SaveAsUnicode = session.SaveAsUnicode;
                    sapsession.ShowDropdownKeys = session.ShowDropdownKeys;
                    // sapsession.SuppressBackendPopups = session.SuppressBackendPopups;
                    sapsession.TestToolMode = session.TestToolMode;
                    sessions.Add(sapsession);
                    sapsessions.Add(sapsession);
                }
                sapconnection.sessions = sapsessions.ToArray();
                connections.Add(sapconnection);
            }
            Sessions = sessions.ToArray();
            Connections = connections.ToArray();
        }
        public bool Recording { get; private set; } = false;
        public void BeginRecord(bool VisualizationEnabled)
        {
            var application = app;
            if (app.Connections.Count == 0) return;
            for (int i = 0; i < app.Children.Count; i++)
            {
                var con = application.Children.ElementAt(i) as GuiConnection;
                if (con.Sessions.Count == 0) continue;

                for (int j = 0; j < con.Sessions.Count; j++)
                {
                    var session = con.Children.ElementAt(j) as GuiSession;
                    session.Change -= Session_Change;
                    session.Change += Session_Change;
                    session.AbapScriptingEvent -= Session_AbapScriptingEvent;
                    session.AbapScriptingEvent += Session_AbapScriptingEvent;
                    session.Record = true;
                    Recording = true;
                }
                if (VisualizationEnabled)
                {
                    for (int j = 0; j < con.Children.Count; j++)
                    {
                        var ses = con.Children.ElementAt(i) as GuiSession;
                        for (int y = 0; y < ses.Children.Count; y++)
                        {
                            var fWin = ses.Children.ElementAt(y) as GuiFrameWindow;
                            if (fWin != null)
                            {
                                fWin.ElementVisualizationMode = true;
                            }
                        }
                    }
                }
            }
        }
        internal void Login(SAPLoginEvent message)
        {
            SAPLogon l = new SAPLogon();
            l.StartProcess();
            GuiSession session = null;
            try
            {
                if (app.Connections.Count != 0)
                {
                    for (int i = 0; i < app.Children.Count; i++)
                    {
                        var con = app.Children.ElementAt(i) as GuiConnection;
                        if (con.Sessions.Count == 0) continue;

                        for (int j = 0; j < con.Sessions.Count; j++)
                        {
                            var ses = con.Children.ElementAt(j) as GuiSession;
                            if (ses.Info.SystemName.ToLower() == message.SystemName.ToLower())
                            {
                                session = ses; break;
                            }
                        }
                        if (session != null) break;
                    }
                }

                // SAPTestHelper.Current.CloseAllConnections();
                // SAPTestHelper.Current.SetSession();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (app == null || session == null)
            {
                l.OpenConnection(message.Host);
                l.Login(message.Username, message.Password, message.Client, message.Language);
            }
        }
        public GuiSession GetSession(string SystemName)
        {
            var application = app;
            if (app.Connections.Count == 0) return null;
            for (int i = 0; i < app.Children.Count; i++)
            {
                var con = application.Children.ElementAt(i) as GuiConnection;
                if (con.Sessions.Count == 0) continue;

                for (int j = 0; j < con.Sessions.Count; j++)
                {
                    var session = con.Children.ElementAt(j) as GuiSession;
                    if (session.Info.SystemName.ToLower() == SystemName.ToLower()) return session;
                }
            }
            return null;
        }
        public void EndRecord()
        {
            var application = app;
            if (app.Connections.Count == 0) return;
            for (int i = 0; i < app.Children.Count; i++)
            {
                var con = application.Children.ElementAt(i) as GuiConnection;
                if (con.Sessions.Count == 0) continue;

                for (int j = 0; j < con.Sessions.Count; j++)
                {
                    var session = con.Children.ElementAt(j) as GuiSession;
                    session.Change -= Session_Change;
                    session.AbapScriptingEvent -= Session_AbapScriptingEvent;
                    Recording = false;
                    session.Record = false;
                }
                for (int j = 0; j < con.Children.Count; j++)
                {
                    var ses = con.Children.ElementAt(i) as GuiSession;
                    for (int y = 0; y < ses.Children.Count; y++)
                    {
                        var fWin = ses.Children.ElementAt(y) as GuiFrameWindow;
                        if (fWin != null)
                        {
                            fWin.ElementVisualizationMode = false;
                        }
                    }
                }
            }
        }
        private void Session_AbapScriptingEvent(string param)
        {
            Program.log(param);
        }
        private void Session_Change(GuiSession Session, GuiComponent Component, object CommandArray)
        {
            if (Program.recordstarting) return;
            object[] objs = CommandArray as object[];
            objs = objs[0] as object[];
            var Action = "SetProperty";
            switch (objs[0].ToString().ToLower())
            {
                case "m":
                    Action = "InvokeMethod";
                    break;
                case "sp":
                    Action = "SetProperty";
                    break;
            }
            var ActionName = objs[1].ToString();
            upperFirstChar(ref ActionName);


            string id = Component.Id;
            var pathToRoot = new List<GuiComponent>();
            GuiComponent element = Component;
            while (element != null)
            {
                Program.log(element.Id);
                if(element is GuiSession)
                {
                    id = id.Substring(element.Id.Length + 1);
                }

                //var Type = element.Type;
                //GuiContainer container = element as GuiContainer;
                //if(container != null)
                //{
                //    var count = container.Children.Count;
                //    for (int i = 0; i < count; i++)
                //    {
                //        GuiComponent comp = container.Children.ElementAt(i);
                //    }

                //}

                //if (Type == "GuiToolbar")
                //{
                //    var menu = element as GuiToolbar;
                //} 
                pathToRoot.Add(element);
                element = element.Parent as GuiComponent;
                
            }

                var e = new SAPRecordingEvent();
            e.Action = Action;
            e.ActionName = ActionName;
            e.Name = Component.Name;
            e.Type = Component.Type;
            e.TypeAsNumber = Component.TypeAsNumber;
            e.ContainerType = Component.ContainerType;
            e.Id = id;
            try
            {
                if(objs.Length > 2)
                {
                    var s = objs[1];
                    //e.Parameters = new object[objs.Length - 2];
                    ////objs.CopyTo(e.parameters, 3);
                    //Array.Copy(objs, 2, e.Parameters, 0, e.Parameters.Length);

                    //var _params = new List<SAPEventParameter>();
                    //for(var i = 2; i < objs.Length; i++)
                    //{
                    //    if (objs[i] != null)
                    //    {
                    //        _params.Add(new SAPEventParameter() { Value = objs[i], ValueType = objs[i].GetType().FullName });
                    //    }
                    //    else
                    //    {
                    //        _params.Add(new SAPEventParameter() { Value = null, ValueType = typeof(object).FullName});
                    //    }
                    //}
                    //e.Parameters = _params.ToArray();

                    var _temparr = new object[objs.Length - 2];
                    Array.Copy(objs, 2, _temparr, 0, _temparr.Length);
                    e.Parameters = JsonConvert.SerializeObject(_temparr);

                }
                Program.log(e.Action + " " + e.ActionName + " " + e.Id);
                e.SystemName = Session.Info.SystemName;
                var msg = new SAPEvent("recorderevent"); msg.Set(e);
                Program.log("[send] " + msg.action);
                Program.pipe.PushMessage(msg);
            }
            catch (Exception ex)
            {
                Program.log(ex.ToString());
            }
            // OnRecordEvent?.Invoke(Element);
        }
        private static void upperFirstChar(ref string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                s = char.ToUpper(s[0]) + s.Substring(1);
            }
        }
        public static GuiApplication GetSAPGuiApp(int secondsOfTimeout = 10)
        {
            GuiApplication result = null;
            try
            {
                SapROTWr.CSapROTWrapper sapROTWrapper = new SapROTWr.CSapROTWrapper();
                result = getSAPGuiApp(sapROTWrapper, secondsOfTimeout);
            }
            catch (Exception ex)
            {
                Program.log(ex.ToString());
            }
            return result;
        }
        private static GuiApplication getSAPGuiApp(CSapROTWrapper sapROTWrapper, int secondsOfTimeout)
        {
            object SapGuilRot = sapROTWrapper.GetROTEntry("SAPGUI");
            if (secondsOfTimeout < 0)
            {
                throw new TimeoutException(string.Format("Can get sap script engine in {0} seconds", secondsOfTimeout));
            }
            else
            {
                if (SapGuilRot == null)
                {
                    System.Threading.Thread.Sleep(1000);
                    return getSAPGuiApp(sapROTWrapper, secondsOfTimeout - 1);
                }
                else
                {
                    object engine = SapGuilRot.GetType().InvokeMember("GetSCriptingEngine", System.Reflection.BindingFlags.InvokeMethod, null, SapGuilRot, null);
                    if (engine == null)
                        throw new NullReferenceException("No SAP GUI application found");
                    return engine as GuiApplication;
                }
            }
        }
        public void SetVisualMode(bool on)
        {
            for (int y = 0; y < app.Connections.Count; y++)
            {
                var con = app.Connections.Item(y) as GuiConnection;
                for (int j = 0; j < con.Children.Count; j++)
                {
                    var ses = con.Children.ElementAt(j) as GuiSession;
                    for (int x = 0; x < ses.Children.Count; x++)
                    {
                        var fWin = ses.Children.ElementAt(x) as GuiFrameWindow;
                        if (fWin != null)
                        {
                            fWin.ElementVisualizationMode = on;
                        }
                    }
                }
            }
        }
    }
}
