using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockTwitterAPI.Data;
using MockTwitterAPI.Models;

namespace MockTwitterAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TweetController : ControllerBase
    {
        private readonly ILogger<TweetController> _logger;
        private readonly ApplicationDbContext _db;

        public TweetController(ILogger<TweetController> logger,
            ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        // GET: Tweet
        [HttpGet]
        public async Task<IActionResult> GetTweet()
        {
            var tweets = await _db.Tweets.OrderBy(tweet=>tweet.Timestamp).ToListAsync();
            return Ok(tweets);
        }

        // GET: Tweet/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTweet(Guid id)
        {
            var tweetModel = await _db.Tweets.FindAsync(id);

            if (tweetModel == null)
            {
                return NotFound();
            }

            return Ok(tweetModel);
        }

        // PUT: Tweet/5
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTweet(Guid id, string content)
        {
            if (User == null)
            {
                return Problem(detail: "User not found, are you signed in?", statusCode: 422);
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                return Problem(detail: "A tweet cannot have no content.", statusCode: 400);
            }

            var Tweet = _db.Tweets.FirstOrDefault(tweet => tweet.Id == id);
            if (Tweet == null)
            {
                return Problem(detail:"Tweet not found.",statusCode:404);
            }
            string Username = User.Identity.Name;
            if(Tweet.Username != Username)
            {
                return Problem(detail:"You cannot update someone else's tweet!",statusCode:400);
            }
            TweetModel EditedTweet = new TweetModel();
            EditedTweet.Username = Username;
            EditedTweet.Content = content;
            EditedTweet.Id = id;
            EditedTweet.Timestamp = DateTime.Now;
            var oldTweet = _db.Tweets.Find(id);
            _db.Tweets.Remove(oldTweet);
            _db.Tweets.Add(EditedTweet);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TweetExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("PutTweet",EditedTweet);
        }

        // POST: Tweet
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<TweetModel>> PostTweet(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Problem(detail:"A tweet cannot have no content",statusCode:400);
            }
            if (User == null)
            {
                return Problem(detail: "User not found, are you signed in?", statusCode: 422);
            }
            TweetModel tweet = new TweetModel();
            tweet.Content = content;
            tweet.Username = User.Identity.Name;
            tweet.Timestamp = DateTime.Now;

            _db.Tweets.Add(tweet);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch(Exception e)
            {
                _logger.LogError($"An error occured while trying to save changes to database. Message: {e.Message}");
                return Problem(detail: "An error occured while trying to save changes to database.", statusCode: 500);
            }

            return CreatedAtAction("PostTweet", new { id = tweet.Id }, tweet);
        }

        // DELETE: Tweet/5
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTweet(Guid id)
        {
            if (User == null)
            {
                return Problem(detail: "User not found, are you signed in?", statusCode: 422);
            }
            string Username = User.Identity.Name;
            var tweet = await _db.Tweets.FindAsync(id);
            if (tweet == null)
            {
                return Problem(detail: "Tweet not found", statusCode: 404);
            }
            if (tweet.Username != Username)
            {
                return Problem(detail:"You cannot delete someone else's tweet!",statusCode:400);
            }

            _db.Tweets.Remove(tweet);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError($"An error occured while trying to save changes to database. Message: {e.Message}");
                return Problem(detail: "An error occured while trying to save changes to database.", statusCode: 500);
            }

            return NoContent();
        }

        private bool TweetExists(Guid id)
        {
            return _db.Tweets.Any(e => e.Id == id);
        }
    }
}
