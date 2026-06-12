using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Utils
{
    public class SystemEvent
    {
        public enum SystemEventAlertType
        {
            None = 0,
            Sound = 1,
            MessageBox = 2,
            SoundAndMessageBox = 3
        }

        public enum SystemEventSoundType
        {
            None = 0,
            Filled = 1,
            Rejected = 2,
            NetworkFailure = 3
        }

        private string pEventType = null;
        private string pDescription = null;
        private DateTime pEventTime = DateTime.Now;
        //private object pEventObject = null;
        private SystemEventAlertType pAlert = SystemEventAlertType.None;
        private SystemEventSoundType pSound = SystemEventSoundType.None;
        private Color pBackColor = Color.Empty;
        private int pOrderNo = 0;
        private string pOrderNoSort = null;
        private string pAECode = null;

        public SystemEvent()
        {
        }

        public SystemEvent(string EventType)
        {
            this.pEventType = EventType;
        }

        public SystemEvent(string EventType, string Description)
        {
            this.pEventType = EventType;
            this.pDescription = Description;
        }

        public SystemEvent(string EventType, string Description, DateTime EventTime)
        {
            this.pEventType = EventType;
            this.pDescription = Description;
            if (EventTime != DateTime.MinValue) this.pEventTime = EventTime;
        }

        public SystemEvent(string EventType, string Description, SystemEventAlertType Alert, SystemEventSoundType Sound, Color BackColor)
        {
            this.pEventType = EventType;
            this.pDescription = Description;
            this.pAlert = Alert;
            this.pSound = Sound;
            if (BackColor != Color.Empty) this.pBackColor = BackColor;
        }

        public SystemEvent(string EventType, string Description, DateTime EventTime, SystemEventAlertType Alert, SystemEventSoundType Sound, Color BackColor)
        {
            this.pEventType = EventType;
            this.pDescription = Description;
            if (EventTime != DateTime.MinValue) this.pEventTime = EventTime;
            this.pAlert = Alert;
            this.pSound = Sound;
            if (BackColor != Color.Empty) this.pBackColor = BackColor;
        }

        public SystemEvent(string EventType, string Description, SystemEventAlertType Alert, SystemEventSoundType Sound, Color BackColor, int OrderNo, string OrderNoSort, string AECode)
        {
            this.pEventType = EventType;
            this.pDescription = Description;
            this.pAlert = Alert;
            this.pSound = Sound;
            this.pOrderNo = OrderNo;
            this.pOrderNoSort = OrderNoSort;
            this.pAECode = AECode;
            if (BackColor != Color.Empty) this.pBackColor = BackColor;
        }

        public SystemEvent(string EventType, string Description, DateTime EventTime, SystemEventAlertType Alert, SystemEventSoundType Sound, Color BackColor, int OrderNo, string OrderNoSort, string AECode)
        {
            this.pEventType = EventType;
            this.pDescription = Description;
            if (EventTime != DateTime.MinValue) this.pEventTime = EventTime;
            this.pAlert = Alert;
            this.pSound = Sound;
            this.pOrderNo = OrderNo;
            this.pOrderNoSort = OrderNoSort;
            this.pAECode = AECode;
            if (BackColor != Color.Empty) this.pBackColor = BackColor;
        }

        /*
        public SystemEvent(string EventType, string Description, DateTime EventTime, SystemEventAlertType Alert, SystemEventSoundType Sound, Color BackColor, int OrderNo, string OrderNoSort, string AECode, object EventObject)
        {
            this.pEventType = EventType;
            this.pDescription = Description;
            if (EventTime != DateTime.MinValue) this.pEventTime = EventTime;
            this.pAlert = Alert;
            this.pSound = Sound;
            this.pOrderNo = OrderNo;
            this.pOrderNoSort = OrderNoSort;
            this.pAECode = AECode;
            if (BackColor != Color.Empty) this.pBackColor = BackColor;
            pEventObject = EventObject;
        }
         */

        public string EventType
        {
            get { return pEventType; }
        }

        public string Description
        {
            get { return pDescription; }
        }

        public DateTime EventTime
        {
            get { return pEventTime; }
        }

        /*
        public object EventObject
        {
            get { return pEventObject; }
        }
         */

        public SystemEventAlertType Alert
        {
            get { return pAlert; }
        }

        public SystemEventSoundType Sound
        {
            get { return pSound; }
        }

        public Color BackColor
        {
            get { return pBackColor; }
        }

        public int OrderNo
        {
            get { return pOrderNo; }
        }

        public string OrderNoSort
        {
            get { return pOrderNoSort; }
        }

        public string AECode
        {
            get { return pAECode; }
        }

        public override string ToString()
        {
            return string.Format("{0:MM/dd hh:mm:ss} - {1}: {2}", EventTime, EventType, Description);
        }
    }
}
