﻿using Iot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XmlRpcCore
{
    public class XmlRpc : ISendModule
    {
        private Uri m_Uri;
        private TimeSpan m_TimeOut;
        private bool m_Mock;

        public ISendModule NextSendModule { get; set; }

        public void Open(Dictionary<string, object> args)
        {
            if (args == null || !args.ContainsKey("Uri"))
                throw new ArgumentException("Arguments must contain value for 'Uri'!");
            else
                m_Uri = new Uri(args["Uri"] as string);

            if (args.ContainsKey("TimeOut"))
                m_TimeOut = new TimeSpan(Convert.ToInt32(args["TimeOut"] as string));

            if (args.ContainsKey("Mock"))
                m_Mock = bool.Parse(args["Mock"].ToString());
        }

        public async Task SendAsync(IList<object> sensorMessages, Action<IList<object>> onSuccess = null, Action<IList<object>, Exception> onError = null, Dictionary<string, object> args = null)
        {
            try
            {
                foreach (var msg in sensorMessages)
                {
                    await this.SendAsync(sensorMessages, (msgs) =>
                    {
                        onSuccess?.Invoke(msgs);
                    },
                    (msgs, err) =>
                    {
                        onError?.Invoke(new List<object> { msg }, err);
                        return;
                    },
                    args);
                }

                onSuccess?.Invoke(sensorMessages);
            }
            catch (Exception ex)
            {
                onError?.Invoke(sensorMessages, ex);
            }
        }

        public async Task SendAsync(object sensorMessage, Action<IList<object>> onSuccess = null, Action<IList<object>, Exception> onError = null, Dictionary<string, object> args = null)
        {
          
            XmlRpcProxy proxy = new XmlRpcProxy(m_Uri, m_TimeOut, m_Mock);

            await Task.Run(() =>
             {
                 try
                 {
                     
                     var sensorMsg = new Message(sensorMessage).GetBody<MethodCall>();

                     var requestStr = proxy.SerializeRequest(sensorMsg);
                     var res = proxy.SendRequest(requestStr).Result;
                     var response = proxy.DeserializeResponse(res);

                     onSuccess?.Invoke(new List<object>() { response });

                 }
                 catch (XmlRpcFaultException faultEx)
                 {
                     onError?.Invoke(new List<object> { faultEx.Message }, faultEx);
                 }
             });
        }
    }
}
