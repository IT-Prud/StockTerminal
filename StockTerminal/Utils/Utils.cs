using System;
using System.Collections.Generic;
using System.Text;
using TradeDB;
using System.Diagnostics;
using System.Reflection;

namespace StockTerminal.Utils
{
    public class Utils
    {
        private static Order.OrderStatusEnum GetStatusEnum(int status)
        {
            Order.OrderStatusEnum currStatus;
            if (status == (int)Order.OrderStatusEnum.Sending)
                currStatus = Order.OrderStatusEnum.Sending;
            else if (status == (int)Order.OrderStatusEnum.SendSuccess)
                currStatus = Order.OrderStatusEnum.SendSuccess;
            else if (status == (int)Order.OrderStatusEnum.SendFail)
                currStatus = Order.OrderStatusEnum.SendFail;
            else if (status == (int)Order.OrderStatusEnum.Pending)
                currStatus = Order.OrderStatusEnum.Pending;
            else if (status == (int)Order.OrderStatusEnum.CreditChecking)
                currStatus = Order.OrderStatusEnum.CreditChecking;
            else if (status == (int)Order.OrderStatusEnum.CreditPass)
                currStatus = Order.OrderStatusEnum.CreditPass;
            else if (status == (int)Order.OrderStatusEnum.CreditFailed)
                currStatus = Order.OrderStatusEnum.CreditFailed;
            else if (status == (int)Order.OrderStatusEnum.Queue)
                currStatus = Order.OrderStatusEnum.Queue;
            else if (status == (int)Order.OrderStatusEnum.Completed)
                currStatus = Order.OrderStatusEnum.Completed;
            else if (status == (int)Order.OrderStatusEnum.Cancelled)
                currStatus = Order.OrderStatusEnum.Cancelled;
            else if (status == (int)Order.OrderStatusEnum.PartiallyCompleted)
                currStatus = Order.OrderStatusEnum.PartiallyCompleted;
            else if (status == (int)Order.OrderStatusEnum.SentToOG)
                currStatus = Order.OrderStatusEnum.SentToOG;
            else if (status == (int)Order.OrderStatusEnum.RejectedByOG)
                currStatus = Order.OrderStatusEnum.RejectedByOG;
            else if (status == (int)Order.OrderStatusEnum.RejectedBySupervisor)
                currStatus = Order.OrderStatusEnum.RejectedBySupervisor;
            else if (status == (int)Order.OrderStatusEnum.InvalidTradePass)
                currStatus = Order.OrderStatusEnum.InvalidTradePass;
            else if (status == (int)Order.OrderStatusEnum.InputError)
                currStatus = Order.OrderStatusEnum.InputError;
            else //Unknown
                currStatus = Order.OrderStatusEnum.Unknown;
            return currStatus;
        }

        private static Order.OrderStatusEnum GetStatusByName(string statusname)
        {
            Order.OrderStatusEnum currStatus;
            
            if (statusname == Order.OrderStatusEnum.Sending.ToString())
                currStatus = Order.OrderStatusEnum.Sending;
            else if (statusname == Order.OrderStatusEnum.SendSuccess.ToString())
                currStatus = Order.OrderStatusEnum.SendSuccess;
            else if (statusname == Order.OrderStatusEnum.SendFail.ToString())
                currStatus = Order.OrderStatusEnum.SendFail;
            else if (statusname == Order.OrderStatusEnum.Pending.ToString())
                currStatus = Order.OrderStatusEnum.Pending;
            else if (statusname == Order.OrderStatusEnum.CreditChecking.ToString())
                currStatus = Order.OrderStatusEnum.CreditChecking;
            else if (statusname == Order.OrderStatusEnum.CreditPass.ToString())
                currStatus = Order.OrderStatusEnum.CreditPass;
            else if (statusname == Order.OrderStatusEnum.CreditFailed.ToString())
                currStatus = Order.OrderStatusEnum.CreditFailed;
            else if (statusname == Order.OrderStatusEnum.Queue.ToString())
                currStatus = Order.OrderStatusEnum.Queue;
            else if (statusname == Order.OrderStatusEnum.Completed.ToString())
                currStatus = Order.OrderStatusEnum.Completed;
            else if (statusname == Order.OrderStatusEnum.Cancelled.ToString())
                currStatus = Order.OrderStatusEnum.Cancelled;
            else if (statusname == Order.OrderStatusEnum.PartiallyCompleted.ToString())
                currStatus = Order.OrderStatusEnum.PartiallyCompleted;
            else if (statusname == Order.OrderStatusEnum.SentToOG.ToString())
                currStatus = Order.OrderStatusEnum.SentToOG;
            else if (statusname == Order.OrderStatusEnum.RejectedByOG.ToString())
                currStatus = Order.OrderStatusEnum.RejectedByOG;
            else if (statusname == Order.OrderStatusEnum.RejectedBySupervisor.ToString())
                currStatus = Order.OrderStatusEnum.RejectedBySupervisor;
            else if (statusname == Order.OrderStatusEnum.InvalidTradePass.ToString())
                currStatus = Order.OrderStatusEnum.InvalidTradePass;
            else if (statusname == Order.OrderStatusEnum.InputError.ToString())
                currStatus = Order.OrderStatusEnum.InputError;
            else //Unknown
                currStatus = Order.OrderStatusEnum.Unknown;
                return currStatus;
        }

        public static Order.OrderStatusEnum GetStatusType(int status)
        {
            return GetStatusEnum(status);
        }

        public static string GetStatusString(int status)
        {
            return GetStatusEnum(status).ToString();
        }

        public static Order.OrderStatusEnum GetStatusTypeByName(string statusname)
        {
            return GetStatusByName(statusname.ToString());
        }

        public static bool IsTargetForm(string target, string instanceID)
        {
            int result = instanceID.IndexOf(target);

            if (result < 0)
                return false;
            else
            {
                return  int.TryParse(instanceID.Substring(result + target.Length + 1), out result) ? true : false;
            }
        }

        public static string GetFunctionName()
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame stackFrame = stackTrace.GetFrame(1);
            MethodBase methodBase = stackFrame.GetMethod();
            return methodBase.Name;
        }

        public static void EnableDoubleBuffered(System.Windows.Forms.Control c)
        {
            //Taxes: Remote Desktop Connection and painting
            //http://blogs.msdn.com/oldnewthing/archive/2006/01/03/508694.aspx
            //if (System.Windows.Forms.SystemInformation.TerminalServerSession)
            //    return;

            System.Reflection.PropertyInfo aProp = typeof(System.Windows.Forms.Control).GetProperty("DoubleBuffered", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            aProp.SetValue(c, true, null);
        }

        public enum OrderStatusState { All, Filled, Queue, CancelRejected };
    }
}
