using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MockTwitterAPI.Models
{
    public class ChatModel
    {
        public Guid Id { get; set; }
        public string OriginalSender { get; set; }
        public string OriginalReceiver { get; set; }
    }
}
