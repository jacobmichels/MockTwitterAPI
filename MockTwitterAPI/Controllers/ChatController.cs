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
    public class ChatController : ControllerBase
    {
        private readonly ILogger<ChatController> _logger;
        private readonly ApplicationDbContext _db;
        public ChatController(ILogger<ChatController> logger,
            ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        [HttpGet("[controller]")]
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
            return Ok(chats);
        }

        [HttpPost("[controller]")]
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
                return Problem(detail:"Recipient does not exist.",statusCode:400);
            }
            if (Recipient == Username)
            {
                return Problem(detail:"You cannot create a chat with yourself.",statusCode:400);
            }
            var ExistingChat = await _db.Chats.FirstOrDefaultAsync(chat => (chat.OriginalReceiver==Recipient && chat.OriginalSender==Username) || (chat.OriginalReceiver==Username && chat.OriginalSender==Recipient));
            if (ExistingChat != null)
            {
                return Problem(detail:"A chat already exists between you and the recipient.",statusCode:400);
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
            return Ok(new { message="Chat created.", chatCreated=chat });
        }

        [HttpPost("[controller]/{chatid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> SendMessage(string content, string chatid)
        {
            if (User == null)
            {
                return Problem(detail: "User not found, are you signed in?", statusCode: 422);
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                return Problem(detail: "Message content cannot be empty/whitespace.", statusCode: 400);
            }
            Guid guid;
            if (!Guid.TryParse(chatid, out guid))
            {
                return Problem(detail: "ChatID has an invalid format.", statusCode: 400);
            }
            var chat = _db.Chats.FirstOrDefault(chat => chat.Id == guid);
            if (chat==null)
            {
                return Problem(detail:"Chat does not exist.",statusCode:400);
            }
            string Username = User.Identity.Name;
            if(Username!=chat.OriginalReceiver && Username != chat.OriginalSender)
            {
                return Problem(detail:"You do not have access to send messages in this chat.",statusCode:400);
            }
            string Recipient = Username == chat.OriginalReceiver ? chat.OriginalSender : chat.OriginalReceiver;
            MessageModel message = new MessageModel();
            message.Sender = Username;
            message.Receiver = Recipient;
            message.Content = content;
            message.ChatId = guid;
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
            return Ok(new { Message="Message sent", MessageModel=message });
        }

        [HttpGet("[controller]/{chatid}")]
        public IActionResult ViewMessages(string chatid)
        {
            if (User == null)
            {
                return Problem(detail: "User not found, are you signed in?", statusCode: 422);
            }
            Guid guid;
            if(!Guid.TryParse(chatid, out guid))
            {
                return Problem(detail: "ChatID has an invalid format.", statusCode: 400);
            }
            var chat = _db.Chats.FirstOrDefault(chat => chat.Id == guid);
            if (chat == null)
            {
                return Problem(detail:"Chat does not exist.",statusCode:400);
            }
            string Username = User.Identity.Name;
            if(Username!=chat.OriginalReceiver && Username != chat.OriginalSender)
            {
                return Problem(detail:"You do not have permission to view messages in this chat",statusCode:403);
            }
            var MessageList = _db.Messages.Where(message=>message.ChatId==guid).OrderBy(message=>message.SentDateTime).ToList();
            
            return Ok(MessageList);
        }
    }
}
