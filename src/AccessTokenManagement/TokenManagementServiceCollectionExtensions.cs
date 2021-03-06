﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityModel.AspNetCore.AccessTokenManagement;
using System;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for IServiceCollection to register the token management services
    /// </summary>
    public static class TokenManagementServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the token management services to DI
        /// </summary>
        /// <param name="services"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static TokenManagementBuilder AddAccessTokenManagement(this IServiceCollection services,
            Action<AccessTokenManagementOptions> options = null)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            
            services.AddUserAccessTokenManagement();
            services.AddClientAccessTokenManagement();
            
            return new TokenManagementBuilder(services);
        }

        
        /// <summary>
        /// Adds the services required for client access token management
        /// </summary>
        /// <param name="services"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static TokenManagementBuilder AddClientAccessTokenManagement(this IServiceCollection services,
            Action<AccessTokenManagementOptions> options = null)
        {
            if (options != null)
            {
                services.Configure(options);
            }

            services.AddDistributedMemoryCache();
            services.TryAddSingleton<ISystemClock, SystemClock>();
            services.TryAddSingleton<IAuthenticationSchemeProvider, AuthenticationSchemeProvider>();
            
            services.TryAddTransient<IClientAccessTokenManagementService, ClientAccessTokenManagementService>();
            services.TryAddTransient<ClientAccessTokenHandler>();
            services.TryAddTransient<IClientAccessTokenCache, ClientAccessTokenCache>();
            
            services.TryAddTransient<ITokenClientConfigurationService, DefaultTokenClientConfigurationService>();
            services.TryAddTransient<ITokenEndpointService, TokenEndpointService>();
            
            services.AddHttpClient(AccessTokenManagementDefaults.BackChannelHttpClientName);

            return new TokenManagementBuilder(services);
        }
        
        /// <summary>
        /// Adds the services required for user access token management
        /// </summary>
        /// <param name="services"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static TokenManagementBuilder AddUserAccessTokenManagement(this IServiceCollection services,
            Action<AccessTokenManagementOptions> options = null)
        {
            if (options != null)
            {
                services.Configure(options);
            }

            services.AddHttpContextAccessor();
            services.AddAuthentication();
            
            services.TryAddTransient<IUserAccessTokenManagementService, UserAccessAccessTokenManagementService>();
            services.TryAddTransient<UserAccessTokenHandler>();
            services.TryAddTransient<IUserTokenStore, AuthenticationSessionUserTokenStore>();

            services.TryAddTransient<ITokenClientConfigurationService, DefaultTokenClientConfigurationService>();
            services.TryAddTransient<ITokenEndpointService, TokenEndpointService>();
            
            services.AddHttpClient(AccessTokenManagementDefaults.BackChannelHttpClientName);

            return new TokenManagementBuilder(services);
        }
        
        /// <summary>
        /// Adds a named HTTP client for the factory that automatically sends the current user access token
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name of the client.</param>
        /// <param name="parameters"></param>
        /// <param name="configureClient">Additional configuration.</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddUserAccessTokenClient(this IServiceCollection services, 
            string name,
            UserAccessTokenParameters parameters = null, 
            Action<HttpClient> configureClient = null)
        {
            if (configureClient != null)
            {
                return services.AddHttpClient(name, configureClient)
                    .AddUserAccessTokenHandler(parameters);
            }

            return services.AddHttpClient(name)
                .AddUserAccessTokenHandler(parameters);
        }

        /// <summary>
        /// Adds a named HTTP client for the factory that automatically sends the a client access token
        /// </summary>
        /// <param name="services"></param>
        /// <param name="clientName">The name of the client.</param>
        /// <param name="tokenClientName">The name of the token client.</param>
        /// <param name="configureClient">Additional configuration.</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddClientAccessTokenClient(this IServiceCollection services, string clientName,
            string tokenClientName = AccessTokenManagementDefaults.DefaultTokenClientName,
            Action<HttpClient> configureClient = null)
        {
            if (configureClient != null)
            {
                return services.AddHttpClient(clientName, configureClient)
                    .AddClientAccessTokenHandler(tokenClientName);
            }

            return services.AddHttpClient(clientName)
                .AddClientAccessTokenHandler(tokenClientName);
        }

        /// <summary>
        /// Adds the client access token handler to an HttpClient
        /// </summary>
        /// <param name="httpClientBuilder"></param>
        /// <param name="tokenClientName"></param>
        /// <returns></returns>
        public static IHttpClientBuilder AddClientAccessTokenHandler(this IHttpClientBuilder httpClientBuilder,
            string tokenClientName = AccessTokenManagementDefaults.DefaultTokenClientName)
        {
            return httpClientBuilder.AddHttpMessageHandler(provider =>
            {
                var accessTokenManagementService = provider.GetRequiredService<IClientAccessTokenManagementService>();

                return new ClientAccessTokenHandler(accessTokenManagementService, tokenClientName);
            });
        }

        /// <summary>
        /// Adds the user access token handler to an HttpClient
        /// </summary>
        /// <param name="httpClientBuilder"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static IHttpClientBuilder AddUserAccessTokenHandler(this IHttpClientBuilder httpClientBuilder,
            UserAccessTokenParameters parameters = null)
        {
            return httpClientBuilder.AddHttpMessageHandler(provider =>
            {
                var contextAccessor = provider.GetRequiredService<IHttpContextAccessor>();

                return new UserAccessTokenHandler(contextAccessor, parameters);
            });
        }
    }
}