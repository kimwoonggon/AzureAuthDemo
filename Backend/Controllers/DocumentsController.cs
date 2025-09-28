using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DocumentsController> _logger;
    
    public DocumentsController(AppDbContext context, ILogger<DocumentsController> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Document>>> GetDocuments([FromQuery] string? search)
    {
        _logger.LogInformation($"Getting documents with search: {search}");
        
        var query = _context.Documents.AsQueryable();
        
        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            query = query.Where(d => 
                d.Title.ToLower().Contains(search) || 
                d.Content.ToLower().Contains(search) || 
                d.Category.ToLower().Contains(search));
        }
        
        var documents = await query
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
        
        _logger.LogInformation($"Found {documents.Count} documents");
        
        return documents;
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Document>> GetDocument(int id)
    {
        var document = await _context.Documents.FindAsync(id);
        
        if (document == null)
        {
            _logger.LogWarning($"Document {id} not found");
            return NotFound(new { error = "Document not found" });
        }
        
        return document;
    }
    
    [HttpPost]
    public async Task<ActionResult<Document>> CreateDocument([FromBody] Document document)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { error = "User not found" });
        }
        
        document.UserId = int.Parse(userId);
        document.CreatedAt = DateTime.UtcNow;
        
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation($"Created document {document.Id} for user {userId}");
        
        return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
    }
}
