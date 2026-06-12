using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Utils;

namespace TradeDB.Net
{
    public partial class QuoteConnector
    {
        #region Session Handling

        private int GetPasswordSalt()
        {
            string userId = pUserId;
            string protocol, requestString, postData, responseData;
            string[] fields, fieldParts;
            byte[] postDataBytes, responseBuffer;
            int responseBufferTail, responseReadCount;
            HostEndPoint pSessionHost = sapMgr.CurrentSessionHost;
            WebRequest request;
            int i, n;

            if (pSessionHost != null)
            {
                protocol = pSessionHost.Port != 443 ? "http" : "https";

                if (userId != null && userId.Length > 0)
                {
                    requestString = protocol + "://" + pSessionHost.Name + (pSessionHost.Port != 80 && pSessionHost.Port != 443 ? pSessionHost.Port.ToString(":#") : "") +
                        "/InternetStock/GetSalt.asp";
                    request = HttpWebRequest.Create(requestString);

                    postData = "UserID=" + HttpUtility.UrlEncode(userId);
                    postDataBytes = Encoding.UTF8.GetBytes(postData);

                    request.Timeout = 2000;
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = postDataBytes.Length;
                    request.Headers.Clear();
                    ((HttpWebRequest)request).Accept = "image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/vnd.ms-powerpoint, application/vnd.ms-excel, application/msword, */*";
                    //request.Headers.Add("Accept-Language", "zh-hk");
                    //request.Headers.Add("Accept-Encoding", "gzip, deflate");
                    ((HttpWebRequest)request).ContentType = "application/x-www-form-urlencoded";
                    ((HttpWebRequest)request).UserAgent = "Mozilla/4.0+(compatible;+MSIE+8.0;+Windows+NT+5.1;+Trident/4.0;+InfoPath.1;+.NET+CLR+1.1.4322;+.NET+CLR+2.0.50727;+.NET+CLR+3.0.4506.2152;+.NET+CLR+3.5.30729)";
                    //((HttpWebRequest)request).Connection = "Keep-Alive";

                    try
                    {
                        using (Stream newStream = request.GetRequestStream())
                        {
                            newStream.Write(postDataBytes, 0, postDataBytes.Length);
                            newStream.Close();
                        }
                    }
                    catch (Exception exp)
                    {
                        AppendOutLog("HTTPOut", exp.Message);
                    }

                    responseBuffer = new byte[1024];
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

                    responseData = Encoding.ASCII.GetString(responseBuffer, 0, responseBufferTail);
                    fields = responseData.Split('&');
                    fieldParts = null;

                    for (i = 0, n = fields.Length; i < n; i++)
                    {
                        fieldParts = fields[i].Split('=');
                        if (fieldParts.Length >= 2)
                        {
                            if (fieldParts[0] == "Salt")
                            {
                                pPasswordSalt = fieldParts[1].Trim();

                                if (pPasswordSalt != null && pPasswordSalt.Length > 0)
                                    return 0;
                            }
                        }
                    }
                }
            }

            return -1000;
        }

        private int SessionLogin()
        {
            string userId = "", password = "";
            string protocol, requestString, postData, responseData;
            byte[] postDataBytes, responseBuffer;
            int responseBufferTail, responseReadCount;
            string[] fields, fieldParts;

            ASCIIEncoding encoding = new ASCIIEncoding();
            HostEndPoint pSessionHost = sapMgr.CurrentSessionHost;
            WebRequest request;
            int i, n;

            int responseNo = -1000, sessionId = -1, passwordForceChange, serverPort = -1;
            string deviceToken = null;
            IPAddress serverIP = null;
            IPEndPoint serverEP = null;

            if (pSessionHost != null)
            {
                protocol = pSessionHost.Port != 443 ? "http" : "https";

                if (pUserId == null || 
                    pPassword == null || 
                    pUserSystemName == null || pUserSystemName.Length <= 0) return -2000;

                if (pUserId.Length > 0)
                {
                    userId = pUserId.Trim().ToUpper();
                    password = DataConverter.ToHex(DataConverter.SHA1(Encoding.UTF8.GetBytes(pPassword.ToUpper() + pPasswordSalt)));
                }

                if (pVerificationCode != null && pVerificationCode.Length > 0)
                    pDeviceToken = DataConverter.ToHex(DataConverter.SHA1(Encoding.UTF8.GetBytes(pVerificationCode + pPasswordSalt)));

                if (pDeviceToken != null && pDeviceToken.Length > 0)
                {
                    deviceToken = pDeviceToken.Trim();
                    if (deviceToken.Length > 0)
                        deviceToken = DataConverter.ToHex(DataConverter.SHA1(Encoding.UTF8.GetBytes(deviceToken + pPasswordSalt + userId)));
                    else
                        deviceToken = null;
                }

                requestString = protocol + "://" + pSessionHost.Name +
                    (pSessionHost.Port != 80 && pSessionHost.Port != 443 ? pSessionHost.Port.ToString(":#") : "") +
                    "/InternetStock/QuoteLogin.asp";
                request = HttpWebRequest.Create(requestString);

                postData = "UserID=" + HttpUtility.UrlEncode(userId) +
                    "&Password=" + HttpUtility.UrlEncode(password) +
                    (pVerificationCodeSendMethod != null ? "&VerificationCodeSendMethod=" + HttpUtility.UrlEncode(pVerificationCodeSendMethod) : "") +
                    (deviceToken != null ? "&DeviceToken=" + HttpUtility.UrlEncode(deviceToken) : "") +
                    "&UserSystemName=" + HttpUtility.UrlEncode(pUserSystemName) +
                    "&UserSystemVersion=" + HttpUtility.UrlEncode(pUserSystemVersion != null ? pUserSystemVersion : "") +
                    "&DeploymentGroupName=" + HttpUtility.UrlEncode(pDeploymentGroupName != null ? pDeploymentGroupName : "");
                postDataBytes = encoding.GetBytes(postData);
                
                request.Timeout = 2000;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postDataBytes.Length;
                request.Headers.Clear();
                ((HttpWebRequest)request).Accept = "image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/vnd.ms-powerpoint, application/vnd.ms-excel, application/msword, */*";
                ((HttpWebRequest)request).ContentType = "application/x-www-form-urlencoded";
                ((HttpWebRequest)request).UserAgent = "Mozilla/4.0+(compatible;+MSIE+8.0;+Windows+NT+5.1;+Trident/4.0;+InfoPath.1;+.NET+CLR+1.1.4322;+.NET+CLR+2.0.50727;+.NET+CLR+3.0.4506.2152;+.NET+CLR+3.5.30729)";

                try
                {
                    using (Stream newStream = request.GetRequestStream())
                    {
                        newStream.Write(postDataBytes, 0, postDataBytes.Length);
                        newStream.Close();
                    }
                }
                catch (Exception exp)
                {
                    AppendOutLog("HTTPOut", exp.Message);

                    return responseNo;
                }

                responseBuffer = new byte[1024];
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

                    return responseNo;
                }

                if (responseBufferTail > 0)
                    AppendInLog("HTTPIn", Encoding.ASCII.GetString(responseBuffer, 0, responseBufferTail));

                responseData = encoding.GetString(responseBuffer, 0, responseBufferTail);
                fields = responseData.Split('&');
                fieldParts = null;

                for (i = 0, n = fields.Length; i < n; i++)
                {
                    fieldParts = fields[i].Split('=');
                    if (fieldParts.Length >= 2)
                    {
                        switch (fieldParts[0])
                        {
                            case "ResponseNo":
                                int.TryParse(fieldParts[1], out responseNo);
                                break;
                            case "SessionID":
                                int.TryParse(fieldParts[1], out sessionId);
                                break;
                            case "PasswordForceChange":
                                int.TryParse(fieldParts[1], out passwordForceChange);
                                break;
                            case "Email":
                                pEmail = fieldParts[1];
                                break;
                            case "SMSNo":
                                pSMSNo = fieldParts[1];
                                break;
                            case "ServerIP":
                                IPAddress.TryParse(fieldParts[1], out serverIP);
                                break;
                            case "ServerPort":
                                int.TryParse(fieldParts[1], out serverPort);
                                break;
                        }
                    }
                }

                if (responseNo == 0 && sessionId >= 0 &&
                    serverIP != null && serverPort > 0 && serverPort < 65536)
                {
                    pSessionId = sessionId;
                    serverEP = new IPEndPoint(serverIP, serverPort);
                    if (!sapMgr.IsServerAttempted(serverEP))
                    {
                        TcpSock.RemoteEP = serverEP;
                        return 0;
                    }
                    else
                    {
                        return -4000;           // ServerIP/Port already attempted
                    }
                }
                else
                {
                    return responseNo;
                }
            }

            return -1000;
        }

        private bool ServerLoginSessionID()
        {
            if (pSessionId < 0) return false;

            TradeMessage msg;
            Dictionary<string, TradeMessageTag> tags;
            string guid = Guid.NewGuid().ToString();

            SHA256 shaM = new SHA256Managed();
            string userIdHash = Convert.ToBase64String(shaM.ComputeHash(Encoding.UTF8.GetBytes(pUserId.ToUpper())));

            msg = new TradeMessage();
            msg.MessageId = "LRE";
            msg.MessageType = "01";
            tags = new Dictionary<string, TradeMessageTag>(5);
            tags.Add("SSID", new TradeMessageTag("SSID", pSessionId.ToString()));
            tags.Add("SSVF", new TradeMessageTag("SSVF", userIdHash));
            tags.Add("REF", new TradeMessageTag("REF", guid));
            msg.Tags = tags;
            Send(msg);

            return true;
        }

        private void GetSpreadTable()
        {
            string protocol, requestString;
            HostEndPoint pSessionHost = sapMgr.CurrentSessionHost;
            HttpWebRequest request;
            string spreadTableStr = null;

            if (pSessionHost != null)
            {
                protocol = pSessionHost.Port != 443 ? "http" : "https";

                requestString = protocol + "://" + pSessionHost.Name + (pSessionHost.Port != 80 && pSessionHost.Port != 443 ? pSessionHost.Port.ToString(":#") : "") +
                    "/InternetStock/SpreadTable.asp";

                request = (HttpWebRequest)WebRequest.Create(requestString);

                request.Method = "GET";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Headers.Clear();
                ((HttpWebRequest)request).UserAgent = "Mozilla/4.0+(compatible;+MSIE+8.0;+Windows+NT+5.1;+Trident/4.0;+InfoPath.1;+.NET+CLR+1.1.4322;+.NET+CLR+2.0.50727;+.NET+CLR+3.0.4506.2152;+.NET+CLR+3.5.30729)";

                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        spreadTableStr = reader.ReadToEnd();
                    }
                }
                catch (Exception exp)
                {
                    AppendOutLog("HTTPOut", exp.Message);
                }

                AppendInLog("HTTPIn", spreadTableStr);

                if (spreadTableStr != null && spreadTableStr.Length > 0)
                {
                    string[] entries = spreadTableStr.Split('|');
                    string[] entryParts;
                    int code, lower, upper, delta;

                    for (int i = 0; i < entries.Length; i++)
                    {
                        entryParts = entries[i].Split(',');

                        if (entryParts.Length == 4 &&
                            int.TryParse(entryParts[0], out code) && int.TryParse(entryParts[1], out lower) && 
                            int.TryParse(entryParts[2], out upper) && int.TryParse(entryParts[3], out delta) &&
                            SpreadTables != null)
                        {
                            SpreadTables.Add(code, lower, upper, delta);
                        }
                    }
                }
            }
        }

        private bool ProcessSessionMessage(int CurrentState)
        {
            if (SessionMessageQueue.Count <= 0)
                return false;

            TradeMessage msg;
            TradeMessageTag tag;
            Dictionary<string, TradeMessageTag> tags;

            while (SessionMessageQueue.Count > 0)
            {
                msg = SessionMessageQueue.Dequeue();
                tags = msg.Tags;

                if (msg.MessageType == "07")
                {
                    pLastError = LastErrorEnum.ForceLogout;
                    ServerLoginState = 6;   // Stop
                    break;  // break for
                }
                else if (msg.MessageType == "05")
                {
                    DateTime ServerTime;
                    if (tags.TryGetValue("TIME", out tag) == true && tag != null
                        && DateTime.TryParse(tag.Value, out ServerTime))
                    {
                        ServerTimeLast = ServerTime;
                        ServerTimeLocalTick = ProcessTimer.TickCount;
                        ServerClientTimeDiffValid = true;
                    }
                }
                else if (CurrentState == (int)ProcessState.ServerLogin)
                {
                    switch (ServerLoginState)
                    {
                        case 1:
                            if (msg.MessageType == "02")
                            {
                                ServerLoginState = 2;
                            }
                            else
                            {
                                ServerLoginState = 5;
                            }
                            break;

                        default:
                            ServerLoginState = 6;   // Stop
                            break;
                    }
                    break;  // break for
                }
            }

            return true;
        }

        #endregion
    }
}
