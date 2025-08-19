using LoreRAG.DTOs;
using LoreRAG.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LoreRAG.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LoreController : ControllerBase
{
    private readonly ILoreRetriever _retriever;
    private readonly ILogger<LoreController> _logger;

    public LoreController(ILoreRetriever retriever, ILogger<LoreController> logger)
    {
        _retriever = retriever;
        _logger = logger;
    }

    /// <summary>
    /// Search the lore knowledge base for information
    /// </summary>
    /// <param name="q">The question or search query</param>
    /// <param name="k">The maximum number of results to return (default: 6, max: 20)</param>
    /// <returns>Search results with relevant excerpts and citations</returns>
    /// <response code="200">Returns the search results</response>
    /// <response code="400">If the query is invalid</response>
    /// <response code="500">If there was an internal error</response>
    [HttpGet("ask")]
    [ProducesResponseType(typeof(LoreSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoreSearchResponse>> AskAsync(
        [FromQuery, Required] string q,
        [FromQuery, Range(1, 20)] int k = 6)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            _logger.LogWarning("Empty query received");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Query",
                Detail = "The query parameter 'q' cannot be empty",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (q.Length > 500)
        {
            _logger.LogWarning("Query too long: {Length} characters", q.Length);
            return BadRequest(new ProblemDetails
            {
                Title = "Query Too Long",
                Detail = "The query must be 500 characters or less",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            _logger.LogInformation("Processing query: {Query} with k={K}", q, k);
            var response = await _retriever.AskAsync(q, k);
            
            _logger.LogInformation("Successfully processed query with {Count} results", response.Hits.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process query: {Query}", q);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}