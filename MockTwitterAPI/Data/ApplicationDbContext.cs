using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MockTwitterAPI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MockTwitterAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public DbSet<MessageModel> Messages { get; set; }
        public DbSet<ChatModel> Chats { get; set; }
        public DbSet<TweetModel> Tweets { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}
