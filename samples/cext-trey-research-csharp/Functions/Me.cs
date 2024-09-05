﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using cext_trey_research_csharp.Models;
using cext_trey_research_csharp.Services;
using cext_trey_research_csharp.Utilities;
using Newtonsoft.Json;
using System.IO;

namespace cext_trey_research_csharp
{
    public class MeFunction
    {
        private readonly ConsultantApiService _consultantApiService;
        private readonly IdentityService _identityService;

        public MeFunction(ConsultantApiService consultantApiService, IdentityService identityService)
        {
            _consultantApiService = consultantApiService;
            _identityService = identityService;
        }

        [FunctionName("Me")]
        public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "me/{command?}")] HttpRequest req,
    string command,
    ILogger log)
        {
            log.LogInformation("HTTP trigger function me processed a request.");

            try
            {
                _identityService.InitializeFromRequest(req);
                var identity = _identityService;

                switch (req.Method.ToUpper())
                {
                    case "GET":
                        if (!string.IsNullOrEmpty(command))
                        {
                            throw new Utilities.HttpError((int)HttpStatusCode.BadRequest, $"Invalid command: {command}");
                        }

                        log.LogInformation("➡️ GET /api/me request");

                        var consultant = await _consultantApiService.GetApiConsultantById(identity, identity.Id);
                        var jsonResponse = new { results = new List<object> { consultant } };
                        log.LogInformation($"   ✅ GET /me response status OK; 1 consultant returned");

                        return new OkObjectResult(jsonResponse);

                    case "POST":
                        if (string.IsNullOrEmpty(command))
                        {
                            throw new Utilities.HttpError((int)HttpStatusCode.BadRequest, "Command is required for POST requests.");
                        }

                        switch (command.ToLower())
                        {
                            case "chargetime":
                                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                                if (string.IsNullOrWhiteSpace(requestBody))
                                {
                                    throw new Utilities.HttpError((int)HttpStatusCode.BadRequest, "No body to process this request.");
                                }

                                dynamic body = JsonConvert.DeserializeObject(requestBody);

                                var projectName = Utility.CleanUpParameter("projectName", (string)body.projectName);
                                if (string.IsNullOrEmpty(projectName))
                                {
                                    throw new Utilities.HttpError((int)HttpStatusCode.BadRequest, "Missing project name");
                                }

                                var hours = (double?)body.hours;
                                if (!hours.HasValue || hours < 0 || hours > 24)
                                {
                                    throw new Utilities.HttpError((int)HttpStatusCode.BadRequest, $"Invalid hours: {hours}");
                                }

                                log.LogInformation($"➡️ POST /api/me/chargetime request for project {projectName}, hours {hours}");

                                var chargeTimeResult = await _consultantApiService.ChargeTimeToProject(identity, projectName, identity.Id, hours.Value);

                                var postResponse = new
                                {
                                    status = 200,
                                    clientName = chargeTimeResult.ClientName,
                                    projectName = chargeTimeResult.ProjectName,
                                    remainingForecast = chargeTimeResult.RemainingForecast,
                                    message = chargeTimeResult.Message
                                };
                                log.LogInformation($"   ✅ POST /api/me/chargetime response status OK; {chargeTimeResult.Message}");

                                return new OkObjectResult(postResponse);

                            default:
                                throw new Utilities.HttpError((int)HttpStatusCode.BadRequest, $"Invalid command: {command}");
                        }

                    default:
                        throw new Utilities.HttpError((int)HttpStatusCode.MethodNotAllowed, $"Method not allowed: {req.Method}");
                }
            }
            catch (HttpError httpError)
            {
                log.LogError($"   ⛔ Returning error status code {(int)httpError.Status}: {httpError.Message}");
                return new ObjectResult(new { error = httpError.Message }) { StatusCode = (int)httpError.Status };
            }
            catch (Exception ex)
            {
                log.LogError($"   ⛔ Returning error status code 500: {ex.Message}");
                return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
            }
        }
    }
}