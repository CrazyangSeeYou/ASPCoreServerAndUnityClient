using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

[ApiController]
[Route("File")]
[Authorize] // Apply authorization globally for the entire controller
public class FileController : ControllerBase
{
    private readonly ILogger<FileController> _logger;
    private string path_FileRoot = "E:\\GitHub_Project\\ASP.NETServer\\File";

    public FileController(ILogger<FileController> logger)
    {
        _logger = logger;
    }

    [HttpPost("upload")]
    [Authorize] // Authorize each method individually
    public async Task<IActionResult> Upload([FromBody] FileRequest fileRequest)
    {
        try
        {
            // Ensure the request contains a valid user ID
            if (string.IsNullOrEmpty(fileRequest?.UserId))
            {
                return BadRequest("User ID is required.");
            }

            _logger.LogInformation($"Received upload request. UserID: {fileRequest.UserId}, FileName: {fileRequest.FileName}, FileLength: {fileRequest.FileData.Length}");

            // Create a folder for the user if not exists
            string userFolderPath = Path.Combine(path_FileRoot, fileRequest.UserId);
            if (!Directory.Exists(userFolderPath))
            {
                Directory.CreateDirectory(userFolderPath);
            }

            // Save the uploaded file to the user's folder
            string filePath = Path.Combine(userFolderPath, fileRequest.FileName);
            await System.IO.File.WriteAllBytesAsync(filePath, fileRequest.FileData);

            _logger.LogInformation($"File uploaded successfully for User ID: {fileRequest.UserId}");

            // Return a 200 OK response with a success message
            return Ok("File uploaded successfully.");
        }
        catch (Exception ex)
        {
            // Log the error
            _logger.LogError($"Error uploading file: {ex.Message}");

            // Return a 500 Internal Server Error response with an error message
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpGet("download")]
    [Authorize] // Authorize each method individually
    public IActionResult Download([FromQuery] string userId, [FromQuery] string fileName)
    {
        try
        {
            // Ensure the request contains a valid user ID
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required.");
            }

            _logger.LogInformation($"Received download request. UserID: {userId}, FileName: {fileName}");

            // Construct the file path based on user ID and file name
            string filePath = Path.Combine(path_FileRoot, userId, fileName);

            // Check if the file exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found.");
            }

            // Read the file content
            byte[] fileData = System.IO.File.ReadAllBytes(filePath);

            _logger.LogInformation($"File downloaded successfully for User ID: {userId}");
            return File(fileData, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error downloading file: {ex.Message}");
            return StatusCode(500, "Internal Server Error");
        }
    }
}

public class FileRequest
{
    public string UserId { get; set; }
    public string FileName { get; set; }
    public byte[] FileData { get; set; }
}