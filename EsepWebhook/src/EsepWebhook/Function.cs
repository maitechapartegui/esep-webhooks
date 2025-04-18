using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    private static readonly HttpClient client = new HttpClient();
    
    public async Task<string> FunctionHandler(object input, ILambdaContext context)
    {
        // Log the input for debugging
        context.Logger.LogInformation($"Input: {JsonConvert.SerializeObject(input)}");
        
        try
        {
            // Parse the GitHub webhook payload
            var githubEvent = JsonConvert.DeserializeObject<dynamic>(input.ToString());
            
            // Extract the issue URL - the path may vary depending on the exact payload structure
            string issueUrl = githubEvent?.issue?.html_url?.ToString();
            
            if (string.IsNullOrEmpty(issueUrl))
            {
                context.Logger.LogInformation("No issue URL found in the payload");
                return "No issue URL found";
            }
            
            // Get the Slack webhook URL from environment variables
            string webhookUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            
            if (string.IsNullOrEmpty(webhookUrl))
            {
                context.Logger.LogError("SLACK_URL environment variable is not set");
                return "Slack webhook URL not configured";
            }
            
            // Create the message for Slack
            var slackPayload = new
            {
                text = $"New GitHub Issue Created: {issueUrl}"
            };
            
            // Convert to JSON
            var content = new StringContent(
                JsonConvert.SerializeObject(slackPayload),
                Encoding.UTF8,
                "application/json");
            
            // Post to Slack
            var response = await client.PostAsync(webhookUrl, content);
            
            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                context.Logger.LogInformation("Message sent to Slack successfully");
                return "Message sent to Slack successfully";
            }
            else
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                context.Logger.LogError($"Failed to send message to Slack. Status: {response.StatusCode}, Response: {responseContent}");
                return $"Failed to send message to Slack: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing webhook: {ex.Message}");
            return $"Error: {ex.Message}";
        }
    }
}