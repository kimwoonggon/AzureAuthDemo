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
                
                return new AzureUserInfo
                {
                    Id = userData.id,
                    Email = userData.mail ?? userData.userPrincipalName,
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
