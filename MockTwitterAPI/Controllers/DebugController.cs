using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MockTwitterAPI.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MockTwitterAPI.Controllers
{
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly ILogger<DebugController> _logger;
        private readonly ApplicationDbContext _db;
        public DebugController(ILogger<DebugController> logger,
            ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }
        [HttpDelete("[controller]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ClearDatabase()
        {
            //remove all the users, tweets, chats, and messages from the database. This leaves the structure of the database, but just deletes all the entries.
            _db.Users.RemoveRange(_db.Users.AsEnumerable());
            _db.Tweets.RemoveRange(_db.Tweets.AsEnumerable());
            _db.Chats.RemoveRange(_db.Chats.AsEnumerable());
            _db.Messages.RemoveRange(_db.Messages.AsEnumerable());

            try
            {
                _logger.LogInformation("Deleting all database data...");
                await _db.SaveChangesAsync();
            }
            catch(Exception e)
            {
                _logger.LogError($"An error occured while saving changes to the database: {e.Message}");
                return Problem(detail: "An error occured while saving changes to the database.", statusCode: 500);
            }
            _logger.LogInformation("Database cleared.");
            return Ok(new { message = "Database cleared." });
        }
    }
}
