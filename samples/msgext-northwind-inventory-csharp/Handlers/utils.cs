﻿using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using msgext_northwind_inventory_csharp.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace msgext_northwind_inventory_csharp.Handlers
{
    public static class Utils
    {
        public static InvokeResponse CreateInvokeResponse(int status, object body = null)
        {
            return new InvokeResponse
            {
                Status = status,
                Body = body
            };
        }

        public static AdaptiveCardInvokeResponse CreateAdaptiveCardInvokeResponse(int statusCode, Dictionary<string, object> body = null)
        {
            return new AdaptiveCardInvokeResponse
            {
                StatusCode = statusCode,
                Type = "application/vnd.microsoft.card.adaptive",
                Value = body
            };
        }

        public static AdaptiveCardInvokeResponse CreateActionErrorResponse(int statusCode, int errorCode = -1, string errorMessage = "Unknown error")
        {
            var errorResponse = new
            {
                statusCode = statusCode,
                type = "application/vnd.microsoft.error",
                value = new
                {
                    error = new
                    {
                        code = errorCode,
                        message = errorMessage
                    }
                }
            };

            return new AdaptiveCardInvokeResponse
            {
                StatusCode = statusCode,
                Type = "application/vnd.microsoft.error",
                Value = JObject.FromObject(errorResponse)
            };
        }

        public static string GetInventoryStatus(IProduct product)
        {
            if (product.UnitsInStock >= product.ReorderLevel)
            {
                return "In stock";
            }
            else if (product.UnitsInStock < product.ReorderLevel && product.UnitsOnOrder == 0)
            {
                return "Low stock";
            }
            else if (product.UnitsInStock < product.ReorderLevel && product.UnitsOnOrder > 0)
            {
                return "On order";
            }
            else if (product.UnitsInStock == 0)
            {
                return "Out of stock";
            }
            else
            {
                return "Unknown"; // fallback
            }
        }
    }
}
