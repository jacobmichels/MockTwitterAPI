using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MockTwitterAPI.Models
{
    public class MessageModel
    {
        public Guid Id { get; set; }
        public ChatModel Chat { get; set; }
        public Guid ChatId { get; set; }
        public string Content { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public DateTime SentDateTime { get; set; }

    }
}
