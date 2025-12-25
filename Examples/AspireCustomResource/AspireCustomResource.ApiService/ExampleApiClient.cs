public class ExampleApiClient(HttpClient httpClient)
{
    public async Task<string> GetDataAsync()
    {
        return await httpClient.GetStringAsync("/data");
    }
}
