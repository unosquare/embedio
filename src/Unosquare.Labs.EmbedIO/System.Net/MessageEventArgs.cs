namespace Unosquare.Net
{
    using System;
    using Swan;
    
    internal class MessageEventArgs : EventArgs
    {
        private readonly byte[] _rawData;
        private string _data;
        private bool _dataSet;

        internal MessageEventArgs(WebSocketFrame frame)
        {
            Opcode = frame.Opcode;
            _rawData = frame.PayloadData.ApplicationData.ToArray();
        }

        internal MessageEventArgs(Opcode opcode, byte[] rawData)
        {
            if ((ulong)rawData.Length > PayloadData.MaxLength)
                throw new WebSocketException(CloseStatusCode.TooBig);

            Opcode = opcode;
            _rawData = rawData;
        }
        
        public string Data
        {
            get
            {
                SetData();
                return _data;
            }
        }
        
        public bool IsText => Opcode == Opcode.Text;
        
        public byte[] RawData
        {
            get
            {
                SetData();
                return _rawData;
            }
        }

        internal Opcode Opcode { get; }

        private void SetData()
        {
            if (_dataSet)
                return;

            if (Opcode == Opcode.Binary)
            {
                _dataSet = true;
                return;
            }

            _data = _rawData.ToText();
            _dataSet = true;
        }
    }
}