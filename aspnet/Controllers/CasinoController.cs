using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

[Route("kasina")]
public class CasinoController : Controller
{
    private readonly ICasinoRepository _repo;
    private readonly CasinoDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public CasinoController(ICasinoRepository repo, CasinoDbContext dbContext, IWebHostEnvironment environment)
    {
        _repo = repo;
        _dbContext = dbContext;
        _environment = environment;
    }

    [Route("")]
    [AllowAnonymous]
    public IActionResult Index() => View(_repo.GetAll());

    [Route("{id:int}")]
    [Authorize]
    public IActionResult Details(int id)
    {
        var casino = _repo.GetById(id);
        if (casino is null) return NotFound();
        return View(casino);
    }

    [Route("novi")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create() => View(new Casino());

    [HttpPost]
    [Route("novi")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create(Casino casino)
    {
        if (!ModelState.IsValid) return View(casino);
        _repo.Create(casino);
        return RedirectToAction("Details", new { id = casino.Id });
    }

    [Route("{id:int}/uredi")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Edit(int id)
    {
        var casino = _repo.GetById(id);
        if (casino is null) return NotFound();
        return View(casino);
    }

    [HttpPost]
    [Route("{id:int}/uredi")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Edit(int id, Casino casino)
    {
        if (!ModelState.IsValid) return View(casino);
        _repo.Update(casino);
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [Route("{id:int}/obrisi")]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete(int id)
    {
        _repo.Delete(id);
        return RedirectToAction("Index");
    }

    [Route("pretraga")]
    [AllowAnonymous]
    public IActionResult Search(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(new List<object>());
        var results = _repo.Search(q).Select(c => new
        {
            id = c.Id,
            name = c.Name,
            address = c.Address,
            licenseNumber = c.LicenseNumber
        });
        return Json(results);
    }

    [Route("autocomplete")]
    [AllowAnonymous]
    public IActionResult Autocomplete(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(new List<object>());
        var results = _repo.Search(q).Select(c => new
        {
            id = c.Id,
            label = c.Name
        });
        return Json(results);
    }

    // ── Privitci (Lab 5) ─────────────────────────────────────────────────────

    [HttpPost]
    [Route("{casinoId:int}/privitci")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult UploadAttachment(int casinoId, IFormFile file)
    {
        var casino = _dbContext.Casinos.FirstOrDefault(c => c.Id == casinoId);
        if (casino is null) return NotFound();

        if (file == null || file.Length == 0) return BadRequest();

        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "casinos", casinoId.ToString());
        Directory.CreateDirectory(uploadsPath);

        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            file.CopyTo(stream);
        }

        var attachment = new Attachment
        {
            CasinoId = casinoId,
            FileName = file.FileName,
            FilePath = $"/uploads/casinos/{casinoId}/{fileName}",
            ContentType = file.ContentType,
            FileSize = file.Length,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Attachments.Add(attachment);
        _dbContext.SaveChanges();

        return Json(new { success = true, id = attachment.Id });
    }

    [Route("{casinoId:int}/privitci")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult GetAttachments(int casinoId)
    {
        var attachments = _dbContext.Attachments
            .Where(a => a.CasinoId == casinoId)
            .OrderByDescending(a => a.CreatedAt)
            .ToList();

        return PartialView("_AttachmentList", attachments);
    }

    [HttpPost]
    [Route("privitci/{id:int}/obrisi")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult DeleteAttachment(int id)
    {
        var attachment = _dbContext.Attachments.FirstOrDefault(a => a.Id == id);
        if (attachment is null) return NotFound();

        var physicalPath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/'));
        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }

        _dbContext.Attachments.Remove(attachment);
        _dbContext.SaveChanges();

        return Json(new { success = true });
    }
}
