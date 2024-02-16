using Microsoft.AspNetCore.Mvc;

namespace BlobStorageUpload.Controllers;

[ApiController]
[Route("files")]
public class FilesController : ControllerBase
{
    private readonly FileService _fileService;

    public FilesController(FileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost]
    [Route("upload")]
    public async Task<IActionResult> Upload([FromForm] string email, [FromForm] IFormFile file)
    {
        var result = await _fileService.UploadAsync(email, file);
        return Ok(result);
    }
}
