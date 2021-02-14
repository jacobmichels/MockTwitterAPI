using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockTwitterAPI.Data;
using MockTwitterAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockTwitterAPI.Controllers
{
    [ApiController]
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ILogger<ChatController> _logger;
        private readonly ApplicationDbContext _db;
        public ChatController(ILogger<ChatController> logger,
            ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        [HttpGet("[controller]/listchats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ListChats()
        {
            if(User == null)
            {
                return Problem(detail:"User not found, are you signed in?", statusCode:422);
            }
            string Username = User.Identity.Name;
            var chats = await _db.Chats.Where(chat => chat.OriginalReceiver == Username || chat.OriginalSender == Username).ToListAsync();
            StringBuilder sb = new StringBuilder();
            sb.Append($"{chats.Count} chats found.");
            foreach(ChatModel chat in chats)
            {
                int MessageCount = CountChatMessages(chat);
                sb.Append($"\nChatID: [{chat.Id}] Original Message Sender: [{chat.OriginalSender}] Original Message Receiver: [{chat.OriginalReceiver}] Message Count: [{MessageCount}]");
            }
            return Ok(sb.ToString());
        }

        [HttpGet("[controller]/{chatid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ListChatInfo(string chatid)
        {
            if (User == null)
            {
                return Problem(detail: "User not found, are you signed in?", statusCode: 422);
            }
            string Username = User.Identity.Name;
            ChatModel chat = await _db.Chats.FirstOrDefaultAsync(chat => chat.Id == Guid.Parse(chatid));
            if (chat == null)
            {
                return Problem(detail: $"Chat with Id {chatid} does not exist",statusCode:422);
            }
            if(chat.OriginalReceiver!=Username && chat.OriginalSender != Username)
            {
                return Problem(detail:"You do not have access to this chat.",statusCode:403);
            }
            return Ok($"Chat Info\nChatID: [{chat.Id}] Chat OriginalSender: [{chat.OriginalSender}] Chat OriginalReceiver: [{chat.OriginalReceiver}] Chat Message Count: [{CountChatMessages(chat)}]");
        }



        [HttpPost("[controller]/createchat")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CreateChat(string Recipient)
        {
            if (User == null)
            {
                return Problem(detail: "User not found, are you signed in?", statusCode: 422);
            }
            string Username = User.Identity.Name;
            var RecipientUser = await _db.Users.FirstOrDefaultAsync(user => user.UserName == Recipient);
            if (RecipientUser == null)
            {
                return BadRequest("Recipient does not exist.");
            }
            if (Recipient == Username)
            {
                return BadRequest("You cannot create a chat with yourself.");
            }
            var ExistingChat = await _db.Chats.FirstOrDefaultAsync(chat => (chat.OriginalReceiver==Recipient && chat.OriginalSender==Username) || (chat.OriginalReceiver==Username && chat.OriginalSender==Recipient));
            if (ExistingChat != null)
            {
                return BadRequest("A chat already exists between you and the recipient.");
            }
            ChatModel chat = new ChatModel();
            chat.OriginalReceiver = Recipient;
            chat.OriginalSender = Username;

            _db.Chats.Add(chat);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch(Exception e)
            {
                _logger.LogError($"An error occured while trying to save changes to database. Message: {e.Message}");
                return Problem(detail: "An error occured while trying to save changes to database.",statusCode:500);
            }
            return Ok("Chat created.");
        }

        [HttpPost("[controller]/{chatid}/sendmessage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> SendMessage(string content, string chatid)
        {
            if (User == null)
            {
                return Problem(detail: "User not found, are you signed in?", statusCode: 422);
            }
            var chat = _db.Chats.FirstOrDefault(chat => chat.Id == Guid.Parse(chatid));
            if (chat==null)
            {
                return BadRequest("Chat does not exist.");
            }
            string Username = User.Identity.Name;
            if(Username!=chat.OriginalReceiver && Username != chat.OriginalSender)
            {
                return BadRequest("You do not have access to send messages in this chat.");
            }
            string Recipient = Username == chat.OriginalReceiver ? chat.OriginalSender : chat.OriginalReceiver;
            MessageModel message = new MessageModel();
            message.Sender = Username;
            message.Receiver = Recipient;
            message.Content = content;
            message.ChatId = Guid.Parse(chatid);
            message.Chat = chat;
            message.SentDateTime = DateTime.Now;
            _db.Messages.Add(message);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError($"An error occured while trying to save changes to database. Message: {e.Message}");
                return Problem(detail: "An error occured while trying to save changes to database.", statusCode: 500);
            }
            return Ok("Message sent.");
        }

        [HttpGet("[controller]/{chatid}/viewmessages")]
        public IActionResult ViewMessages(string chatid)
        {
            if (User == null)
            {
                return Problem(detail: "User not found, are you signed in?", statusCode: 422);
            }
            var chat = _db.Chats.FirstOrDefault(chat => chat.Id == Guid.Parse(chatid));
            if (chat == null)
            {
                return BadRequest("Chat does not exist.");
            }
            string Username = User.Identity.Name;
            if(Username!=chat.OriginalReceiver && Username != chat.OriginalSender)
            {
                return Problem(detail:"You do not have permission to view messages in this chat",statusCode:403);
            }
            var MessageList = _db.Messages.Where(message=>message.ChatId==Guid.Parse(chatid)).OrderBy(message=>message.SentDateTime).ToList();
            StringBuilder sb = new StringBuilder();
            sb.Append($"Message count: {MessageList.Count}");
            foreach(MessageModel message in MessageList)
            {
                sb.Append($"\nID: [{message.Id}] Datetime: [{message.SentDateTime}] Sent by: [{message.Sender}] Sent to: [{message.Receiver}] Message Content: [{message.Content}]");
            }
            return Ok(sb.ToString());
        }

        private int CountChatMessages(ChatModel chat)
        {
            return _db.Messages.Count(message=>message.ChatId==chat.Id);
        }

    }
}
