using CloudFlare.Client;
using CloudFlare.Client.Api.Result;
using CloudFlare.Client.Api.Zones;
using CloudFlare.Client.Api.Zones.DnsRecord;
using CloudFlare.Client.Enumerators;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DyndnsUpdater.Server.Controllers
{
    /// <summary>
    /// APIs for Cloudflare.
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class CloudflareController : ControllerBase
    {
        private ILogger _logger;

        public CloudflareController(ILogger<CloudflareController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Adds or updates an A-Record inside a Cloudflare zone.
        /// </summary>
        /// <param name="token">The Cloudflare API token.</param>
        /// <param name="zone">The zone in which to add or modify an A-Record.</param>
        /// <param name="record">The A-Record to add or modify</param>
        /// <param name="ipv4">The IPv4 Address to set on the given A-Record.</param>
        /// <param name="proxied">Determines whether to enable Cloudflare proxying or not.</param>
        /// <response code="200">The Cloudflare A-Record has been successfully updated.</response>
        /// <response code="201">The Cloudflare A-Record has been successfully created.</response>
        /// <response code="400">At least one of the required parameters is missing.</response>
        /// <response code="404">The zone was not found.</response>
        /// <response code="424">If something fails during Clouflare calls.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CloudflareError), StatusCodes.Status424FailedDependency)]
        public async Task<IActionResult> UpdateZoneRecord(
            [Required] string? token = null,
            [Required] string? zone = null,
            [Required] string? record = null,
            [Required] string? ipv4 = null,
            bool proxied = false)
        {
            _logger.LogInformation($"Updating Cloudflare record {record} of zone {zone} to {ipv4}...");

            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("token query parameter is missing.");
            if (string.IsNullOrWhiteSpace(zone))
                return BadRequest("zone query parameter is missing.");
            if (string.IsNullOrWhiteSpace(record))
                return BadRequest("record query parameter is missing.");
            if (string.IsNullOrWhiteSpace(ipv4))
                return BadRequest("ipv4 query parameter is missing.");

            using var client = new CloudFlareClient(token);

            var zones = await client.Zones.GetAsync(new ZoneFilter { Name = zone });
            if (zones is null || !zones.Success)
                return StatusCode(StatusCodes.Status424FailedDependency, new CloudflareError("Failed to get zones from cloudflare", zones));
            if (zones.Result.Count < 1)
                return NotFound($"A zone with the name \"{zone}\" was not found.");

            var recordFullName = record.EndsWith(zone) ? record : $"{record}.{zone}";
            var records = await client.Zones.DnsRecords.GetAsync(zones.Result[0].Id, new DnsRecordFilter
            {
                Name = recordFullName,
            });
            if (records is null || !records.Success)
                return StatusCode(StatusCodes.Status424FailedDependency, new CloudflareError($"Failed to get records from zone \"{zones.Result[0].Id}\" from cloudflare", records));

            CloudFlareResult<DnsRecord> result;
            bool recordExisted = records.Result.Count >= 1;
            if (!recordExisted)
            {
                result = await client.Zones.DnsRecords.AddAsync(
                    zones.Result[0].Id,
                    new NewDnsRecord
                    {
                        Name = recordFullName,
                        Type = DnsRecordType.A,
                        Proxied = proxied,
                        Content = ipv4,
                    });
            }
            else
            {
                result = await client.Zones.DnsRecords.UpdateAsync(
                    zones.Result[0].Id,
                    records.Result[0].Id,
                    new ModifiedDnsRecord
                    {
                        Name = recordFullName,
                        Type = DnsRecordType.A,
                        Proxied = proxied,
                        Content = ipv4,
                    });
            }

            if (result is null || !result.Success)
                return StatusCode(StatusCodes.Status424FailedDependency, new CloudflareError($"Failed to add or update record \"{recordFullName}\" of zone \"{zones.Result[0].Id}\" on cloudflare", result));

            _logger.LogInformation($"Successfully updated Cloudflare record {record} of zone {zone} to {ipv4}");
            return recordExisted ? StatusCode(StatusCodes.Status201Created) : Ok();
        }

        /// <summary>
        /// Information about an error during the communication with Cloudflare.
        /// </summary>
        public class CloudflareError
        {
            /// <summary>
            /// The description of the error.
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// The response from Cloudflare.
            /// </summary>
            public object? Response { get; set; }

            public CloudflareError(string description, object? response)
            {
                Description = description;
                Response = response;
            }
        }
    }
}
