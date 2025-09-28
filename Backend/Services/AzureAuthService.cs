using System.Net.Http.Headers;
using Newtonsoft.Json;
using Backend.Models;

namespace Backend.Services;

public class AzureAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureAuthService> _logger;
    
    public AzureAuthService(HttpClient httpClient, ILogger<AzureAuthService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<AzureUserInfo?> ValidateTokenAndGetUser(string accessToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await _httpClient.GetAsync(
                "https://graph.microsoft.com/v1.0/me");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                dynamic userData = JsonConvert.DeserializeObject(content)!;
                
                // Log the response for debugging
                _logger.LogInformation($"Graph API response: {content}");
                
                // Handle various email field possibilities
                string email = userData.mail?.ToString() 
                    ?? userData.userPrincipalName?.ToString() 
                    ?? userData.preferredUsername?.ToString()
                    ?? $"{userData.id}@azure.local"; // Fallback email if none found
                
                return new AzureUserInfo
                {
                    Id = userData.id,
                    Email = email,
                    DisplayName = userData.displayName ?? "User",
                    GivenName = userData.givenName ?? "",
                    Surname = userData.surname ?? ""
                };
            }
            else
            {
                _logger.LogWarning($"Azure token validation failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Azure token");
        }
        
        return null;
    }
}
