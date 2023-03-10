using System;
using System.IO;
using Plugin;
using SitePlugin;
using System.ComponentModel.Composition;
using Common;
using System.Diagnostics;
using TwitchSitePlugin;
using WebSocketSharp.Server;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Configuration;


namespace MCV_TestPlugin
{
    [Export(typeof(IPlugin))]
    public class TestPlugin : IPlugin, IDisposable
    {

        public string Name { get { return "コメジェネWebSocket"; } }
        public string Description { get { return "WebSocket版コメジェネにコメントを飛ばします"; } }
        public IPluginHost Host { get; set; }

        private WebSocketServer _ws;

        public virtual void OnLoaded()
        {
            string filePath = @"./plugins/WSComeGene/WebSocketConfig.json";
            string jsonString = File.ReadAllText(filePath);
            JsonDocument jsonDocument = JsonDocument.Parse(jsonString);
            JsonElement root = jsonDocument.RootElement;
            int port = root.GetProperty("port").GetInt32();
            try
            {
                _ws = new WebSocketServer(port);
                _ws.AddWebSocketService<Sender>("/");
                _ws.Start();
                Debug.WriteLine("start");
            }
            catch(Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public void OnClosing()
        {
            _ws.Stop();
            _ws = null;
        }

        public void OnMessageReceived(ISiteMessage message, IMessageMetadata messageMetadata)
        {
            ITwitchComment twitchComment = message as ITwitchComment;
            if (twitchComment != null)
            {
                Debug.WriteLine(twitchComment.DisplayName);
                string sendDisplay = twitchComment.DisplayName;
                List<Dictionary<string,string>> sendComment = new List<Dictionary<string,string>>();
                foreach (IMessagePart value in twitchComment.CommentItems)
                {
                    if(typeof(MessageImage) == value.GetType())
                    {
                        MessageImage imageComment = value as MessageImage;
                        sendComment.Add(new Dictionary<string, string>()
                        {
                            {"type","image"},
                            {"content",imageComment.Url }
                        });
                    }
                    else
                    {
                        sendComment.Add(new Dictionary<string, string>() 
                        {
                            {"type","message"},
                            {"content",value.ToString() }
                        });
                    }
                };
                if (_ws != null)
                {
                    Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>()
                        {
                            {"attr", new Dictionary<string, string>
                            {
                                { "handle", sendDisplay },
                                { "id",twitchComment.Id },
                                { "userName",twitchComment.UserName },
                                { "siteType",twitchComment.SiteType.ToString() },
                                { "userIcon",""},
                                { "PostTime",twitchComment.PostTime }
                            }
                            },
                            {
                              "ComText",sendComment
                            }
                        };
                    string sendJson = JsonSerializer.Serialize(dic);
                    _ws.WebSocketServices.BroadcastAsync(sendJson, (() => { }));
                }
                Debug.WriteLine(sendComment);
            };
        }

        public void OnTopmostChanged(bool isTopmost)
        {

        }
        
        public void ShowSettingView()
        {

        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {

        }

    }

    public class Sender : WebSocketBehavior
    {
    }
}
