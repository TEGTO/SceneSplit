using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;
using HttpMethod = Amazon.CDK.AWS.Apigatewayv2.HttpMethod;

namespace SceneSplit.Cdk.Constructs;

public class ApiGatewayConstruct : Construct
{
    public HttpApi ApiGateway { get; }

    public ApiGatewayConstruct(Construct scope, string id, ApplicationLoadBalancedFargateService apiService) : base(scope, id)
    {
        ApiGateway = new HttpApi(this, "HttpApi", new HttpApiProps
        {
            ApiName = "SceneSplit API",
            Description = "HTTP API routing to ECS API services",
            CorsPreflight = new CorsPreflightOptions
            {
                AllowOrigins = ["*"],
                AllowMethods =
                [
                    CorsHttpMethod.GET,
                    CorsHttpMethod.POST,
                    CorsHttpMethod.PUT,
                    CorsHttpMethod.PATCH,
                    CorsHttpMethod.DELETE,
                    CorsHttpMethod.OPTIONS
                ],
                AllowHeaders = ["*"]
            }
        });

        ApiGateway.AddRoutes(new AddRoutesOptions
        {
            Path = "/{proxy+}",
            Methods = [HttpMethod.ANY],
            Integration = new HttpUrlIntegration(
                "EcsServiceIntegration",
                $"http://{apiService.LoadBalancer.LoadBalancerDnsName}"
            )
        });
    }
}