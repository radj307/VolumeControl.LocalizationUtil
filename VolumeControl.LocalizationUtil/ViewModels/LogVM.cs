using System.Collections.Generic;
using VolumeControl.Log;

namespace VolumeControl.LocalizationUtil.ViewModels
{
    public class LogVM
    {
        public LogVM()
        {
            Capacity = 1024;
            MessageQueue = new(Capacity);

            FLog.Log.MessageReceived += this.Log_MessageReceived;
        }

        public int Capacity { get; set; }
        public Queue<LogMessage> MessageQueue { get; }

        private void Log_MessageReceived(object? sender, LogMessage e)
        {
            while (MessageQueue.Count >= Capacity)
            {
                MessageQueue.Dequeue();
            }

            MessageQueue.Enqueue(e);
        }
    }
}
