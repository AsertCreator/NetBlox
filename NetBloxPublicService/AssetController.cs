using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace NetBloxPublicService
{
    [Route("api/assets")]
    [ApiController]
    public class AssetController : ControllerBase
    {
        private readonly AssetContext _context;

        public AssetController(AssetContext context)
        {
            _context = context;
        }

        // GET: api/assets/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CommonAssetItem>> GetAssetByID(long id)
        {
            var commonAssetItem = await _context.CommonAssets.FindAsync(id);

            if (commonAssetItem == null)
                return NotFound();

            return commonAssetItem;
        }

		// POST: api/assets/create
		[HttpPost("create")]
		public async Task<ActionResult<CommonAssetItem>> CreateAsset(CommonAssetType assetType, string name)
		{
			var assetItem = new CommonAssetItem();
			assetItem.OwnerId = 0;
			assetItem.Type = assetType;
			assetItem.Name = name;
			assetItem.Description = "A new cool thing.";
			assetItem.DateCreated = DateTime.Now;
			assetItem.DateUpdated = assetItem.DateCreated;

			_context.CommonAssets.Add(assetItem);

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				throw;
			}

			return assetItem;
		}

		// POST: api/assets/updateDescription
		[HttpPost("updateDescription")]
        public async Task<ActionResult<CommonAssetItem>> UpdateAssetDescription(long id, string desc)
        {
			var entity = await _context.FindAsync<CommonAssetItem>(id);

			if (entity == null)
				return NotFound();

			entity.Description = desc;
			var entry = _context.Entry(entity);
			entry.State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
				throw;
            }

            return entity;
        }

		// POST: api/assets/updateName
		[HttpPost("updateName")]
		public async Task<ActionResult<CommonAssetItem>> UpdateAssetName(long id, string name)
		{
			var entity = await _context.FindAsync<CommonAssetItem>(id);

			if (entity == null)
				return NotFound();

			entity.Name = name;
			var entry = _context.Entry(entity);
			entry.State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				throw;
			}

			return entity;
		}
    }
}
