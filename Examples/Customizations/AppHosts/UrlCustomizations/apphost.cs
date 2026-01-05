#:sdk Aspire.AppHost.Sdk@13.1.0


var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddCSharpApp("api", "../../Services/AspireCustomResource.ApiService/");

var web = builder.AddCSharpApp("frontend", "../../Services/AspireCustomResource.Web/");

// apiService.WithUrl("https://www.google.com", "google");

// apiService.WithUrl($"{apiService.Resource.GetEndpoint("https")}/scalar", "Scalar");

// apiService
//     .WithUrls(c => 
//     {
//         c.Urls.ForEach(u => 
//         {   
//             u.DisplayText = $"API - ({u.Endpoint?.EndpointName})";
//             u.DisplayLocation = UrlDisplayLocation.DetailsOnly;
//         });

//         var ep = c.GetEndpoint("http");
//         c.Urls.Add(new ResourceUrlAnnotation() { Url = $"{ep.Url}/scalar", DisplayText = "Test API", DisplayLocation=UrlDisplayLocation.SummaryAndDetails });
//     });

apiService.WithUrlForEndpoint("https", ep => new() { Url = $"{ep.Url}/scalar", DisplayText = "Try API", DisplayLocation = UrlDisplayLocation.SummaryAndDetails });



builder.Build().Run();