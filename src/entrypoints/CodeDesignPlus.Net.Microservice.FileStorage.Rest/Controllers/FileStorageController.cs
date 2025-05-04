using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.DataTransferObjects;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Rest.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FileStorageController(IMediator mediator) : ControllerBase
{

    [HttpGet]
    public async Task<IActionResult> GetFiles([FromQuery] C.Criteria criteria, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAllFileStorageQuery(criteria), cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFileById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetFileStorageByIdQuery(id);

        var result = await mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("[action]/{id}")]
    public async Task<IActionResult> Download(Guid id, [FromQuery] string file, [FromQuery] string target, [FromQuery] bool viewInBrowser, CancellationToken cancellationToken)
    {
        var query = new DownloadQuery(id, file, target);

        var result = await mediator.Send(query, cancellationToken);

        var contentDisposition = viewInBrowser ? "inline" : "attachment";
        Response.Headers.Append("Content-Disposition", $"{contentDisposition}; filename=\"{result.File.FullName}\"");

        return File(result.Stream, result.File.Mime.MimeType);
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Upload([FromForm] FileUploadDto data, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var command = new CreateFileStorageCommand(data.Id, file.OpenReadStream(), file.FileName, data.Target, data.Renowned);

        await mediator.Send(command, cancellationToken);

        return Ok("File uploaded successfully.");
    }
}