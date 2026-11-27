using AttendScan.Application.Dtos.People;
using AttendScan.Application.Interfaces.People;
using AttendScan.core.QueryFilters.People;
using AttendScan.core.Entities.ResponseApi.Details;
using AttendScan.core.Entities.ResponseApi.DisplayFormat;
using AttendScan.core.QueryFilters.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendScan.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// Sin DELETE a propósito: los carnets nunca se anulan (ver IPersonService).
public class PersonsController : ControllerBase
{
    /// <summary>Tope del Excel de la lista: la de 240 personas pesa ~40 KB.</summary>
    private const long MaxImportBytes = 5 * 1024 * 1024;

    private readonly IPersonService _personService;
    private readonly IPersonImportService _personImportService;
    private readonly IPersonQrService _personQrService;

    public PersonsController(IPersonService personService, IPersonImportService personImportService, IPersonQrService personQrService)
    {
        _personService = personService;
        _personImportService = personImportService;
        _personQrService = personQrService;
    }

    /// <summary>QR del carnet de una persona (PNG). Contiene solo su código.</summary>
    [Authorize(Policy = "person.view")]
    [HttpGet("{id}/qr")]
    public async Task<IActionResult> GetQr(long id)
    {
        var qr = await _personQrService.GetQr(id);
        return qr is null ? NotFound() : File(qr.Content, qr.ContentType, qr.FileName);
    }

    /// <summary>ZIP con el QR (PNG) de todas las personas.</summary>
    [Authorize(Policy = "person.view")]
    [HttpGet("qr-zip")]
    public async Task<IActionResult> GetAllQrZip()
    {
        var zip = await _personQrService.GetAllQrZip();
        return File(zip.Content, zip.ContentType, zip.FileName);
    }

    /// <summary>
    /// Importa la lista de personas (Excel). commit=false: vista previa, no guarda nada.
    /// commit=true: crea las nuevas con su código de carnet (todo o nada).
    /// </summary>
    [Authorize(Policy = "person.manage")]
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImportBytes)]
    public async Task<IActionResult> Import(IFormFile file, [FromQuery] bool commit = false)
    {
        if (file is null || file.Length == 0 || file.Length > MaxImportBytes)
        {
            return BadRequest(new ResponseGetObject
            {
                Messages = [new Message { Type = MessageType.Error, Description = "Upload the Excel (.xlsx) of the list, up to 5 MB." }],
                StatusCode = System.Net.HttpStatusCode.BadRequest
            });
        }

        await using var stream = file.OpenReadStream();
        var result = await _personImportService.Import(stream, commit);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "person.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQueryFilter paginationQueryFilter, [FromQuery] PersonQueryFilter queryFilter)
    {
        var result = await _personService.GetAllPersons(paginationQueryFilter, queryFilter);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "person.manage")]
    [HttpPost]
    public async Task<IActionResult> Insert([FromBody] PersonDto dto)
    {
        var result = await _personService.InsertPerson(dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "person.manage")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] PersonDto dto)
    {
        var result = await _personService.UpdatePerson(id, dto);
        return StatusCode((int)result.StatusCode, result);
    }
}
