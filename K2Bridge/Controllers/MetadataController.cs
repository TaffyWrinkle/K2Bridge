﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace K2Bridge.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Handles the request that goes directly to the underlying elasticsearch
    /// instance that handles all metadata requests.
    /// </summary>
    [Route("/Metadata")]
    [ApiController]
    public class MetadataController : ControllerBase
    {
        internal const string ElasticMetadataClientName = "elasticMetadata";
        private readonly IHttpClientFactory clientFactory;
        private readonly ILogger<MetadataController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataController"/> class.
        /// </summary>
        /// <param name="clientFactory">An instance of <see cref="IHttpClientFactory"/>.</param>
        /// <param name="logger">An instance of <see cref="ILogger"/>.</param>
        public MetadataController(IHttpClientFactory clientFactory, ILogger<MetadataController> logger)
        {
            this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handle metadata requests to the elasticsearch.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [HttpPost]
        [HttpHead]
        [HttpPut]
        [HttpPatch]
        [HttpOptions]
        [HttpGet]
        [HttpDelete]
        public async Task<IActionResult> Passthrough()
        {
            try
            {
                return await PassthroughInternal();
            }
            catch (Exception exception)
            {
                return StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    exception);
            }
        }

        /// <summary>
        /// Forwards an http message to the metadata client.
        /// </summary>
        /// <param name="clientFactory">HTTP client factory that will be used to initialize an http client.</param>
        /// <param name="message">The original HTTP message.</param>
        /// <returns>HTTP response.</returns>
        internal static async Task<HttpResponseMessage> ForwardMessageToMetadataClient(IHttpClientFactory clientFactory, HttpRequestMessage message)
        {
            var httpClient = clientFactory.CreateClient(ElasticMetadataClientName);

            // update the target host of the request
            message.RequestUri =
                new Uri(httpClient.BaseAddress, message.RequestUri.AbsolutePath);
            message.Headers.Clear();
            return await httpClient.SendAsync(message);
        }

        /// <summary>
        /// Internal implementation of the pass through API.
        /// </summary>
        /// <returns>Http response from the metadata client.</returns>
        internal async Task<IActionResult> PassthroughInternal()
        {
            HttpContext.Request.Path = ControllerExtractMethods.ReplaceBackTemplateString(HttpContext.Request.Path.Value);
            var remoteResponse =
                await ForwardMessageToMetadataClient(
                    clientFactory,
                    new HttpRequestMessageFeature(HttpContext).HttpRequestMessage);

            HttpContext.Response.RegisterForDispose(remoteResponse);
            if (remoteResponse.IsSuccessStatusCode)
            {
                string resultStr = await remoteResponse.Content.ReadAsStringAsync();
                return StatusCode((int)remoteResponse.StatusCode, resultStr);
            }

            return StatusCode(
                (int)remoteResponse.StatusCode,
                remoteResponse.ReasonPhrase);
        }
    }
}