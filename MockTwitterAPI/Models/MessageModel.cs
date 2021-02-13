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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime SentDateTime { get; set; }      //generate this when inserted into database

    }
}
