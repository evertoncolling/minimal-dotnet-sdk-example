using CogniteSdk;
using dotenv.net;
using Microsoft.Identity.Client;

namespace minimal_dotnet_sdk_example
{
    public static class Program
    {
        private static async Task FetchData()
        {
            // read authentication variables from a .env file
            DotEnv.Load();
            DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[]
                {"/Users/evertoncolling/Documents/GitHub/minimal-dotnet-sdk-example/.env"}));
            var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
            var cluster = Environment.GetEnvironmentVariable("CDF_CLUSTER");
            var project = Environment.GetEnvironmentVariable("CDF_PROJECT");

            // fetch a valid token from the Azure Active directory
            var scopes = new List<string> {$"https://{cluster}.cognitedata.com/.default"};
            var app = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                .WithRedirectUri("http://localhost")
                .Build();

            var result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
            var accessToken = result?.AccessToken;

            // instantiate a Cognite SDK client
            var httpClient = new HttpClient();
            var client = Client.Builder.Create(httpClient)
                .SetAppId("testNotebook")
                .AddHeader("Authorization", $"Bearer {accessToken}")
                .SetProject(project)
                .SetBaseUrl(new Uri($"https://{cluster}.cognitedata.com"))
                .Build();

            // find some time series with unit "barg" in the project we have authenticated to
            var resTs = await client.TimeSeries.ListAsync(
                new TimeSeriesQuery
                {
                    Filter = new TimeSeriesFilter {Unit = "barg"},
                    Limit = 2
                }
            );
            var tsList = resTs.Items;
            var tsExternalId = tsList.Last().ExternalId;

            // check the timestamp for the latest datapoint for the selected time series
            var resLatestDps = await client.DataPoints.LatestAsync(new DataPointsLatestQuery
            {
                Items = new List<IdentityWithBefore> {new IdentityWithBefore(externalId: tsExternalId, before: "now")}
            });
            var latestTimeStamp = resLatestDps.First().DataPoints?.First().Timestamp;
            if (latestTimeStamp != null)
            {
                var latestTimeStampString = DateTimeOffset.FromUnixTimeMilliseconds(latestTimeStamp.Value).DateTime;
                Console.WriteLine(
                    $"Latest data point found at {latestTimeStampString} for time series {tsExternalId}\n"
                );
            }

            // fetch data points from the past 7 days from one of the time series listed above
            var resDps = await client.DataPoints.ListAsync(new DataPointsQuery
            {
                Start = "7d-ago",
                End = "now",
                Items = new List<DataPointsQueryItem>
                {
                    new DataPointsQueryItem
                    {
                        ExternalId = tsExternalId,
                        Aggregates = new List<string> {"average"},
                        Granularity = "1d",
                        Limit = 10_000
                    }
                }
            });
            var ts = resDps.Items[0];

            // get only the timestamp and the desired aggregate from the response
            // convert the timestamp from ms since epoch to DateTime
            var dps = ts.AggregateDatapoints?.Datapoints?.Select(
                dp => new
                {
                    DateTimeOffset.FromUnixTimeMilliseconds(dp.Timestamp).DateTime,
                    dp.Average
                });
            if (dps == null)
            {
                throw new Exception("No data points were located in the selected time window.");
            }

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

        private static async Task Main()
        {
            await FetchData();
        }
    }
}