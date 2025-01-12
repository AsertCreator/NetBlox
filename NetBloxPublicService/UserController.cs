using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using NetBlox;
using System.Security.Cryptography;
using System.Text;

namespace NetBloxPublicService
{
	[Route("api/users")]
	[ApiController]
	public class UserController : ControllerBase
	{
		private readonly AssetContext _context;

		public UserController(AssetContext context)
		{
			_context = context;
		}
		public UserItem? GetUserIdentity()
		{
			string? tokenstr = Request.Cookies["nblogtok"];
			Guid token = tokenstr != null ? Guid.Parse(tokenstr) : default;

			return _context.Users.Where(x => x.LoginToken == token).FirstOrDefault();
		}

		// GET: api/users/5
		[HttpGet("{id}")]
		public async Task<ActionResult<UserItem>> GetUserByID(long id)
		{
			var userItem = await _context.Users.FindAsync(id);

			if (userItem == null)
				return NotFound();

			return userItem;
		}

		// GET: api/users/self
		[HttpGet("self")]
		public ActionResult<UserItem> GetSelfUser()
		{
			var callerUser = GetUserIdentity();

			if (callerUser == default)
				return Forbid();

			return callerUser;
		}

		// GET: api/users/feed
		[HttpGet("feed")]
		public ActionResult<object[]> GetUserGameFeed()
		{
			var callerUser = GetUserIdentity();

			if (callerUser == default)
				return Forbid();

			return new object[] { callerUser };
		}

		// POST: api/users/create
		[HttpPost("create")]
		public async Task<ActionResult<object>> CreateUser(string username, string password)
		{
			if (_context.Users.Any(x => x.UserName == username))
				return BadRequest();

			var userItem = new UserItem();
			userItem.InventoryString = "";
			userItem.MembershipType = UserMembershipType.None;
			userItem.HashedPassword = string.Concat(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLower();
			userItem.UserName = username;
			userItem.Status = "A newbie in NetBlox!";
			userItem.DateCreated = DateTime.Now;
			userItem.DateLastOnline = userItem.DateCreated;
			userItem.LoginToken = Guid.NewGuid();

			_context.Users.Add(userItem);

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				throw;
			}

			// i mean we dont make users every millisecond

			return new { user = userItem, loginToken = userItem.LoginToken };
		}

		// POST: api/users/updateStatus
		[HttpPost("updateStatus")]
		public async Task<IActionResult> UpdateUserStatus(string desc)
		{
			var callerUser = GetUserIdentity();

			if (callerUser == default)
				return Forbid();

			callerUser.Status = desc;
			_context.Entry(callerUser).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				throw;
			}

			return NoContent();
		}

		// POST: api/users/updatePresence
		[HttpPost("updatePresence")]
		public async Task<IActionResult> UpdateUserStatus(OnlineMode mode)
		{
			var callerUser = GetUserIdentity();

			if (callerUser == default)
				return Forbid();

			callerUser.OnlineMode = mode;
			_context.Entry(callerUser).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				throw;
			}

			return NoContent();
		}

		// obfuscated password: password's bytes are altered in following fashion (x = 127 - x)
		// POST: api/users/login
		[HttpPost("login")]
		public async Task<ActionResult<object>> Login(string user, string obfuscatedpassword)
		{
			var entity = _context.Users.First(x => x.UserName == user);

			if (entity == null)
				return NotFound();

			var deobfuscated = RollString(obfuscatedpassword);

			if (entity.HashedPassword != string.Concat(SHA256.HashData(Encoding.UTF8.GetBytes(deobfuscated))).ToLower())
				return NotFound();

			entity.LoginToken = Guid.NewGuid();

			_context.Entry(entity).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				throw;
			}

			var d = new { loginToken = entity.LoginToken };
			return d;
		}

		// POST: api/users/logoff
		[HttpPost("logoff")]
		public async Task<IActionResult> Logoff()
		{
			var callerUser = GetUserIdentity();

			if (callerUser == default)
				return Forbid();

			callerUser.LoginToken = default;
			_context.Entry(callerUser).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				throw;
			}

			return NoContent();
		}

		internal static string RollString(string str)
		{
			byte[] data = Encoding.ASCII.GetBytes(str);
			for (int i = 0; i < data.Length; i++)
				data[i] = (byte)(127 - data[i]);
			return Encoding.ASCII.GetString(data);
		}
	}
}
