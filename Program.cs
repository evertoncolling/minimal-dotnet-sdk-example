using Microsoft.Identity.Client;
using CogniteSdk;
using dotenv.net;

namespace MinimalExample
{
    public class Program
    {
        public static async Task FetchData()
        {
            /// read authentication variables from a .env file
            DotEnv.Load();
            var clientId = System.Environment.GetEnvironmentVariable("CLIENT_ID");
            var tenantId = System.Environment.GetEnvironmentVariable("TENANT_ID");
            var cluster = System.Environment.GetEnvironmentVariable("CDF_CLUSTER");
            var project = System.Environment.GetEnvironmentVariable("CDF_PROJECT");

            /// fetch a valid token from the Azure Active directory
            var scopes = new List<string> { $"https://{cluster}.cognitedata.com/.default" };
            var app = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                .WithRedirectUri("http://localhost")
                .Build();

            AuthenticationResult result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
            string accessToken = result.AccessToken;

            /// instantiate a Cognite SDK client
            var httpClient = new HttpClient();
            var client = Client.Builder.Create(httpClient)
                .SetAppId("testNotebook")
                .AddHeader("Authorization", $"Bearer {accessToken}")
                .SetProject(project)
                .SetBaseUrl(new Uri($"https://{cluster}.cognitedata.com"))
                .Build();

            /// find some time series with unit "barg" in the project we have authenticated to
            var resTs = await client.TimeSeries.ListAsync(
                new TimeSeriesQuery
                {
                    Filter = new TimeSeriesFilter { Unit = "barg" },
                    Limit = 2
                }
            );
            var tsList = resTs.Items;
            var tsExternalId = tsList.Last().ExternalId;

            /// fetch data points from the past 7 days from one of the time series listed above
            var resDps = await client.DataPoints.ListAsync(new DataPointsQuery
            {
                Start = "7d-ago",
                End = "now",
                Items = new List<DataPointsQueryItem> {
                    new DataPointsQueryItem {
                        ExternalId = tsExternalId,
                        Aggregates = new List<string> { "average" },
                        Granularity="1d",
                        Limit = 10_000
                    }
                }
            });
            var ts = resDps.Items[0];
            
            // get only the timestamp and the desired aggregate from the response
            // convert the timestamp from ms since epoch to DateTime
            var dps = ts.AggregateDatapoints.Datapoints.Select(
                dp => new
                {
                    DateTimeOffset.FromUnixTimeMilliseconds(dp.Timestamp).DateTime,
                    dp.Average
                });
            
            // print each datapoint in a new line
            Console.WriteLine($"Data fetched from time series {tsExternalId}:");
            foreach (var dp in dps)
                Console.WriteLine(dp);
            
            // save the data points to a csv
            using (var writer = new StreamWriter("output.csv"))
            {
                writer.WriteLine("DateTime,Average");
                foreach (var dp in dps)
                    writer.WriteLine($"{dp.DateTime},{dp.Average}");
            }
        }

        static async Task Main(string[] args)
        {
            await FetchData();
        }

    }
}