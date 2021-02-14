using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MockTwitterAPI.Models
{
    public class TweetModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        [StringLength(280,ErrorMessage = "A tweet cannot exceed 280 characters!")]
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
