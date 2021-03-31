using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Collections;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.DistributedTask.Common.Contracts;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System.Collections.Generic;
using System.Text.Json;

namespace csharp_azmonitor_data_collector
{

    public class Agent
    {
        public int AgentID { get; set; }
        public string AgentName { get; set; }

        public int PoolID { get; set; }
        public string PoolName { get; set; }
        public string Organization { get; set; }

        public string Status { get; set; }
    }

    class Program
    {

        // Update organizationName to your ADO organization name
        static string organizationName = "";

        // Update adoPAT to your the Personal Access Token that you'll use to access the ADO Rest API
        static string adoPAT = "";

        // Update customerId to your Log Analytics workspace ID
        static string customerId = "";

        // For Log Analytics sharedKey, use either the primary or the secondary Connected Sources client authentication key   
        static string sharedKey = "";

        // LogName is name of the event type that is being submitted to Azure Monitor
        static string LogName = "ADOAgentMonitor";

        // You can use an optional field to specify the timestamp from the data. If the time field is not specified, Azure Monitor assumes the time is the message ingestion time
        static string TimeStampField = "";

        static void Main(string[] args)
        {

            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(1);

            var intervalCount = 0;

            var timer = new System.Threading.Timer((e) =>
            {
                intervalCount++;

                var agents = GetAgents();

                string jsonString = JsonSerializer.Serialize(agents);

                Console.WriteLine(jsonString);

                // Create a hash for the API signature
                var datestring = DateTime.UtcNow.ToString("r");
                var jsonBytes = Encoding.UTF8.GetBytes(jsonString);
                string stringToHash = "POST\n" + jsonBytes.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";
                string hashedString = BuildSignature(stringToHash, sharedKey);
                string signature = "SharedKey " + customerId + ":" + hashedString;

                Console.WriteLine(signature);

                PostData(signature, datestring, jsonString);

            }, null, startTimeSpan, periodTimeSpan);

            var trackInterval = 0;
            while (intervalCount < 12)
            {
                if(trackInterval != intervalCount)
                {
                    Console.WriteLine($"Interval Count: {intervalCount}");
                    trackInterval = intervalCount;
                }
                
            }
        }

        // Build the API signature
        public static string BuildSignature(string message, string secret)
        {
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = Convert.FromBase64String(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hash = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }

        }

        // Send a request to the POST API endpoint
        public static void PostData(string signature, string date, string json)
        {
            try
            {
                string url = "https://" + customerId + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

                System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Log-Type", LogName);
                client.DefaultRequestHeaders.Add("Authorization", signature);
                client.DefaultRequestHeaders.Add("x-ms-date", date);
                client.DefaultRequestHeaders.Add("time-generated-field", TimeStampField);

                System.Net.Http.HttpContent httpContent = new StringContent(json, Encoding.UTF8);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                Task<System.Net.Http.HttpResponseMessage> response = client.PostAsync(new Uri(url), httpContent);

                System.Net.Http.HttpContent responseContent = response.Result.Content;
                string result = responseContent.ReadAsStringAsync().Result;
                Console.WriteLine("Return Result: " + result);
            }
            catch (Exception excep)
            {
                Console.WriteLine("API Post Exception: " + excep.Message);
            }
        }

        public static List<Agent> GetAgents()
        {
            var ret = new List<Agent>();

            string collectionUri = $"https://dev.azure.com/{organizationName}";

            var creds = new VssBasicCredential(string.Empty, adoPAT);

            // Connect to Azure DevOps Services
            var connection = new VssConnection(new Uri(collectionUri), creds);

            using var taskClient = connection.GetClient<TaskAgentHttpClient>();

            var pools = taskClient.GetAgentPoolsAsync().Result;

            foreach (var pool in pools)
            {
                if (pool.Name.StartsWith("Hosted") == false && pool.Name != "Azure Pipelines")
                {

                    Console.WriteLine(pool.Name);

                    var agents = taskClient.GetAgentsAsync(pool.Id).Result;

                    foreach (var agent in agents)
                    {
                        Console.WriteLine($"{agent.Name} - {agent.Status}");

                        ret.Add(new Agent()
                        {
                            AgentID = agent.Id,
                            AgentName = agent.Name,
                            Status = agent.Status.ToString(),
                            PoolID = pool.Id,
                            PoolName = pool.Name,
                            Organization = organizationName
                        });
                    }


                }

            }

            return ret;

        }
    }
}
