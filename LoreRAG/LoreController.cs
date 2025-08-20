using LoreRAG.DTOs;

using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

namespace LoreRAG;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LoreController(
    ILoreRetriever _retriever, 
    SemanticKernelFactory _kernelFactory,
    ILogger<LoreController> _logger) : 
    ControllerBase
{

    /// <summary>
    /// Look up relevant information from the lore knowledge base
    /// </summary>
    /// <param name="q">The search query</param>
    /// <param name="k">The maximum number of results to return (default: 6, max: 20)</param>
    /// <returns>Search results with relevant excerpts and citations</returns>
    /// <response code="200">Returns the search results</response>
    /// <response code="400">If the query is invalid</response>
    /// <response code="500">If there was an internal error</response>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(LoreSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoreSearchResponse>> LookupAsync(
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
            _logger.LogInformation("Processing lookup query: {Query} with k={K}", q, k);
            var kernel = _kernelFactory.Build();
            var response = await _retriever.LookupAsync(kernel, q, k);
            
            _logger.LogInformation("Successfully processed lookup with {Count} results", response.Hits.Count);
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

    /// <summary>
    /// Ask a question and get an AI-generated answer based on the lore knowledge base
    /// </summary>
    /// <param name="q">The question to answer</param>
    /// <param name="k">The maximum number of context sources to use (default: 6, max: 20)</param>
    /// <returns>An AI-generated answer with source citations</returns>
    /// <response code="200">Returns the answer with sources</response>
    /// <response code="400">If the question is invalid</response>
    /// <response code="500">If there was an internal error</response>
    [HttpGet("ask")]
    [ProducesResponseType(typeof(LoreAnswerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoreAnswerResponse>> AskAsync(
        [FromQuery, Required] string q,
        [FromQuery, Range(1, 20)] int k = 6)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            _logger.LogWarning("Empty question received");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Question",
                Detail = "The question parameter 'q' cannot be empty",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (q.Length > 500)
        {
            _logger.LogWarning("Question too long: {Length} characters", q.Length);
            return BadRequest(new ProblemDetails
            {
                Title = "Question Too Long",
                Detail = "The question must be 500 characters or less",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            _logger.LogInformation("Processing question: {Question} with k={K}", q, k);
            var kernel = _kernelFactory.Build();
            var response = await _retriever.AskAsync(kernel, q, k);
            
            _logger.LogInformation("Successfully generated answer for question");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to answer question: {Question}", q);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}