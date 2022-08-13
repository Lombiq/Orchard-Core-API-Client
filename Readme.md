# Lombiq API Client for Orchard Core

## About

A client library for communicating with the [Orchard Core](https://www.orchardcore.net/) web APIs. Currently, it contains an implementation for the tenant management API and a console application for testing and demonstration.

Do you want to quickly try out this project and see it in action? Check it out in our [Open-Source Orchard Core Extensions](https://github.com/Lombiq/Open-Source-Orchard-Core-Extensions) full Orchard Core solution and also see our other useful Orchard Core-related open-source projects!

## Documentation

This project is about creating and setting up tenants through an API Client using [RestEase](https://github.com/canton7/RestEase).
The project requires the OpenId features to be enabled and set up. For easy use, enable the Deployment feature and import this [recipe](Lombiq.OrchardCoreApiClient.Tester/Recipes/Lombiq.OrchardCoreApiClient.Tester.OpenId.recipe.json) as a deployment package.

If you want to set up the OpenId features without the recipe, you need to enable these features:

- OpenID Authorization Server
- OpenID Core Components
- OpenID Management Interface
- OpenID Token Validation

1. On the Admin Dashboard open Security → OpenID Connect → Settings → Authorization server, and make sure that the "Enable Token Endpoint" and "Allow Client Credentials Flow" are enabled, and the Token Format should be JSON Web Token.
2. Head over to Security → OpenID Connect → Settings → Token validation, and make sure the Authorization server tenant is set to Default.
3. Last, but not least, go to Security → OpenID Connect → Management → Applications. Here you can add your applications and you may specify your Client Id and Client Secret. The application will use these parameters for authentication since the application uses Client Credentials Flow. The Allow Client Credentials Flow option should be enabled.

For testing, run your Orchard Core app first, then `Lombiq.OrchardCoreApiClient.Tester`. You may need to edit the [_Program.cs_](Lombiq.OrchardCoreApiClient.Tester/Program.cs) according to your OpenID Application's settings (`ClientId`, `ClientSecret`, and `DefaultTenantUri`) and then run the console application.

## Recipes

Lombiq OrchardCore API Client OpenId - Recipe for enabling OpenId features and setting it up for the console tester app. To use this recipe, enable the Deployment feature and import this as a deployment package.

## Contributing and support

Bug reports, feature requests, comments, questions, code contributions and love letters are warmly welcome. You can send them to us via GitHub issues and pull requests. Please adhere to our [open-source guidelines](https://lombiq.com/open-source-guidelines) while doing so.

This project is developed by [Lombiq Technologies](https://lombiq.com/). Commercial-grade support is available through Lombiq.
