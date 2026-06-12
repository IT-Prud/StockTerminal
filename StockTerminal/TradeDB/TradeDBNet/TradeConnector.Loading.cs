using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Processors;
using Utils;

namespace TradeDB.Net
{
    public partial class TradeConnector : Processor
    {
        private bool LoadStockRelation()
        {
            string protocol, requestString;
            HostEndPoint pSessionHost = sapMgr.CurrentSessionHost;
            byte[] responseBuffer;
            int responseBufferTail, responseReadCount;

            WebRequest request;

            if (pSessionHost != null)
            {
                protocol = pSessionHost.Port != 443 ? "http" : "https";

                // https://192.1.2.59/InternetStock/Get/Get.asp?DataType=StockRelated

                requestString = protocol + "://" + pSessionHost.Name + (pSessionHost.Port != 80 && pSessionHost.Port != 443 ? pSessionHost.Port.ToString(":#") : "") +
                    "/InternetStock/Get/Get.asp?DataType=StockRelated";
                request = HttpWebRequest.Create(requestString);

                request.Timeout = 2000;
                request.Method = "GET";
                request.Headers.Clear();

                responseBuffer = new byte[1048576];
                responseBufferTail = 0;
                responseReadCount = 0;

                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    using (Stream stream = response.GetResponseStream())
                    {
                        do
                        {
                            responseReadCount = stream.Read(responseBuffer, responseBufferTail, responseBuffer.Length - responseBufferTail);
                            responseBufferTail += responseReadCount;
                        } while (responseReadCount > 0 && responseBufferTail < responseBuffer.Length);
                    }
                }
                catch (Exception exp)
                {
                    AppendOutLog("HTTPOut", exp.Message);
                }

                if (responseBufferTail > 0)
                    AppendInLog("HTTPIn", Encoding.ASCII.GetString(responseBuffer, 0, responseBufferTail));

                pStockRelationCsv = Encoding.ASCII.GetString(responseBuffer, 0, responseBufferTail);
            }

            return true;
        }
    }
}
