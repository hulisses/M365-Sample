﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using cext_trey_research_csharp.Services;
using cext_trey_research_csharp.Utilities;

namespace cext_trey_research_csharp.Functions
{
    public class ConsultantsFunction
    {
        private readonly ConsultantApiService _consultantApiService;
        private readonly IdentityService _identityService;

        public ConsultantsFunction(ConsultantApiService consultantApiService, IdentityService identityService)
        {
            _consultantApiService = consultantApiService;
            _identityService = identityService;
        }

        [FunctionName("Consultants")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "consultants/{id?}")] HttpRequest req,
            string id,
            ILogger log)
        {
            log.LogInformation("HTTP trigger function consultants processed a request.");

            var jsonResponse = new { results = new List<object>() };

            try
            {
                // Initialize identity
                _identityService.InitializeFromRequest(req);
                var identity = _identityService; 

                // Get the input parameters
                var consultantName = req.Query.ContainsKey("consultantName") ? req.Query["consultantName"].ToString().ToLower() : "";
                var projectName = req.Query.ContainsKey("projectName") ? req.Query["projectName"].ToString().ToLower() : "";
                var skill = req.Query.ContainsKey("skill") ? req.Query["skill"].ToString().ToLower() : "";
                var certification = req.Query.ContainsKey("certification") ? req.Query["certification"].ToString().ToLower() : "";
                var role = req.Query.ContainsKey("role") ? req.Query["role"].ToString().ToLower() : "";
                var hoursAvailable = req.Query.ContainsKey("hoursAvailable") ? req.Query["hoursAvailable"].ToString().ToLower() : "";

                if (!string.IsNullOrEmpty(id))
                {
                    log.LogInformation($"➡️ GET /api/consultants/{id}: request for consultant {id}");
                    var result = await _consultantApiService.GetApiConsultantById(identity, id.ToLower());
                    jsonResponse = new { results = new List<object> { result } };
                    log.LogInformation($"   ✅ GET /api/consultants/{id}: response status 1 consultant returned");
                    return new OkObjectResult(jsonResponse);
                }

                log.LogInformation($"➡️ GET /api/consultants: request for consultantName={consultantName}, projectName={projectName}, skill={skill}, certification={certification}, role={role}, hoursAvailable={hoursAvailable}");

                // Clean up parameters
                consultantName = Utility.CleanUpParameter("consultantName", consultantName);
                projectName = Utility.CleanUpParameter("projectName", projectName);
                skill = Utility.CleanUpParameter("skill", skill);
                certification = Utility.CleanUpParameter("certification", certification);
                role = Utility.CleanUpParameter("role", role);
                hoursAvailable = Utility.CleanUpParameter("hoursAvailable", hoursAvailable);

                
                var results = await _consultantApiService.GetApiConsultants(identity, consultantName, projectName, skill, certification, role, hoursAvailable);
                jsonResponse = new { results = results.Cast<object>().ToList() };
                log.LogInformation($"   ✅ GET /api/consultants: response status OK; {results.Count} consultants returned");

                return new OkObjectResult(jsonResponse);
            }
            catch (Exception ex)
            {
                log.LogError($"   ⛔ Returning error status code 500: {ex.Message}");
                return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
            }
        }
    }
}