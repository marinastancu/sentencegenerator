using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace SentenceGenerator
{
    public static class SentenceOrchestrator
    {
        static Random random = new Random();

        [FunctionName("SentenceOrchestrator")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            string sentence = "";

            // Data
            List<string> subjects = new List<string> { "A programmer", "An eagle", "cats", "I" };
            List<string> adjectives = new List<string> { "lazy", "ginger", "loud", "fast" };
            List<string> verbs = new List<string> { "jumped", "is writing", "runs", "ate" };
            List<string> objects = new List<string> { "in a bottle", "outside", "the table", "grass" };

            // Check for articles (definite or indefinite)
            string subject = (string)await context.CallActivityAsync<string>("Choose", subjects);
            if (subject.Contains(" "))
            {
                string[] subjectArray = subject.Split(" ");
                sentence += subjectArray[0] + " ";
                sentence += await context.CallActivityAsync<string>("Choose", adjectives) + " ";
                sentence += subjectArray[1] + " ";
            }

            else
            {
                sentence += await context.CallActivityAsync<string>("Choose", adjectives) + " ";
                sentence += subject + " ";
            }

            // Build sentence
            sentence += await context.CallActivityAsync<string>("Choose", verbs) + " ";
            sentence += await context.CallActivityAsync<string>("Choose", objects);

            return sentence;
        }



        [FunctionName("Choose")]
        public static string Choose([ActivityTrigger] List<string> choices)
        {
            string chosen = choices[random.Next(choices.Count)];
            return $"{chosen}";
        }

        [FunctionName("HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
           [HttpTrigger(AuthorizationLevel.Anonymous, methods: "get", Route = "orchestrators/{functionName}")] HttpRequestMessage req,
           [DurableClient] IDurableOrchestrationClient starter,
           string functionName,
           ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync(functionName, null);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}